using NutriApp.Domain.Entidades;

namespace NutriApp.Domain.Motor;

/// <summary>
/// ACT-10 · Un alimento tal como lo usa el motor: valores por 100 g (o 100 ml), su porción típica,
/// sus alérgenos y las comidas en las que puede ir.
/// </summary>
public sealed record AlimentoMotor(
    int Id, string Nombre, string GrupoCodigo, string UnidadBase,
    double KcalPor100, double ProteinaPor100, double CarbohidratosPor100, double GrasaPor100,
    double? AzucaresPor100, double? SodioMgPor100, int? IndiceGlucemico, string CategoriaIG,
    bool EsProductoDeMarca, bool AptoMotor,
    string PorcionDescripcion, double PorcionGramos,
    IReadOnlySet<int> Alergenos, IReadOnlySet<int> TiposComida)
{
    public bool EsLiquido => UnidadBase.Contains("ml", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// ACT-10 · Un plato tal como lo usa el motor: valores por porción y alérgenos derivados de sus ingredientes.
/// </summary>
public sealed record PlatoMotor(
    int Id, string Nombre, bool EsEcuatoriana, string Clasificacion, string CalidadNutricional, bool Revisar,
    double? PorcionG, double? KcalPorcion, double? ProteinaGPorcion, double? CarbohidratosGPorcion, double? GrasaGPorcion,
    double? AzucaresGPorcion, double? SodioMgPorcion, int? IndiceGlucemico,
    IReadOnlySet<int> Alergenos, IReadOnlySet<int> TiposComida)
{
    /// <summary>Valor por 100 g a partir del valor por porción (para comparar con los umbrales del semáforo).</summary>
    public double? Por100g(double? valorPorcion) =>
        valorPorcion is null || PorcionG is null or <= 0 ? null : valorPorcion.Value / PorcionG.Value * 100;
}

/// <summary>
/// ACT-10 · Catálogo en memoria para el motor (997 alimentos y 471 platos: cabe holgado en memoria).
/// Se arma una vez a partir de las entidades, ya sea desde la base (API) o desde los CSV (pruebas).
/// </summary>
public sealed class CatalogoMotor
{
    public required IReadOnlyList<AlimentoMotor> Alimentos { get; init; }
    public required IReadOnlyList<PlatoMotor> Platos { get; init; }
    /// <summary>Id → nombre del alérgeno (para los textos de las reglas).</summary>
    public required IReadOnlyDictionary<int, string> NombresAlergenos { get; init; }

    public static CatalogoMotor Construir(
        IEnumerable<Alimento> alimentos,
        IEnumerable<Porcion> porciones,
        IEnumerable<AlimentoAlergeno> alimentoAlergenos,
        IEnumerable<AlimentoTipoComida> alimentoComidas,
        IEnumerable<Plato> platos,
        IEnumerable<PlatoIngrediente> ingredientes,
        IEnumerable<PlatoTipoComida> platoComidas,
        IEnumerable<Alergeno> alergenos,
        ParametrosFiltro? parametros = null)
    {
        var p = parametros ?? ParametrosFiltro.Predeterminados;

        var porcion = porciones
            .GroupBy(x => x.AlimentoId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.EsPredeterminada).ThenBy(x => x.Id).First());
        var alergAlim = alimentoAlergenos.GroupBy(x => x.AlimentoId)
            .ToDictionary(g => g.Key, g => (IReadOnlySet<int>)g.Select(x => x.AlergenoId).ToHashSet());
        var comidasAlim = alimentoComidas.GroupBy(x => x.AlimentoId)
            .ToDictionary(g => g.Key, g => (IReadOnlySet<int>)g.Select(x => x.TipoComidaId).ToHashSet());
        var comidasPlato = platoComidas.GroupBy(x => x.PlatoId)
            .ToDictionary(g => g.Key, g => (IReadOnlySet<int>)g.Select(x => x.TipoComidaId).ToHashSet());
        IReadOnlySet<int> vacio = new HashSet<int>();

        var listaAlimentos = new List<AlimentoMotor>();
        foreach (var a in alimentos)
        {
            if (!porcion.TryGetValue(a.Id, out var por)) continue;   // sin porción no se puede recomendar

            // Alérgenos = los de AlimentoAlergeno (ACT-06) + los que nombra el propio nombre del alimento.
            // Segunda red de seguridad: p. ej. "Crema, de avellana, leche y chocolate" no tenía lácteos en ACT-06.
            var ale = new HashSet<int>(alergAlim.GetValueOrDefault(a.Id, vacio));
            var texto = ParametrosFiltro.Normalizar(a.Nombre + " | " + a.NombreIngles);
            foreach (var regla in p.PalabrasAlergenos)
                if (regla.DetectaEnNombre(texto)) ale.Add(regla.AlergenoId);

            listaAlimentos.Add(new AlimentoMotor(
                a.Id, a.Nombre, a.GrupoCodigo, a.UnidadBase,
                (double)a.KcalPor100g, (double)a.ProteinaG, (double)a.CarbohidratosG, (double)a.GrasaG,
                (double?)a.AzucaresG, (double?)a.SodioMg, a.IndiceGlucemico, a.CategoriaIG,
                a.EsProductoDeMarca, a.AptoMotor,
                por.Descripcion, (double)por.Gramos,
                ale, comidasAlim.GetValueOrDefault(a.Id, vacio)));
        }

        // Alérgenos de cada plato = los de sus ingredientes enlazados + los que nombran sus textos.
        var porPlato = ingredientes.GroupBy(i => i.PlatoId).ToDictionary(g => g.Key, g => g.ToList());
        var listaPlatos = new List<PlatoMotor>();
        foreach (var pl in platos)
        {
            var ale = new HashSet<int>();
            var textos = new List<string> { pl.Nombre };
            foreach (var ing in porPlato.GetValueOrDefault(pl.Id, []))
            {
                if (ing.AlimentoId is int aid && alergAlim.TryGetValue(aid, out var s)) ale.UnionWith(s);
                textos.Add(ing.Texto);
            }
            foreach (var t in textos.Select(ParametrosFiltro.Normalizar))
                foreach (var regla in p.PalabrasAlergenos)
                    if (regla.DetectaEnTexto(t)) ale.Add(regla.AlergenoId);

            listaPlatos.Add(new PlatoMotor(
                pl.Id, pl.Nombre, pl.EsEcuatoriana, pl.Clasificacion, pl.CalidadNutricional, pl.Revisar,
                pl.PorcionG, (double?)pl.KcalPorcion, (double?)pl.ProteinaGPorcion, (double?)pl.CarbohidratosGPorcion,
                (double?)pl.GrasaGPorcion, (double?)pl.AzucaresGPorcion, (double?)pl.SodioMgPorcion, pl.IndiceGlucemico,
                ale, comidasPlato.GetValueOrDefault(pl.Id, vacio)));
        }

        return new CatalogoMotor
        {
            Alimentos = listaAlimentos,
            Platos = listaPlatos,
            NombresAlergenos = alergenos.ToDictionary(a => a.Id, a => a.Nombre),
        };
    }
}

/// <summary>De dónde saca el motor el catálogo (base de datos en la API; CSV en las pruebas).</summary>
public interface IProveedorCatalogo
{
    Task<CatalogoMotor> ObtenerAsync(CancellationToken ct = default);
}
