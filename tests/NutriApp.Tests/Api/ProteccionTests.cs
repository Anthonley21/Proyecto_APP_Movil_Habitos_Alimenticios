using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace NutriApp.Tests.Api;

/// <summary>ACT-09 · Acceso: nadie entra sin un token válido y nadie ve datos de otra persona.</summary>
public class ProteccionTests(FabricaApi api) : IClassFixture<FabricaApi>
{
    public static TheoryData<string, string> EndpointsProtegidos => new()
    {
        { "GET", "/api/perfil" },
        { "POST", "/api/perfil" },
        { "PUT", "/api/perfil" },
        { "GET", "/api/perfil/requerimiento" },
        { "GET", "/api/cuenta/mis-datos" },
        { "POST", "/api/cuenta/cerrar-sesion" },
        { "POST", "/api/cuenta/eliminar" },
    };

    [Theory, MemberData(nameof(EndpointsProtegidos))]
    public async Task Sin_token_responde_401(string metodo, string ruta)
    {
        var r = await api.CreateClient().SendAsync(Peticion(metodo, ruta));
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Theory, MemberData(nameof(EndpointsProtegidos))]
    public async Task Token_falso_responde_401(string metodo, string ruta)
    {
        var c = api.CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "esto.no.esuntoken");
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.SendAsync(Peticion(metodo, ruta))).StatusCode);
    }

    [Fact]
    public async Task Token_con_firma_alterada_responde_401()
    {
        var (c, token, _) = await api.ClienteAutenticado();
        var partes = token.Split('.');
        var firma = partes[2];
        partes[2] = (firma[0] == 'A' ? 'B' : 'A') + firma[1..];
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Join('.', partes));

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/perfil")).StatusCode);
    }

    [Fact]
    public async Task Token_con_contenido_alterado_responde_401()
    {
        // Cambiar el "sub" por el de otra persona invalida la firma
        var (_, tokenA, _) = await api.ClienteAutenticado();
        var (c, tokenB, _) = await api.ClienteAutenticado();
        var a = tokenA.Split('.');
        var b = tokenB.Split('.');
        var falsificado = $"{b[0]}.{a[1]}.{b[2]}";   // contenido de A con la firma de B
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", falsificado);

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/perfil")).StatusCode);
    }

    [Fact]
    public async Task Token_sin_firma_alg_none_responde_401()
    {
        var (c, token, _) = await api.ClienteAutenticado();
        var cabecera = Base64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var sinFirma = $"{cabecera}.{token.Split('.')[1]}.";
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sinFirma);

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/perfil")).StatusCode);
    }

    [Fact]
    public async Task Token_vencido_responde_401()
    {
        // Se fabrica un token correcto en todo (usuario, sello, clave, emisor) salvo la fecha
        var (c, token, _) = await api.ClienteAutenticado();
        var contenido = AutenticacionTests.LeerContenido(token);
        string sub = contenido.GetProperty("sub").GetString()!, sst = contenido.GetProperty("sst").GetString()!;
        var ahora = DateTime.UtcNow;

        // Control: con fechas vigentes el token fabricado SÍ funciona (404 = autenticado, aún sin perfil)
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Fabricar(sub, sst, ahora.AddMinutes(-1), ahora.AddMinutes(29)));
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/perfil")).StatusCode);

        // Vencido hace una hora
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Fabricar(sub, sst, ahora.AddHours(-2), ahora.AddHours(-1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/perfil")).StatusCode);
    }

    private static string Fabricar(string sub, string sst, DateTime desde, DateTime hasta) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "NutriApp.Api",
            Audience = "NutriApp.Mobile",
            IssuedAt = desde,
            NotBefore = desde,
            Expires = hasta,
            Claims = new Dictionary<string, object> { ["sub"] = sub, ["sst"] = sst },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(FabricaApi.ClavePruebas)), SecurityAlgorithms.HmacSha256),
        });

    [Fact]
    public async Task Cerrar_sesion_invalida_el_token()
    {
        var (c, _, _) = await api.ClienteAutenticado();
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/perfil")).StatusCode);   // válido (aún sin perfil)

        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsync("/api/cuenta/cerrar-sesion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/perfil")).StatusCode);
    }

    [Fact]
    public async Task Eliminar_cuenta_pide_contrasena_y_borra_el_acceso()
    {
        var (c, _, correo) = await api.ClienteAutenticado();
        (await c.PostAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy))).EnsureSuccessStatusCode();

        var mal = await c.PostAsJsonAsync("/api/cuenta/eliminar", new { contrasena = "Incorrecta2026x" });
        Assert.Equal(HttpStatusCode.Unauthorized, mal.StatusCode);

        var bien = await c.PostAsJsonAsync("/api/cuenta/eliminar", new { contrasena = FabricaApi.ContrasenaValida });
        Assert.Equal(HttpStatusCode.NoContent, bien.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/perfil")).StatusCode);                        // token muerto
        Assert.Equal(HttpStatusCode.Unauthorized, (await FabricaApi.Login(api.CreateClient(), correo)).StatusCode);    // cuenta borrada
    }

    [Fact]
    public async Task Cada_usuario_solo_ve_su_propio_perfil()
    {
        var (ana, _, _) = await api.ClienteAutenticado();
        var (luis, _, _) = await api.ClienteAutenticado();
        (await ana.PostAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy, pesoKg: 70))).EnsureSuccessStatusCode();

        // Luis no tiene perfil: no recibe el de Ana
        Assert.Equal(HttpStatusCode.NotFound, (await luis.GetAsync("/api/perfil")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await luis.GetAsync("/api/perfil/requerimiento")).StatusCode);

        // Luis crea el suyo y modificarlo no toca el de Ana
        (await luis.PostAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy, pesoKg: 95))).EnsureSuccessStatusCode();
        (await luis.PutAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy, pesoKg: 90))).EnsureSuccessStatusCode();

        var deAna = await ana.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/perfil");
        Assert.Equal(70m, deAna.GetProperty("pesoKg").GetDecimal());
    }

    [Fact]
    public async Task Las_respuestas_no_exponen_ids_internos()
    {
        var (c, _, _) = await api.ClienteAutenticado();
        var r = await c.PostAsJsonAsync("/api/perfil", FabricaApi.PerfilDelPlan(FabricaApi.Hoy));
        var cuerpo = await r.Content.ReadAsStringAsync();

        Assert.DoesNotContain("usuarioId", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Respuestas_de_la_api_llevan_cabeceras_de_seguridad()
    {
        var r = await api.CreateClient().GetAsync("/api/perfil");

        Assert.Equal("nosniff", r.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", r.Headers.GetValues("X-Frame-Options").Single());
        Assert.True(r.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Demasiados_intentos_de_login_responden_429()
    {
        using var limitada = new FabricaApi { LimitePorMinuto = 3 };
        var c = limitada.CreateClient();

        for (int i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await FabricaApi.Login(c, FabricaApi.CorreoNuevo())).StatusCode);

        Assert.Equal(HttpStatusCode.TooManyRequests, (await FabricaApi.Login(c, FabricaApi.CorreoNuevo())).StatusCode);
    }

    private static HttpRequestMessage Peticion(string metodo, string ruta) => new(new HttpMethod(metodo), ruta)
    {
        Content = metodo == "GET" ? null : JsonContent.Create(new { }),
    };

    private static string Base64Url(string texto) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(texto)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
