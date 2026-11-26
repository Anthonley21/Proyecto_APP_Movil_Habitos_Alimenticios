using System.ComponentModel.DataAnnotations;

namespace NutriApp.Api.Contratos;

// Lo que entra y sale de la API. Las entidades de la base NUNCA se devuelven directamente:
// así no se filtran columnas internas (UsuarioId, hashes, etc.).

// ---------- Autenticación ----------

public sealed record RegistroSolicitud
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [MaxLength(254)]
    public string Correo { get; init; } = string.Empty;

    /// <summary>Mínimo 10 caracteres, con mayúscula, minúscula y número.</summary>
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MaxLength(128, ErrorMessage = "La contraseña no puede pasar de 128 caracteres.")]
    public string Contrasena { get; init; } = string.Empty;

    /// <summary>Consentimiento para tratar datos de salud (LOPDP, datos sensibles). Debe ser true.</summary>
    public bool AceptaTratamientoDatos { get; init; }
}

public sealed record LoginSolicitud
{
    [Required, EmailAddress, MaxLength(254)]
    public string Correo { get; init; } = string.Empty;

    [Required, MaxLength(128)]
    public string Contrasena { get; init; } = string.Empty;
}

public sealed record ConfirmarContrasenaSolicitud
{
    [Required(ErrorMessage = "Confirma con tu contraseña.")]
    [MaxLength(128)]
    public string Contrasena { get; init; } = string.Empty;
}

// ---------- Perfil ----------

public sealed record PerfilSolicitud
{
    /// <summary>'F' o 'M'</summary>
    [Required(ErrorMessage = "El sexo es obligatorio.")]
    [RegularExpression("^[FMfm]$", ErrorMessage = "El sexo debe ser F o M.")]
    public string Sexo { get; init; } = string.Empty;

    /// <summary>Formato yyyy-MM-dd. Edad permitida: 18 a 80 años.</summary>
    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateOnly? FechaNacimiento { get; init; }

    [Required(ErrorMessage = "La estatura es obligatoria.")]
    [Range(100, 250, ErrorMessage = "La estatura debe estar entre 100 y 250 cm.")]
    public decimal? EstaturaCm { get; init; }

    [Required(ErrorMessage = "El peso es obligatorio.")]
    [Range(25, 300, ErrorMessage = "El peso debe estar entre 25 y 300 kg.")]
    public decimal? PesoKg { get; init; }

    /// <summary>sedentario · ligero · moderado · activo · muy_activo</summary>
    [Required(ErrorMessage = "El nivel de actividad es obligatorio.")]
    [MaxLength(20)]
    public string NivelActividad { get; init; } = string.Empty;

    /// <summary>bajar · mantener · subir</summary>
    [Required(ErrorMessage = "El objetivo es obligatorio.")]
    [RegularExpression("^(bajar|mantener|subir)$", ErrorMessage = "El objetivo debe ser bajar, mantener o subir.")]
    public string Objetivo { get; init; } = string.Empty;

    public bool TieneDiabetes { get; init; }
    public bool TieneHipertension { get; init; }

    /// <summary>Ids de la tabla Alergenos (1 gluten … 9 ajonjolí).</summary>
    [MaxLength(9, ErrorMessage = "Hay solo 9 alérgenos.")]
    public int[]? AlergenoIds { get; init; } = [];
}

public sealed record AlergenoRespuesta(int Id, string Nombre);

public sealed record PerfilRespuesta(
    string Sexo,
    DateOnly FechaNacimiento,
    int Edad,
    decimal EstaturaCm,
    decimal PesoKg,
    string NivelActividad,
    decimal FactorActividad,
    string Objetivo,
    bool TieneDiabetes,
    bool TieneHipertension,
    IReadOnlyList<AlergenoRespuesta> Alergenos,
    DateTime ActualizadoEn);

public sealed record PesoRespuesta(DateOnly Fecha, decimal PesoKg);

/// <summary>Todo lo que la app guarda de la persona (derecho de acceso, LOPDP).</summary>
public sealed record MisDatosRespuesta(
    string Correo,
    DateTimeOffset? ConsentimientoDatosSalud,
    PerfilRespuesta? Perfil,
    IReadOnlyList<PesoRespuesta> HistorialPeso);
