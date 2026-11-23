namespace NutriApp.Domain.Entidades;

/// <summary>
/// Alternativas que generó el motor por comida, con su explicación (botón «¿Por qué?»).
/// Tabla: "Recomendaciones" (ACT-03).
/// </summary>
public class Recomendacion
{
    /// <summary>Identificador</summary>
    public int Id { get; set; }

    /// <summary>Perfil</summary>
    public int PerfilId { get; set; }

    /// <summary>Día del menú</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Comida del día</summary>
    public int TipoComidaId { get; set; }

    /// <summary>Alimento recomendado (o vacío si es plato)</summary>
    public int? AlimentoId { get; set; }

    /// <summary>Plato recomendado (o vacío si es alimento)</summary>
    public int? PlatoId { get; set; }

    /// <summary>1 a 5 según la similitud</summary>
    public int Posicion { get; set; }

    /// <summary>Similitud coseno con el objetivo de la comida</summary>
    public decimal Similitud { get; set; }

    /// <summary>Cantidad sugerida [g]</summary>
    public decimal Gramos { get; set; }

    /// <summary>Energía de la cantidad sugerida [kcal]</summary>
    public decimal Kcal { get; set; }

    /// <summary>Texto de la explicación (plantilla)</summary>
    public string Explicacion { get; set; } = string.Empty;

    /// <summary>El usuario pulsó «Elegir»</summary>
    public bool Elegida { get; set; }

    /// <summary>Momento de la generación</summary>
    public DateTime CreadoEn { get; set; }

    // Navegación
    public PerfilUsuario Perfil { get; set; } = null!;
    public TipoComida TipoComida { get; set; } = null!;
    public Alimento? Alimento { get; set; }
    public Plato? Plato { get; set; }
}
