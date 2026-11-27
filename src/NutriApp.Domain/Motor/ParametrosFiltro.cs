using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NutriApp.Domain.Motor;

/// <summary>Una regla de palabras clave para detectar un alérgeno en un texto (ACT-06, reutilizada en ACT-10).</summary>
public sealed class ReglaPalabrasAlergeno
{
    public required int AlergenoId { get; init; }
    public required string Alergeno { get; init; }
    public required Regex Patron { get; init; }
    public Regex? Excepcion { get; init; }

    /// <summary>
    /// Para el NOMBRE de un alimento (un solo producto): igual que ACT-06, si aparece una excepción
    /// ("leche de coco", "nuez moscada") la regla no se aplica a ese nombre.
    /// </summary>
    public bool DetectaEnNombre(string textoNormalizado) =>
        Patron.IsMatch(textoNormalizado) && !(Excepcion?.IsMatch(textoNormalizado) ?? false);

    /// <summary>
    /// Para una LÍNEA de ingredientes o el nombre de un plato (pueden mencionar varias cosas): cuenta cualquier
    /// coincidencia que NO esté dentro de una excepción. "1 taza de leche de coco y 1/2 taza de leche" → lácteos.
    /// </summary>
    public bool DetectaEnTexto(string textoNormalizado)
    {
        var excepciones = Excepcion?.Matches(textoNormalizado);
        foreach (Match m in Patron.Matches(textoNormalizado))
        {
            bool dentro = excepciones is not null && excepciones.Any(e =>
                m.Index >= e.Index && m.Index + m.Length <= e.Index + e.Length);
            if (!dentro) return true;
        }
        return false;
    }
}

/// <summary>
/// ACT-10 · Parámetros del filtro por reglas, leídos del mismo parametros_motor.json (ACT-05).
/// </summary>
public sealed class ParametrosFiltro
{
    // Energía por comida
    public required double ToleranciaPrincipal { get; init; }       // ±10 %
    public required double ToleranciaAmpliada { get; init; }        // ±15 % si con ±10 % hay menos de MinimoCandidatos
    public required int MinimoCandidatos { get; init; }             // 3
    public required IReadOnlyList<double> MultiplicadoresAlimento { get; init; }
    public required IReadOnlyList<double> MultiplicadoresPlato { get; init; }

    // Diabetes
    public required int IgAltoDesde { get; init; }                  // 70 (ISO 26642)
    public required IReadOnlySet<string> CategoriasIgExcluidas { get; init; }
    public required double AzucaresAltoDesdePor100g { get; init; }  // 15 g/100 g
    public required double AzucaresAltoDesdePor100ml { get; init; } // 7,5 g/100 ml
    public required bool ExcluirAzucaresSinDato { get; init; }

    // Hipertensión
    public required double SodioAltoDesdeMgPor100g { get; init; }   // 600 mg/100 g
    public required double SodioAltoDesdeMgPor100ml { get; init; }
    public required bool ExcluirSodioSinDato { get; init; }

    // Alérgenos
    public required IReadOnlyList<ReglaPalabrasAlergeno> PalabrasAlergenos { get; init; }
    public required IReadOnlySet<string> GruposMarcaConfirmarEtiqueta { get; init; }

    // Catálogo
    public required bool UsarSoloAptoMotor { get; init; }
    public required IReadOnlySet<string> PlatosExcluirClasificacion { get; init; }
    public required IReadOnlySet<string> PlatosExcluirCalidad { get; init; }
    public required bool PlatosExcluirRevisar { get; init; }

    /// <summary>Los parámetros del JSON incrustado (se leen una sola vez).</summary>
    public static ParametrosFiltro Predeterminados { get; } = DesdeJson(ParametrosMotor.LeerJsonIncrustado());

    public static ParametrosFiltro DesdeJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        var comidas = r.GetProperty("comidas");
        var tol = comidas.GetProperty("tolerancia_energia");
        var mult = comidas.GetProperty("multiplicadores_porcion");
        var salud = r.GetProperty("reglas_salud");
        var dia = salud.GetProperty("diabetes");
        var hta = salud.GetProperty("hipertension");
        var ale = salud.GetProperty("alergenos");
        var cat = r.GetProperty("catalogo");
        var platos = cat.GetProperty("platos");

        static IReadOnlySet<string> Conjunto(JsonElement e) =>
            e.EnumerateArray().Select(x => x.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        static List<double> Numeros(JsonElement e) => e.EnumerateArray().Select(x => x.GetDouble()).ToList();
        const RegexOptions op = RegexOptions.CultureInvariant;

        var p = new ParametrosFiltro
        {
            ToleranciaPrincipal = tol.GetProperty("principal").GetDouble(),
            ToleranciaAmpliada = tol.GetProperty("ampliada").GetDouble(),
            MinimoCandidatos = tol.GetProperty("minimo_candidatos").GetInt32(),
            MultiplicadoresAlimento = Numeros(mult.GetProperty("alimento")),
            MultiplicadoresPlato = Numeros(mult.GetProperty("plato")),

            IgAltoDesde = dia.GetProperty("ig_alto_desde").GetInt32(),
            CategoriasIgExcluidas = Conjunto(dia.GetProperty("excluir_categoria_ig")),
            AzucaresAltoDesdePor100g = dia.GetProperty("azucares_alto_desde").GetProperty("100 g").GetDouble(),
            AzucaresAltoDesdePor100ml = dia.GetProperty("azucares_alto_desde").GetProperty("100 ml").GetDouble(),
            ExcluirAzucaresSinDato = dia.GetProperty("excluir_azucares_sin_dato").GetBoolean(),

            SodioAltoDesdeMgPor100g = hta.GetProperty("sodio_alto_desde_mg").GetProperty("100 g").GetDouble(),
            SodioAltoDesdeMgPor100ml = hta.GetProperty("sodio_alto_desde_mg").GetProperty("100 ml").GetDouble(),
            ExcluirSodioSinDato = hta.GetProperty("excluir_sodio_sin_dato").GetBoolean(),

            PalabrasAlergenos = ale.GetProperty("palabras_clave").GetProperty("reglas").EnumerateArray()
                .Select(x =>
                {
                    var exc = x.GetProperty("excepcion").GetString();
                    return new ReglaPalabrasAlergeno
                    {
                        AlergenoId = x.GetProperty("id").GetInt32(),
                        Alergeno = x.GetProperty("alergeno").GetString()!,
                        Patron = new Regex(x.GetProperty("patron").GetString()!, op),
                        Excepcion = string.IsNullOrWhiteSpace(exc) ? null : new Regex(exc, op),
                    };
                })
                .ToList(),
            GruposMarcaConfirmarEtiqueta = Conjunto(ale.GetProperty("marca_confirmar_etiqueta").GetProperty("grupos")),

            UsarSoloAptoMotor = cat.GetProperty("usar_solo_apto_motor").GetBoolean(),
            PlatosExcluirClasificacion = Conjunto(platos.GetProperty("excluir_clasificacion")),
            PlatosExcluirCalidad = Conjunto(platos.GetProperty("excluir_calidad_nutricional")),
            PlatosExcluirRevisar = platos.GetProperty("excluir_revisar").GetBoolean(),
        };
        p.Validar();
        return p;
    }

    public void Validar()
    {
        if (ToleranciaPrincipal <= 0 || ToleranciaAmpliada < ToleranciaPrincipal)
            throw new InvalidOperationException("La tolerancia ampliada debe ser mayor o igual que la principal, y ambas positivas.");
        if (MinimoCandidatos < 1)
            throw new InvalidOperationException("minimo_candidatos debe ser al menos 1.");
        if (MultiplicadoresAlimento.Count == 0 || MultiplicadoresPlato.Count == 0 ||
            MultiplicadoresAlimento.Concat(MultiplicadoresPlato).Any(m => m <= 0))
            throw new InvalidOperationException("Los multiplicadores de porción deben ser positivos.");
        if (PalabrasAlergenos.Count == 0)
            throw new InvalidOperationException("Faltan las reglas de palabras clave de alérgenos.");
    }

    /// <summary>Minúsculas, sin tildes y con un solo espacio: igual que normalizar() de ACT-06.</summary>
    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
        var d = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }
}
