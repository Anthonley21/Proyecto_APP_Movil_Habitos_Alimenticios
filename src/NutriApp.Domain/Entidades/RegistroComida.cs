namespace NutriApp.Domain.Entidades;

/// <summary>
/// Lo que el usuario comió. Guarda copia de kcal y macronutrientes del momento.
/// Tabla: "RegistrosComida" (ACT-03).
/// </summary>
public class RegistroComida
{
    /// <summary>Identificador</summary>
    public int Id { get; set; }

    /// <summary>Perfil</summary>
    public int PerfilId { get; set; }

    /// <summary>Día consumido</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Comida del día</summary>
    public int TipoComidaId { get; set; }

    /// <summary>Alimento consumido (o vacío si es plato)</summary>
    public int? AlimentoId { get; set; }

    /// <summary>Plato consumido (o vacío si es alimento)</summary>
    public int? PlatoId { get; set; }

    /// <summary>Recomendación elegida, si vino del motor</summary>
    public int? RecomendacionId { get; set; }

    /// <summary>Cantidad consumida [g]</summary>
    public decimal Gramos { get; set; }

    /// <summary>Energía (copia al registrar) [kcal]</summary>
    public decimal Kcal { get; set; }

    /// <summary>Proteína (copia) [g]</summary>
    public decimal ProteinaG { get; set; }

    /// <summary>Carbohidratos (copia) [g]</summary>
    public decimal CarbohidratosG { get; set; }

    /// <summary>Grasa (copia) [g]</summary>
    public decimal GrasaG { get; set; }

    /// <summary>Momento del registro</summary>
    public DateTime CreadoEn { get; set; }

    // Navegación
    public PerfilUsuario Perfil { get; set; } = null!;
    public TipoComida TipoComida { get; set; } = null!;
    public Alimento? Alimento { get; set; }
    public Plato? Plato { get; set; }
    public Recomendacion? Recomendacion { get; set; }
}
