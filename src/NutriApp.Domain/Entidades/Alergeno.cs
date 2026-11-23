namespace NutriApp.Domain.Entidades;

/// <summary>
/// Lista cerrada de alérgenos principales.
/// Tabla: "Alergenos" (ACT-03).
/// </summary>
public class Alergeno
{
    /// <summary>Identificador</summary>
    public int Id { get; set; }

    /// <summary>gluten, lácteos, huevo, maní, frutos secos, pescado, mariscos, soya, ajonjolí</summary>
    public string Nombre { get; set; } = string.Empty;
}
