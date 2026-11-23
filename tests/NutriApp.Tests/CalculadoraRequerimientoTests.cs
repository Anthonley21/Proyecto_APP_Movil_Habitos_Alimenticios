using NutriApp.Domain.Entidades;
using NutriApp.Domain.Motor;

namespace NutriApp.Tests;

/// <summary>ACT-08: cálculo del requerimiento. Los valores esperados están calculados a mano con las fórmulas de ACT-05.</summary>
public class CalculadoraRequerimientoTests
{
    private static Requerimiento Calc(string sexo, int edad, double peso, double estatura, double factor, string objetivo) =>
        CalculadoraRequerimiento.Calcular(new DatosRequerimiento(sexo, edad, peso, estatura, factor, objetivo));

    // ---------- Caso del plan, paso a paso ----------

    [Fact]
    public void Caso_del_plan_detalla_cada_paso()
    {
        var r = Calc("F", 30, 70, 160, 1.375, "bajar");

        Assert.Equal(1389, r.TmbKcal);            // 700 + 1000 − 150 − 161
        Assert.Equal(1910, r.GastoTotalKcal);     // 1389 × 1,375 = 1909,875
        Assert.Equal(-500, r.AjusteObjetivoKcal);
        Assert.Equal(1410, r.KcalDia);            // 1409,875
        Assert.False(r.SeAplicoMinimo);

        Assert.Equal(70.5, r.MacrosDia.ProteinaG);         // 1410 × 20 % / 4
        Assert.Equal(176.3, r.MacrosDia.CarbohidratosG);   // 1410 × 50 % / 4 = 176,25
        Assert.Equal(47.0, r.MacrosDia.GrasaG);            // 1410 × 30 % / 9
        Assert.Equal(35.3, r.AzucaresLibresMaxG);          // 1410 × 10 % / 4 = 35,25
    }

    [Fact]
    public void Caso_del_plan_reparte_las_cuatro_comidas()
    {
        var r = Calc("F", 30, 70, 160, 1.375, "bajar");

        Assert.Collection(r.Comidas,
            c => { Assert.Equal((1, "desayuno"), (c.TipoComidaId, c.Nombre)); Assert.Equal(352.5, c.Kcal); },
            c => { Assert.Equal((2, "almuerzo"), (c.TipoComidaId, c.Nombre)); Assert.Equal(493.5, c.Kcal); },
            c => { Assert.Equal((3, "merienda"), (c.TipoComidaId, c.Nombre)); Assert.Equal(141.0, c.Kcal); },
            c => { Assert.Equal((4, "cena"), (c.TipoComidaId, c.Nombre)); Assert.Equal(423.0, c.Kcal); });

        var almuerzo = r.Comidas[1].Macros;                 // 493,5 kcal
        Assert.Equal(24.7, almuerzo.ProteinaG);             // 24,675
        Assert.Equal(61.7, almuerzo.CarbohidratosG);        // 61,6875
        Assert.Equal(16.5, almuerzo.GrasaG);                // 16,45
    }

    // ---------- Otros perfiles calculados a mano ----------

    [Theory]
    //          sexo edad peso estat factor   objetivo     TMB   gasto  kcal/día
    [InlineData("M", 25, 80.0, 180.0, 1.55, "mantener", 1805.0, 2798.0, 2798.0)]   // 1805 × 1,55 = 2797,75
    [InlineData("M", 40, 90.0, 176.0, 1.2, "subir", 1805.0, 2166.0, 2466.0)]   // 2166 + 300
    [InlineData("F", 22, 55.0, 165.0, 1.725, "subir", 1310.0, 2260.0, 2560.0)]   // 1310,25 × 1,725 = 2260,18
    [InlineData("F", 45, 85.0, 158.0, 1.9, "bajar", 1452.0, 2758.0, 2258.0)]   // 1451,5 × 1,9 = 2757,85
    [InlineData("M", 60, 100.0, 170.0, 1.375, "bajar", 1768.0, 2430.0, 1930.0)]   // 1767,5 × 1,375 = 2430,31
    public void Perfiles_variados(string sexo, int edad, double peso, double estatura, double factor, string objetivo,
                                  double tmb, double gasto, double kcalDia)
    {
        var r = Calc(sexo, edad, peso, estatura, factor, objetivo);
        Assert.Equal(tmb, r.TmbKcal);
        Assert.Equal(gasto, r.GastoTotalKcal);
        Assert.Equal(kcalDia, r.KcalDia);
    }

    // ---------- Mínimo seguro ----------

    [Fact]
    public void Mujer_bajo_el_minimo_sube_a_1200()
    {
        var r = Calc("F", 70, 45, 150, 1.2, "bajar");      // 876,5 × 1,2 − 500 = 551,8
        Assert.True(r.SeAplicoMinimo);
        Assert.Equal(1200, r.KcalDia);
    }

    [Fact]
    public void Hombre_bajo_el_minimo_sube_a_1500()
    {
        var r = Calc("M", 75, 50, 155, 1.2, "bajar");      // 1098,75 × 1,2 − 500 = 818,5
        Assert.True(r.SeAplicoMinimo);
        Assert.Equal(1500, r.KcalDia);
    }

    // ---------- Propiedades que deben cumplirse siempre ----------

    public static TheoryData<string, int, double, double, double, string> Perfiles => new()
    {
        { "F", 30, 70, 160, 1.375, "bajar" },
        { "M", 18, 25, 100, 1.2, "bajar" },       // valores mínimos permitidos
        { "F", 80, 300, 250, 1.9, "subir" },      // valores máximos permitidos
        { "M", 35, 72.4, 171.5, 1.55, "mantener" },
        { "F", 50, 62, 155, 1.6, "mantener" },    // factor intermedio (no está en la lista, pero está en el rango)
    };

    [Theory, MemberData(nameof(Perfiles))]
    public void Las_comidas_suman_el_total_del_dia(string s, int e, double p, double est, double f, string o)
    {
        var r = Calc(s, e, p, est, f, o);
        Assert.InRange(r.Comidas.Sum(c => c.Kcal), r.KcalDia - 0.2, r.KcalDia + 0.2);   // redondeo a 1 decimal
        Assert.Equal(4, r.Comidas.Count);
    }

    [Theory, MemberData(nameof(Perfiles))]
    public void Los_macros_dan_las_kcal_del_dia(string s, int e, double p, double est, double f, string o)
    {
        var r = Calc(s, e, p, est, f, o);
        var m = r.MacrosDia;
        double kcal = 4 * m.ProteinaG + 4 * m.CarbohidratosG + 9 * m.GrasaG;
        Assert.InRange(kcal, r.KcalDia - 2, r.KcalDia + 2);   // solo diferencias de redondeo
    }

    [Theory, MemberData(nameof(Perfiles))]
    public void Nunca_baja_del_minimo(string s, int e, double p, double est, double f, string o)
    {
        var r = Calc(s, e, p, est, f, o);
        Assert.True(r.KcalDia >= (s == "M" ? 1500 : 1200));
    }

    [Fact]
    public void Subir_da_mas_que_mantener_y_mantener_mas_que_bajar()
    {
        double bajar = Calc("M", 30, 75, 175, 1.55, "bajar").KcalDia;
        double mantener = Calc("M", 30, 75, 175, 1.55, "mantener").KcalDia;
        double subir = Calc("M", 30, 75, 175, 1.55, "subir").KcalDia;
        Assert.True(bajar < mantener && mantener < subir);
        Assert.Equal(500, mantener - bajar);
        Assert.Equal(300, subir - mantener);
    }

    [Fact]
    public void Acepta_minusculas_y_espacios_en_sexo_y_objetivo()
    {
        var a = Calc("F", 30, 70, 160, 1.375, "bajar");
        var b = Calc(" f ", 30, 70, 160, 1.375, " BAJAR ");
        Assert.Equal(a.KcalDia, b.KcalDia);
    }

    // ---------- Datos inválidos ----------

    [Theory]
    [InlineData("X", 30, 70.0, 160.0, 1.375, "bajar")]     // sexo
    [InlineData("", 30, 70.0, 160.0, 1.375, "bajar")]
    [InlineData("F", 17, 70.0, 160.0, 1.375, "bajar")]     // menor de edad (fuera del alcance)
    [InlineData("F", 81, 70.0, 160.0, 1.375, "bajar")]
    [InlineData("F", 30, 24.9, 160.0, 1.375, "bajar")]   // peso
    [InlineData("F", 30, 301.0, 160.0, 1.375, "bajar")]
    [InlineData("F", 30, 70.0, 99.0, 1.375, "bajar")]      // estatura
    [InlineData("F", 30, 70.0, 251.0, 1.375, "bajar")]
    [InlineData("F", 30, 70.0, 160.0, 1.1, "bajar")]       // factor
    [InlineData("F", 30, 70.0, 160.0, 2.0, "bajar")]
    [InlineData("F", 30, 70.0, 160.0, 1.375, "adelgazar")] // objetivo
    [InlineData("F", 30, double.NaN, 160.0, 1.375, "bajar")]
    public void Rechaza_datos_fuera_de_rango(string s, int e, double p, double est, double f, string o)
    {
        Assert.ThrowsAny<ArgumentException>(() => Calc(s, e, p, est, f, o));
    }

    // ---------- Edad y perfil guardado ----------

    [Theory]
    [InlineData("1996-11-23", "2026-11-22", 29)]   // un día antes del cumpleaños
    [InlineData("1996-11-23", "2026-11-23", 30)]   // el día del cumpleaños
    [InlineData("2000-02-29", "2026-02-28", 25)]   // nacido un 29 de febrero
    [InlineData("2000-02-29", "2026-03-01", 26)]
    [InlineData("2000-02-29", "2028-02-29", 28)]
    public void Calcula_la_edad_cumplida(string nacimiento, string hoy, int esperada)
    {
        Assert.Equal(esperada, CalculadoraRequerimiento.CalcularEdad(Fecha(nacimiento), Fecha(hoy)));
    }

    [Fact]
    public void Rechaza_fecha_de_nacimiento_futura()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CalculadoraRequerimiento.CalcularEdad(new DateOnly(2030, 1, 1), new DateOnly(2026, 11, 22)));
    }

    [Fact]
    public void Calcula_desde_un_PerfilUsuario_guardado()
    {
        var perfil = new PerfilUsuario
        {
            Sexo = "F",
            FechaNacimiento = new DateOnly(1996, 5, 10),   // 30 años al 22/11/2026
            PesoKg = 70m,
            EstaturaCm = 160m,
            FactorActividad = 1.375m,
            Objetivo = "bajar",
        };
        var r = CalculadoraRequerimiento.Calcular(perfil, new DateOnly(2026, 11, 22));
        Assert.Equal(1410, r.KcalDia);
    }

    // ---------- Parámetros ----------

    [Fact]
    public void Los_parametros_incrustados_coinciden_con_ACT05()
    {
        var p = ParametrosMotor.Predeterminados;
        Assert.Equal(new[] { 1.2, 1.375, 1.55, 1.725, 1.9 }, p.NivelesActividad.Select(n => n.Factor));
        Assert.Equal((-500.0, 0.0, 300.0), (p.AjusteObjetivoKcal["bajar"], p.AjusteObjetivoKcal["mantener"], p.AjusteObjetivoKcal["subir"]));
        Assert.Equal((0.50, 0.20, 0.30), (p.FraccionCarbohidratos, p.FraccionProteina, p.FraccionGrasa));
        Assert.Equal(1.375, p.FactorDe("ligero"));
    }

    [Fact]
    public void El_reparto_coincide_con_TiposComida_csv()
    {
        // La tabla TiposComida (ACT-06/07) y el JSON (ACT-05) deben decir lo mismo
        var csv = File.ReadAllLines(RutaRepositorio("datos", "bd", "TiposComida.csv")).Skip(1)
            .Select(l => l.Split(','))
            .Select(c => (Id: int.Parse(c[0]), Nombre: c[1], Pct: double.Parse(c[2], System.Globalization.CultureInfo.InvariantCulture) / 100))
            .OrderBy(x => x.Id).ToList();
        var json = ParametrosMotor.Predeterminados.Comidas.Select(c => (Id: c.TipoComidaId, c.Nombre, Pct: c.Porcentaje)).ToList();
        Assert.Equal(csv, json);
    }

    [Fact]
    public void Rechaza_parametros_que_no_suman_100()
    {
        var json = LeerJsonIncrustado().Replace("\"proteina\": 0.20", "\"proteina\": 0.25");
        Assert.Throws<InvalidOperationException>(() => ParametrosMotor.DesdeJson(json));
    }

    private static DateOnly Fecha(string iso) =>
        DateOnly.ParseExact(iso, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private static string LeerJsonIncrustado()
    {
        using var s = typeof(ParametrosMotor).Assembly.GetManifestResourceStream("NutriApp.Domain.parametros_motor.json")!;
        return new StreamReader(s).ReadToEnd();
    }

    private static string RutaRepositorio(params string[] partes)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "NutriApp.slnx")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine([dir!.FullName, .. partes]);
    }
}
