using NutriApp.Domain.Motor;

namespace NutriApp.Tests.Motor;

/// <summary>
/// ACT-10 · El filtro con el catálogo REAL (datos/bd: 997 alimentos y 471 platos) y 20 perfiles variados.
/// Comprueba, para cada perfil y cada comida, que hay al menos 3 candidatos y que NINGUNO rompe una regla.
/// </summary>
public class FiltroCatalogoRealTests
{
    /// <summary>Se pasa el nombre (texto) para que el Explorador de pruebas muestre los 20 casos por separado.</summary>
    public static TheoryData<string> Perfiles => new(PerfilesPrueba.Todos.Select(p => p.Nombre));

    private static readonly ParametrosFiltro P = ParametrosFiltro.Predeterminados;

    [Theory]
    [MemberData(nameof(Perfiles))]
    public void Cada_comida_tiene_al_menos_3_candidatos_y_ninguno_rompe_una_regla(string nombrePerfil)
    {
        var perfil = PerfilesPrueba.Todos.Single(p => p.Nombre == nombrePerfil);
        var cat = CatalogoCsv.Real;
        var req = CalculadoraRequerimiento.Calcular(perfil.Datos);
        var r = FiltroReglas.Filtrar(cat, perfil.Filtro, req);

        Assert.Equal(4, r.Comidas.Count);
        foreach (var comida in r.Comidas)
        {
            Assert.True(comida.Candidatos.Count >= 3, $"{comida.Comida}: solo {comida.Candidatos.Count} candidatos");
            Assert.True(comida.Suficiente);

            foreach (var c in comida.Candidatos)
            {
                var donde = $"{comida.Comida} → {c.Nombre}";

                // Energía dentro del margen aplicado
                Assert.True(Math.Abs(c.Kcal - comida.MetaKcal) <= comida.MetaKcal * comida.ToleranciaAplicada + 0.1, $"energía: {donde}");

                if (c.Tipo == TipoCandidato.Alimento)
                {
                    var a = cat.Alimentos.Single(x => x.Id == c.Id);
                    Assert.Contains(comida.TipoComidaId, a.TiposComida);
                    Assert.False(a.Alergenos.Overlaps(perfil.Filtro.Alergenos), $"alérgeno: {donde}");
                    if (perfil.Filtro.TieneDiabetes)
                    {
                        Assert.False(a.IndiceGlucemico >= P.IgAltoDesde || a.CategoriaIG == "alto", $"IG: {donde}");
                        Assert.True(a.AzucaresPor100 < (a.EsLiquido ? P.AzucaresAltoDesdePor100ml : P.AzucaresAltoDesdePor100g), $"azúcares: {donde}");
                    }
                    if (perfil.Filtro.TieneHipertension)
                        Assert.True(a.SodioMgPor100 < P.SodioAltoDesdeMgPor100g, $"sodio: {donde}");
                }
                else
                {
                    var pl = cat.Platos.Single(x => x.Id == c.Id);
                    Assert.Contains(comida.TipoComidaId, pl.TiposComida);
                    Assert.False(pl.Alergenos.Overlaps(perfil.Filtro.Alergenos), $"alérgeno: {donde}");
                    Assert.NotEqual("baja", pl.CalidadNutricional);
                    if (perfil.Filtro.TieneDiabetes)
                    {
                        Assert.False(pl.IndiceGlucemico >= P.IgAltoDesde, $"IG: {donde}");
                        Assert.True(pl.Por100g(pl.AzucaresGPorcion) < P.AzucaresAltoDesdePor100g, $"azúcares: {donde}");
                    }
                    if (perfil.Filtro.TieneHipertension)
                        Assert.True(pl.Por100g(pl.SodioMgPorcion) < P.SodioAltoDesdeMgPor100g, $"sodio: {donde}");
                }
            }
        }
    }

    [Fact]
    public void Catalogo_real_completo()
    {
        Assert.Equal(997, CatalogoCsv.Real.Alimentos.Count);
        Assert.Equal(471, CatalogoCsv.Real.Platos.Count);
    }

    [Fact]
    public void El_almuerzo_del_plan_incluye_platos_ecuatorianos_del_prototipo()
    {
        var plan = PerfilesPrueba.Todos[0];   // F 30, ligera, bajar, sin lácteos → almuerzo 493,5 kcal
        var almuerzo = FiltroReglas.Filtrar(CatalogoCsv.Real, plan.Filtro, CalculadoraRequerimiento.Calcular(plan.Datos)).Comidas[1];
        var nombres = almuerzo.Candidatos.Select(c => c.Nombre).ToList();
        Assert.Contains("Seco de Carne", nombres);
        Assert.Contains("Caldo de Gallina", nombres);
    }

    [Fact]
    public void Guatita_tiene_mani_por_la_pasta_de_mani_y_no_se_recomienda_sin_mani()
    {
        var guatita = CatalogoCsv.Real.Platos.Single(p => p.Nombre == "Guatita");
        Assert.Contains(4, guatita.Alergenos);

        var sinMani = PerfilesPrueba.Todos.Single(p => p.Nombre.StartsWith("08"));
        var r = FiltroReglas.Filtrar(CatalogoCsv.Real, sinMani.Filtro, CalculadoraRequerimiento.Calcular(sinMani.Datos));
        Assert.DoesNotContain(r.Comidas.SelectMany(c => c.Candidatos), c => c.Nombre == "Guatita");
    }

    [Theory]
    [InlineData("Nusco", 2)]          // crema de avellana con leche: ACT-06 no le había puesto lácteos
    [InlineData("Caballa, fresco", 6)]
    [InlineData("Pony Malta", 1)]     // malta de cebada → gluten
    public void Segunda_red_de_seguridad_en_los_nombres_de_alimentos(string parteDelNombre, int alergeno)
    {
        var a = CatalogoCsv.Real.Alimentos.First(x => x.Nombre.Contains(parteDelNombre));
        Assert.Contains(alergeno, a.Alergenos);
    }
}
