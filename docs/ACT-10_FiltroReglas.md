# ACT-10 · Filtro por reglas

**Sprint 3 · M3: Motor.** Para cada comida del día, el motor deja solo los alimentos y platos que cumplen
**todas** las reglas del perfil. El resultado es la lista de candidatos con la que trabajará el ranking (ACT-11).

- **Entregable del plan:** "Lista de alimentos candidatos por comida que cumplen todas las reglas del perfil".
- **Cambio respecto al plan:** la regla de **presupuesto** no se implementa, porque el vacío socioeconómico se
  quitó del proyecto (los datasets no tienen precios).
- **Resultado:** 20 perfiles variados × 4 comidas → siempre **3 o más candidatos** y **0 violaciones** de
  alérgenos, diabetes, hipertensión, tipo de comida o energía.

---

## 1. Las reglas, en orden

Cada alimento o plato se revisa en este orden. Si no cumple una regla, se descarta y se cuenta en esa regla
(`descartes`), así se puede explicar por qué quedó fuera.

| # | Regla (clave en `descartes`) | Se descarta si… | Fuente |
|---|---|---|---|
| 1 | `calidad_dato` | Plato excluido por ingredientes, con calidad nutricional "baja", marcado para revisar o sin kcal | ACT-06 |
| 2 | `tipo_comida` | No es apto para esa comida (`AlimentoTipoComida` / `PlatoTipoComida`) | ACT-06 |
| 3 | `alergeno` | Contiene un alérgeno de la persona (**exclusión total**) | ACT-05 |
| 4 | `alergeno_marca_sin_confirmar` | La persona tiene alguna alergia y es un producto de marca de cereales, snacks, azúcares, bebidas o lácteos, que ACT-06 marcó para confirmar "puede contener" en la etiqueta | ACT-06 |
| 5 | `diabetes_indice_glucemico` | Con diabetes: índice glucémico alto (≥ 70) | ISO 26642 |
| 6 | `diabetes_azucares` | Con diabetes: azúcares ≥ 15 g/100 g (≥ 7,5 g/100 ml en bebidas) o sin dato | Semáforo nutricional de Ecuador |
| 7 | `hipertension_sodio` | Con hipertensión: sodio ≥ 600 mg/100 g o sin dato | Acuerdo Ministerial 4832 |
| 8 | `energia` | Ni ajustando la porción queda cerca de la meta de la comida (ver sección 2) | ACT-05 |

Ningún número está en el código: todos salen de `parametros_motor.json` (secciones `comidas`, `reglas_salud`
y `catalogo`).

**Sin dato de IG con diabetes:** se permite (muchos alimentos no tienen IG medido), pero el candidato lleva
`IgSinDato = true` para que el ranking (ACT-11) lo deje después de los que sí tienen un IG bajo.

## 2. Energía: ajuste de la porción y tolerancia

Un plato de 485 kcal encaja en un almuerzo de 494 kcal, pero una manzana de 70 kcal no encaja en una merienda
de 141 kcal… a menos que sean dos. Por eso el filtro prueba varias cantidades de la porción típica y se queda
con la más cercana a la meta:

| Tipo | Multiplicadores probados |
|---|---|
| Alimento | ½ · 1 · 1½ · 2 porciones típicas |
| Plato | ½ · 1 · 1½ porciones |

Después aplica la tolerancia de ACT-05:

1. Se aceptan los que quedan a **±10 %** de la meta de la comida.
2. Si así quedan **menos de 3**, se amplía a **±15 %**.
3. Si aun así hay menos de 3, se devuelve lo que hay con `suficiente = false` y un aviso. **Nunca se relaja una
   regla de salud ni de alérgenos** para conseguir más opciones.

La lista sale ordenada por cercanía a la meta. Es un orden provisional: el definitivo lo da la similitud coseno
de ACT-11.

## 3. Alérgenos de los platos: dos redes de seguridad

Los alérgenos de un **alimento** vienen de `AlimentoAlergeno` (ACT-06). Los de un **plato** se calculan al
cargar el catálogo, sumando:

1. Los alérgenos de cada ingrediente enlazado a un alimento.
2. Los que nombran los **textos** de los ingredientes y el nombre del plato, con las palabras clave de ACT-06.
   Esto cubre los ingredientes que no se enlazaron a ningún alimento: 1 193 condimentos, 83 sin equivalente,
   86 sin dato nutricional, etc. Por ejemplo, "2 cucharadas de salsa de soja" (soya y gluten) o
   "3 cucharadas de pasta de maní" (maní, en la Guatita).

Además, las palabras clave se aplican también al **nombre** de cada alimento, como segunda red.

### Problemas encontrados y corregidos

| Hallazgo | Corrección |
|---|---|
| Las palabras de ACT-06 se hicieron para nombres de alimentos ecuatorianos; las recetas usan otro vocabulario (nata, parmesano, mascarpone, cerveza, salsa de soja, gambas, merluza, anchoas, mazapán, cacahuete…) | Se agregaron al final de cada patrón en `parametros_motor.json` |
| "Crema de avellana, leche y chocolate (Nusco)", Nutella y similares no tenían lácteos: ACT-06 tenía "crema de avellana" como excepción | Se quitó esa excepción |
| Caballa, jurel (pescado) y Pony Malta (gluten, por la cebada) no tenían su alérgeno | Detectados por la segunda red (nombre del alimento) |
| "Champiñones" contiene "piñon" y salía como fruto seco | El patrón pasó a `\bpinon` (inicio de palabra) |
| En una línea como "1 taza de leche de coco y 1/2 taza de leche", la excepción "leche de coco" anulaba toda la línea | En los **ingredientes** solo se ignora la parte del texto que es excepción; en el **nombre de un alimento** (un solo producto) la excepción anula la regla, como en ACT-06 |

Revisión independiente: para los 20 perfiles se buscó, con una lista simple de palabras (leche, queso, nata,
pescado, gamba, soja…), cada candidato contra los alérgenos de la persona. Todas las coincidencias fueron falsos
positivos de la lista simple: "pasta de maní" no lleva gluten y "mantequilla de maní" no lleva lácteos.

## 4. Diseño

| Pieza | Capa | Qué hace |
|---|---|---|
| `ParametrosFiltro` | Domain | Lee del JSON tolerancias, multiplicadores, umbrales y palabras clave |
| `CatalogoMotor` | Domain | Catálogo en memoria (997 alimentos y 471 platos) con alérgenos y comidas ya resueltos |
| `FiltroReglas` | Domain | Las reglas, el ajuste de la porción y la tolerancia. Sin base de datos: se prueba con datos en memoria |
| `IProveedorCatalogo` / `ProveedorCatalogoBd` | Domain / Infrastructure | Lee el catálogo de Neon una sola vez y lo guarda (lecturas simples de tablas, sin LINQ que PostgreSQL deba traducir) |
| `RecomendacionesController` | Api | `GET /api/recomendaciones/candidatos` |

Si se vuelve a ejecutar `cargar-datos`, hay que **reiniciar la API** para que lea el catálogo nuevo.

## 5. Endpoint

`GET /api/recomendaciones/candidatos?comida=almuerzo&limite=10` (con token)

| Parámetro | Valores | Por defecto |
|---|---|---|
| `comida` | desayuno, almuerzo, merienda, cena | las 4 |
| `limite` | 1 a 100 candidatos por comida | 10 |

| Respuesta | Cuándo |
|---|---|
| 200 | Candidatos de cada comida |
| 400 | `limite` fuera de 1–100 o `comida` desconocida |
| 401 | Sin token |
| 404 | La persona aún no tiene perfil |
| 409 | Con los años, la edad salió de 18–80 (ACT-09.1) |

Cada comida trae: `metaKcal`, `toleranciaPorcentaje`, `minKcal`–`maxKcal`, `totalCandidatos`, `suficiente`,
`aviso`, `descartes` y la lista de candidatos. Cada candidato trae la cantidad ya ajustada
(p. ej. "2 × 1/2 taza", "1 porción (331 g)"), kcal, % de la meta, macronutrientes, azúcares, sodio, IG y
**`reglas`**: las reglas que cumple, en palabras. Ese es el punto de partida de la explicación de ACT-12:

```
493 kcal: 100 % de la meta de almuerzo (494 kcal, margen ±10 %)
Apto para almuerzo
Sin lácteos
Índice glucémico 68 (menor que 70)
Azúcares 1,0 g por 100 g (menor que 15,0)
Sodio 48 mg por 100 g (menor que 600)
```

## 6. Resultados con 20 perfiles

Número de candidatos por comida (catálogo real). Son los mismos perfiles que usará el Hito 3 (ACT-14).

| Perfil | kcal/día | Desayuno | Almuerzo | Merienda | Cena |
|---|---|---|---|---|---|
| 01 Plan: F 30, ligera, bajar, sin lácteos | 1410 | 15 | 86 | 72 | 82 |
| 02 M 23, moderada, mantener | 2581 | 8 | 58 | 184 | 71 |
| 03 F 55, sedentaria, bajar, diabetes | 1200 | 33 | 104 | 61 | 129 |
| 04 M 60, sedentario, mantener, hipertensión | 2001 | 15 | 82 | 166 | 82 |
| 05 F 65, diabetes + hipertensión, sin gluten y lácteos | 1200 | 6 | 45 | 21 | 75 |
| 06 M 25, muy activo, subir | 3445 | 7 | 31 | 94 | 37 |
| 07 F 80, sedentaria, sin pescado ni mariscos | 1200 | 46 | 101 | 105 | 125 |
| 08 F 18, activa, mantener, sin maní | 2262 | 14 | 65 | 124 | 74 |
| 09 M 45, ligera, bajar, diabetes, sin huevo | 2049 | 5 | 51 | 48 | 57 |
| 10 F 40, moderada, mantener, sin gluten | 1932 | 4 | 51 | 85 | 46 |
| 11 M 35, activo, mantener, sin mariscos | 2971 | 9 | 36 | 100 | 56 |
| 12 F 28, ligera, subir, sin frutos secos ni maní | 1964 | 13 | 78 | 101 | 80 |
| 13 M 70, sedentario, bajar, diabetes + hipertensión | 1500 | 13 | 103 | 62 | 103 |
| 14 F 50, ligera, mantener, hipertensión, sin soya | 1745 | 17 | 75 | 120 | 107 |
| 15 M 19, moderada, subir, sin lácteos ni huevo | 2786 | 4 (±15 %) | 18 | 50 | 35 |
| 16 F 35, sedentaria, bajar, sin gluten, lácteos y huevo | 1414 | 9 | 60 | 44 | 60 |
| 17 M 52, moderada, bajar, hipertensión, sin pescado | 2495 | 9 | 44 | 100 | 62 |
| 18 F 62, ligera, mantener, diabetes, sin ajonjolí | 1592 | 13 | 90 | 67 | 110 |
| 19 M 30, muy activo, mantener, sin gluten | 3479 | 4 | 15 | 31 | 19 |
| 20 F 45, moderada, bajar, diabetes, sin lácteos | 1628 | 7 | 55 | 36 | 87 |

El **desayuno** es la comida más ajustada: hay pocos alimentos y platos de desayuno, y en los perfiles de
muchas calorías la meta es alta (hasta 870 kcal). ACT-11 debe tenerlo en cuenta al pedir "3 a 5 alternativas
sin repetir grupo".

En un perfil extremo (los 9 alérgenos + diabetes + hipertensión) el desayuno queda en 0 y la API lo dice con
`suficiente = false` y el aviso, sin relajar ninguna regla.

## 7. Pruebas

| Archivo | Pruebas | Qué cubre |
|---|---|---|
| `Motor/FiltroReglasTests.cs` | 20 | Cada regla por separado con un catálogo pequeño: tipo de comida, alérgenos (también en textos de ingredientes, "leche de coco", "nuez moscada", nata, parmesano), marca, diabetes (IG, azúcares, bebidas), hipertensión, calidad del dato, ajuste de la porción, ±10 % → ±15 %, aviso cuando no alcanza, textos de las reglas, parámetros del JSON |
| `Motor/FiltroCatalogoRealTests.cs` | 26 | Los 20 perfiles con el catálogo real (≥ 3 candidatos y 0 violaciones en cada comida), catálogo completo, platos del prototipo en el almuerzo del plan, Guatita con maní, segunda red (Nusco, caballa, Pony Malta) |
| `Api/RecomendacionesTests.cs` | 7 | 401, 404, perfil del plan (4 comidas, ≥ 3 candidatos, "Sin lácteos"), filtro por comida y límite, 400 por parámetros inválidos |

Apoyo: `Motor/CatalogoCsv.cs` lee los CSV de `datos/bd` (sin base de datos) y `Motor/PerfilesPrueba.cs` define
los 20 perfiles.

Total esperado: **114 + 53 = 167 pruebas**.
