namespace NutriApp.Domain.Entidades;

/// <summary>
/// Alérgenos que el usuario debe evitar (varios a varios).
/// Tabla: "PerfilAlergeno" (ACT-03).
/// </summary>
public class PerfilAlergeno
{
    /// <summary>Perfil</summary>
    public int PerfilId { get; set; }

    /// <summary>Alérgeno</summary>
    public int AlergenoId { get; set; }

    // Navegación
    public PerfilUsuario Perfil { get; set; } = null!;
    public Alergeno Alergeno { get; set; } = null!;
}
