namespace NutriApp.Tests;

/// <summary>
/// Caso de prueba de ACT-08 (preparado desde ACT-05): mujer, 30 años, 70 kg, 160 cm, actividad ligera,
/// objetivo bajar → ≈ 1410 kcal/día. Cuando exista el servicio de cálculo en Domain (ACT-08),
/// este test debe llamar a ese servicio en lugar de repetir la fórmula aquí.
/// </summary>
public class ReglasNutricionalesTests
{
    [Fact]
    public void Perfil_de_ejemplo_da_aprox_1410_kcal()
    {
        double tmb = 10 * 70 + 6.25 * 160 - 5 * 30 - 161;   // Mifflin-St Jeor, mujer
        double meta = tmb * 1.375 - 500;                     // actividad ligera, bajar de peso
        Assert.InRange(meta, 1409, 1411);
    }
}
