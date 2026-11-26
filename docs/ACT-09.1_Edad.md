# ACT-09.1 · Edad en lugar de fecha de nacimiento

Ajuste correctivo de la ACT-09. El perfil deja de pedir la fecha de nacimiento y pide la **edad en años**.
Se llama 09.1 porque corrige la ACT-09; no es una actividad nueva del plan.

- **Rama:** `ACT-09.1` (desde `main`, después del PR #1 de la ACT-09)
- **Migración:** `20261126223824_EdadEnLugarDeFecha`
- **Resultado:** 114 pruebas superadas; migración aplicada en Neon; perfiles existentes convertidos sin pérdida.

---

## 1. Por qué se cambió

| Motivo | Detalle |
|---|---|
| El cálculo solo usa años | Mifflin-St Jeor: 10·peso + 6,25·estatura − 5·edad + 5 (hombres) o − 161 (mujeres). Un año de diferencia cambia la TMB en 5 kcal; la fecha exacta no aporta nada. |
| Minimización de datos (LOPDP, Ecuador 2021) | Solo se piden los datos que el sistema necesita. La fecha de nacimiento identifica más a la persona que la edad. |
| Trabajos relacionados | La mayoría pide la edad directamente (artículos 2, 3, 7 y 24 de la revisión). Solo FitLifeBuddy (artículo 19) pide la fecha, y la convierte a años. |
| Observación en clase | "Si solo se necesitan los años, se piden los años." |

## 2. Diseño

La columna `FechaNacimiento` se reemplaza por dos:

| Columna | Tipo | Qué guarda |
|---|---|---|
| `Edad` | integer, `CHECK (Edad BETWEEN 18 AND 80)` | Los años que escribe la persona |
| `EdadRegistradaEn` | date | El día en que la escribió |

**Edad actual** = `Edad` + aniversarios completos desde `EdadRegistradaEn`. Así la edad no se queda congelada
y nunca se guarda la fecha de nacimiento. Si con los años pasa de 80, `GET /api/perfil/requerimiento`
responde 409 "Confirma tu edad" y la persona la actualiza con `PUT /api/perfil`.

`EdadRegistradaEn` solo cambia cuando cambia la edad: si un `PUT` envía la misma edad actual, se conserva la fecha.

## 3. Archivos cambiados

| Capa | Archivo | Cambio |
|---|---|---|
| Domain | `Entidades/PerfilUsuario.cs` | `FechaNacimiento` → `Edad` + `EdadRegistradaEn` |
| Domain | `Motor/CalculadoraRequerimiento.cs` | `CalcularEdad(fecha)` → `EdadActual(edad, registradaEn, hoy)` |
| Infrastructure | `Persistencia/ConfiguracionModelo.cs` | Columnas nuevas y `CK_PerfilesUsuario_Edad` (17 → 18 CHECK) |
| Infrastructure | `Migraciones/20261126223824_EdadEnLugarDeFecha.cs` | Migración editada a mano (sección 4) |
| Api | `Contratos/Contratos.cs` | `edad` con `[Range(18, 80)]`; la respuesta ya no trae la fecha |
| Api | `Controllers/PerfilController.cs` | Validación de edad, fecha de registro, 409 si la edad sale de rango |
| Api | `NutriApp.Api.http` | El ejemplo envía `"edad": 30` |
| Tests | `CalculadoraRequerimientoTests.cs` | 6 casos de fechas → 6 casos de edad actual (aniversario, 29 de febrero, fecha anterior) |
| Tests | `ModeloDatosTests.cs` | 18 restricciones CHECK |
| Tests | `Api/FabricaApi.cs`, `Api/PerfilTests.cs` | Perfil con `edad = 30`; inválidos: edad 17, 81, en texto, sin edad |

Las migraciones anteriores no se editaron: son historia.

## 4. La migración

EF genera "borrar la fecha, crear `Edad` = 0 y el CHECK". En Neon ya había perfiles, así que el CHECK habría
fallado y la edad de esas personas se habría perdido. Se editó `Up()` para hacer, en una sola transacción:

1. Agregar `Edad` y `EdadRegistradaEn` con un valor temporal.
2. Convertir los perfiles existentes:
   ```sql
   UPDATE "PerfilesUsuario"
   SET "Edad" = LEAST(80, GREATEST(18, date_part('year', age(CURRENT_DATE, "FechaNacimiento"))::int)),
       "EdadRegistradaEn" = CURRENT_DATE;
   ```
3. Borrar `FechaNacimiento`.
4. Recién entonces crear `CK_PerfilesUsuario_Edad`.

`Down()` deshace todo y reconstruye una fecha aproximada con la misma edad (la fecha exacta ya no existe).

El script se revisó antes de aplicarlo con
`dotnet ef migrations script FactorActividadTresDecimales EdadEnLugarDeFecha ...`: el `UPDATE` sale antes del
`DROP COLUMN` y del `ADD CONSTRAINT`. El `DEFAULT DATE '-infinity'` que aparece es cómo Npgsql traduce el valor
temporal `new DateOnly(1, 1, 1)`; el `UPDATE` siguiente lo reemplaza en todas las filas.

## 5. Verificación

| Prueba | Resultado |
|---|---|
| `dotnet test` | **114 superadas, 0 fallidas** (salen 6 pruebas de fechas, entran 6 de edad actual) |
| `dotnet ef database update` | `Applying migration '20261126223824_EdadEnLugarDeFecha'` → `Done.` |
| Consola SQL de Neon (base `nutriapp`) | 2 perfiles convertidos: edad 30 y 23, `EdadRegistradaEn` = 2026-11-26 |
| Swagger · `GET /api/perfil` | 200 con `"edad": 23`, sin `fechaNacimiento` |
| Swagger · `GET /api/perfil/requerimiento` | Hombre, 23 años, 172 cm, 70 kg, moderada, mantener → TMB 1 665, **2 581 kcal** (700 + 1 075 − 115 + 5 = 1 665; × 1,55) |
| Swagger · esquema del `PUT` | El ejemplo muestra `"edad": 80`, el máximo del rango |

El perfil del plan (mujer, 30 años, 70 kg, 160 cm, ligera, bajar) sigue dando **1 410 kcal**.

## 6. Problemas encontrados

| Problema | Causa | Solución |
|---|---|---|
| `git log` de la rama nueva mostraba la ACT-08 arriba | La copia local no tenía el merge del PR #1 (`origin/main` desactualizado) | `git switch main`, `git pull`, `git switch ACT-09.1`, `git merge main` (fast-forward) |
| En la consola SQL de Neon: `relation "PerfilesUsuario" does not exist` | La consola estaba en la base `neondb` (la que Neon crea por defecto); la app usa `nutriapp` | Cambiar el desplegable de base de datos a `nutriapp` |

## 7. Fuera del repositorio

El diccionario de datos de la ACT-03 (`diccionario_datos.csv`) se actualizó en el entregable: la fila
`FechaNacimiento` pasa a `Edad` y `EdadRegistradaEn`, y `FactorActividad` queda como `numeric(4,3)`.
