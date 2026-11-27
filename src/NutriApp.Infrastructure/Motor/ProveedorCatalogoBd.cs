using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NutriApp.Domain.Motor;
using NutriApp.Infrastructure.Persistencia;

namespace NutriApp.Infrastructure.Motor;

/// <summary>
/// ACT-10 · Lee el catálogo de la base UNA sola vez y lo guarda en memoria (es pequeño y casi no cambia).
/// Las consultas son lecturas simples de tablas completas: todo el trabajo se hace en C#, así que no hay
/// LINQ que PostgreSQL tenga que traducir (lección de ACT-09).
/// Si se vuelve a ejecutar cargar-datos, hay que reiniciar la API para que lea el catálogo nuevo.
/// </summary>
public sealed class ProveedorCatalogoBd(IServiceScopeFactory fabrica) : IProveedorCatalogo
{
    private readonly SemaphoreSlim _cerrojo = new(1, 1);
    private CatalogoMotor? _catalogo;

    public async Task<CatalogoMotor> ObtenerAsync(CancellationToken ct = default)
    {
        if (_catalogo is not null) return _catalogo;
        await _cerrojo.WaitAsync(ct);
        try
        {
            if (_catalogo is not null) return _catalogo;   // otro hilo ya lo cargó mientras esperábamos

            using var alcance = fabrica.CreateScope();
            var db = alcance.ServiceProvider.GetRequiredService<NutriAppDbContext>();

            var alimentos = await db.Alimentos.AsNoTracking().ToListAsync(ct);
            var porciones = await db.Porciones.AsNoTracking().ToListAsync(ct);
            var alimentoAlergenos = await db.AlimentoAlergeno.AsNoTracking().ToListAsync(ct);
            var alimentoComidas = await db.AlimentoTipoComida.AsNoTracking().ToListAsync(ct);
            var platos = await db.Platos.AsNoTracking().ToListAsync(ct);
            var ingredientes = await db.PlatoIngredientes.AsNoTracking().ToListAsync(ct);
            var platoComidas = await db.PlatoTipoComida.AsNoTracking().ToListAsync(ct);
            var alergenos = await db.Alergenos.AsNoTracking().ToListAsync(ct);

            _catalogo = CatalogoMotor.Construir(alimentos, porciones, alimentoAlergenos, alimentoComidas,
                platos, ingredientes, platoComidas, alergenos);
            return _catalogo;
        }
        finally
        {
            _cerrojo.Release();
        }
    }
}
