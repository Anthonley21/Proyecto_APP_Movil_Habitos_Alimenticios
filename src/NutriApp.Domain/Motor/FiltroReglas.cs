using System.Globalization;

namespace NutriApp.Domain.Motor;

public enum TipoCandidato { Alimento, Plato }

/// <summary>Lo que el filtro necesita saber de la persona.</summary>
public sealed record PerfilFiltro(bool TieneDiabetes, bool TieneHipertension, IReadOnlySet<int> Alergenos);

/// <summary>
/// Un alimento o plato que cumple TODAS las reglas del perfil para una comida, ya con la cantidad ajustada.
/// </summary>
/// <param name="Multiplicador">Veces la porción típica (0,5 · 1 · 1,5 · 2)</param>
/// <param name="Cantidad">Texto para la persona: "1 taza", "1½ × 1 taza", "1 porción (331 g)"</param>
/// <param name="DesvioEnergia">(kcal − meta) / meta: −0,02 = 2 % por debajo de la meta de la comida</param>
/// <param name="IgSinDato">Sin índice glucémico: con diabetes se permite, pero el ranking (ACT-11) lo deja después</param>
/// <param name="Reglas">Reglas que cumple, en palabras (base de la explicación de ACT-12)</param>
public sealed record Candidato(
    TipoCandidato Tipo, int Id, string Nombre, string Grupo,
    double Multiplicador, string Cantidad, double Gramos,
    double Kcal, double ProteinaG, double CarbohidratosG, double GrasaG,
    double? AzucaresG, double? SodioMg, int? IndiceGlucemico,
    double DesvioEnergia, bool IgSinDato,
    IReadOnlyList<string> Reglas);

/// <summary>Resultado del filtro para una comida.</summary>
/// <param name="ToleranciaAplicada">0,10 normalmente; 0,15 si con ±10 % quedaban menos de 3 candidatos</param>
/// <param name="Descartes">Cuántos se descartaron por cada regla (la primera que no cumplen)</param>
/// <param name="Suficiente">true si hay al menos 3 candidatos (el mínimo para dar alternativas)</param>
/// <param name="Aviso">Si no es suficiente, qué pasó (para mostrarlo a la persona); si no, null</param>
public sealed record CandidatosComida(
    int TipoComidaId, string Comida, double MetaKcal, double ToleranciaAplicada,
    double MinKcal, double MaxKcal, int Evaluados,
    IReadOnlyDictionary<string, int> Descartes,
    IReadOnlyList<Candidato> Candidatos,
    bool Suficiente, string? Aviso);

public sealed record ResultadoFiltro(IReadOnlyList<CandidatosComida> Comidas);

/// <summary>
/// ACT-10 · Filtro por reglas. Para cada comida del día deja solo los alimentos y platos que:
///  1. tienen datos confiables (catálogo),
///  2. son aptos para esa comida,
///  3. no contienen ningún alérgeno de la persona (exclusión total),
///  4. con diabetes: no tienen índice glucémico alto ni muchos azúcares,
///  5. con hipertensión: no tienen mucho sodio,
///  6. ajustando la porción (½, 1, 1½, 2), quedan a ±10 % de la meta de la comida (±15 % si faltan opciones).
/// Todos los umbrales salen de parametros_motor.json. El orden final lo decide el ranking (ACT-11).
/// </summary>
public static class FiltroReglas
{
    // Nombres de las reglas (claves de Descartes)
    public const string Catalogo = "calidad_dato";
    public const string TipoComida = "tipo_comida";
    public const string Alergeno = "alergeno";
    public const string AlergenoMarca = "alergeno_marca_sin_confirmar";
    public const string DiabetesIg = "diabetes_indice_glucemico";
    public const string DiabetesAzucar = "diabetes_azucares";
    public const string HipertensionSodio = "hipertension_sodio";
    public const string Energia = "energia";

    public static readonly string[] OrdenReglas =
        [Catalogo, TipoComida, Alergeno, AlergenoMarca, DiabetesIg, DiabetesAzucar, HipertensionSodio, Energia];

    /// <summary>Coma decimal (como en Ecuador) sin depender de la cultura instalada en el servidor.</summary>
    private static readonly NumberFormatInfo Es = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = " " };

    public static ResultadoFiltro Filtrar(CatalogoMotor catalogo, PerfilFiltro perfil, Requerimiento requerimiento,
        ParametrosFiltro? parametros = null)
    {
        var p = parametros ?? ParametrosFiltro.Predeterminados;
        var comidas = requerimiento.Comidas.Select(c => FiltrarComida(catalogo, perfil, c, p)).ToList();
        return new ResultadoFiltro(comidas);
    }

    public static CandidatosComida FiltrarComida(CatalogoMotor catalogo, PerfilFiltro perfil, RequerimientoComida comida,
        ParametrosFiltro? parametros = null)
    {
        var p = parametros ?? ParametrosFiltro.Predeterminados;
        var descartes = OrdenReglas.ToDictionary(r => r, _ => 0);
        var dentroAmpliada = new List<Candidato>();
        int evaluados = 0;

        foreach (var a in catalogo.Alimentos)
        {
            evaluados++;
            var falla = ReglaQueFalla(a, perfil, comida.TipoComidaId, p);
            if (falla is not null) { descartes[falla]++; continue; }
            var c = AjustarAlimento(a, comida, perfil, catalogo, p);
            if (Math.Abs(c.DesvioEnergia) <= p.ToleranciaAmpliada + 1e-9) dentroAmpliada.Add(c);
            else descartes[Energia]++;
        }
        foreach (var pl in catalogo.Platos)
        {
            evaluados++;
            var falla = ReglaQueFalla(pl, perfil, comida.TipoComidaId, p);
            if (falla is not null) { descartes[falla]++; continue; }
            var c = AjustarPlato(pl, comida, perfil, catalogo, p);
            if (Math.Abs(c.DesvioEnergia) <= p.ToleranciaAmpliada + 1e-9) dentroAmpliada.Add(c);
            else descartes[Energia]++;
        }

        // ±10 %; si quedan menos de 3, ±15 %
        var principal = dentroAmpliada.Where(c => Math.Abs(c.DesvioEnergia) <= p.ToleranciaPrincipal + 1e-9).ToList();
        var tolerancia = p.ToleranciaPrincipal;
        var elegidos = principal;
        if (principal.Count < p.MinimoCandidatos)
        {
            tolerancia = p.ToleranciaAmpliada;
            elegidos = dentroAmpliada;
        }
        else descartes[Energia] += dentroAmpliada.Count - principal.Count;

        // Orden provisional (el definitivo es el ranking de ACT-11): más cerca de la meta primero
        var ordenados = elegidos
            .Select(c => c with { Reglas = c.Reglas.Prepend(TextoEnergia(c, comida, tolerancia)).ToList() })
            .OrderBy(c => Math.Abs(c.DesvioEnergia)).ThenBy(c => c.Tipo).ThenBy(c => c.Id)
            .ToList();

        // Nunca se relaja una regla de salud: si no alcanza, se avisa (las reglas de salud mandan sobre la variedad).
        bool suficiente = ordenados.Count >= p.MinimoCandidatos;
        string? aviso = suficiente ? null
            : $"Solo hay {ordenados.Count} opción(es) para {comida.Nombre} que cumplen todas tus restricciones " +
              $"con ±{N(tolerancia * 100, 0)} % de la meta ({N(comida.Kcal, 0)} kcal). No se relajan las reglas de salud ni de alérgenos.";

        return new CandidatosComida(comida.TipoComidaId, comida.Nombre, comida.Kcal, tolerancia,
            Math.Round(comida.Kcal * (1 - tolerancia), 1), Math.Round(comida.Kcal * (1 + tolerancia), 1),
            evaluados, descartes, ordenados, suficiente, aviso);
    }

    // ------------------------------------------------------------------ reglas (sin la de energía)

    /// <summary>Primera regla que el alimento NO cumple, o null si las cumple todas.</summary>
    public static string? ReglaQueFalla(AlimentoMotor a, PerfilFiltro perfil, int tipoComidaId, ParametrosFiltro p)
    {
        if (p.UsarSoloAptoMotor && !a.AptoMotor) return Catalogo;
        if (a.PorcionGramos <= 0 || a.KcalPor100 <= 0) return Catalogo;
        if (!a.TiposComida.Contains(tipoComidaId)) return TipoComida;
        if (a.Alergenos.Overlaps(perfil.Alergenos)) return Alergeno;
        if (perfil.Alergenos.Count > 0 && a.EsProductoDeMarca && p.GruposMarcaConfirmarEtiqueta.Contains(a.GrupoCodigo))
            return AlergenoMarca;
        if (perfil.TieneDiabetes)
        {
            if (p.CategoriasIgExcluidas.Contains(a.CategoriaIG) || a.IndiceGlucemico >= p.IgAltoDesde) return DiabetesIg;
            if (a.AzucaresPor100 is null ? p.ExcluirAzucaresSinDato
                : a.AzucaresPor100 >= (a.EsLiquido ? p.AzucaresAltoDesdePor100ml : p.AzucaresAltoDesdePor100g))
                return DiabetesAzucar;
        }
        if (perfil.TieneHipertension)
        {
            if (a.SodioMgPor100 is null ? p.ExcluirSodioSinDato
                : a.SodioMgPor100 >= (a.EsLiquido ? p.SodioAltoDesdeMgPor100ml : p.SodioAltoDesdeMgPor100g))
                return HipertensionSodio;
        }
        return null;
    }

    /// <summary>Primera regla que el plato NO cumple, o null si las cumple todas.</summary>
    public static string? ReglaQueFalla(PlatoMotor pl, PerfilFiltro perfil, int tipoComidaId, ParametrosFiltro p)
    {
        if (p.PlatosExcluirClasificacion.Contains(pl.Clasificacion)
            || p.PlatosExcluirCalidad.Contains(pl.CalidadNutricional)
            || (p.PlatosExcluirRevisar && pl.Revisar)
            || pl.KcalPorcion is null or <= 0 || pl.PorcionG is null or <= 0
            || pl.ProteinaGPorcion is null || pl.CarbohidratosGPorcion is null || pl.GrasaGPorcion is null)
            return Catalogo;
        if (!pl.TiposComida.Contains(tipoComidaId)) return TipoComida;
        if (pl.Alergenos.Overlaps(perfil.Alergenos)) return Alergeno;
        if (perfil.TieneDiabetes)
        {
            if (pl.IndiceGlucemico >= p.IgAltoDesde) return DiabetesIg;
            var az = pl.Por100g(pl.AzucaresGPorcion);
            if (az is null ? p.ExcluirAzucaresSinDato : az >= p.AzucaresAltoDesdePor100g) return DiabetesAzucar;
        }
        if (perfil.TieneHipertension)
        {
            var na = pl.Por100g(pl.SodioMgPorcion);
            if (na is null ? p.ExcluirSodioSinDato : na >= p.SodioAltoDesdeMgPor100g) return HipertensionSodio;
        }
        return null;
    }

    // ------------------------------------------------------------------ ajuste de la porción

    private static double MejorMultiplicador(IReadOnlyList<double> opciones, double kcalUna, double meta) =>
        opciones.OrderBy(m => Math.Abs(kcalUna * m - meta)).ThenBy(m => Math.Abs(m - 1)).First();

    private static Candidato AjustarAlimento(AlimentoMotor a, RequerimientoComida comida, PerfilFiltro perfil,
        CatalogoMotor cat, ParametrosFiltro p)
    {
        double kcalUna = a.KcalPor100 * a.PorcionGramos / 100;
        double m = MejorMultiplicador(p.MultiplicadoresAlimento, kcalUna, comida.Kcal);
        double g = a.PorcionGramos * m;
        double f = g / 100;
        double kcal = a.KcalPor100 * f;
        var reglas = ReglasCumplidas(perfil, cat, p, a.Nombre, comida.Nombre,
            ig: a.IndiceGlucemico, categoriaIg: a.CategoriaIG,
            azucar100: a.AzucaresPor100, sodio100: a.SodioMgPor100, liquido: a.EsLiquido);
        return new Candidato(TipoCandidato.Alimento, a.Id, a.Nombre, a.GrupoCodigo, m,
            Cantidad(m, a.PorcionDescripcion), R(g, 0),
            R(kcal, 1), R(a.ProteinaPor100 * f, 1), R(a.CarbohidratosPor100 * f, 1), R(a.GrasaPor100 * f, 1),
            a.AzucaresPor100 is null ? null : R(a.AzucaresPor100.Value * f, 1),
            a.SodioMgPor100 is null ? null : R(a.SodioMgPor100.Value * f, 0),
            a.IndiceGlucemico, (kcal - comida.Kcal) / comida.Kcal, a.IndiceGlucemico is null, reglas);
    }

    private static Candidato AjustarPlato(PlatoMotor pl, RequerimientoComida comida, PerfilFiltro perfil,
        CatalogoMotor cat, ParametrosFiltro p)
    {
        double m = MejorMultiplicador(p.MultiplicadoresPlato, pl.KcalPorcion!.Value, comida.Kcal);
        double kcal = pl.KcalPorcion.Value * m;
        double g = pl.PorcionG!.Value * m;
        var reglas = ReglasCumplidas(perfil, cat, p, pl.Nombre, comida.Nombre,
            ig: pl.IndiceGlucemico, categoriaIg: null,
            azucar100: pl.Por100g(pl.AzucaresGPorcion), sodio100: pl.Por100g(pl.SodioMgPorcion), liquido: false);
        var porciones = m <= 1 ? $"{Fraccion(m)} porción" : $"{Fraccion(m)} porciones";
        return new Candidato(TipoCandidato.Plato, pl.Id, pl.Nombre, "PLATO", m,
            $"{porciones} ({R(g, 0).ToString("0", Es)} g)", R(g, 0),
            R(kcal, 1), R(pl.ProteinaGPorcion!.Value * m, 1), R(pl.CarbohidratosGPorcion!.Value * m, 1), R(pl.GrasaGPorcion!.Value * m, 1),
            pl.AzucaresGPorcion is null ? null : R(pl.AzucaresGPorcion.Value * m, 1),
            pl.SodioMgPorcion is null ? null : R(pl.SodioMgPorcion.Value * m, 0),
            pl.IndiceGlucemico, (kcal - comida.Kcal) / comida.Kcal, pl.IndiceGlucemico is null, reglas);
    }

    // ------------------------------------------------------------------ textos de las reglas cumplidas

    private static List<string> ReglasCumplidas(PerfilFiltro perfil, CatalogoMotor cat, ParametrosFiltro p,
        string nombre, string comida, int? ig, string? categoriaIg, double? azucar100, double? sodio100, bool liquido)
    {
        var u = liquido ? "100 ml" : "100 g";
        var r = new List<string> { $"Apto para {comida}" };
        foreach (var id in perfil.Alergenos.Order())
            r.Add($"Sin {cat.NombresAlergenos.GetValueOrDefault(id, $"alérgeno {id}")}");
        if (perfil.TieneDiabetes)
        {
            r.Add(ig is null
                ? (categoriaIg is not null && categoriaIg.StartsWith("no aplica", StringComparison.OrdinalIgnoreCase)
                    ? "Índice glucémico: no aplica (casi sin carbohidratos)"
                    : "Índice glucémico sin dato (no es alto según la regla)")
                : $"Índice glucémico {ig} (menor que {p.IgAltoDesde})");
            r.Add(azucar100 is null
                ? "Azúcares sin dato"
                : $"Azúcares {N(azucar100.Value, 1)} g por {u} (menor que {N(liquido ? p.AzucaresAltoDesdePor100ml : p.AzucaresAltoDesdePor100g, 1)})");
        }
        if (perfil.TieneHipertension)
            r.Add(sodio100 is null
                ? "Sodio sin dato"
                : $"Sodio {N(sodio100.Value, 0)} mg por {u} (menor que {N(liquido ? p.SodioAltoDesdeMgPor100ml : p.SodioAltoDesdeMgPor100g, 0)})");
        return r;
    }

    private static string TextoEnergia(Candidato c, RequerimientoComida comida, double tol) =>
        $"{N(c.Kcal, 0)} kcal: {N(c.Kcal / comida.Kcal * 100, 0)} % de la meta de {comida.Nombre} ({N(comida.Kcal, 0)} kcal, margen ±{N(tol * 100, 0)} %)";

    // ------------------------------------------------------------------ utilidades

    private static double R(double x, int d) => Math.Round(x, d, MidpointRounding.AwayFromZero);
    private static string N(double x, int d) => R(x, d).ToString(d == 0 ? "0" : "0.0", Es);

    /// <summary>0,5 → "½"; 1,5 → "1½"; 2 → "2".</summary>
    public static string Fraccion(double m)
    {
        int entero = (int)Math.Floor(m);
        bool medio = Math.Abs(m - entero - 0.5) < 1e-9;
        if (!medio) return m.ToString("0.##", Es);
        return entero == 0 ? "½" : $"{entero}½";
    }

    private static string Cantidad(double m, string porcion) =>
        m == 1 ? porcion : $"{Fraccion(m)} × {porcion}";
}
