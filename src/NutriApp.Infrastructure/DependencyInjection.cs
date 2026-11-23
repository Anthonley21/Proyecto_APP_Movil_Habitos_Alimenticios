using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddDbContext<NutriAppDbContext>(o => o.UseNpgsql(cadena));
        return services;
    }
}
