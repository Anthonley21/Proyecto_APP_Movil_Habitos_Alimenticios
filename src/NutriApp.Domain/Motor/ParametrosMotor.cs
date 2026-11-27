using System.Globalization;
using System.Text.Json;

namespace NutriApp.Domain.Motor;

/// <summary>Un nivel de actividad física y su multiplicador (ACT-05).</summary>
public sealed record NivelActividad(string Clave, double Factor, string Descripcion);

/// <summary>Parte de la energía diaria que va a una comida. TipoComidaId coincide con la tabla TiposComida.</summary>
public sealed record RepartoComida(int TipoComidaId, string Nombre, double Porcentaje);

/// <summary>
/// Parámetros del motor leídos de <c>parametros_motor.json</c> (ACT-05), que va incrustado en NutriApp.Domain.
/// Un solo origen: si se cambia un valor, se cambia en el JSON y no en el código.
/// </summary>
public sealed class ParametrosMotor
{
    public required int EdadMin { get; init; }
    public required int EdadMax { get; init; }

    // Mifflin-St Jeor: TMB = a·peso + b·estatura + c·edad + constante(sexo)
    public required double CoefPesoKg { get; init; }
    public required double CoefEstaturaCm { get; init; }
    public required double CoefEdadAnios { get; init; }
    public required double ConstanteMasculino { get; init; }
    public required double ConstanteFemenino { get; init; }

    public required IReadOnlyList<NivelActividad> NivelesActividad { get; init; }

    /// <summary>bajar −500 · mantener 0 · subir +300 [kcal/día]</summary>
    public required IReadOnlyDictionary<string, double> AjusteObjetivoKcal { get; init; }
    public required double MinimoKcalFemenino { get; init; }
    public required double MinimoKcalMasculino { get; init; }

    public required double FraccionCarbohidratos { get; init; }
    public required double FraccionProteina { get; init; }
    public required double FraccionGrasa { get; init; }
    public required double KcalPorGramoCarbohidratos { get; init; }
    public required double KcalPorGramoProteina { get; init; }
    public required double KcalPorGramoGrasa { get; init; }
    public required double FraccionMaxAzucaresLibres { get; init; }

    public required IReadOnlyList<RepartoComida> Comidas { get; init; }

    private const string NombreRecurso = "NutriApp.Domain.parametros_motor.json";

    /// <summary>Los parámetros del JSON incrustado (se leen una sola vez).</summary>
    public static ParametrosMotor Predeterminados { get; } = CargarIncrustado();

    private static ParametrosMotor CargarIncrustado() => DesdeJson(LeerJsonIncrustado());

    /// <summary>Texto de parametros_motor.json incrustado (también lo usa <see cref="ParametrosFiltro"/>, ACT-10).</summary>
    internal static string LeerJsonIncrustado()
    {
        using var stream = typeof(ParametrosMotor).Assembly.GetManifestResourceStream(NombreRecurso)
            ?? throw new InvalidOperationException(
                $"No se encontró el recurso '{NombreRecurso}'. Revisa el <EmbeddedResource> de NutriApp.Domain.csproj.");
        using var lector = new StreamReader(stream);
        return lector.ReadToEnd();
    }

    /// <summary>Lee y valida un JSON con la misma estructura que parametros_motor.json.</summary>
    public static ParametrosMotor DesdeJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;

        var tmb = r.GetProperty("tmb_mifflin_st_jeor");
        var obj = r.GetProperty("objetivo");
        var mac = r.GetProperty("macronutrientes");
        var pct = mac.GetProperty("porcentaje_energia");
        var kpg = mac.GetProperty("kcal_por_gramo");

        var p = new ParametrosMotor
        {
            EdadMin = r.GetProperty("alcance").GetProperty("edad_min").GetInt32(),
            EdadMax = r.GetProperty("alcance").GetProperty("edad_max").GetInt32(),

            CoefPesoKg = tmb.GetProperty("coef_peso_kg").GetDouble(),
            CoefEstaturaCm = tmb.GetProperty("coef_estatura_cm").GetDouble(),
            CoefEdadAnios = tmb.GetProperty("coef_edad_anios").GetDouble(),
            ConstanteMasculino = tmb.GetProperty("constante").GetProperty("M").GetDouble(),
            ConstanteFemenino = tmb.GetProperty("constante").GetProperty("F").GetDouble(),

            NivelesActividad = r.GetProperty("factores_actividad").GetProperty("niveles").EnumerateArray()
                .Select(n => new NivelActividad(
                    n.GetProperty("clave").GetString()!,
                    n.GetProperty("factor").GetDouble(),
                    n.GetProperty("descripcion").GetString()!))
                .ToList(),

            AjusteObjetivoKcal = obj.GetProperty("ajuste_kcal").EnumerateObject()
                .ToDictionary(x => x.Name, x => x.Value.GetDouble(), StringComparer.OrdinalIgnoreCase),
            MinimoKcalFemenino = obj.GetProperty("minimo_kcal").GetProperty("F").GetDouble(),
            MinimoKcalMasculino = obj.GetProperty("minimo_kcal").GetProperty("M").GetDouble(),

            FraccionCarbohidratos = pct.GetProperty("carbohidratos").GetDouble(),
            FraccionProteina = pct.GetProperty("proteina").GetDouble(),
            FraccionGrasa = pct.GetProperty("grasa").GetDouble(),
            KcalPorGramoCarbohidratos = kpg.GetProperty("carbohidratos").GetDouble(),
            KcalPorGramoProteina = kpg.GetProperty("proteina").GetDouble(),
            KcalPorGramoGrasa = kpg.GetProperty("grasa").GetDouble(),
            FraccionMaxAzucaresLibres = mac.GetProperty("azucares_libres_max_pct_energia").GetDouble(),

            Comidas = r.GetProperty("comidas").GetProperty("reparto").EnumerateArray()
                .Select(c => new RepartoComida(
                    c.GetProperty("id").GetInt32(),
                    c.GetProperty("nombre").GetString()!,
                    c.GetProperty("porcentaje").GetDouble()))
                .OrderBy(c => c.TipoComidaId)
                .ToList(),
        };
        p.Validar();
        return p;
    }

    /// <summary>Comprueba que los parámetros sean coherentes (los porcentajes suman 100 %, etc.).</summary>
    public void Validar()
    {
        static bool CasiUno(double x) => Math.Abs(x - 1.0) < 1e-9;

        if (!CasiUno(FraccionCarbohidratos + FraccionProteina + FraccionGrasa))
            throw new InvalidOperationException("Los porcentajes de macronutrientes no suman 100 %.");
        if (!CasiUno(Comidas.Sum(c => c.Porcentaje)))
            throw new InvalidOperationException("El reparto por comida no suma 100 %.");
        if (Comidas.Select(c => c.TipoComidaId).Distinct().Count() != Comidas.Count)
            throw new InvalidOperationException("Hay comidas repetidas en el reparto.");
        if (NivelesActividad.Count == 0)
            throw new InvalidOperationException("No hay niveles de actividad.");
        foreach (var o in new[] { "bajar", "mantener", "subir" })
            if (!AjusteObjetivoKcal.ContainsKey(o))
                throw new InvalidOperationException($"Falta el ajuste para el objetivo '{o}'.");
        if (EdadMin > EdadMax)
            throw new InvalidOperationException("edad_min es mayor que edad_max.");
    }

    public double FactorMinimo => NivelesActividad.Min(n => n.Factor);
    public double FactorMaximo => NivelesActividad.Max(n => n.Factor);

    /// <summary>Devuelve el factor de una clave ("ligero" → 1,375).</summary>
    public double FactorDe(string clave) =>
        NivelesActividad.FirstOrDefault(n => string.Equals(n.Clave, clave?.Trim(), StringComparison.OrdinalIgnoreCase))?.Factor
        ?? throw new ArgumentException(
            $"Nivel de actividad desconocido: '{clave}'. Valores válidos: {string.Join(", ", NivelesActividad.Select(n => n.Clave))}.",
            nameof(clave));

    public override string ToString() => string.Create(CultureInfo.InvariantCulture,
        $"Mifflin-St Jeor, {NivelesActividad.Count} niveles, macros {FraccionCarbohidratos:P0}/{FraccionProteina:P0}/{FraccionGrasa:P0}, {Comidas.Count} comidas");
}
