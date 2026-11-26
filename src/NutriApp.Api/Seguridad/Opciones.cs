using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace NutriApp.Api.Seguridad;

/// <summary>
/// Sección "Jwt" de la configuración. Emisor, audiencia y minutos van en appsettings.json;
/// la Clave va SOLO en user-secrets (desarrollo) o en una variable de entorno (servidor): nunca en el repo.
/// </summary>
public sealed class OpcionesJwt
{
    public const string Seccion = "Jwt";

    /// <summary>Clave de firma HS256. Mínimo 32 bytes (256 bits).</summary>
    [Required(ErrorMessage = "Falta 'Jwt:Clave'. Guárdala en los secretos de usuario de NutriApp.Api (ver docs/ACT-09_Seguridad.md).")]
    [MinLength(32, ErrorMessage = "'Jwt:Clave' debe tener al menos 32 caracteres (256 bits).")]
    public string Clave { get; set; } = string.Empty;

    [Required] public string Emisor { get; set; } = "NutriApp.Api";
    [Required] public string Audiencia { get; set; } = "NutriApp.Mobile";

    /// <summary>Validez del token. Corta a propósito: si se roba, sirve poco tiempo.</summary>
    [Range(5, 240)] public int MinutosValidez { get; set; } = 30;

    public SymmetricSecurityKey ClaveDeFirma() => new(Encoding.UTF8.GetBytes(Clave));
}

/// <summary>Sección "Seguridad" de la configuración.</summary>
public sealed class OpcionesSeguridad
{
    public const string Seccion = "Seguridad";

    /// <summary>Peticiones por minuto y por IP a registro e inicio de sesión (frena fuerza bruta).</summary>
    [Range(1, 10_000)] public int LimitePeticionesPorMinuto { get; set; } = 10;
}
