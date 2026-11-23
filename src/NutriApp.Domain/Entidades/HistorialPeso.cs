namespace NutriApp.Domain.Entidades;

/// <summary>
/// Pesos registrados en el tiempo (gráfico de progreso).
/// Tabla: "HistorialPeso" (ACT-03).
/// </summary>
public class HistorialPeso
{
    /// <summary>Identificador</summary>
    public int Id { get; set; }

    /// <summary>Perfil</summary>
    public int PerfilId { get; set; }

    /// <summary>Fecha del registro</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Peso [kg]</summary>
    public decimal PesoKg { get; set; }

    // Navegación
    public PerfilUsuario Perfil { get; set; } = null!;
}
