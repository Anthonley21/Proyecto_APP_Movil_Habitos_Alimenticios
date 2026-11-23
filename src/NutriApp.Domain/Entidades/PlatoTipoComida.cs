namespace NutriApp.Domain.Entidades;

/// <summary>
/// En qué comidas del día es apto cada plato (varios a varios).
/// Tabla: "PlatoTipoComida" (ACT-03).
/// </summary>
public class PlatoTipoComida
{
    /// <summary>Plato</summary>
    public int PlatoId { get; set; }

    /// <summary>Tipo de comida</summary>
    public int TipoComidaId { get; set; }

    // Navegación
    public Plato Plato { get; set; } = null!;
    public TipoComida TipoComida { get; set; } = null!;
}
