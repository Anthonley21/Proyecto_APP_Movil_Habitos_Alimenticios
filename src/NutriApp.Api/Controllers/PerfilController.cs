using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutriApp.Api.Contratos;
using NutriApp.Api.Seguridad;
using NutriApp.Domain.Entidades;
using NutriApp.Domain.Motor;
using NutriApp.Infrastructure.Persistencia;

namespace NutriApp.Api.Controllers;

/// <summary>
/// Perfil del usuario que inició sesión. El usuario SIEMPRE sale del token: no hay ningún id en la ruta,
/// así que es imposible pedir o modificar el perfil de otra persona.
/// </summary>
[ApiController]
[Route("api/perfil")]
[Authorize]
[Produces("application/json")]
public sealed class PerfilController(NutriAppDbContext db, TimeProvider reloj) : ControllerBase
{
    private DateOnly Hoy => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

    /// <summary>Devuelve el perfil propio.</summary>
    [HttpGet]
    [ProducesResponseType<PerfilRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener()
    {
        var perfil = await BuscarPerfil();
        return perfil is null ? PerfilNoExiste() : Ok(await Mapeo.APerfil(db, perfil, Hoy));
    }

    /// <summary>Crea el perfil propio (una sola vez; después se usa PUT).</summary>
    [HttpPost]
    [ProducesResponseType<PerfilRespuesta>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(PerfilSolicitud s)
    {
        if (await BuscarPerfil() is not null)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Ya tienes un perfil", detail: "Usa PUT /api/perfil para modificarlo.");

        var factor = await Validar(s);
        if (factor is null)
            return ValidationProblem(ModelState);

        var perfil = new PerfilUsuario { UsuarioId = User.IdUsuario() };
        Copiar(s, factor.Value, perfil);
        db.PerfilesUsuario.Add(perfil);
        await db.SaveChangesAsync();   // genera perfil.Id

        await GuardarAlergenos(perfil.Id, s.AlergenoIds ?? []);
        RegistrarPeso(perfil.Id, perfil.PesoKg, await db.HistorialPeso.FirstOrDefaultAsync(h => h.PerfilId == perfil.Id && h.Fecha == Hoy));
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Obtener), await Mapeo.APerfil(db, perfil, Hoy));
    }

    /// <summary>Modifica el perfil propio. Si cambia el peso, queda en el historial.</summary>
    [HttpPut]
    [ProducesResponseType<PerfilRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Actualizar(PerfilSolicitud s)
    {
        var perfil = await BuscarPerfil();
        if (perfil is null)
            return PerfilNoExiste();

        var factor = await Validar(s);
        if (factor is null)
            return ValidationProblem(ModelState);

        Copiar(s, factor.Value, perfil);
        await GuardarAlergenos(perfil.Id, s.AlergenoIds ?? []);
        RegistrarPeso(perfil.Id, perfil.PesoKg, await db.HistorialPeso.FirstOrDefaultAsync(h => h.PerfilId == perfil.Id && h.Fecha == Hoy));
        await db.SaveChangesAsync();

        return Ok(await Mapeo.APerfil(db, perfil, Hoy));
    }

    /// <summary>Requerimiento diario (ACT-08) calculado con el perfil propio.</summary>
    [HttpGet("requerimiento")]
    [ProducesResponseType<Requerimiento>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerRequerimiento()
    {
        var perfil = await BuscarPerfil();
        return perfil is null ? PerfilNoExiste() : Ok(CalculadoraRequerimiento.Calcular(perfil, Hoy));
    }

    // ---------------------------------------------------------------------------------------------

    private Task<PerfilUsuario?> BuscarPerfil()
    {
        var id = User.IdUsuario();
        return db.PerfilesUsuario.FirstOrDefaultAsync(p => p.UsuarioId == id);
    }

    private ObjectResult PerfilNoExiste() => Problem(statusCode: StatusCodes.Status404NotFound,
        title: "Aún no tienes perfil", detail: "Créalo con POST /api/perfil.");

    /// <summary>
    /// Valida con las MISMAS reglas del motor (ACT-05/08) y de los CHECK de la base (ACT-03).
    /// Devuelve el factor de actividad o null si hay errores (quedan en ModelState).
    /// </summary>
    private async Task<double?> Validar(PerfilSolicitud s)
    {
        var p = ParametrosMotor.Predeterminados;
        double factor = 0;
        try
        {
            factor = p.FactorDe(s.NivelActividad);
        }
        catch (ArgumentException)
        {
            ModelState.AddModelError(nameof(s.NivelActividad),
                $"Nivel de actividad no válido. Usa: {string.Join(", ", p.NivelesActividad.Select(n => n.Clave))}.");
        }

        int edad = 0;
        if (s.FechaNacimiento is { } nacimiento)
        {
            if (nacimiento > Hoy)
                ModelState.AddModelError(nameof(s.FechaNacimiento), "La fecha de nacimiento está en el futuro.");
            else
            {
                edad = CalculadoraRequerimiento.CalcularEdad(nacimiento, Hoy);
                if (edad < p.EdadMin || edad > p.EdadMax)
                    ModelState.AddModelError(nameof(s.FechaNacimiento),
                        $"NutriApp es para personas de {p.EdadMin} a {p.EdadMax} años.");
            }
        }

        var ids = (s.AlergenoIds ?? []).Distinct().ToArray();
        if (ids.Length > 0)
        {
            var existentes = await db.Alergenos.CountAsync(a => ids.Contains(a.Id));
            if (existentes != ids.Length)
                ModelState.AddModelError(nameof(s.AlergenoIds), "Hay alérgenos que no existen (válidos: 1 a 9).");
        }

        if (!ModelState.IsValid)
            return null;

        // Última barrera: el propio cálculo rechaza cualquier combinación fuera de rango.
        try
        {
            CalculadoraRequerimiento.Calcular(new DatosRequerimiento(
                s.Sexo, edad, (double)s.PesoKg!.Value, (double)s.EstaturaCm!.Value, factor, s.Objetivo));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(ex.ParamName ?? "perfil", ex.Message.Split(" (Parameter")[0]);
            return null;
        }
        return factor;
    }

    private void Copiar(PerfilSolicitud s, double factor, PerfilUsuario perfil)
    {
        perfil.Sexo = s.Sexo.Trim().ToUpperInvariant();
        perfil.FechaNacimiento = s.FechaNacimiento!.Value;
        perfil.EstaturaCm = Math.Round(s.EstaturaCm!.Value, 1);   // numeric(5,1)
        perfil.PesoKg = Math.Round(s.PesoKg!.Value, 1);
        perfil.FactorActividad = (decimal)factor;
        perfil.Objetivo = s.Objetivo;
        perfil.TieneDiabetes = s.TieneDiabetes;
        perfil.TieneHipertension = s.TieneHipertension;
        perfil.ActualizadoEn = reloj.GetUtcNow().UtcDateTime;
    }

    private async Task GuardarAlergenos(int perfilId, int[] alergenoIds)
    {
        var actuales = await db.PerfilAlergeno.Where(x => x.PerfilId == perfilId).ToListAsync();
        db.PerfilAlergeno.RemoveRange(actuales);
        db.PerfilAlergeno.AddRange(alergenoIds.Distinct().Select(id => new PerfilAlergeno { PerfilId = perfilId, AlergenoId = id }));
    }

    // Un registro por día: si ya hay uno hoy, se actualiza.
    private void RegistrarPeso(int perfilId, decimal pesoKg, HistorialPeso? deHoy)
    {
        if (deHoy is null)
            db.HistorialPeso.Add(new HistorialPeso { PerfilId = perfilId, Fecha = Hoy, PesoKg = pesoKg });
        else
            deHoy.PesoKg = pesoKg;
    }
}

/// <summary>Convierte entidades en respuestas (sin exponer columnas internas).</summary>
internal static class Mapeo
{
    public static async Task<PerfilRespuesta> APerfil(NutriAppDbContext db, PerfilUsuario p, DateOnly hoy)
    {
        var alergenos = await db.PerfilAlergeno
            .Where(x => x.PerfilId == p.Id)
            .Join(db.Alergenos, pa => pa.AlergenoId, a => a.Id, (pa, a) => a)
            .OrderBy(a => a.Id)                                   // ordenar ANTES de proyectar: PostgreSQL no
            .Select(a => new AlergenoRespuesta(a.Id, a.Nombre))   // puede ordenar por un objeto creado en C#
            .ToListAsync();

        var niveles = ParametrosMotor.Predeterminados.NivelesActividad;
        var nivel = niveles.MinBy(n => Math.Abs(n.Factor - (double)p.FactorActividad))!.Clave;

        return new PerfilRespuesta(p.Sexo, p.FechaNacimiento, CalculadoraRequerimiento.CalcularEdad(p.FechaNacimiento, hoy),
            p.EstaturaCm, p.PesoKg, nivel, p.FactorActividad, p.Objetivo, p.TieneDiabetes, p.TieneHipertension,
            alergenos, p.ActualizadoEn);
    }
}

/// <summary>Lectura del usuario autenticado.</summary>
internal static class UsuarioActual
{
    /// <summary>Id del usuario según el token ("sub"). Solo se usa en endpoints con [Authorize].</summary>
    public static string IdUsuario(this ClaimsPrincipal usuario) =>
        usuario.FindFirst(ClaimsToken.Sujeto)?.Value
        ?? throw new InvalidOperationException("El token no tiene 'sub'.");
}