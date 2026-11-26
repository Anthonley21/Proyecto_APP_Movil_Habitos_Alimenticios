namespace NutriApp.Domain.Entidades;

/// <summary>
/// Datos que necesita el motor para calcular el requerimiento y filtrar (1 por usuario).
/// Tabla: "PerfilesUsuario" (ACT-03).
/// </summary>
public class PerfilUsuario
{
    /// <summary>Identificador</summary>
    public int Id { get; set; }

    /// <summary>Usuario de Identity</summary>
    public string UsuarioId { get; set; } = string.Empty;

    /// <summary>'F' o 'M' (Mifflin-St Jeor)</summary>
    public string Sexo { get; set; } = string.Empty;

    /// <summary>Años cumplidos que indicó la persona (18 a 80). ACT-09.1: se pide la edad, no la fecha de nacimiento.</summary>
    public int Edad { get; set; }

    /// <summary>Día en que se registró o cambió <see cref="Edad"/>; con él se suma un año por cada aniversario.</summary>
    public DateOnly EdadRegistradaEn { get; set; }

    /// <summary>Estatura [cm]</summary>
    public decimal EstaturaCm { get; set; }

    /// <summary>Peso actual (el último de HistorialPeso) [kg]</summary>
    public decimal PesoKg { get; set; }

    /// <summary>1,2 sedentario … 1,9 muy activo</summary>
    public decimal FactorActividad { get; set; }

    /// <summary>bajar · mantener · subir</summary>
    public string Objetivo { get; set; } = string.Empty;

    /// <summary>Activa la regla de azúcares e IG</summary>
    public bool TieneDiabetes { get; set; }

    /// <summary>Activa la regla de sodio</summary>
    public bool TieneHipertension { get; set; }

    /// <summary>Última modificación</summary>
    public DateTime ActualizadoEn { get; set; }
}
