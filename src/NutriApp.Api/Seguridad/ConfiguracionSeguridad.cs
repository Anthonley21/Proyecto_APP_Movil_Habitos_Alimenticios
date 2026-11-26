using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NutriApp.Infrastructure.Persistencia;

namespace NutriApp.Api.Seguridad;

/// <summary>
/// ACT-09 · Toda la configuración de seguridad de la API en un solo lugar:
/// Identity (contraseñas y bloqueo), JWT, autorización por defecto, límite de peticiones,
/// cabeceras de seguridad y Swagger con el botón Authorize.
/// </summary>
public static class ConfiguracionSeguridad
{
    public const string PoliticaAutenticacion = "autenticacion";

    public static IServiceCollection AddSeguridad(this IServiceCollection services, IConfiguration configuracion)
    {
        // ---- Opciones (la clave se valida al arrancar: sin clave, la API no inicia) ----
        services.AddOptions<OpcionesJwt>().Bind(configuracion.GetSection(OpcionesJwt.Seccion))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<OpcionesSeguridad>().Bind(configuracion.GetSection(OpcionesSeguridad.Seccion))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ServicioTokens>();

        // ---- Identity: contraseñas con hash PBKDF2 + sal, bloqueo por intentos fallidos ----
        services.AddIdentityCore<IdentityUser>(o =>
            {
                o.User.RequireUniqueEmail = true;

                o.Password.RequiredLength = 10;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredUniqueChars = 4;

                o.Lockout.AllowedForNewUsers = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddErrorDescriber<DescriptorErroresEs>()
            .AddEntityFrameworkStores<NutriAppDbContext>();

        // ---- JWT ----
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<OpcionesJwt>>((o, jwt) =>
            {
                var j = jwt.Value;
                o.MapInboundClaims = false;        // los claims llegan con su nombre real ("sub", "sst")
                o.IncludeErrorDetails = false;     // no explicar al cliente por qué falló el token
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = j.Emisor,
                    ValidateAudience = true,
                    ValidAudience = j.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = j.ClaveDeFirma(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],   // rechaza "alg: none" y otros
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimsToken.Sujeto,
                };
                o.Events = new JwtBearerEvents
                {
                    // El token solo vale si el usuario sigue existiendo y su sello no cambió
                    // (cerrar sesión o borrar la cuenta invalida todos sus tokens al instante).
                    OnTokenValidated = async ctx =>
                    {
                        var id = ctx.Principal?.FindFirst(ClaimsToken.Sujeto)?.Value;
                        var sello = ctx.Principal?.FindFirst(ClaimsToken.Sello)?.Value;
                        var usuarios = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                        var usuario = id is null ? null : await usuarios.FindByIdAsync(id);
                        if (usuario is null || sello is null || !string.Equals(usuario.SecurityStamp, sello, StringComparison.Ordinal))
                            ctx.Fail("Sesión no válida.");
                    },
                };
            });

        // ---- Autorización: TODO exige token salvo lo marcado con [AllowAnonymous] ----
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        // ---- Límite de peticiones en registro e inicio de sesión (por IP) ----
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(PoliticaAutenticacion, ctx =>
            {
                var limite = ctx.RequestServices.GetRequiredService<IOptions<OpcionesSeguridad>>().Value.LimitePeticionesPorMinuto;
                return RateLimitPartition.GetFixedWindowLimiter(
                    ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
            });
        });

        return services;
    }

    /// <summary>Swagger con el botón "Authorize" para pegar el token.</summary>
    public static IServiceCollection AddDocumentacionApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "NutriApp API",
                Version = "v1",
                Description = "1) POST /api/auth/registro · 2) POST /api/auth/login · 3) Authorize con el token · 4) /api/perfil",
            });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Pega solo el token (sin la palabra Bearer).",
            });
            o.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", documento)] = [],
            });
        });
        return services;
    }

    /// <summary>Cabeceras que evitan que el navegador interprete o guarde en caché las respuestas.</summary>
    public static IApplicationBuilder UseCabecerasSeguridad(this IApplicationBuilder app) => app.Use(async (ctx, next) =>
    {
        var h = ctx.Response.Headers;
        h["X-Content-Type-Options"] = "nosniff";
        h["X-Frame-Options"] = "DENY";
        h["Referrer-Policy"] = "no-referrer";
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            h.CacheControl = "no-store";   // datos de salud: que nada los guarde en caché
            h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        }
        await next();
    });
}
