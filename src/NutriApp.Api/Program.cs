using Microsoft.EntityFrameworkCore;
using NutriApp.Infrastructure;
using NutriApp.Infrastructure.Datos;
using NutriApp.Infrastructure.Persistencia;

// ACT-07 · Carga del catálogo (repetible):
//   dotnet run --project src/NutriApp.Api -- cargar-datos datos/bd
//   dotnet run --project src/NutriApp.Api -- cargar-datos datos/bd --forzar   (si ya hay datos de usuarios)
if (args.Length > 0 && args[0] == "cargar-datos")
{
    var configuracion = WebApplication.CreateBuilder().Configuration;   // appsettings + user-secrets + variables de entorno
    var cadena = configuracion.GetConnectionString("NutriApp");
    if (string.IsNullOrWhiteSpace(cadena))
    {
        Console.WriteLine("Falta 'ConnectionStrings:NutriApp'. Ver README de ACT-07 (dotnet user-secrets set ...).");
        return 1;
    }
    var carpeta = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : Path.Combine("datos", "bd");
    return await CargadorCatalogo.CargarAsync(cadena, carpeta, args.Contains("--forzar"), Console.Out);
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddInfraestructura(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Comprobación rápida de que la API llega a la base y el catálogo está cargado
app.MapGet("/api/salud/bd", async (NutriAppDbContext db) => Results.Ok(new
{
    alimentos = await db.Alimentos.CountAsync(),
    aptosMotor = await db.Alimentos.CountAsync(a => a.AptoMotor),
    platos = await db.Platos.CountAsync(),
    porciones = await db.Porciones.CountAsync(),
    alergenosAsignados = await db.AlimentoAlergeno.CountAsync(),
}));

app.Run();
return 0;
