using System.Text;
using Npgsql;

namespace NutriApp.Infrastructure.Datos;

/// <summary>
/// ACT-07 · Carga repetible del catálogo (CSV de ACT-06, carpeta datos/bd) en PostgreSQL.
///
/// Repetible: en una sola transacción vacía las tablas del catálogo y las vuelve a llenar con COPY,
/// así que ejecutarlo dos veces deja la base igual. Si algo falla, no queda nada a medias (ROLLBACK).
/// Al final compara cuántas filas tiene cada tabla con cuántos registros trae cada CSV.
///
/// Protección: si ya hay datos de usuarios que apuntan al catálogo (recomendaciones, registros de
/// comida o alérgenos de un perfil), no hace nada salvo que se pase --forzar, porque vaciar el
/// catálogo borraría también esos datos.
/// </summary>
public static class CargadorCatalogo
{
    /// <summary>Orden de carga: primero las tablas de las que dependen las demás.</summary>
    public static readonly string[] Tablas =
    [
        "GruposAlimento", "TiposComida", "Alergenos",
        "Alimentos", "Porciones", "AlimentoAlergeno", "AlimentoTipoComida",
        "Platos", "PlatoIngredientes", "PlatoTipoComida",
    ];

    /// <summary>Tablas de usuario que apuntan al catálogo.</summary>
    private static readonly string[] TablasDeUsuario = ["Recomendaciones", "RegistrosComida", "PerfilAlergeno"];

    /// <summary>Tablas cargadas desde CSV cuyo Id es autonumérico: hay que mover su contador.</summary>
    private static readonly string[] TablasConIdentidad = ["Porciones"];

    public static async Task<int> CargarAsync(string cadenaConexion, string carpeta, bool forzar, TextWriter log)
    {
        // 1. Comprobar que están todos los CSV antes de tocar la base
        var faltan = Tablas.Where(t => !File.Exists(Path.Combine(carpeta, t + ".csv"))).ToList();
        if (faltan.Count > 0)
        {
            log.WriteLine($"Faltan archivos en '{Path.GetFullPath(carpeta)}': {string.Join(", ", faltan.Select(t => t + ".csv"))}");
            return 2;
        }

        await using var conexion = new NpgsqlConnection(cadenaConexion);
        await conexion.OpenAsync();

        // 2. Comprobar que la migración ya creó las tablas
        foreach (var tabla in Tablas.Concat(TablasDeUsuario))
        {
            var existe = await EscalarAsync(conexion, null, $"SELECT to_regclass('\"{tabla}\"') IS NOT NULL");
            if (existe is not true)
            {
                log.WriteLine($"No existe la tabla \"{tabla}\". Primero aplica la migración: dotnet ef database update");
                return 3;
            }
        }

        await using var tx = await conexion.BeginTransactionAsync();

        // 3. Protección de datos de usuarios
        long filasUsuario = 0;
        foreach (var tabla in TablasDeUsuario)
            filasUsuario += (long)(await EscalarAsync(conexion, tx, $"SELECT COUNT(*) FROM \"{tabla}\""))!;
        if (filasUsuario > 0 && !forzar)
        {
            log.WriteLine($"Hay {filasUsuario} filas de usuarios que dependen del catálogo. No se cargó nada.");
            log.WriteLine("Si de verdad quieres reemplazar el catálogo (se borrarán esas filas), agrega --forzar.");
            return 4;
        }

        // 4. Vaciar el catálogo (CASCADE solo alcanza a tablas de usuario si se usó --forzar)
        var lista = string.Join(", ", Tablas.Select(t => $"\"{t}\""));
        await EjecutarAsync(conexion, tx, $"TRUNCATE {lista} RESTART IDENTITY CASCADE");

        // 5. Cargar cada CSV con COPY, en orden
        var esperado = new Dictionary<string, long>();
        foreach (var tabla in Tablas)
        {
            var ruta = Path.Combine(carpeta, tabla + ".csv");
            var contenido = await File.ReadAllTextAsync(ruta, Encoding.UTF8);   // quita el BOM
            var columnas = PrimeraLinea(contenido).Split(',').Select(c => $"\"{c.Trim()}\"");
            esperado[tabla] = ContarRegistros(contenido);

            var copy = $"COPY \"{tabla}\" ({string.Join(", ", columnas)}) FROM STDIN (FORMAT csv, HEADER true)";
            await using (var escritor = await conexion.BeginTextImportAsync(copy))
                await escritor.WriteAsync(contenido);
        }

        // 6. Ajustar los contadores autonuméricos al Id más alto cargado
        foreach (var tabla in TablasConIdentidad)
            await EjecutarAsync(conexion, tx,
                $"SELECT setval(pg_get_serial_sequence('\"{tabla}\"', 'Id'), COALESCE((SELECT MAX(\"Id\") FROM \"{tabla}\"), 0) + 1, false)");

        // 7. Verificar conteos contra los CSV
        log.WriteLine($"{"Tabla",-20} {"CSV",7} {"Base",7}  Estado");
        var todoBien = true;
        foreach (var tabla in Tablas)
        {
            var enBase = (long)(await EscalarAsync(conexion, tx, $"SELECT COUNT(*) FROM \"{tabla}\""))!;
            var ok = enBase == esperado[tabla];
            todoBien &= ok;
            log.WriteLine($"{tabla,-20} {esperado[tabla],7} {enBase,7}  {(ok ? "OK" : "DIFERENTE")}");
        }

        if (!todoBien)
        {
            await tx.RollbackAsync();
            log.WriteLine("Los conteos no coinciden: se deshizo la carga.");
            return 5;
        }

        await tx.CommitAsync();
        log.WriteLine($"Catálogo cargado: {esperado.Values.Sum()} filas en {Tablas.Length} tablas.");
        return 0;
    }

    /// <summary>Cuenta los registros de un CSV (sin encabezado), respetando comillas y saltos de línea dentro de campos.</summary>
    public static long ContarRegistros(string csv)
    {
        long registros = 0;
        var entreComillas = false;
        var hayDatos = false;
        foreach (var c in csv)
        {
            if (c == '"') entreComillas = !entreComillas;
            if (c == '\n' && !entreComillas)
            {
                if (hayDatos) registros++;
                hayDatos = false;
            }
            else if (c != '\r') hayDatos = true;
        }
        if (hayDatos) registros++;
        return Math.Max(0, registros - 1);   // menos el encabezado
    }

    private static string PrimeraLinea(string texto)
    {
        var fin = texto.IndexOfAny(['\r', '\n']);
        return (fin < 0 ? texto : texto[..fin]).TrimStart('﻿');
    }

    private static async Task<object?> EscalarAsync(NpgsqlConnection c, NpgsqlTransaction? tx, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, c, tx);
        return await cmd.ExecuteScalarAsync();
    }

    private static async Task EjecutarAsync(NpgsqlConnection c, NpgsqlTransaction tx, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, c, tx);
        await cmd.ExecuteNonQueryAsync();
    }
}
