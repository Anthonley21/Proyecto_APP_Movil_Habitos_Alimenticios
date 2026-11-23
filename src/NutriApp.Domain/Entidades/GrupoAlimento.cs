namespace NutriApp.Domain.Entidades;

/// <summary>
/// Grupos de alimentos según la estructura del MSP Ecuador (11 grupos).
/// Tabla: "GruposAlimento" (ACT-03).
/// </summary>
public class GrupoAlimento
{
    /// <summary>Código del grupo: CER, FRU, VEG, LEG, CAR, PES, LAC, GRA, AZU, SNA, BEB</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Nombre del grupo (p. ej. 'Cereales, tubérculos y plátanos')</summary>
    public string Nombre { get; set; } = string.Empty;
}
