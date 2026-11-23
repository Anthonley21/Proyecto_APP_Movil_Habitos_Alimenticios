namespace NutriApp.Domain.Entidades;

/// <summary>
/// Alérgenos que contiene cada alimento (varios a varios).
/// Tabla: "AlimentoAlergeno" (ACT-03).
/// </summary>
public class AlimentoAlergeno
{
    /// <summary>Alimento</summary>
    public int AlimentoId { get; set; }

    /// <summary>Alérgeno</summary>
    public int AlergenoId { get; set; }

    // Navegación
    public Alimento Alimento { get; set; } = null!;
    public Alergeno Alergeno { get; set; } = null!;
}
