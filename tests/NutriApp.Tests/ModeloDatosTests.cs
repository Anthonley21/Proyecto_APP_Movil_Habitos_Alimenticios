using Microsoft.EntityFrameworkCore;
using NutriApp.Infrastructure.Datos;
using NutriApp.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace NutriApp.Tests;

/// <summary>ACT-07: el modelo de EF Core coincide con el modelo de ACT-03. No necesita base de datos.</summary>
public class ModeloDatosTests
{
    private static readonly string[] TablasPropias =
    [
        "GruposAlimento", "Alimentos", "Porciones", "Alergenos", "AlimentoAlergeno", "TiposComida", "AlimentoTipoComida",
        "Platos", "PlatoIngredientes", "PlatoTipoComida",
        "PerfilesUsuario", "PerfilAlergeno", "HistorialPeso", "Recomendaciones", "RegistrosComida",
    ];

    private static NutriAppDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<NutriAppDbContext>()
            .UseNpgsql("Host=localhost;Database=solo_modelo")   // no se conecta: solo construye el modelo
            .Options);

    [Fact]
    public void Tiene_las_15_tablas_propias_del_modelo()
    {
        using var db = CrearContexto();
        var tablas = db.Model.GetEntityTypes().Select(e => e.GetTableName()).ToHashSet();
        foreach (var t in TablasPropias)
            Assert.Contains(t, tablas);
    }

    [Fact]
    public void Tiene_las_23_claves_foraneas_del_modelo()
    {
        using var db = CrearContexto();
        var fks = db.Model.GetEntityTypes()
            .Where(e => TablasPropias.Contains(e.GetTableName()))
            .SelectMany(e => e.GetForeignKeys())
            .Count();
        Assert.Equal(23, fks);
    }

    [Fact]
    public void Tiene_las_17_restricciones_CHECK_del_modelo()
    {
        using var db = CrearContexto();
        // Las restricciones CHECK solo se guardan en el modelo de diseño (el de las migraciones)
        var modelo = db.GetService<IDesignTimeModel>().Model;
        var checks = modelo.GetEntityTypes().SelectMany(e => e.GetCheckConstraints()).Count();
        Assert.Equal(17, checks);
    }

    [Fact]
    public void Cuenta_registros_de_CSV_con_comillas_y_saltos_de_linea()
    {
        var csv = "Id,Nombre\n1,\"Arroz, cocido\"\n2,\"Texto\ncon salto\"\r\n3,Pan\n";
        Assert.Equal(3, CargadorCatalogo.ContarRegistros(csv));
    }
}
