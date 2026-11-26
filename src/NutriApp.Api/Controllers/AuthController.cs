using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NutriApp.Api.Contratos;
using NutriApp.Api.Seguridad;

namespace NutriApp.Api.Controllers;

/// <summary>Registro e inicio de sesión. Son los únicos endpoints sin token, y tienen límite de peticiones.</summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting(ConfiguracionSeguridad.PoliticaAutenticacion)]
[Produces("application/json")]
public sealed class AuthController(
    UserManager<IdentityUser> usuarios,
    IPasswordHasher<IdentityUser> hasher,
    ServicioTokens tokens,
    TimeProvider reloj,
    ILogger<AuthController> log) : ControllerBase
{
    /// <summary>Tipo del claim donde se guarda la fecha del consentimiento.</summary>
    public const string ClaimConsentimiento = "consentimiento_datos_salud";

    // Hash "señuelo": si el correo no existe se verifica igual una contraseña, para que la respuesta
    // tarde lo mismo y no se pueda adivinar qué correos están registrados midiendo el tiempo.
    private static readonly IdentityUser Senuelo = new() { Id = "senuelo" };
    private static string? _hashSenuelo;

    /// <summary>Crea una cuenta. No inicia sesión: después hay que llamar a /api/auth/login.</summary>
    [HttpPost("registro")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Registrar(RegistroSolicitud s)
    {
        if (!s.AceptaTratamientoDatos)
        {
            ModelState.AddModelError(nameof(s.AceptaTratamientoDatos),
                "Debes aceptar el tratamiento de tus datos de salud para usar NutriApp.");
            return ValidationProblem(ModelState);
        }

        var correo = s.Correo.Trim();
        var usuario = new IdentityUser { UserName = correo, Email = correo };
        var r = await usuarios.CreateAsync(usuario, s.Contrasena);

        if (!r.Succeeded)
        {
            // Errores de contraseña: se muestran (no dependen de si el correo existe).
            var deContrasena = r.Errors.Where(e => e.Code.StartsWith("Password", StringComparison.Ordinal)).ToList();
            foreach (var e in deContrasena)
                ModelState.AddModelError(nameof(s.Contrasena), e.Description);
            if (deContrasena.Count > 0)
                return ValidationProblem(ModelState);

            // Correo duplicado u otro: mensaje genérico, sin decir que el correo ya está registrado.
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "No se pudo completar el registro",
                detail: "Revisa los datos o intenta con otro correo.");
        }

        await usuarios.AddClaimAsync(usuario,
            new Claim(ClaimConsentimiento, reloj.GetUtcNow().ToString("O", CultureInfo.InvariantCulture)));

        log.LogInformation("Cuenta creada {UsuarioId}", usuario.Id);   // nunca el correo ni la contraseña
        return StatusCode(StatusCodes.Status201Created, new { mensaje = "Cuenta creada. Ahora inicia sesión." });
    }

    /// <summary>Devuelve un token JWT válido por 30 minutos.</summary>
    [HttpPost("login")]
    [ProducesResponseType<TokenEmitido>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> IniciarSesion(LoginSolicitud s)
    {
        var usuario = await usuarios.FindByEmailAsync(s.Correo.Trim());

        if (usuario is null)
        {
            _hashSenuelo ??= hasher.HashPassword(Senuelo, Guid.NewGuid().ToString());
            hasher.VerifyHashedPassword(Senuelo, _hashSenuelo, s.Contrasena);
            return CredencialesInvalidas();
        }

        if (await usuarios.IsLockedOutAsync(usuario))
        {
            log.LogWarning("Intento de inicio de sesión en cuenta bloqueada {UsuarioId}", usuario.Id);
            return CredencialesInvalidas();
        }

        if (!await usuarios.CheckPasswordAsync(usuario, s.Contrasena))
        {
            await usuarios.AccessFailedAsync(usuario);   // al 5.º fallo bloquea 15 minutos
            log.LogWarning("Contraseña incorrecta {UsuarioId}", usuario.Id);
            return CredencialesInvalidas();
        }

        await usuarios.ResetAccessFailedCountAsync(usuario);
        return Ok(tokens.Crear(usuario));
    }

    // Mismo mensaje para correo inexistente, contraseña incorrecta y cuenta bloqueada.
    private ObjectResult CredencialesInvalidas() => Problem(
        statusCode: StatusCodes.Status401Unauthorized,
        title: "Credenciales inválidas",
        detail: "Correo o contraseña incorrectos. Tras 5 intentos fallidos la cuenta se bloquea 15 minutos.");
}
