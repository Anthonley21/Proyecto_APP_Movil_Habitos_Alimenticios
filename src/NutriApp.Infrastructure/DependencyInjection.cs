using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NutriApp.Infrastructure.Persistencia;

namespace NutriApp.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra la base de datos. La cadena de conexión se lee de "ConnectionStrings:NutriApp"
    /// (en desarrollo: user-secrets; nunca se sube a GitHub).
    /// </summary>
    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration configuracion)
    {
        var cadena = configuracion.GetConnectionString("NutriApp");
        if (string.IsNullOrWhiteSpace(cadena))
            throw new InvalidOperationException(
                "Falta la cadena de conexión 'ConnectionStrings:NutriApp'. Configúrala con: " +
                "dotnet user-secrets set \"ConnectionStrings:NutriApp\" \"Host=...;Database=nutriapp;Username=...;Password=...;SSL Mode=Require\"");

        // Neon suspende la base tras unos minutos sin uso y corta las conexiones abiertas.
        // 1) Las conexiones inactivas se cierran antes (60 s en vez de 300 s), para no reutilizar una ya cortada.
        var b = new NpgsqlConnectionStringBuilder(cadena);
        if (b.ConnectionIdleLifetime == 300)
            b.ConnectionIdleLifetime = 60;

        services.AddDbContext<NutriAppDbContext>(o => o.UseNpgsql(b.ConnectionString, npgsql =>
            // 2) Si aun así una conexión se cae, EF Core reintenta la consulta (hasta 3 veces).
            npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null)));
        return services;
    }
}