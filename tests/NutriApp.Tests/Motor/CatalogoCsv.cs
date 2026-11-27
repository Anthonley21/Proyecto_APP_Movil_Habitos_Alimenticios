using System.Globalization;
using System.Text;
using NutriApp.Domain.Entidades;
using NutriApp.Domain.Motor;

namespace NutriApp.Tests.Motor;

/// <summary>
/// ACT-10 · Arma el catálogo del motor desde los CSV de datos/bd (los mismos que carga ACT-07),
/// sin base de datos. Así las pruebas usan el catálogo REAL: 997 alimentos y 471 platos.
/// </summary>
public static class CatalogoCsv
{
    private static readonly Lazy<CatalogoMotor> _real = new(() => Cargar(BuscarCarpeta()));

    /// <summary>Catálogo real (se lee una sola vez por ejecución de pruebas).</summary>
    public static CatalogoMotor Real => _real.Value;

    /// <summary>Busca datos/bd subiendo desde la carpeta de las pruebas hasta la raíz del repositorio.</summary>
    public static string BuscarCarpeta()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            var c = Path.Combine(d.FullName, "datos", "bd");
            if (File.Exists(Path.Combine(c, "Alimentos.csv"))) return c;
        }
        throw new DirectoryNotFoundException("No se encontró datos/bd/Alimentos.csv subiendo desde " + AppContext.BaseDirectory);
    }

    public static CatalogoMotor Cargar(string carpeta)
    {
        List<Dictionary<string, string>> T(string tabla) => Leer(Path.Combine(carpeta, tabla + ".csv"));

        var alimentos = T("Alimentos").Select(f => new Alimento
        {
            Id = I(f["Id"]), Codigo = f["Codigo"], Nombre = f["Nombre"], NombreIngles = f["NombreIngles"], GrupoCodigo = f["GrupoCodigo"], UnidadBase = f["UnidadBase"],
            KcalPor100g = D(f["KcalPor100g"]), ProteinaG = D(f["ProteinaG"]), CarbohidratosG = D(f["CarbohidratosG"]), GrasaG = D(f["GrasaG"]),
            AzucaresG = Dn(f["AzucaresG"]), SodioMg = Dn(f["SodioMg"]), IndiceGlucemico = In(f["IndiceGlucemico"]),
            CategoriaIG = f["CategoriaIG"], EsProductoDeMarca = B(f["EsProductoDeMarca"]), AptoMotor = B(f["AptoMotor"]),
        }).ToList();
        var porciones = T("Porciones").Select(f => new Porcion
        {
            Id = I(f["Id"]), AlimentoId = I(f["AlimentoId"]), Descripcion = f["Descripcion"], Gramos = D(f["Gramos"]),
            EsPredeterminada = B(f["EsPredeterminada"]),
        }).ToList();
        var alimAle = T("AlimentoAlergeno").Select(f => new AlimentoAlergeno { AlimentoId = I(f["AlimentoId"]), AlergenoId = I(f["AlergenoId"]) }).ToList();
        var alimCom = T("AlimentoTipoComida").Select(f => new AlimentoTipoComida { AlimentoId = I(f["AlimentoId"]), TipoComidaId = I(f["TipoComidaId"]) }).ToList();
        var platos = T("Platos").Select(f => new Plato
        {
            Id = I(f["Id"]), Nombre = f["Nombre"], EsEcuatoriana = B(f["EsEcuatoriana"]), Clasificacion = f["Clasificacion"],
            PorcionG = In(f["PorcionG"]), KcalPorcion = Dn(f["KcalPorcion"]), ProteinaGPorcion = Dn(f["ProteinaGPorcion"]),
            CarbohidratosGPorcion = Dn(f["CarbohidratosGPorcion"]), GrasaGPorcion = Dn(f["GrasaGPorcion"]),
            AzucaresGPorcion = Dn(f["AzucaresGPorcion"]), SodioMgPorcion = Dn(f["SodioMgPorcion"]), IndiceGlucemico = In(f["IndiceGlucemico"]),
            CalidadNutricional = f["CalidadNutricional"], Revisar = B(f["Revisar"]),
        }).ToList();
        var ingredientes = T("PlatoIngredientes").Select(f => new PlatoIngrediente
        {
            Id = I(f["Id"]), PlatoId = I(f["PlatoId"]), Orden = I(f["Orden"]), Texto = f["Texto"], AlimentoId = In(f["AlimentoId"]),
            Gramos = Dn(f["Gramos"]), Estado = f["Estado"],
        }).ToList();
        var platoCom = T("PlatoTipoComida").Select(f => new PlatoTipoComida { PlatoId = I(f["PlatoId"]), TipoComidaId = I(f["TipoComidaId"]) }).ToList();
        var alergenos = T("Alergenos").Select(f => new Alergeno { Id = I(f["Id"]), Nombre = f["Nombre"] }).ToList();

        return CatalogoMotor.Construir(alimentos, porciones, alimAle, alimCom, platos, ingredientes, platoCom, alergenos);
    }

    // ---------------- lectura de CSV (RFC 4180: comillas, comas y saltos de línea dentro de comillas)

    public static List<Dictionary<string, string>> Leer(string ruta)
    {
        var filas = Partir(File.ReadAllText(ruta, Encoding.UTF8));
        var cab = filas[0].Select(c => c.Trim().TrimStart('﻿')).ToArray();
        return filas.Skip(1).Where(f => f.Count > 1 || f[0].Length > 0)
            .Select(f => cab.Select((c, i) => (c, v: i < f.Count ? f[i] : "")).ToDictionary(x => x.c, x => x.v))
            .ToList();
    }

    private static List<List<string>> Partir(string texto)
    {
        var filas = new List<List<string>>();
        var fila = new List<string>();
        var campo = new StringBuilder();
        bool comillas = false;
        for (int i = 0; i < texto.Length; i++)
        {
            char c = texto[i];
            if (comillas)
            {
                if (c == '"' && i + 1 < texto.Length && texto[i + 1] == '"') { campo.Append('"'); i++; }
                else if (c == '"') comillas = false;
                else campo.Append(c);
            }
            else if (c == '"') comillas = true;
            else if (c == ',') { fila.Add(campo.ToString()); campo.Clear(); }
            else if (c == '\n' || c == '\r')
            {
                if (c == '\r' && i + 1 < texto.Length && texto[i + 1] == '\n') i++;
                fila.Add(campo.ToString()); campo.Clear();
                filas.Add(fila); fila = [];
            }
            else campo.Append(c);
        }
        if (campo.Length > 0 || fila.Count > 0) { fila.Add(campo.ToString()); filas.Add(fila); }
        return filas;
    }

    private static int I(string s) => int.Parse(s, CultureInfo.InvariantCulture);
    private static int? In(string s) => string.IsNullOrWhiteSpace(s) ? null : (int)decimal.Parse(s, CultureInfo.InvariantCulture);
    private static decimal D(string s) => decimal.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static decimal? Dn(string s) => string.IsNullOrWhiteSpace(s) ? null : D(s);
    private static bool B(string s) => s.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) || s.Trim() == "1";
}
