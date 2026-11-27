using NutriApp.Domain.Motor;

namespace NutriApp.Tests.Motor;

/// <summary>Un perfil de prueba: datos para el requerimiento (ACT-08) y restricciones para el filtro (ACT-10).</summary>
public sealed record PerfilPrueba(string Nombre, DatosRequerimiento Datos, PerfilFiltro Filtro)
{
    public override string ToString() => Nombre;   // así aparece en el Explorador de pruebas
}

/// <summary>
/// ACT-10 · 20 perfiles variados (sexo, edad, peso, actividad, objetivo, diabetes, hipertensión y 0 a 3 alérgenos).
/// Son los mismos que usará el Hito 3 (ACT-14). Alérgenos: 1 gluten · 2 lácteos · 3 huevo · 4 maní ·
/// 5 frutos secos · 6 pescado · 7 mariscos · 8 soya · 9 ajonjolí.
/// </summary>
public static class PerfilesPrueba
{
    private static PerfilPrueba P(string nombre, string sexo, int edad, double peso, double estatura, double factor,
        string objetivo, bool diabetes = false, bool hipertension = false, params int[] alergenos) =>
        new(nombre, new DatosRequerimiento(sexo, edad, peso, estatura, factor, objetivo),
            new PerfilFiltro(diabetes, hipertension, alergenos.ToHashSet()));

    public static readonly IReadOnlyList<PerfilPrueba> Todos =
    [
        P("01 Plan: F 30, ligera, bajar, sin lácteos", "F", 30, 70, 160, 1.375, "bajar", false, false, 2),
        P("02 M 23, moderada, mantener", "M", 23, 70, 172, 1.55, "mantener"),
        P("03 F 55, sedentaria, bajar, diabetes", "F", 55, 80, 155, 1.2, "bajar", diabetes: true),
        P("04 M 60, sedentario, mantener, hipertensión", "M", 60, 90, 170, 1.2, "mantener", hipertension: true),
        P("05 F 65, diabetes + hipertensión, sin gluten y lácteos", "F", 65, 75, 150, 1.2, "bajar", true, true, 1, 2),
        P("06 M 25, muy activo, subir", "M", 25, 65, 180, 1.9, "subir"),
        P("07 F 80, sedentaria, sin pescado ni mariscos", "F", 80, 45, 150, 1.2, "mantener", false, false, 6, 7),
        P("08 F 18, activa, mantener, sin maní", "F", 18, 55, 162, 1.725, "mantener", false, false, 4),
        P("09 M 45, ligera, bajar, diabetes, sin huevo", "M", 45, 98, 175, 1.375, "bajar", true, false, 3),
        P("10 F 40, moderada, mantener, sin gluten", "F", 40, 62, 158, 1.55, "mantener", false, false, 1),
        P("11 M 35, activo, mantener, sin mariscos", "M", 35, 78, 178, 1.725, "mantener", false, false, 7),
        P("12 F 28, ligera, subir, sin frutos secos ni maní", "F", 28, 48, 165, 1.375, "subir", false, false, 4, 5),
        P("13 M 70, sedentario, bajar, diabetes + hipertensión", "M", 70, 85, 168, 1.2, "bajar", true, true),
        P("14 F 50, ligera, mantener, hipertensión, sin soya", "F", 50, 68, 160, 1.375, "mantener", false, true, 8),
        P("15 M 19, moderada, subir, sin lácteos ni huevo", "M", 19, 60, 175, 1.55, "subir", false, false, 2, 3),
        P("16 F 35, sedentaria, bajar, sin gluten, lácteos y huevo", "F", 35, 90, 165, 1.2, "bajar", false, false, 1, 2, 3),
        P("17 M 52, moderada, bajar, hipertensión, sin pescado", "M", 52, 105, 182, 1.55, "bajar", false, true, 6),
        P("18 F 62, ligera, mantener, diabetes, sin ajonjolí", "F", 62, 66, 155, 1.375, "mantener", true, false, 9),
        P("19 M 30, muy activo, mantener, sin gluten", "M", 30, 82, 185, 1.9, "mantener", false, false, 1),
        P("20 F 45, moderada, bajar, diabetes, sin lácteos", "F", 45, 74, 163, 1.55, "bajar", true, false, 2),
    ];
}
