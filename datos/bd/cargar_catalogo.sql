-- ACT-07 · Carga repetible del catálogo con psql (alternativa al comando 'cargar-datos' de la API).
-- Hace lo mismo que CargadorCatalogo.cs: vacía el catálogo y lo vuelve a llenar en UNA transacción.
-- ATENCIÓN: TRUNCATE ... CASCADE borra también recomendaciones/registros de usuarios si existieran.
-- Uso (desde esta carpeta, después de 'dotnet ef database update'):
--   psql "postgresql://USUARIO:CLAVE@HOST/nutriapp?sslmode=require" -v ON_ERROR_STOP=1 -f cargar_catalogo.sql

BEGIN;
TRUNCATE "GruposAlimento", "TiposComida", "Alergenos", "Alimentos", "Porciones", "AlimentoAlergeno", "AlimentoTipoComida", "Platos", "PlatoIngredientes", "PlatoTipoComida" RESTART IDENTITY CASCADE;

\copy "GruposAlimento" ("Codigo", "Nombre") FROM 'GruposAlimento.csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
\copy "TiposComida" ("Id", "Nombre", "PorcentajeEnergia", "Orden") FROM 'TiposComida.csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
\copy "Alergenos" ("Id", "Nombre") FROM 'Alergenos.csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
\copy "Alimentos" ("Id", "Codigo", "Nombre", "NombreIngles", "GrupoCodigo", "UnidadBase", "KcalPor100g", "ProteinaG", "CarbohidratosG", "GrasaG", "FibraG", "AzucaresG", "CarbDisponibleG", "SodioMg", "GrasaSaturadaG", "ColesterolMg", "CalcioMg", "HierroMg", "PotasioMg", "ZincMg", "VitaminaCMg", "VitaminaAUg", "FolatosUg", "VitaminaB12Ug", "IndiceGlucemico", "CategoriaIG", "CargaGlucemica100g", "CalidadAzucar", "CalidadIG", "EsProductoDeMarca", "AptoMotor", "Revisar", "Fuente", "NotaLimpieza") FROM 'Alimentos.csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
\copy "Porciones" ("Id", "AlimentoId", "Descripcion", "Gramos", "EsPredeterminada", "Fuente") FROM 'Porciones.csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
\copy "AlimentoAlergeno" ("AlimentoId", "AlergenoId") FROM 'AlimentoAlergeno.csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
\copy "AlimentoTipoComida" ("AlimentoId", "TipoComidaId") FROM 'AlimentoTipoComida.csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
\copy "Platos" ("Id", "Nombre", "Categoria", "PaisOrigen", "EsEcuatoriana", "Clasificacion", "Raciones", "PorcionG", "KcalPorcion", "ProteinaGPorcion", "CarbohidratosGPorcion", "GrasaGPorcion", "FibraGPorcion", "AzucaresGPorcion", "SodioMgPorcion", "IndiceGlucemico", "CargaGlucemicaPorcion", "CalidadNutricional", "CoberturaIngredientes", "Revisar", "IngredientesSinEquivalente", "Tiempo", "Dificultad", "Url", "ImagenUrl") FROM 'Platos.csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
\copy "PlatoIngredientes" ("Id", "PlatoId", "Orden", "Texto", "AlimentoId", "Gramos", "Estado") FROM 'PlatoIngredientes.csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
\copy "PlatoTipoComida" ("PlatoId", "TipoComidaId") FROM 'PlatoTipoComida.csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')

SELECT setval(pg_get_serial_sequence('"Porciones"', 'Id'), COALESCE((SELECT MAX("Id") FROM "Porciones"), 0) + 1, false);
COMMIT;

-- Conteos esperados (CSV de ACT-06)
SELECT tabla, filas, esperado, CASE WHEN filas = esperado THEN 'OK' ELSE 'DIFERENTE' END AS estado FROM (
SELECT 'GruposAlimento' AS tabla, (SELECT COUNT(*) FROM "GruposAlimento") AS filas, 11 AS esperado
  UNION ALL SELECT 'TiposComida' AS tabla, (SELECT COUNT(*) FROM "TiposComida") AS filas, 4 AS esperado
  UNION ALL SELECT 'Alergenos' AS tabla, (SELECT COUNT(*) FROM "Alergenos") AS filas, 9 AS esperado
  UNION ALL SELECT 'Alimentos' AS tabla, (SELECT COUNT(*) FROM "Alimentos") AS filas, 997 AS esperado
  UNION ALL SELECT 'Porciones' AS tabla, (SELECT COUNT(*) FROM "Porciones") AS filas, 997 AS esperado
  UNION ALL SELECT 'AlimentoAlergeno' AS tabla, (SELECT COUNT(*) FROM "AlimentoAlergeno") AS filas, 330 AS esperado
  UNION ALL SELECT 'AlimentoTipoComida' AS tabla, (SELECT COUNT(*) FROM "AlimentoTipoComida") AS filas, 1599 AS esperado
  UNION ALL SELECT 'Platos' AS tabla, (SELECT COUNT(*) FROM "Platos") AS filas, 471 AS esperado
  UNION ALL SELECT 'PlatoIngredientes' AS tabla, (SELECT COUNT(*) FROM "PlatoIngredientes") AS filas, 4925 AS esperado
  UNION ALL SELECT 'PlatoTipoComida' AS tabla, (SELECT COUNT(*) FROM "PlatoTipoComida") AS filas, 733 AS esperado
) c;
