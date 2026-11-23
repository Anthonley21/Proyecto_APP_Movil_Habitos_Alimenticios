namespace NutriApp.Domain.Entidades;

/// <summary>
/// Cada línea de la receta, convertida a gramos y enlazada a un alimento.
/// Tabla: "PlatoIngredientes" (ACT-03).
/// </summary>
public class PlatoIngrediente
{
    /// <summary>Identificador</summary>
    public int Id { get; set; }

    /// <summary>Plato</summary>
    public int PlatoId { get; set; }

    /// <summary>Posición en la receta</summary>
    public int Orden { get; set; }

    /// <summary>Línea original ('2 tazas de arroz')</summary>
    public string Texto { get; set; } = string.Empty;

    /// <summary>Alimento equivalente (vacío: condimento, agua o sin equivalente)</summary>
    public int? AlimentoId { get; set; }

    /// <summary>Gramos comestibles usados en el cálculo [g]</summary>
    public decimal? Gramos { get; set; }

    /// <summary>ok · condimento · agua · sin cantidad · sin equivalente…</summary>
    public string Estado { get; set; } = string.Empty;

    // Navegación
    public Plato Plato { get; set; } = null!;
    public Alimento? Alimento { get; set; }
}
