"""ACT-07 · Genera las entidades (Domain) y la configuración de EF Core (Infrastructure) desde el modelo
de ACT-03 (TABLAS de generar_modelo.py), para que el DDL, el diccionario de datos y EF Core no se contradigan.

Uso:  python generar_entidades.py <ruta a la carpeta de ACT-03 con generar_modelo.py> <raíz del repositorio>
Ej.:  python datos/herramientas/generar_entidades.py ../ACT-03_Modelo_de_datos .
Sobrescribe: src/NutriApp.Domain/Entidades/*.cs y src/NutriApp.Infrastructure/Persistencia/{ConfiguracionModelo,NutriAppDbContext}.cs
"""
import re, sys
from pathlib import Path
sys.path.insert(0, str(Path(sys.argv[1]).resolve()))
from generar_modelo import TABLAS, CHECKS_TABLA, UNICOS, INDICES, AUTONUMERICAS

RAIZ = Path(sys.argv[2])
DOM = RAIZ / "src/NutriApp.Domain/Entidades"; DOM.mkdir(parents=True, exist_ok=True)
INF = RAIZ / "src/NutriApp.Infrastructure/Persistencia"; INF.mkdir(parents=True, exist_ok=True)

CLASE = {"GruposAlimento": "GrupoAlimento", "Alimentos": "Alimento", "Porciones": "Porcion", "Alergenos": "Alergeno",
         "AlimentoAlergeno": "AlimentoAlergeno", "TiposComida": "TipoComida", "AlimentoTipoComida": "AlimentoTipoComida",
         "Platos": "Plato", "PlatoIngredientes": "PlatoIngrediente", "PlatoTipoComida": "PlatoTipoComida",
         "PerfilesUsuario": "PerfilUsuario", "PerfilAlergeno": "PerfilAlergeno", "HistorialPeso": "HistorialPeso",
         "Recomendaciones": "Recomendacion", "RegistrosComida": "RegistroComida"}
NAV = {"GrupoCodigo": "Grupo", "AlimentoId": "Alimento", "AlergenoId": "Alergeno", "TipoComidaId": "TipoComida",
       "PlatoId": "Plato", "PerfilId": "Perfil", "RecomendacionId": "Recomendacion"}
# Qué pasa al borrar el padre
CASCADA = {("PerfilAlergeno", "PerfilId"), ("HistorialPeso", "PerfilId"), ("Recomendaciones", "PerfilId"),
           ("RegistrosComida", "PerfilId"), ("PlatoIngredientes", "PlatoId"), ("PlatoTipoComida", "PlatoId"),
           ("AlimentoAlergeno", "AlimentoId"), ("AlimentoTipoComida", "AlimentoId"), ("Porciones", "AlimentoId")}
SET_NULL = {("RegistrosComida", "RecomendacionId")}

def cs_tipo(sql, nulo):
    s = sql.lower()
    if s == "integer": t = "int"
    elif s.startswith("numeric"): t = "decimal"
    elif s == "boolean": t = "bool"
    elif s == "date": t = "DateOnly"
    elif s == "timestamptz": t = "DateTime"
    else: t = "string"
    return t + ("?" if nulo else "")

def opciones(o):
    """Separa por comas, pero un 'check:' se lleva el resto (su expresión puede tener comas)."""
    if not o:
        return []
    r, partes = [], o.split(",")
    for i, x in enumerate(partes):
        if x.strip().startswith("check:"):
            r.append(",".join(partes[i:]).strip())
            break
        r.append(x.strip())
    return r

def fks(cols):
    r = []
    for n, t, u, d, o in cols:
        for x in opciones(o):
            if x.startswith("fk:"):
                tabla, campo = x[3:].split(".")
                r.append((n, tabla, campo, "null" in opciones(o)))
    return r

tablas = {k: v for k, v in TABLAS.items() if k != "AspNetUsers"}

# ---------------- Domain
for tabla, (zona, desc, cols) in tablas.items():
    clase = CLASE[tabla]
    L = ["namespace NutriApp.Domain.Entidades;", "", "/// <summary>", f"/// {desc}", f"/// Tabla: \"{tabla}\" (ACT-03).", "/// </summary>",
         f"public class {clase}", "{"]
    for n, t, u, d, o in cols:
        ops = opciones(o)
        nulo = "null" in ops
        tipo = cs_tipo(t, nulo)
        unidad = f" [{u}]" if u else ""
        L.append(f"    /// <summary>{d}{unidad}</summary>".replace("&", "y").replace("<", "‹").replace(">", "›").replace("/// ‹summary›", "/// <summary>").replace("‹/summary›", "</summary>"))
        init = " = string.Empty;" if tipo == "string" else ""
        L.append(f"    public {tipo} {n} {{ get; set; }}{init}")
        L.append("")
    navs = [(n, ta, nul) for n, ta, c, nul in fks(cols) if ta != "AspNetUsers"]
    if navs:
        L.append("    // Navegación")
        for n, ta, nul in navs:
            L.append(f"    public {CLASE[ta]}{'?' if nul else ''} {NAV[n]} {{ get; set; }}{'' if nul else ' = null!;'}")
    while L[-1] == "": L.pop()
    L.append("}")
    (DOM / f"{clase}.cs").write_text("\n".join(L) + "\n", encoding="utf-8")

# ---------------- Infrastructure: configuración
C = ["// Generado desde el modelo de ACT-03 (generar_modelo.py · TABLAS). No editar a mano:",
     "// si cambia el modelo, se regenera para que DDL, diccionario y EF Core no se contradigan.",
     "using Microsoft.AspNetCore.Identity;", "using Microsoft.EntityFrameworkCore;", "using NutriApp.Domain.Entidades;", "",
     "namespace NutriApp.Infrastructure.Persistencia;", "", "internal static class ConfiguracionModelo", "{",
     "    public static void Aplicar(ModelBuilder mb)", "    {"]
for tabla, (zona, desc, cols) in tablas.items():
    clase = CLASE[tabla]
    pks = [n for n, t, u, d, o in cols if "pk" in opciones(o)]
    checks = []
    for n, t, u, d, o in cols:
        for x in opciones(o):
            if x.startswith("check:"):
                checks.append((f"CK_{tabla}_{n}", x[6:]))
    for i, ch in enumerate(CHECKS_TABLA.get(tabla, [])):
        expr = re.sub(r"^CHECK\s*\((.*)\)$", r"\1", ch.strip())
        checks.append((f"CK_{tabla}_AlimentoOPlato", expr))
    C.append(f"        mb.Entity<{clase}>(e =>")
    C.append("        {")
    if checks:
        C.append(f"            e.ToTable(\"{tabla}\", t =>")
        C.append("            {")
        for nom, expr in checks:
            C.append(f"                t.HasCheckConstraint(\"{nom}\", {cs_str(expr) if False else '@\"' + expr.replace(chr(34), chr(34)*2) + chr(34)});")
        C.append("            });")
    else:
        C.append(f"            e.ToTable(\"{tabla}\");")
    if len(pks) == 1:
        C.append(f"            e.HasKey(x => x.{pks[0]});")
    else:
        C.append(f"            e.HasKey(x => new {{ {', '.join('x.' + p for p in pks)} }});")
    for n, t, u, d, o in cols:
        ops = opciones(o)
        p = f"            e.Property(x => x.{n})"
        tl = t.lower()
        partes = []
        m = re.match(r"(varchar|char)\((\d+)\)", tl)
        if m:
            partes.append(f".HasMaxLength({m.group(2)})")
            if m.group(1) == "char": partes.append(f".HasColumnType(\"{tl}\")")
        elif tl.startswith("numeric") or tl in ("text", "date", "timestamptz"):
            partes.append(f".HasColumnType(\"{tl}\")")
        if "pk" in ops and n == "Id" and tl == "integer":
            partes.append(".UseIdentityByDefaultColumn()" if tabla in AUTONUMERICAS else ".ValueGeneratedNever()")
        if partes:
            C.append(p + "".join(partes) + ";")
        if "unique" in ops:
            C.append(f"            e.HasIndex(x => x.{n}).IsUnique();")
    for u in UNICOS.get(tabla, []):
        campos = re.findall(r'"(\w+)"', u)
        C.append(f"            e.HasIndex(x => new {{ {', '.join('x.' + c for c in campos)} }}).IsUnique();")
    for (ti, campos) in INDICES:
        if ti == tabla:
            cs = campos.split(",")
            C.append(f"            e.HasIndex(x => {('x.' + cs[0]) if len(cs) == 1 else 'new { ' + ', '.join('x.' + c for c in cs) + ' }'});")
    for n, ta, campo, nul in fks(cols):
        if ta == "AspNetUsers":
            C.append(f"            e.HasOne<IdentityUser>().WithOne().HasForeignKey<{clase}>(x => x.{n}).OnDelete(DeleteBehavior.Cascade);")
            continue
        borrado = "Cascade" if (tabla, n) in CASCADA else ("SetNull" if (tabla, n) in SET_NULL else "Restrict")
        C.append(f"            e.HasOne(x => x.{NAV[n]}).WithMany().HasForeignKey(x => x.{n}).OnDelete(DeleteBehavior.{borrado});")
    C.append("        });")
    C.append("")
C.pop()
C += ["    }", "}"]
(INF / "ConfiguracionModelo.cs").write_text("\n".join(C) + "\n", encoding="utf-8")

# DbContext
sets = "\n".join(f"    public DbSet<{CLASE[t]}> {t} => Set<{CLASE[t]}>();" for t in tablas)
(INF / "NutriAppDbContext.cs").write_text(f'''using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NutriApp.Domain.Entidades;

namespace NutriApp.Infrastructure.Persistencia;

/// <summary>
/// Base de datos de NutriApp: las tablas de ASP.NET Core Identity (AspNetUsers, …) más las 15 tablas
/// propias del modelo de ACT-03. Los nombres de tablas y columnas son los del modelo (PascalCase).
/// </summary>
public class NutriAppDbContext(DbContextOptions<NutriAppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{{
{sets}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {{
        base.OnModelCreating(modelBuilder);   // tablas de Identity
        ConfiguracionModelo.Aplicar(modelBuilder);
    }}
}}
''', encoding="utf-8")
print("ok", len(tablas))
