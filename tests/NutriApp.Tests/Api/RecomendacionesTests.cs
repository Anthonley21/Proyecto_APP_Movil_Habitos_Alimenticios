using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NutriApp.Domain.Motor;
using NutriApp.Tests.Motor;

namespace NutriApp.Tests.Api;

/// <summary>La API de pruebas, pero con el catálogo REAL leído de los CSV (en vez de la base en memoria vacía).</summary>
public sealed class FabricaApiConCatalogo : FabricaApi
{
    private sealed class CatalogoFijo(CatalogoMotor c) : IProveedorCatalogo
    {
        public Task<CatalogoMotor> ObtenerAsync(CancellationToken ct = default) => Task.FromResult(c);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(s =>
        {
            s.RemoveAll<IProveedorCatalogo>();
            s.AddSingleton<IProveedorCatalogo>(new CatalogoFijo(CatalogoCsv.Real));
        });
    }
}

/// <summary>ACT-10 · GET /api/recomendaciones/candidatos</summary>
public class RecomendacionesTests(FabricaApiConCatalogo api) : IClassFixture<FabricaApiConCatalogo>
{
    private const string Url = "/api/recomendaciones/candidatos";

    private async Task<HttpClient> ClienteConPerfil(int[]? alergenos = null)
    {
        var (c, _, _) = await api.ClienteAutenticado();
        (await c.PostAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy, alergenos: alergenos))).EnsureSuccessStatusCode();
        return c;
    }

    [Fact]
    public async Task Sin_token_da_401()
    {
        var r = await api.CreateClient().GetAsync(Url);
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Sin_perfil_da_404()
    {
        var (c, _, _) = await api.ClienteAutenticado();
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(Url)).StatusCode);
    }

    [Fact]
    public async Task Perfil_del_plan_da_4_comidas_con_al_menos_3_candidatos_sin_lacteos()
    {
        var c = await ClienteConPerfil();
        var r = await c.GetAsync(Url);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1410, json.GetProperty("kcalDia").GetDouble());
        Assert.Equal("lácteos", json.GetProperty("restricciones").GetProperty("alergenos")[0].GetString());

        var comidas = json.GetProperty("comidas").EnumerateArray().ToList();
        Assert.Equal(new[] { "desayuno", "almuerzo", "merienda", "cena" }, comidas.Select(x => x.GetProperty("comida").GetString()));
        foreach (var comida in comidas)
        {
            Assert.True(comida.GetProperty("totalCandidatos").GetInt32() >= 3);
            Assert.True(comida.GetProperty("suficiente").GetBoolean());
            var lista = comida.GetProperty("candidatos").EnumerateArray().ToList();
            Assert.InRange(lista.Count, 3, 10);                       // límite por defecto: 10
            Assert.All(lista, x => Assert.Contains("Sin lácteos", x.GetProperty("reglas").EnumerateArray().Select(e => e.GetString())));
        }
        // almuerzo: meta 493,5 kcal
        Assert.Equal(493.5, comidas[1].GetProperty("metaKcal").GetDouble());
    }

    [Fact]
    public async Task Se_puede_pedir_una_sola_comida_y_cambiar_el_limite()
    {
        var c = await ClienteConPerfil();
        var json = await (await c.GetAsync(Url + "?comida=Almuerzo&limite=3")).Content.ReadFromJsonAsync<JsonElement>();
        var comida = Assert.Single(json.GetProperty("comidas").EnumerateArray());
        Assert.Equal("almuerzo", comida.GetProperty("comida").GetString());
        Assert.Equal(3, comida.GetProperty("candidatos").GetArrayLength());
    }

    [Theory]
    [InlineData("?limite=0")]
    [InlineData("?limite=101")]
    [InlineData("?comida=brunch")]
    public async Task Parametros_invalidos_dan_400(string consulta)
    {
        var c = await ClienteConPerfil();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync(Url + consulta)).StatusCode);
    }
}
