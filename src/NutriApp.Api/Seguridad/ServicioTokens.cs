using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace NutriApp.Api.Seguridad;

/// <summary>Token entregado al iniciar sesión.</summary>
public sealed record TokenEmitido(string Token, DateTimeOffset ExpiraEn, string Tipo = "Bearer");

/// <summary>Nombres de los claims propios del token.</summary>
public static class ClaimsToken
{
    /// <summary>Id del usuario (Identity). Es lo ÚNICO que identifica a la persona en el token.</summary>
    public const string Sujeto = "sub";

    /// <summary>Sello de seguridad de Identity: si cambia (cerrar sesión, borrar cuenta), el token deja de valer.</summary>
    public const string Sello = "sst";
}

/// <summary>
/// Emite JWT firmados con HS256. El token NO lleva correo, peso ni datos de salud:
/// cualquiera puede leer el contenido de un JWT (solo está firmado, no cifrado).
/// </summary>
public sealed class ServicioTokens(IOptions<OpcionesJwt> opciones, TimeProvider reloj)
{
    private readonly JsonWebTokenHandler _manejador = new();

    public TokenEmitido Crear(IdentityUser usuario)
    {
        var o = opciones.Value;
        var ahora = reloj.GetUtcNow();
        var expira = ahora.AddMinutes(o.MinutosValidez);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = o.Emisor,
            Audience = o.Audiencia,
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = expira.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [ClaimsToken.Sujeto] = usuario.Id,
                [ClaimsToken.Sello] = usuario.SecurityStamp ?? string.Empty,
                ["jti"] = Guid.NewGuid().ToString("N"),
            },
            SigningCredentials = new SigningCredentials(o.ClaveDeFirma(), SecurityAlgorithms.HmacSha256),
        };

        return new TokenEmitido(_manejador.CreateToken(descriptor), expira);
    }
}
