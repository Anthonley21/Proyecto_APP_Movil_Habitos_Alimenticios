using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NutriApp.Api.Seguridad;
using NutriApp.Domain.Entidades;
using NutriApp.Infrastructure.Persistencia;

namespace NutriApp.Tests.Api;

/// <summary>
/// Levanta la API completa en memoria (sin Neon ni red): misma seguridad, mismos controladores,
/// base de datos EF Core InMemory. Cada fábrica tiene su propia base.
/// </summary>
public class FabricaApi : WebApplicationFactory<Program>
{
    /// <summary>Clave solo para pruebas (no es secreta: no firma nada real).</summary>
    public const string ClavePruebas = "clave-solo-para-pruebas-automaticas-0123456789";

    private readonly string _nombreBase = "nutriapp-pruebas-" + Guid.NewGuid().ToString("N");

    static FabricaApi()
    {
        // Program lee estos valores mientras arma los servicios; como variables de entorno llegan a tiempo.
        Environment.SetEnvironmentVariable("ConnectionStrings__NutriApp", "Host=no-se-usa-en-pruebas");
        Environment.SetEnvironmentVariable("Jwt__Clave", ClavePruebas);
    }

    /// <summary>Peticiones por minuto a /api/auth (alto para que las pruebas normales no choquen con el límite).</summary>
    public int LimitePorMinuto { get; init; } = 10_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Pruebas");
        builder.ConfigureTestServices(services =>
        {
            // Npgsql → InMemory
            services.RemoveAll<DbContextOptions<NutriAppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<NutriAppDbContext>>();
            services.AddDbContext<NutriAppDbContext>(o => o.UseInMemoryDatabase(_nombreBase));

            services.PostConfigure<OpcionesSeguridad>(o => o.LimitePeticionesPorMinuto = LimitePorMinuto);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NutriAppDbContext>();
        if (!db.Alergenos.Any())
        {
            string[] nombres = ["gluten", "lácteos", "huevo", "maní", "frutos secos", "pescado", "mariscos", "soya", "ajonjolí"];
            db.Alergenos.AddRange(nombres.Select((n, i) => new Alergeno { Id = i + 1, Nombre = n }));
            db.SaveChanges();
        }
        return host;
    }

    // ---------------- Ayudas para las pruebas ----------------

    public const string ContrasenaValida = "ClaveSegura2026";

    public static string CorreoNuevo() => $"u{Guid.NewGuid():N}@pruebas.ec";

    public async Task<HttpResponseMessage> Registrar(HttpClient c, string correo, string contrasena = ContrasenaValida, bool acepta = true) =>
        await c.PostAsJsonAsync("/api/auth/registro", new { correo, contrasena, aceptaTratamientoDatos = acepta });

    public static async Task<HttpResponseMessage> Login(HttpClient c, string correo, string contrasena = ContrasenaValida) =>
        await c.PostAsJsonAsync("/api/auth/login", new { correo, contrasena });

    /// <summary>Registra una cuenta nueva, inicia sesión y devuelve un cliente con el token puesto.</summary>
    public async Task<(HttpClient Cliente, string Token, string Correo)> ClienteAutenticado()
    {
        var c = CreateClient();
        var correo = CorreoNuevo();
        (await Registrar(c, correo)).EnsureSuccessStatusCode();
        var login = await Login(c, correo);
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (c, token, correo);
    }

    /// <summary>El perfil del plan: mujer, 30 años, 70 kg, 160 cm, actividad ligera, bajar.</summary>
    public static object PerfilDelPlan(DateOnly hoy, decimal pesoKg = 70, int[]? alergenos = null) => new
    {
        sexo = "F",
        edad = 30,
        estaturaCm = 160m,
        pesoKg,
        nivelActividad = "ligero",
        objetivo = "bajar",
        tieneDiabetes = false,
        tieneHipertension = false,
        alergenoIds = alergenos ?? [2],
    };

    public static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Now);
}
