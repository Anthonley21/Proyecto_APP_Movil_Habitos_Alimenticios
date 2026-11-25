# ACT-09 · Identity, JWT y perfil (Hito 2) — con seguridad desde el diseño

Plan: *"Configurar ASP.NET Core Identity con JWT y los endpoints de registro, login y perfil (GET, POST, PUT)
conectados al cálculo del requerimiento."*
Criterio: *"Usuario que se registra, inicia sesión, guarda su perfil y obtiene su requerimiento diario desde Swagger."*

Enfoque DevSecOps: NutriApp guarda **datos de salud** (diabetes, hipertensión, peso), que la Ley Orgánica de
Protección de Datos Personales del Ecuador (R.O. Supl. 459, 26-05-2021) trata como datos sensibles con
protección reforzada. Por eso la seguridad va en el código y se comprueba con pruebas automáticas en cada cambio.

## Endpoints

| Método | Ruta | Token | Qué hace |
|---|---|---|---|
| POST | `/api/auth/registro` | No | Crea la cuenta (exige aceptar el tratamiento de datos de salud) |
| POST | `/api/auth/login` | No | Devuelve un JWT válido por 30 min |
| GET | `/api/perfil` | Sí | Perfil propio |
| POST | `/api/perfil` | Sí | Crea el perfil propio (una vez) |
| PUT | `/api/perfil` | Sí | Modifica el perfil propio; el peso queda en el historial |
| GET | `/api/perfil/requerimiento` | Sí | Requerimiento diario (ACT-08) con el perfil propio |
| GET | `/api/cuenta/mis-datos` | Sí | Todo lo que la app guarda de la persona (derecho de acceso) |
| POST | `/api/cuenta/cerrar-sesion` | Sí | Invalida todos los tokens de la cuenta |
| POST | `/api/cuenta/eliminar` | Sí + contraseña | Borra la cuenta y todos sus datos (derecho de eliminación) |
| GET | `/api/salud/bd` | No | Conteos del catálogo (ACT-07) |

Ningún endpoint recibe un id de usuario: **el usuario sale siempre del token**.

## Medidas de seguridad y riesgo que cubren (OWASP API Security Top 10, 2023)

| Riesgo OWASP | Medida en NutriApp | Dónde |
|---|---|---|
| API1 · Acceso a objetos de otro usuario (BOLA) | No hay `/perfil/{id}`: el id sale del claim `sub` del token | `PerfilController`, `CuentaController` |
| API2 · Autenticación rota | Contraseñas con hash PBKDF2 + sal (Identity); mínimo 10 caracteres con mayúscula, minúscula, número y 4 distintos; bloqueo de 15 min tras 5 fallos; mismo mensaje para correo inexistente, clave incorrecta y cuenta bloqueada; hash señuelo para que el tiempo de respuesta no delate correos | `ConfiguracionSeguridad`, `AuthController` |
| API2 · Tokens | JWT HS256 de 30 min con clave de 256 bits fuera del repo; se validan firma, algoritmo (rechaza `alg: none`), emisor, audiencia y expiración; el sello de seguridad (`sst`) permite revocar: cerrar sesión o borrar la cuenta invalida todos los tokens al instante | `ServicioTokens`, `ConfiguracionSeguridad` |
| API3 · Exposición de propiedades | Se devuelven DTO, nunca entidades: no salen `UsuarioId`, hashes ni sellos. El token solo lleva el id (no correo ni datos de salud: un JWT se puede leer) | `Contratos.cs` |
| API4 · Consumo sin límite | 10 peticiones/min por IP en registro y login (429); longitudes máximas (contraseña ≤ 128, correo ≤ 254) | `ConfiguracionSeguridad`, `Contratos.cs` |
| API5 · Funciones sin autorización | Política por defecto: **todo** exige token salvo lo marcado `[AllowAnonymous]` | `ConfiguracionSeguridad` |
| API8 · Mala configuración | Errores genéricos (ProblemDetails) sin trazas; cabeceras `nosniff`, `DENY`, `no-referrer`, `no-store` y CSP en `/api`; sin cabecera `Server`; Swagger solo en desarrollo; HTTPS + HSTS en producción; sin CORS (la app móvil no lo necesita) | `Program.cs` |
| API9 · Inventario | Se eliminó el endpoint de ejemplo `WeatherForecast` de la plantilla | — |
| Datos de entrada | Validación con los mismos límites del motor (ACT-05/08) y de los CHECK de la base (ACT-03) | `PerfilController.Validar` |
| Registros (logs) | Nunca se registran correos, contraseñas, tokens ni datos de salud: solo el id del usuario. El SQL de EF Core no se registra | `AuthController`, `appsettings.json` |
| Secretos | Cadena de conexión y clave JWT en user-secrets (desarrollo) o variables de entorno (servidor). `appsettings.json` las tiene vacías | `appsettings.json` |
| Dependencias | `Microsoft.OpenApi` forzado a ≥ 2.7.5: las versiones 2.0–2.7.4 tienen la vulnerabilidad CVE-2026-49451 (GHSA-v5pm-xwqc-g5wc, denegación de servicio) | `NutriApp.Api.csproj` |
| LOPDP | Consentimiento explícito guardado con fecha; acceso a "mis datos"; eliminación de la cuenta en cascada | `AuthController`, `CuentaController` |

Pendiente para otras actividades: guardar el token en `SecureStorage` y la casilla de consentimiento en la app
(ACT-13); HTTPS real, `ForwardedHeaders` detrás del proxy y funciones de seguridad de GitHub (ACT-18).

## Cambio en la base: factor de actividad con 3 decimales

`FactorActividad` era `numeric(3,2)`: guardaba 1,375 como **1,38**, y el perfil del plan daba 1417 kcal en lugar
de 1410. Ahora es `numeric(4,3)` y requiere la migración `FactorActividadTresDecimales` (paso 4 abajo).

## Puesta en marcha

1. **Rama:** `git checkout main`, `git pull`, `git checkout -b act-09-identity`.
2. **Clave JWT** (cada integrante en su PC). Genera una clave aleatoria en PowerShell:
   ```powershell
   [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
   ```
   Clic derecho en **NutriApp.Api → Administrar secretos de usuario** y agrega `Jwt` junto a lo que ya hay:
   ```json
   {
     "ConnectionStrings": { "NutriApp": "Host=...;Database=nutriapp;Username=...;Password=...;SSL Mode=Require" },
     "Jwt": { "Clave": "LA_CLAVE_GENERADA" }
   }
   ```
   Sin esta clave la API no arranca (mensaje: *Falta 'Jwt:Clave'*).
3. **Compilar y probar:** `dotnet build` y `dotnet test` → todas en verde.
4. **Migración** (una sola persona, una sola vez):
   ```powershell
   dotnet ef migrations add FactorActividadTresDecimales --project src/NutriApp.Infrastructure --startup-project src/NutriApp.Api --output-dir Persistencia/Migraciones
   dotnet ef database update --project src/NutriApp.Infrastructure --startup-project src/NutriApp.Api
   ```
5. **Swagger:** `dotnet run --project src/NutriApp.Api` y abre `http://localhost:5222/swagger`.
   1. `POST /api/auth/registro` con `{"correo":"ana@ejemplo.com","contrasena":"ClaveSegura2026","aceptaTratamientoDatos":true}` → 201
   2. `POST /api/auth/login` → copia el `token`
   3. Botón **Authorize** (candado) → pega el token → Authorize
   4. `POST /api/perfil` con el perfil del plan (F, 160 cm, 70 kg, `ligero`, `bajar`, nacida hace 30 años) → 201
   5. `GET /api/perfil/requerimiento` → `kcalDia: 1410`  ← **criterio de ACT-09**

   También se puede probar sin Swagger con `src/NutriApp.Api/NutriApp.Api.http` (Visual Studio: botón *Send request*).
6. **Subir:** commit, `git push -u origin act-09-identity`, abrir el pull request y que el compañero lo revise.

## Pruebas automáticas (tests/NutriApp.Tests/Api)

Levantan la API completa en memoria (sin Neon): mismos controladores y la misma configuración de seguridad.

| Archivo | Casos | Qué comprueban |
|---|---|---|
| `AutenticacionTests` | 17 | Registro, consentimiento, 5 contraseñas débiles, correos inválidos, contraseña enorme, correo repetido sin revelarlo, token de 30 min sin datos personales, mismo mensaje en fallos, bloqueo tras 5 intentos, reinicio del contador |
| `ProteccionTests` | 24 | 7 endpoints sin token y con token falso → 401; firma alterada, contenido alterado, `alg: none` y token vencido → 401; cerrar sesión y borrar cuenta revocan el token; cada usuario solo ve su perfil; sin ids internos; cabeceras de seguridad; 429 por exceso de intentos |
| `PerfilTests` | 20 | **Criterio de ACT-09 (1410 kcal)**, GET/POST/PUT, 404 y 409, historial de un peso por día, 13 perfiles inválidos que no se guardan, mis datos |
