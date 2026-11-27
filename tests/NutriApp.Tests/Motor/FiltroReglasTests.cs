using NutriApp.Domain.Entidades;
using NutriApp.Domain.Motor;

namespace NutriApp.Tests.Motor;

/// <summary>
/// ACT-10 · Cada regla del filtro, probada por separado con un catálogo pequeño hecho a mano.
/// Comida de prueba: almuerzo (id 2) con meta de 500 kcal.
/// </summary>
public class FiltroReglasTests
{
    private const int Desayuno = 1, Almuerzo = 2;
    private static readonly RequerimientoComida AlmuerzoMeta500 =
        new(Almuerzo, "almuerzo", 0.35, 500, new Macronutrientes(25, 62.5, 16.7));

    private static readonly PerfilFiltro SinRestricciones = new(false, false, new HashSet<int>());
    private static PerfilFiltro ConAlergenos(params int[] ids) => new(false, false, ids.ToHashSet());

    // ---------------- constructores de datos de prueba

    /// <summary>Alimento de 500 kcal por porción (100 g) apto para almuerzo, sin alérgenos ni problemas de salud.</summary>
    private static AlimentoMotor A(int id, string nombre = "Alimento", double kcal100 = 500, double porcionG = 100,
        string grupo = "CAR", string unidad = "100 g", double? azucares = 2, double? sodio = 100, int? ig = 40,
        string categoriaIg = "bajo", bool marca = false, int[]? alergenos = null, int[]? comidas = null) =>
        new(id, nombre, grupo, unidad, kcal100, 20, 60, 15, azucares, sodio, ig, categoriaIg, marca, true,
            "1 porción", porcionG, (alergenos ?? []).ToHashSet(), (comidas ?? [Almuerzo]).ToHashSet());

    /// <summary>Plato de 500 kcal en 400 g, apto para almuerzo y con datos confiables.</summary>
    private static PlatoMotor Pl(int id, string nombre = "Plato", double kcal = 500, double porcionG = 400,
        double? azucares = 4, double? sodio = 800, int? ig = 50, string calidad = "confiable",
        string clasificacion = "preparable en Ecuador", bool revisar = false, int[]? alergenos = null, int[]? comidas = null) =>
        new(id, nombre, true, clasificacion, calidad, revisar, porcionG, kcal, 30, 50, 15, azucares, sodio, ig,
            (alergenos ?? []).ToHashSet(), (comidas ?? [Almuerzo]).ToHashSet());

    private static CatalogoMotor Cat(IEnumerable<AlimentoMotor>? alimentos = null, IEnumerable<PlatoMotor>? platos = null) => new()
    {
        Alimentos = (alimentos ?? []).ToList(),
        Platos = (platos ?? []).ToList(),
        NombresAlergenos = new Dictionary<int, string> { [1] = "gluten", [2] = "lácteos", [6] = "pescado", [8] = "soya" },
    };

    private static CandidatosComida Filtrar(CatalogoMotor c, PerfilFiltro p) => FiltroReglas.FiltrarComida(c, p, AlmuerzoMeta500);

    private static IEnumerable<int> Ids(CandidatosComida r) => r.Candidatos.Select(x => x.Id);

    // ---------------- tipo de comida

    [Fact]
    public void Solo_entra_lo_que_es_apto_para_esa_comida()
    {
        var r = Filtrar(Cat([A(1), A(2, comidas: [Desayuno])]), SinRestricciones);
        Assert.Equal(new[] { 1 }, Ids(r));
        Assert.Equal(1, r.Descartes[FiltroReglas.TipoComida]);
    }

    // ---------------- alérgenos (exclusión total)

    [Fact]
    public void Excluye_alimentos_y_platos_con_un_alergeno_del_perfil()
    {
        var r = Filtrar(Cat([A(1), A(2, alergenos: [2])], [Pl(10), Pl(11, alergenos: [2, 6])]), ConAlergenos(2));
        Assert.Equal(new[] { 1, 10 }, Ids(r).Order());
        Assert.Equal(2, r.Descartes[FiltroReglas.Alergeno]);
    }

    [Fact]
    public void Sin_alergias_no_se_excluye_nada_por_alergenos()
    {
        var r = Filtrar(Cat([A(1, alergenos: [2])], [Pl(10, alergenos: [6])]), SinRestricciones);
        Assert.Equal(2, r.Candidatos.Count);
    }

    [Fact]
    public void Producto_de_marca_de_un_grupo_a_confirmar_se_excluye_solo_si_hay_alergias()
    {
        var cat = Cat([A(1, grupo: "SNA", marca: true), A(2, grupo: "CAR", marca: true)]);
        Assert.Equal(new[] { 2 }, Ids(Filtrar(cat, ConAlergenos(4))));          // con alguna alergia: no se arriesga
        Assert.Equal(new[] { 1, 2 }, Ids(Filtrar(cat, SinRestricciones)).Order());
    }

    // ---------------- alérgenos derivados del texto de los ingredientes

    private static CatalogoMotor CatalogoDesdeEntidades(params (int Id, string Nombre, string[] Ingredientes)[] platos)
    {
        var lista = platos.Select(p => new Plato
        {
            Id = p.Id, Nombre = p.Nombre, Clasificacion = "preparable en Ecuador", CalidadNutricional = "confiable",
            PorcionG = 400, KcalPorcion = 500, ProteinaGPorcion = 30, CarbohidratosGPorcion = 50, GrasaGPorcion = 15,
            AzucaresGPorcion = 4, SodioMgPorcion = 800,
        }).ToList();
        var ingredientes = platos.SelectMany(p => p.Ingredientes.Select((t, i) => new PlatoIngrediente
        {
            Id = p.Id * 100 + i, PlatoId = p.Id, Orden = i, Texto = t, Estado = "condimento",   // sin AlimentoId
        })).ToList();
        var comidas = platos.Select(p => new PlatoTipoComida { PlatoId = p.Id, TipoComidaId = Almuerzo }).ToList();
        return CatalogoMotor.Construir([], [], [], [], lista, ingredientes, comidas,
            [new Alergeno { Id = 1, Nombre = "gluten" }, new Alergeno { Id = 2, Nombre = "lácteos" },
             new Alergeno { Id = 5, Nombre = "frutos secos" }, new Alergeno { Id = 8, Nombre = "soya" }]);
    }

    [Fact]
    public void Detecta_alergenos_en_ingredientes_sin_alimento_enlazado()
    {
        var cat = CatalogoDesdeEntidades(
            (1, "Pollo salteado", ["2 cucharadas de salsa de soja", "sal al gusto"]),
            (2, "Arroz con verduras", ["1 taza de arroz", "sal"]));
        var pollo = cat.Platos.Single(p => p.Id == 1);
        Assert.Contains(8, pollo.Alergenos);   // soya
        Assert.Contains(1, pollo.Alergenos);   // gluten: la salsa de soja lleva trigo
        Assert.Empty(cat.Platos.Single(p => p.Id == 2).Alergenos);
    }

    [Fact]
    public void Leche_de_coco_no_es_lacteo_pero_si_hay_leche_ademas_si()
    {
        var cat = CatalogoDesdeEntidades(
            (1, "Encocado", ["1 taza de leche de coco"]),
            (2, "Arroz con leche de coco", ["1 taza de leche de coco", "1/2 taza de leche"]));
        Assert.DoesNotContain(2, cat.Platos.Single(p => p.Id == 1).Alergenos);
        Assert.Contains(2, cat.Platos.Single(p => p.Id == 2).Alergenos);
    }

    [Fact]
    public void Nuez_moscada_no_es_fruto_seco_y_champinones_tampoco()
    {
        var cat = CatalogoDesdeEntidades((1, "Crema de champiñones", ["nuez moscada al gusto", "200 g de champiñones"]));
        Assert.DoesNotContain(5, cat.Platos.Single().Alergenos);
    }

    [Fact]
    public void Vocabulario_de_recetas_nata_y_parmesano_son_lacteos()
    {
        var cat = CatalogoDesdeEntidades(
            (1, "Pasta cremosa", ["200 ml de nata para cocinar"]),
            (2, "Ensalada", ["50 g de parmesano rallado"]));
        Assert.All(cat.Platos, p => Assert.Contains(2, p.Alergenos));
    }

    // ---------------- diabetes

    [Fact]
    public void Diabetes_excluye_indice_glucemico_alto_y_azucares_altos()
    {
        var diabetes = new PerfilFiltro(true, false, new HashSet<int>());
        var cat = Cat(
            [A(1), A(2, ig: 75, categoriaIg: "alto"), A(3, azucares: 15), A(4, azucares: null), A(5, ig: null, categoriaIg: "sin dato")],
            [Pl(10), Pl(11, ig: 70), Pl(12, azucares: 60)]);   // plato 12: 60 g en 400 g = 15 g/100 g
        var r = Filtrar(cat, diabetes);

        Assert.Equal(new[] { 1, 5, 10 }, Ids(r).Order());
        Assert.Equal(2, r.Descartes[FiltroReglas.DiabetesIg]);
        Assert.Equal(3, r.Descartes[FiltroReglas.DiabetesAzucar]);
        Assert.True(r.Candidatos.Single(c => c.Id == 5).IgSinDato);   // permitido, pero el ranking lo deja después
    }

    [Fact]
    public void En_bebidas_el_limite_de_azucares_es_7_5_g_por_100_ml()
    {
        var diabetes = new PerfilFiltro(true, false, new HashSet<int>());
        var cat = Cat([A(1, unidad: "100 ml", azucares: 7.5), A(2, unidad: "100 ml", azucares: 7.4)]);
        Assert.Equal(new[] { 2 }, Ids(Filtrar(cat, diabetes)));
    }

    [Fact]
    public void Sin_diabetes_no_se_mira_el_azucar()
    {
        var r = Filtrar(Cat([A(1, ig: 90, categoriaIg: "alto", azucares: 40)]), SinRestricciones);
        Assert.Single(r.Candidatos);
    }

    // ---------------- hipertensión

    [Fact]
    public void Hipertension_excluye_600_mg_de_sodio_por_100_g_o_mas_y_sin_dato()
    {
        var hta = new PerfilFiltro(false, true, new HashSet<int>());
        var cat = Cat([A(1, sodio: 599), A(2, sodio: 600), A(3, sodio: null)],
                      [Pl(10, sodio: 2000), Pl(11, sodio: 2400)]);   // 500 y 600 mg/100 g
        var r = Filtrar(cat, hta);
        Assert.Equal(new[] { 1, 10 }, Ids(r).Order());
        Assert.Equal(3, r.Descartes[FiltroReglas.HipertensionSodio]);
    }

    // ---------------- calidad del dato

    [Fact]
    public void Platos_con_dato_poco_confiable_no_se_recomiendan()
    {
        var cat = Cat(platos:
        [
            Pl(10), Pl(11, calidad: "baja"), Pl(12, clasificacion: "excluida (ingredientes no disponibles)"),
            Pl(13, revisar: true), Pl(14, kcal: 0),
        ]);
        var r = Filtrar(cat, SinRestricciones);
        Assert.Equal(new[] { 10 }, Ids(r));
        Assert.Equal(4, r.Descartes[FiltroReglas.Catalogo]);
    }

    // ---------------- energía y ajuste de la porción

    [Fact]
    public void Ajusta_la_porcion_para_acercarse_a_la_meta()
    {
        // 250 kcal por porción → 2 porciones = 500 kcal
        var r = Filtrar(Cat([A(1, kcal100: 250)]), SinRestricciones);
        var c = Assert.Single(r.Candidatos);
        Assert.Equal(2, c.Multiplicador);
        Assert.Equal(500, c.Kcal);
        Assert.Equal(200, c.Gramos);
        Assert.Equal("2 × 1 porción", c.Cantidad);
    }

    [Fact]
    public void Plato_a_media_porcion_dice_media_porcion()
    {
        var c = Assert.Single(Filtrar(Cat(platos: [Pl(10, kcal: 1000)]), SinRestricciones).Candidatos);
        Assert.Equal(0.5, c.Multiplicador);
        Assert.Equal("½ porción (200 g)", c.Cantidad);
    }

    [Fact]
    public void Usa_10_por_ciento_si_hay_al_menos_3_candidatos()
    {
        // 500, 520, 540 están a ±10 %; 570 (+14 %) queda fuera
        var r = Filtrar(Cat([A(1), A(2, kcal100: 520), A(3, kcal100: 540), A(4, kcal100: 570)]), SinRestricciones);
        Assert.Equal(0.10, r.ToleranciaAplicada);
        Assert.Equal(new[] { 1, 2, 3 }, Ids(r));
        Assert.Equal(1, r.Descartes[FiltroReglas.Energia]);
        Assert.True(r.Suficiente);
    }

    [Fact]
    public void Amplia_a_15_por_ciento_si_con_10_hay_menos_de_3()
    {
        var r = Filtrar(Cat([A(1), A(2, kcal100: 570), A(3, kcal100: 430), A(4, kcal100: 700)]), SinRestricciones);
        Assert.Equal(0.15, r.ToleranciaAplicada);
        Assert.Equal(new[] { 1, 2, 3 }, Ids(r));            // por cercanía: 0 %, luego +14 % y −14 % (empate: menor id)
        Assert.Equal(1, r.Descartes[FiltroReglas.Energia]);
    }

    [Fact]
    public void Si_no_alcanza_avisa_y_no_relaja_las_reglas_de_salud()
    {
        var r = Filtrar(Cat([A(1), A(2, alergenos: [6]), A(3, alergenos: [6])]), ConAlergenos(6));
        Assert.Equal(new[] { 1 }, Ids(r));                  // los dos con pescado siguen fuera
        Assert.False(r.Suficiente);
        Assert.NotNull(r.Aviso);
    }

    [Fact]
    public void Cada_candidato_explica_las_reglas_que_cumple()
    {
        var perfil = new PerfilFiltro(true, true, new HashSet<int> { 2 });
        var c = Assert.Single(Filtrar(Cat([A(1, sodio: 120)]), perfil).Candidatos);
        Assert.Contains(c.Reglas, r => r.Contains("100 % de la meta de almuerzo"));
        Assert.Contains("Apto para almuerzo", c.Reglas);
        Assert.Contains("Sin lácteos", c.Reglas);
        Assert.Contains(c.Reglas, r => r.StartsWith("Índice glucémico 40"));
        Assert.Contains(c.Reglas, r => r.StartsWith("Sodio 120 mg por 100 g"));
    }

    [Fact]
    public void Los_parametros_salen_del_json()
    {
        var p = ParametrosFiltro.Predeterminados;
        Assert.Equal(0.10, p.ToleranciaPrincipal);
        Assert.Equal(0.15, p.ToleranciaAmpliada);
        Assert.Equal(3, p.MinimoCandidatos);
        Assert.Equal(70, p.IgAltoDesde);
        Assert.Equal(15.0, p.AzucaresAltoDesdePor100g);
        Assert.Equal(600, p.SodioAltoDesdeMgPor100g);
        Assert.Equal(9, p.PalabrasAlergenos.Count);
    }
}
