using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutriApp.Api.Contratos;
using NutriApp.Infrastructure.Persistencia;

namespace NutriApp.Api.Controllers;

/// <summary>Cuenta propia: cerrar sesión, ver todos mis datos y eliminar la cuenta (derechos LOPDP).</summary>
[ApiController]
[Route("api/cuenta")]
[Authorize]
[Produces("application/json")]
public sealed class CuentaController(
    UserManager<IdentityUser> usuarios,
    NutriAppDbContext db,
    TimeProvider reloj,
    ILogger<CuentaController> log) : ControllerBase
{
    /// <summary>Invalida TODOS los tokens de la cuenta (en todos los dispositivos).</summary>
    [HttpPost("cerrar-sesion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CerrarSesion()
    {
        var usuario = await usuarios.FindByIdAsync(User.IdUsuario());
        if (usuario is null)
            return Unauthorized();
        await usuarios.UpdateSecurityStampAsync(usuario);
        return NoContent();
    }

    /// <summary>Todo lo que NutriApp guarda de ti (derecho de acceso).</summary>
    [HttpGet("mis-datos")]
    [ProducesResponseType<MisDatosRespuesta>(StatusCodes.Status200OK)]
    public async Task<IActionResult> MisDatos()
    {
        var usuario = await usuarios.FindByIdAsync(User.IdUsuario());
        if (usuario is null)
            return Unauthorized();

        var claims = await usuarios.GetClaimsAsync(usuario);
        var consentimiento = claims.FirstOrDefault(c => c.Type == AuthController.ClaimConsentimiento)?.Value;
        DateTimeOffset? fechaConsentimiento = DateTimeOffset.TryParse(consentimiento, CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var f) ? f : null;

        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
        var perfil = await db.PerfilesUsuario.FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id);
        List<PesoRespuesta> historial = perfil is null
            ? []
            : await db.HistorialPeso.Where(h => h.PerfilId == perfil.Id)
                .OrderBy(h => h.Fecha)
                .Select(h => new PesoRespuesta(h.Fecha, h.PesoKg))
                .ToListAsync();

        return Ok(new MisDatosRespuesta(
            usuario.Email ?? string.Empty,
            fechaConsentimiento,
            perfil is null ? null : await Mapeo.APerfil(db, perfil, hoy),
            historial));
    }

    /// <summary>
    /// Elimina la cuenta y TODOS sus datos (perfil, alérgenos, historial, recomendaciones, registros),
    /// en cascada desde la base. Pide la contraseña para evitar borrados con un token robado.
    /// </summary>
    [HttpPost("eliminar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Eliminar(ConfirmarContrasenaSolicitud s)
    {
        var usuario = await usuarios.FindByIdAsync(User.IdUsuario());
        if (usuario is null)
            return Unauthorized();

        if (!await usuarios.CheckPasswordAsync(usuario, s.Contrasena))
        {
            await usuarios.AccessFailedAsync(usuario);
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Contraseña incorrecta");
        }

        var r = await usuarios.DeleteAsync(usuario);
        if (!r.Succeeded)
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "No se pudo eliminar la cuenta");

        log.LogInformation("Cuenta eliminada {UsuarioId}", usuario.Id);
        return NoContent();
    }
}
