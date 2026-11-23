using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NutriApp.Domain.Entidades;

namespace NutriApp.Infrastructure.Persistencia;

/// <summary>
/// Base de datos de NutriApp: las tablas de ASP.NET Core Identity (AspNetUsers, …) más las 15 tablas
/// propias del modelo de ACT-03. Los nombres de tablas y columnas son los del modelo (PascalCase).
/// </summary>
public class NutriAppDbContext(DbContextOptions<NutriAppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<GrupoAlimento> GruposAlimento => Set<GrupoAlimento>();
    public DbSet<Alimento> Alimentos => Set<Alimento>();
    public DbSet<Porcion> Porciones => Set<Porcion>();
    public DbSet<Alergeno> Alergenos => Set<Alergeno>();
    public DbSet<AlimentoAlergeno> AlimentoAlergeno => Set<AlimentoAlergeno>();
    public DbSet<TipoComida> TiposComida => Set<TipoComida>();
    public DbSet<AlimentoTipoComida> AlimentoTipoComida => Set<AlimentoTipoComida>();
    public DbSet<Plato> Platos => Set<Plato>();
    public DbSet<PlatoIngrediente> PlatoIngredientes => Set<PlatoIngrediente>();
    public DbSet<PlatoTipoComida> PlatoTipoComida => Set<PlatoTipoComida>();
    public DbSet<PerfilUsuario> PerfilesUsuario => Set<PerfilUsuario>();
    public DbSet<PerfilAlergeno> PerfilAlergeno => Set<PerfilAlergeno>();
    public DbSet<HistorialPeso> HistorialPeso => Set<HistorialPeso>();
    public DbSet<Recomendacion> Recomendaciones => Set<Recomendacion>();
    public DbSet<RegistroComida> RegistrosComida => Set<RegistroComida>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // tablas de Identity
        ConfiguracionModelo.Aplicar(modelBuilder);
    }
}
