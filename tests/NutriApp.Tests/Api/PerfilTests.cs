using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NutriApp.Infrastructure.Persistencia;

namespace NutriApp.Tests.Api;

/// <summary>ACT-09 · Perfil conectado al cálculo del requerimiento (criterio del plan).</summary>
public class PerfilTests(FabricaApi api) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task Criterio_ACT09_registro_login_perfil_y_requerimiento_1410()
    {
        var c = api.CreateClient();
        var correo = FabricaApi.CorreoNuevo();

        Assert.Equal(HttpStatusCode.Created, (await api.Registrar(c, correo)).StatusCode);

        var login = await FabricaApi.Login(c, correo);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        c.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var creado = await c.PostAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy));
        Assert.Equal(HttpStatusCode.Created, creado.StatusCode);

        var req = await c.GetFromJsonAsync<JsonElement>("/api/perfil/requerimiento");
        Assert.Equal(1410, req.GetProperty("kcalDia").GetDouble());
        Assert.Equal(4, req.GetProperty("comidas").GetArrayLength());
    }

    [Fact]
    public async Task Get_devuelve_lo_guardado()
    {
        var (c, _, _) = await api.ClienteAutenticado();
        await c.PostAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy, alergenos: [1, 6]));

        var p = await c.GetFromJsonAsync<JsonElement>("/api/perfil");
        Assert.Equal("F", p.GetProperty("sexo").GetString());
        Assert.Equal(30, p.GetProperty("edad").GetInt32());
        Assert.Equal("ligero", p.GetProperty("nivelActividad").GetString());
        Assert.Equal(1.375m, p.GetProperty("factorActividad").GetDecimal());
        var alergenos = p.GetProperty("alergenos").EnumerateArray().Select(a => a.GetProperty("nombre").GetString()).ToList();
        Assert.Equal(["gluten", "pescado"], alergenos);
    }

    [Fact]
    public async Task Sin_perfil_GET_PUT_y_requerimiento_dan_404()
    {
        var (c, _, _) = await api.ClienteAutenticado();
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/perfil")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/perfil/requerimiento")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PutAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy))).StatusCode);
    }

    [Fact]
    public async Task Crear_dos_veces_da_409()
    {
        var (c, _, _) = await api.ClienteAutenticado();
        await c.PostAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy));
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy))).StatusCode);
    }

    [Fact]
    public async Task Put_actualiza_el_requerimiento_y_un_solo_peso_por_dia()
    {
        var (c, _, correo) = await api.ClienteAutenticado();
        await c.PostAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy, pesoKg: 70));
        var put = await c.PutAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy, pesoKg: 68.44m, alergenos: []));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var p = await c.GetFromJsonAsync<JsonElement>("/api/perfil");
        Assert.Equal(68.4m, p.GetProperty("pesoKg").GetDecimal());   // 1 decimal, como en la base
        Assert.Equal(0, p.GetProperty("alergenos").GetArrayLength());

        var req = await c.GetFromJsonAsync<JsonElement>("/api/perfil/requerimiento");
        Assert.True(req.GetProperty("kcalDia").GetDouble() < 1410);

        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NutriAppDbContext>();
        var usuarioId = (await db.Users.SingleAsync(u => u.Email == correo)).Id;
        var perfilId = (await db.PerfilesUsuario.SingleAsync(x => x.UsuarioId == usuarioId)).Id;
        var historial = await db.HistorialPeso.Where(h => h.PerfilId == perfilId).ToListAsync();
        Assert.Single(historial);
        Assert.Equal(68.4m, historial[0].PesoKg);
    }

    public static TheoryData<string, string> PerfilesInvalidos
    {
        get
        {
            var hoy = FabricaApi.Hoy;
            string Con(string campo, object? valor)
            {
                var d = JsonSerializer.SerializeToNode(FabricaApi.PerfilDelPlan(hoy))!.AsObject();
                d[campo] = JsonSerializer.SerializeToNode(valor);
                return d.ToJsonString();
            }
            return new()
            {
                { "sexo X", Con("sexo", "X") },
                { "sin sexo", Con("sexo", null) },
                { "peso 20", Con("pesoKg", 20) },
                { "peso 301", Con("pesoKg", 301) },
                { "estatura 99", Con("estaturaCm", 99) },
                { "nivel inventado", Con("nivelActividad", "flojo") },
                { "objetivo inventado", Con("objetivo", "adelgazar") },
                { "edad 17", Con("edad", 17) },
                { "edad 81", Con("edad", 81) },
                { "edad en texto", Con("edad", "treinta") },
                { "sin edad", Con("edad", null) },
                { "alérgeno 99", Con("alergenoIds", new[] { 99 }) },
                { "texto en el peso", Con("pesoKg", "setenta") },
            };
        }
    }

    [Theory, MemberData(nameof(PerfilesInvalidos))]
    public async Task Perfil_invalido_da_400_y_no_se_guarda(string caso, string json)
    {
        var (c, _, _) = await api.ClienteAutenticado();
        var r = await c.PostAsync("/api/perfil", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{caso}: {r.StatusCode}");
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/perfil")).StatusCode);
    }

    [Fact]
    public async Task Alergenos_null_o_ausentes_se_aceptan_como_ninguno()
    {
        var (c, _, _) = await api.ClienteAutenticado();
        var d = JsonSerializer.SerializeToNode(FabricaApi.PerfilDelPlan(FabricaApi.Hoy))!.AsObject();
        d["alergenoIds"] = null;
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/perfil", d)).StatusCode);
        d.Remove("alergenoIds");
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync("/api/perfil", d)).StatusCode);
    }

    [Fact]
    public async Task Mis_datos_incluye_consentimiento_perfil_e_historial()
    {
        var (c, _, correo) = await api.ClienteAutenticado();
        await c.PostAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy));

        var d = await c.GetFromJsonAsync<JsonElement>("/api/cuenta/mis-datos");
        Assert.Equal(correo, d.GetProperty("correo").GetString());
        Assert.Equal(JsonValueKind.String, d.GetProperty("consentimientoDatosSalud").ValueKind);
        Assert.Equal(70m, d.GetProperty("perfil").GetProperty("pesoKg").GetDecimal());
        Assert.Equal(1, d.GetProperty("historialPeso").GetArrayLength());
    }
}
