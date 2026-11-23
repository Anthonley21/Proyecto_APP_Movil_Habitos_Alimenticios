namespace NutriApp.Domain.Entidades;

/// <summary>
/// Porciones típicas de cada alimento (ACT-06). Una se marca como predeterminada.
/// Tabla: "Porciones" (ACT-03).
/// </summary>
public class Porcion
{
    /// <summary>Identificador</summary>
    public int Id { get; set; }

    /// <summary>Alimento</summary>
    public int AlimentoId { get; set; }

    /// <summary>Medida casera: '1 taza', '1 unidad mediana'</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Peso comestible de la porción [g]</summary>
    public decimal Gramos { get; set; }

    /// <summary>Porción que usa el motor por defecto</summary>
    public bool EsPredeterminada { get; set; }

    /// <summary>De dónde sale la medida</summary>
    public string? Fuente { get; set; }

    // Navegación
    public Alimento Alimento { get; set; } = null!;
}
