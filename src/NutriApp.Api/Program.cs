using Microsoft.EntityFrameworkCore;
using NutriApp.Api.Seguridad;
using NutriApp.Infrastructure;
using NutriApp.Infrastructure.Datos;
using NutriApp.Infrastructure.Persistencia;

// ACT-07 · Carga del catálogo (repetible):
//   dotnet run --project src/NutriApp.Api -- cargar-datos "$PWD\datos\bd"
//   dotnet run --project src/NutriApp.Api -- cargar-datos "$PWD\datos\bd" --forzar   (si ya hay datos de usuarios)
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
builder.WebHost.ConfigureKestrel(k => k.AddServerHeader = false);   // no anunciar el servidor

builder.Services.AddControllers();
builder.Services.AddProblemDetails();                                // errores en formato estándar, sin trazas
builder.Services.AddInfraestructura(builder.Configuration);          // ACT-07: base de datos
builder.Services.AddSeguridad(builder.Configuration);                // ACT-09: Identity, JWT, límites
builder.Services.AddDocumentacionApi();                              // Swagger con botón Authorize

var app = builder.Build();

app.UseExceptionHandler();      // un error inesperado devuelve 500 genérico, nunca el detalle interno
app.UseStatusCodePages();       // 401/403/404/429 también en formato ProblemDetails

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o => o.DocumentTitle = "NutriApp API");   // http://localhost:5222/swagger
}

if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCabecerasSeguridad();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Comprobación de que la API llega a la base (pública: solo devuelve conteos del catálogo)
app.MapGet("/api/salud/bd", async (NutriAppDbContext db) => Results.Ok(new
{
    alimentos = await db.Alimentos.CountAsync(),
    aptosMotor = await db.Alimentos.CountAsync(a => a.AptoMotor),
    platos = await db.Platos.CountAsync(),
    porciones = await db.Porciones.CountAsync(),
    alergenosAsignados = await db.AlimentoAlergeno.CountAsync(),
})).AllowAnonymous();

app.Run();
return 0;

/// <summary>Visible para las pruebas de integración (WebApplicationFactory).</summary>
public partial class Program;
