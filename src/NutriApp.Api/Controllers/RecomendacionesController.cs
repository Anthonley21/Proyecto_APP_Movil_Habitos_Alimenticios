using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutriApp.Api.Contratos;
using NutriApp.Domain.Motor;
using NutriApp.Infrastructure.Persistencia;

namespace NutriApp.Api.Controllers;

/// <summary>
/// Motor de recomendación. ACT-10: candidatos que cumplen todas las reglas del perfil.
/// (ACT-11 agregará el ranking y ACT-12 las explicaciones y el menú del día.)
/// Igual que el perfil, el usuario sale SIEMPRE del token.
/// </summary>
[ApiController]
[Route("api/recomendaciones")]
[Authorize]
[Produces("application/json")]
public sealed class RecomendacionesController(NutriAppDbContext db, IProveedorCatalogo proveedor, TimeProvider reloj)
    : ControllerBase
{
    private DateOnly Hoy => DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);

    private static readonly string[] NombresComidas = ["desayuno", "almuerzo", "merienda", "cena"];

    /// <summary>
    /// Alimentos y platos que cumplen TODAS las reglas del perfil para cada comida: tipo de comida, alérgenos,
    /// diabetes, hipertensión y energía (±10 %, o ±15 % si faltan opciones). Ordenados por cercanía a la meta.
    /// </summary>
    /// <param name="comida">Opcional: desayuno, almuerzo, merienda o cena. Sin valor: las 4.</param>
    /// <param name="limite">Cuántos candidatos devolver por comida (1 a 100). El total va en totalCandidatos.</param>
    [HttpGet("candidatos")]
    [ProducesResponseType<CandidatosRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Candidatos(
        [FromQuery] string? comida = null,
        [FromQuery, Range(1, 100, ErrorMessage = "El límite debe estar entre 1 y 100.")] int limite = 10,
        CancellationToken ct = default)
    {
        var nombreComida = comida?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(nombreComida) && !NombresComidas.Contains(nombreComida))
        {
            ModelState.AddModelError(nameof(comida), "La comida debe ser desayuno, almuerzo, merienda o cena.");
            return ValidationProblem(ModelState);
        }

        var id = User.IdUsuario();
        var perfil = await db.PerfilesUsuario.AsNoTracking().FirstOrDefaultAsync(p => p.UsuarioId == id, ct);
        if (perfil is null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Aún no tienes perfil",
                detail: "Créalo con POST /api/perfil.");

        Requerimiento requerimiento;
        try
        {
            requerimiento = CalculadoraRequerimiento.Calcular(perfil, Hoy);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Confirma tu edad",
                detail: "Tu edad actual está fuera del rango de NutriApp (18 a 80 años). Actualízala con PUT /api/perfil.");
        }

        // Alérgenos del perfil (consulta simple: se ordena en C#, no en PostgreSQL)
        var alergenoIds = await db.PerfilAlergeno.AsNoTracking()
            .Where(x => x.PerfilId == perfil.Id).Select(x => x.AlergenoId).ToListAsync(ct);

        var catalogo = await proveedor.ObtenerAsync(ct);
        var filtro = new PerfilFiltro(perfil.TieneDiabetes, perfil.TieneHipertension, alergenoIds.ToHashSet());

        var comidas = requerimiento.Comidas
            .Where(c => string.IsNullOrEmpty(nombreComida) || c.Nombre == nombreComida)
            .Select(c => FiltroReglas.FiltrarComida(catalogo, filtro, c))
            .Select(c => Mapear(c, limite))
            .ToList();

        var restricciones = new RestriccionesRespuesta(perfil.TieneDiabetes, perfil.TieneHipertension,
            alergenoIds.Order().Select(a => catalogo.NombresAlergenos.GetValueOrDefault(a, $"alérgeno {a}")).ToList());

        return Ok(new CandidatosRespuesta(requerimiento.KcalDia, restricciones, comidas));
    }

    private static CandidatosComidaRespuesta Mapear(CandidatosComida c, int limite) => new(
        c.TipoComidaId, c.Comida, c.MetaKcal, Math.Round(c.ToleranciaAplicada * 100, 0), c.MinKcal, c.MaxKcal,
        c.Candidatos.Count, c.Suficiente, c.Aviso,
        c.Descartes.Where(d => d.Value > 0).ToDictionary(d => d.Key, d => d.Value),
        c.Candidatos.Take(limite).Select(x => new CandidatoRespuesta(
            x.Tipo == TipoCandidato.Plato ? "plato" : "alimento",
            x.Id, x.Nombre, x.Cantidad, x.Gramos, x.Kcal,
            Math.Round(x.Kcal / c.MetaKcal * 100, 1),
            x.ProteinaG, x.CarbohidratosG, x.GrasaG, x.AzucaresG, x.SodioMg, x.IndiceGlucemico,
            x.Reglas)).ToList());
}
