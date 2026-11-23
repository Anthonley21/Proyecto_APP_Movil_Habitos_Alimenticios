namespace NutriApp.Domain.Entidades;

/// <summary>
/// En qué comidas del día es apto cada alimento (varios a varios).
/// Tabla: "AlimentoTipoComida" (ACT-03).
/// </summary>
public class AlimentoTipoComida
{
    /// <summary>Alimento</summary>
    public int AlimentoId { get; set; }

    /// <summary>Tipo de comida</summary>
    public int TipoComidaId { get; set; }

    // Navegación
    public Alimento Alimento { get; set; } = null!;
    public TipoComida TipoComida { get; set; } = null!;
}
