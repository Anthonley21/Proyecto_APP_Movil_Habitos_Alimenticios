using NutriApp.Domain.Motor;

namespace NutriApp.Tests;

/// <summary>
/// Criterio de ACT-08: mujer, 30 años, 70 kg, 160 cm, actividad ligera, objetivo bajar → ≈ 1410 kcal/día.
/// Ahora llama al servicio de Domain en lugar de repetir la fórmula.
/// </summary>
public class ReglasNutricionalesTests
{
    [Fact]
    public void Perfil_de_ejemplo_da_aprox_1410_kcal()
    {
        var r = CalculadoraRequerimiento.Calcular(new DatosRequerimiento("F", 30, 70, 160, 1.375, "bajar"));
        Assert.Equal(1410, r.KcalDia);
    }
}
