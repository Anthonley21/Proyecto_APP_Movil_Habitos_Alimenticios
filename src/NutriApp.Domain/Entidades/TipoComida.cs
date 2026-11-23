namespace NutriApp.Domain.Entidades;

/// <summary>
/// Comidas del día y su parte de la energía diaria (ACT-05).
/// Tabla: "TiposComida" (ACT-03).
/// </summary>
public class TipoComida
{
    /// <summary>Identificador</summary>
    public int Id { get; set; }

    /// <summary>desayuno, almuerzo, merienda, cena</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>25 / 35 / 10 / 30 [%]</summary>
    public decimal PorcentajeEnergia { get; set; }

    /// <summary>Orden en el día</summary>
    public int Orden { get; set; }
}
