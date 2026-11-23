# ACT-08 · Cálculo del requerimiento (Sprint 2)

Plan: *"Programar el cálculo del requerimiento: tasa metabólica basal, gasto total según la actividad,
ajuste por objetivo y macronutrientes por comida."*
Criterio: *"Cálculo que, para el perfil de ejemplo (mujer, 30 años, 70 kg, 160 cm, actividad ligera),
devuelve ≈ 1 410 kcal/día."*

## Dónde está

| Archivo | Qué hace |
|---|---|
| `src/NutriApp.Domain/Motor/parametros_motor.json` | Parámetros de ACT-05, incrustados en la DLL de Domain (un solo origen) |
| `src/NutriApp.Domain/Motor/ParametrosMotor.cs` | Lee y valida el JSON (macros y comidas suman 100 %) |
| `src/NutriApp.Domain/Motor/CalculadoraRequerimiento.cs` | El cálculo |
| `tests/NutriApp.Tests/CalculadoraRequerimientoTests.cs` | 48 casos de prueba |
| `tests/NutriApp.Tests/ReglasNutricionalesTests.cs` | El criterio de ACT-08, ahora llamando al servicio |

Domain no usa ningún paquete: el cálculo no depende de la base ni de la API.

## Pasos del cálculo

| Paso | Fórmula | Caso del plan |
|---|---|---|
| 1. TMB (Mifflin-St Jeor) | 10·peso + 6,25·estatura − 5·edad + 5 (M) / −161 (F) | 700 + 1000 − 150 − 161 = **1389** |
| 2. Gasto total | TMB × factor (1,2 · 1,375 · 1,55 · 1,725 · 1,9) | 1389 × 1,375 = **1910** |
| 3. Objetivo | bajar −500 · mantener 0 · subir +300 | 1909,875 − 500 = **1410** |
| 4. Mínimo seguro | nunca menos de 1200 (F) / 1500 (M) | no aplica |
| 5. Macros del día | 50 % carbohidratos, 20 % proteína, 30 % grasa (4/4/9 kcal/g) | 176,3 g · 70,5 g · 47,0 g |
| 6. Por comida | desayuno 25 % · almuerzo 35 % · merienda 10 % · cena 30 % | 352,5 · 493,5 · 141 · 423 kcal |

Además devuelve el tope de azúcares libres (10 % de la energía, OMS): 35,3 g para el caso del plan.

Redondeo: kcal del día al entero; comidas y gramos a 1 decimal; 0,5 sube (no el redondeo bancario de .NET).
Las comidas se calculan sobre las kcal ya redondeadas, así que suman exactamente el total.

## Cómo se usa (lo usará la API en ACT-09)

```csharp
using NutriApp.Domain.Motor;

// Con datos sueltos
var r = CalculadoraRequerimiento.Calcular(new DatosRequerimiento("F", 30, 70, 160, 1.375, "bajar"));
// r.KcalDia == 1410, r.Comidas[1].Kcal == 493.5 (almuerzo)

// Con el perfil guardado en la base (calcula la edad a la fecha)
var r2 = CalculadoraRequerimiento.Calcular(perfil, DateOnly.FromDateTime(DateTime.Today));
```

Si un dato está fuera de rango lanza `ArgumentException` con el motivo en español (la API lo devolverá como
error 400): sexo distinto de F/M, edad fuera de 18–80, peso fuera de 25–300 kg, estatura fuera de 100–250 cm,
factor fuera de 1,2–1,9 u objetivo distinto de bajar/mantener/subir. Son los mismos límites que los CHECK de
la tabla `PerfilesUsuario`.

## Qué comprueban las pruebas

- El caso del plan paso a paso (TMB, gasto, kcal, macros, azúcares y las 4 comidas).
- 5 perfiles más, calculados a mano (hombres y mujeres, los 3 objetivos, varios factores).
- El mínimo de 1200/1500 kcal.
- Que las comidas sumen el total, que los macros den las kcal y que bajar < mantener < subir.
- Los valores límite permitidos y 12 datos inválidos que deben rechazarse.
- La edad cumplida (incluido el 29 de febrero) y el cálculo desde un `PerfilUsuario`.
- Que el JSON incrustado tenga los valores de ACT-05 y que su reparto coincida con `datos/bd/TiposComida.csv`.
