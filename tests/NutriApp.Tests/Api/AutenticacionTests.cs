using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace NutriApp.Tests.Api;

/// <summary>ACT-09 · Registro e inicio de sesión.</summary>
public class AutenticacionTests(FabricaApi api) : IClassFixture<FabricaApi>
{
    [Fact]
    public async Task Registro_valido_devuelve_201()
    {
        var r = await api.Registrar(api.CreateClient(), FabricaApi.CorreoNuevo());
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Registro_sin_consentimiento_es_rechazado()
    {
        var r = await api.Registrar(api.CreateClient(), FabricaApi.CorreoNuevo(), acepta: false);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Contains("aceptaTratamientoDatos", await r.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Corta1A")]              // menos de 10
    [InlineData("sinnumerosAAA")]        // sin número
    [InlineData("SINMINUSCULAS123")]     // sin minúscula
    [InlineData("sinmayusculas123")]     // sin mayúscula
    [InlineData("Aaaaaaaaaa1")]          // solo 2 caracteres distintos... más el 1 = 3 (< 4)
    public async Task Contrasena_debil_es_rechazada(string contrasena)
    {
        var r = await api.Registrar(api.CreateClient(), FabricaApi.CorreoNuevo(), contrasena);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Theory]
    [InlineData("no-es-correo")]
    [InlineData("")]
    public async Task Correo_invalido_es_rechazado(string correo)
    {
        var r = await api.Registrar(api.CreateClient(), correo);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Contrasena_enorme_es_rechazada_sin_procesarla()
    {
        var r = await api.Registrar(api.CreateClient(), FabricaApi.CorreoNuevo(), "Aa1" + new string('x', 5000));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Correo_repetido_no_revela_que_ya_existe()
    {
        var c = api.CreateClient();
        var correo = FabricaApi.CorreoNuevo();
        await api.Registrar(c, correo);
        var r = await api.Registrar(c, correo);

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        var cuerpo = await r.Content.ReadAsStringAsync();
        Assert.Contains("No se pudo completar el registro", cuerpo);
        Assert.DoesNotContain("duplic", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(correo, cuerpo);
    }

    [Fact]
    public async Task Login_correcto_devuelve_token_de_30_minutos()
    {
        var c = api.CreateClient();
        var correo = FabricaApi.CorreoNuevo();
        await api.Registrar(c, correo);

        var r = await FabricaApi.Login(c, correo);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var json = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, json.GetProperty("token").GetString()!.Split('.').Length);   // cabecera.contenido.firma
        Assert.Equal("Bearer", json.GetProperty("tipo").GetString());
        var minutos = (json.GetProperty("expiraEn").GetDateTimeOffset() - DateTimeOffset.UtcNow).TotalMinutes;
        Assert.InRange(minutos, 28, 31);
    }

    [Fact]
    public async Task El_token_no_lleva_correo_ni_datos_personales()
    {
        var (_, token, correo) = await api.ClienteAutenticado();
        var contenido = LeerContenido(token);

        Assert.DoesNotContain(correo, contenido.GetRawText());
        var permitidos = new HashSet<string> { "sub", "sst", "jti", "iat", "nbf", "exp", "iss", "aud" };
        foreach (var claim in contenido.EnumerateObject())
            Assert.Contains(claim.Name, permitidos);
    }

    [Fact]
    public async Task Correo_inexistente_y_contrasena_incorrecta_responden_igual()
    {
        var c = api.CreateClient();
        var correo = FabricaApi.CorreoNuevo();
        await api.Registrar(c, correo);

        var inexistente = await FabricaApi.Login(c, FabricaApi.CorreoNuevo());
        var incorrecta = await FabricaApi.Login(c, correo, "OtraClave2026x");

        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, incorrecta.StatusCode);
        var a = await inexistente.Content.ReadFromJsonAsync<JsonElement>();
        var b = await incorrecta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(a.GetProperty("title").GetString(), b.GetProperty("title").GetString());
        Assert.Equal(a.GetProperty("detail").GetString(), b.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Cinco_intentos_fallidos_bloquean_la_cuenta()
    {
        var c = api.CreateClient();
        var correo = FabricaApi.CorreoNuevo();
        await api.Registrar(c, correo);

        for (int i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await FabricaApi.Login(c, correo, "Incorrecta2026x")).StatusCode);

        // Ni con la contraseña correcta entra mientras dure el bloqueo
        Assert.Equal(HttpStatusCode.Unauthorized, (await FabricaApi.Login(c, correo)).StatusCode);
    }

    [Fact]
    public async Task Un_fallo_suelto_no_bloquea_y_el_contador_se_reinicia()
    {
        var c = api.CreateClient();
        var correo = FabricaApi.CorreoNuevo();
        await api.Registrar(c, correo);

        for (int ronda = 0; ronda < 2; ronda++)
        {
            for (int i = 0; i < 4; i++)
                await FabricaApi.Login(c, correo, "Incorrecta2026x");
            Assert.Equal(HttpStatusCode.OK, (await FabricaApi.Login(c, correo)).StatusCode);   // reinicia el contador
        }
    }

    [Fact]
    public async Task El_correo_no_distingue_mayusculas()
    {
        var c = api.CreateClient();
        var correo = FabricaApi.CorreoNuevo();
        await api.Registrar(c, correo);
        Assert.Equal(HttpStatusCode.OK, (await FabricaApi.Login(c, correo.ToUpperInvariant())).StatusCode);
    }

    internal static JsonElement LeerContenido(string jwt)
    {
        var parte = jwt.Split('.')[1].Replace('-', '+').Replace('_', '/');
        parte = parte.PadRight(parte.Length + (4 - parte.Length % 4) % 4, '=');
        return JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(parte))).RootElement;
    }
}
