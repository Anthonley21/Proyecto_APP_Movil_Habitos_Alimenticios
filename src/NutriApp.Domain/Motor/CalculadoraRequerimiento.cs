using NutriApp.Domain.Entidades;

namespace NutriApp.Domain.Motor;

/// <summary>Datos de la persona que necesita el cálculo. Se puede armar a mano o desde un PerfilUsuario.</summary>
/// <param name="Sexo">'F' o 'M'</param>
/// <param name="Edad">Años cumplidos</param>
/// <param name="PesoKg">Peso actual [kg]</param>
/// <param name="EstaturaCm">Estatura [cm]</param>
/// <param name="FactorActividad">1,2 sedentario … 1,9 muy activo</param>
/// <param name="Objetivo">bajar · mantener · subir</param>
public sealed record DatosRequerimiento(
    string Sexo, int Edad, double PesoKg, double EstaturaCm, double FactorActividad, string Objetivo)
{
    /// <summary>Arma los datos desde el perfil guardado, con la edad actualizada a la fecha indicada.</summary>
    public static DatosRequerimiento DesdePerfil(PerfilUsuario perfil, DateOnly hoy) => new(
        perfil.Sexo,
        CalculadoraRequerimiento.EdadActual(perfil.Edad, perfil.EdadRegistradaEn, hoy),
        (double)perfil.PesoKg,
        (double)perfil.EstaturaCm,
        (double)perfil.FactorActividad,
        perfil.Objetivo);
}

/// <summary>Gramos de cada macronutriente.</summary>
public sealed record Macronutrientes(double ProteinaG, double CarbohidratosG, double GrasaG);

/// <summary>Energía y macronutrientes objetivo de una comida.</summary>
public sealed record RequerimientoComida(
    int TipoComidaId, string Nombre, double Porcentaje, double Kcal, Macronutrientes Macros);

/// <summary>Resultado del cálculo (ACT-08). Kcal del día redondeadas al entero; comidas y gramos a 1 decimal.</summary>
/// <param name="TmbKcal">Tasa metabólica basal (Mifflin-St Jeor)</param>
/// <param name="GastoTotalKcal">TMB × factor de actividad</param>
/// <param name="AjusteObjetivoKcal">−500 / 0 / +300</param>
/// <param name="KcalDia">Objetivo diario, ya con el mínimo aplicado</param>
/// <param name="SeAplicoMinimo">true si el resultado quedó por debajo de 1200 (F) / 1500 (M) y se subió a ese mínimo</param>
/// <param name="MacrosDia">Gramos diarios con el reparto 50/20/30</param>
/// <param name="AzucaresLibresMaxG">Tope de azúcares libres (10 % de la energía, OMS)</param>
/// <param name="Comidas">Desayuno, almuerzo, merienda y cena</param>
public sealed record Requerimiento(
    double TmbKcal,
    double GastoTotalKcal,
    double AjusteObjetivoKcal,
    double KcalDia,
    bool SeAplicoMinimo,
    Macronutrientes MacrosDia,
    double AzucaresLibresMaxG,
    IReadOnlyList<RequerimientoComida> Comidas);

/// <summary>
/// ACT-08 · Cálculo del requerimiento diario: TMB → gasto total → ajuste por objetivo → mínimo seguro →
/// macronutrientes → reparto por comida. Todos los valores salen de parametros_motor.json (ACT-05).
/// </summary>
public static class CalculadoraRequerimiento
{
    private const double MinPesoKg = 25, MaxPesoKg = 300;          // CHECK de PerfilesUsuario (ACT-03)
    private const double MinEstaturaCm = 100, MaxEstaturaCm = 250;

    public static Requerimiento Calcular(PerfilUsuario perfil, DateOnly hoy, ParametrosMotor? parametros = null) =>
        Calcular(DatosRequerimiento.DesdePerfil(perfil, hoy), parametros);

    public static Requerimiento Calcular(DatosRequerimiento d, ParametrosMotor? parametros = null)
    {
        var p = parametros ?? ParametrosMotor.Predeterminados;
        var sexo = Validar(d, p);
        var objetivo = d.Objetivo.Trim().ToLowerInvariant();

        // 1. Tasa metabólica basal (Mifflin-St Jeor, 1990)
        double tmb = p.CoefPesoKg * d.PesoKg
                   + p.CoefEstaturaCm * d.EstaturaCm
                   + p.CoefEdadAnios * d.Edad
                   + (sexo == 'M' ? p.ConstanteMasculino : p.ConstanteFemenino);

        // 2. Gasto total según la actividad
        double gasto = tmb * d.FactorActividad;

        // 3. Ajuste por objetivo y 4. mínimo seguro
        double ajuste = p.AjusteObjetivoKcal[objetivo];
        double minimo = sexo == 'M' ? p.MinimoKcalMasculino : p.MinimoKcalFemenino;
        double conAjuste = gasto + ajuste;
        bool seAplicoMinimo = conAjuste < minimo;
        double kcalDia = Redondear(seAplicoMinimo ? minimo : conAjuste, 0);

        // 5. Macronutrientes del día y 6. reparto por comida (sobre las kcal ya redondeadas, para que sumen)
        var comidas = p.Comidas
            .Select(c =>
            {
                double kcal = kcalDia * c.Porcentaje;
                return new RequerimientoComida(c.TipoComidaId, c.Nombre, c.Porcentaje, Redondear(kcal, 1), Macros(kcal, p));
            })
            .ToList();

        return new Requerimiento(
            TmbKcal: Redondear(tmb, 0),
            GastoTotalKcal: Redondear(gasto, 0),
            AjusteObjetivoKcal: ajuste,
            KcalDia: kcalDia,
            SeAplicoMinimo: seAplicoMinimo,
            MacrosDia: Macros(kcalDia, p),
            AzucaresLibresMaxG: Redondear(kcalDia * p.FraccionMaxAzucaresLibres / p.KcalPorGramoCarbohidratos, 1),
            Comidas: comidas);
    }

    /// <summary>
    /// ACT-09.1 · Edad a la fecha <paramref name="hoy"/>: la edad que indicó la persona más un año por cada
    /// aniversario completo del día en que la indicó. Un registro hecho un 29 de febrero cumple el 1 de marzo
    /// en años no bisiestos. Si <paramref name="hoy"/> es anterior al registro, se devuelve la edad tal cual.
    /// </summary>
    public static int EdadActual(int edadRegistrada, DateOnly registradaEn, DateOnly hoy)
    {
        if (hoy <= registradaEn)
            return edadRegistrada;
        int anios = hoy.Year - registradaEn.Year;
        if (hoy.Month < registradaEn.Month || (hoy.Month == registradaEn.Month && hoy.Day < registradaEn.Day))
            anios--;
        return edadRegistrada + anios;
    }

    private static Macronutrientes Macros(double kcal, ParametrosMotor p) => new(
        ProteinaG: Redondear(kcal * p.FraccionProteina / p.KcalPorGramoProteina, 1),
        CarbohidratosG: Redondear(kcal * p.FraccionCarbohidratos / p.KcalPorGramoCarbohidratos, 1),
        GrasaG: Redondear(kcal * p.FraccionGrasa / p.KcalPorGramoGrasa, 1));

    // Redondeo "escolar" (0,5 sube), no el bancario por defecto de .NET.
    private static double Redondear(double x, int decimales) => Math.Round(x, decimales, MidpointRounding.AwayFromZero);

    private static char Validar(DatosRequerimiento d, ParametrosMotor p)
    {
        ArgumentNullException.ThrowIfNull(d);
        var sexo = (d.Sexo ?? "").Trim().ToUpperInvariant();
        if (sexo is not ("F" or "M"))
            throw new ArgumentException($"Sexo inválido: '{d.Sexo}'. Use 'F' o 'M'.", nameof(d.Sexo));
        if (d.Edad < p.EdadMin || d.Edad > p.EdadMax)
            throw new ArgumentOutOfRangeException(nameof(d.Edad), d.Edad,
                $"La edad debe estar entre {p.EdadMin} y {p.EdadMax} años (alcance de ACT-05).");
        if (!(d.PesoKg >= MinPesoKg && d.PesoKg <= MaxPesoKg))
            throw new ArgumentOutOfRangeException(nameof(d.PesoKg), d.PesoKg, $"El peso debe estar entre {MinPesoKg} y {MaxPesoKg} kg.");
        if (!(d.EstaturaCm >= MinEstaturaCm && d.EstaturaCm <= MaxEstaturaCm))
            throw new ArgumentOutOfRangeException(nameof(d.EstaturaCm), d.EstaturaCm,
                $"La estatura debe estar entre {MinEstaturaCm} y {MaxEstaturaCm} cm.");
        if (!(d.FactorActividad >= p.FactorMinimo && d.FactorActividad <= p.FactorMaximo))
            throw new ArgumentOutOfRangeException(nameof(d.FactorActividad), d.FactorActividad,
                $"El factor de actividad debe estar entre {p.FactorMinimo} y {p.FactorMaximo}.");
        if (d.Objetivo is null || !p.AjusteObjetivoKcal.ContainsKey(d.Objetivo.Trim()))
            throw new ArgumentException(
                $"Objetivo inválido: '{d.Objetivo}'. Use {string.Join(", ", p.AjusteObjetivoKcal.Keys)}.", nameof(d.Objetivo));
        return sexo[0];
    }
}
