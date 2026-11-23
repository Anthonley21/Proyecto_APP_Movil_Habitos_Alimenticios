# ACT-07 · Base de datos (Sprint 2)

Plan: *"Programar las entidades y el DbContext con EF Core + Npgsql, generar la migración inicial y cargar
los CSV en PostgreSQL con un script repetible."*
Criterio: *"Base nutriapp con el catálogo cargado y conteos verificados contra los CSV."*

## Qué trae este paquete (copiar en la raíz del repositorio, respetando las carpetas)

| Ruta | Qué es |
|---|---|
| `src/NutriApp.Domain/Entidades/*.cs` | 15 entidades, una por tabla del modelo de ACT-03 |
| `src/NutriApp.Infrastructure/Persistencia/NutriAppDbContext.cs` | El `DbContext` (incluye las tablas de ASP.NET Core Identity) |
| `src/NutriApp.Infrastructure/Persistencia/ConfiguracionModelo.cs` | Tipos de columna, claves, 23 claves foráneas, 17 CHECK e índices |
| `src/NutriApp.Infrastructure/Datos/CargadorCatalogo.cs` | Carga repetible de los CSV con verificación de conteos |
| `src/NutriApp.Infrastructure/DependencyInjection.cs` | Registra la base en la API |
| `src/NutriApp.Api/Program.cs` | **Reemplaza** el de la plantilla: agrega la base, el comando `cargar-datos` y `/api/salud/bd` |
| `src/NutriApp.Api/appsettings.json` | **Reemplaza** el de la plantilla: agrega `ConnectionStrings:NutriApp` vacío |
| `tests/NutriApp.Tests/*.cs` | 5 pruebas: el modelo coincide con ACT-03 y el caso ≈ 1410 kcal de ACT-08 |
| `datos/bd/*.csv` | Catálogo de ACT-06 (10 tablas, 10 076 filas) |
| `datos/bd/cargar_catalogo.sql` | La misma carga, para quien prefiera `psql` |
| `datos/herramientas/generar_entidades.py` | Regenera entidades y configuración si cambia el modelo de ACT-03 |

Borra los archivos de plantilla que ya no sirven: `src/NutriApp.Domain/Class1.cs`,
`src/NutriApp.Infrastructure/Class1.cs` y `tests/NutriApp.Tests/UnitTest1.cs`.

## Pasos (una sola vez)

En la raíz del repositorio (donde está `NutriApp.slnx`):

```powershell
# 1. Paquetes
dotnet add src/NutriApp.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/NutriApp.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/NutriApp.Api package Microsoft.EntityFrameworkCore.Design
dotnet tool install --global dotnet-ef

# 2. Compilar y probar (no necesita base de datos)
dotnet build
dotnet test
```

**3. Crear la base `nutriapp` en Neon** (Neon → tu proyecto → Databases → New database → `nutriapp`).
En *Connection details* desactiva **Connection pooling** (usa el host sin `-pooler` para migrar y cargar) y
arma la cadena en formato Npgsql (no acepta la forma `postgresql://...`):

```
Host=ep-xxxx.us-east-2.aws.neon.tech;Database=nutriapp;Username=TU_USUARIO;Password=TU_CLAVE;SSL Mode=Require
```

```powershell
# 4. Guardar la cadena como secreto local (NO va en appsettings.json ni en GitHub)
dotnet user-secrets init --project src/NutriApp.Api
dotnet user-secrets set "ConnectionStrings:NutriApp" "Host=...;Database=nutriapp;Username=...;Password=...;SSL Mode=Require" --project src/NutriApp.Api

# 5. Migración inicial (se guarda en Infrastructure/Persistencia/Migraciones y SÍ se sube a GitHub)
dotnet ef migrations add Inicial --project src/NutriApp.Infrastructure --startup-project src/NutriApp.Api --output-dir Persistencia/Migraciones

# 6. Crear las tablas en Neon
dotnet ef database update --project src/NutriApp.Infrastructure --startup-project src/NutriApp.Api

# 7. Cargar el catálogo (repetible: se puede ejecutar las veces que haga falta)
dotnet run --project src/NutriApp.Api -- cargar-datos datos/bd
```

Salida esperada del paso 7:

```
Tabla                    CSV    Base  Estado
GruposAlimento            11      11  OK
TiposComida                4       4  OK
Alergenos                  9       9  OK
Alimentos                997     997  OK
Porciones                997     997  OK
AlimentoAlergeno         330     330  OK
AlimentoTipoComida      1599    1599  OK
Platos                   471     471  OK
PlatoIngredientes       4925    4925  OK
PlatoTipoComida          733     733  OK
Catálogo cargado: 10076 filas en 10 tablas.
```

**8. Comprobar desde la API:** `dotnet run --project src/NutriApp.Api` y abre
`https://localhost:PUERTO/api/salud/bd` (el puerto sale en la consola). Debe responder
`{"alimentos":997,"aptosMotor":997,"platos":471,"porciones":997,"alergenosAsignados":330}`.

## Cómo funciona la carga repetible

En **una transacción**: comprueba que estén los 10 CSV y las tablas → vacía el catálogo
(`TRUNCATE … RESTART IDENTITY`) → carga cada CSV con `COPY` en orden de dependencias → ajusta el contador
de `Porciones` → compara filas de la base con registros del CSV. Si algo falla o un conteo no coincide, hace
ROLLBACK y la base queda como estaba. Si ya hay datos de usuarios que apuntan al catálogo (recomendaciones,
registros de comida, alérgenos de un perfil), **no carga nada** salvo que se agregue `--forzar`.

## Decisiones

- Nombres de tablas y columnas iguales al modelo de ACT-03 (PascalCase), así el diccionario de datos sigue
  siendo válido.
- `Id` autonumérico solo en las tablas que llena la app (perfiles, historial, recomendaciones, registros,
  porciones). Alimentos y platos conservan el Id de los CSV (EC0001 → 1) para que los datos sean trazables.
- Al borrar un perfil se borran sus datos (historial, recomendaciones, registros); el catálogo nunca se
  borra en cascada desde los datos de usuarios.
- La migración la genera EF Core a partir de este código; por eso el DDL de ACT-03 queda como documentación
  y la fuente de verdad de la base pasa a ser la migración.

## Qué se verificó

- `NutriApp.Domain` compila. El resto del código no tiene errores de sintaxis.
- No se pudo compilar Infrastructure, Api ni Tests contra EF Core/Npgsql en el entorno donde se preparó,
  porque no había acceso a NuGet. Si `dotnet build` muestra un error, se corrige.
- Los 10 CSV se cargaron dos veces seguidas en un PostgreSQL 16 con el esquema de ACT-03
  (`cargar_catalogo.sql`): mismos conteos las dos veces, ninguna restricción violada, y la siguiente porción
  que se inserta recibe el Id 998.
