// Generado desde el modelo de ACT-03 (generar_modelo.py · TABLAS). No editar a mano:
// si cambia el modelo, se regenera para que DDL, diccionario y EF Core no se contradigan.
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NutriApp.Domain.Entidades;

namespace NutriApp.Infrastructure.Persistencia;

internal static class ConfiguracionModelo
{
    public static void Aplicar(ModelBuilder mb)
    {
        mb.Entity<GrupoAlimento>(e =>
        {
            e.ToTable("GruposAlimento");
            e.HasKey(x => x.Codigo);
            e.Property(x => x.Codigo).HasMaxLength(3);
            e.Property(x => x.Nombre).HasMaxLength(100);
        });

        mb.Entity<Alimento>(e =>
        {
            e.ToTable("Alimentos", t =>
            {
                t.HasCheckConstraint("CK_Alimentos_UnidadBase", @"""UnidadBase"" IN ('100 g','100 ml')");
                t.HasCheckConstraint("CK_Alimentos_KcalPor100g", @"""KcalPor100g"" >= 0");
                t.HasCheckConstraint("CK_Alimentos_ProteinaG", @"""ProteinaG"" >= 0");
                t.HasCheckConstraint("CK_Alimentos_CarbohidratosG", @"""CarbohidratosG"" >= 0");
                t.HasCheckConstraint("CK_Alimentos_GrasaG", @"""GrasaG"" >= 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Codigo).HasMaxLength(10);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.Property(x => x.Nombre).HasMaxLength(200);
            e.Property(x => x.NombreIngles).HasMaxLength(200);
            e.Property(x => x.GrupoCodigo).HasMaxLength(3);
            e.Property(x => x.UnidadBase).HasMaxLength(10);
            e.Property(x => x.KcalPor100g).HasColumnType("numeric(7,2)");
            e.Property(x => x.ProteinaG).HasColumnType("numeric(7,2)");
            e.Property(x => x.CarbohidratosG).HasColumnType("numeric(7,2)");
            e.Property(x => x.GrasaG).HasColumnType("numeric(7,2)");
            e.Property(x => x.FibraG).HasColumnType("numeric(7,2)");
            e.Property(x => x.AzucaresG).HasColumnType("numeric(7,2)");
            e.Property(x => x.CarbDisponibleG).HasColumnType("numeric(7,2)");
            e.Property(x => x.SodioMg).HasColumnType("numeric(8,2)");
            e.Property(x => x.GrasaSaturadaG).HasColumnType("numeric(7,2)");
            e.Property(x => x.ColesterolMg).HasColumnType("numeric(7,2)");
            e.Property(x => x.CalcioMg).HasColumnType("numeric(8,2)");
            e.Property(x => x.HierroMg).HasColumnType("numeric(7,2)");
            e.Property(x => x.PotasioMg).HasColumnType("numeric(8,2)");
            e.Property(x => x.ZincMg).HasColumnType("numeric(7,2)");
            e.Property(x => x.VitaminaCMg).HasColumnType("numeric(7,2)");
            e.Property(x => x.VitaminaAUg).HasColumnType("numeric(8,2)");
            e.Property(x => x.FolatosUg).HasColumnType("numeric(8,2)");
            e.Property(x => x.VitaminaB12Ug).HasColumnType("numeric(7,2)");
            e.Property(x => x.CategoriaIG).HasMaxLength(40);
            e.Property(x => x.CargaGlucemica100g).HasColumnType("numeric(6,1)");
            e.Property(x => x.CalidadAzucar).HasMaxLength(20);
            e.Property(x => x.CalidadIG).HasMaxLength(20);
            e.Property(x => x.Fuente).HasMaxLength(60);
            e.Property(x => x.NotaLimpieza).HasColumnType("text");
            e.HasIndex(x => x.GrupoCodigo);
            e.HasOne(x => x.Grupo).WithMany().HasForeignKey(x => x.GrupoCodigo).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Porcion>(e =>
        {
            e.ToTable("Porciones", t =>
            {
                t.HasCheckConstraint("CK_Porciones_Gramos", @"""Gramos"" > 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Descripcion).HasMaxLength(60);
            e.Property(x => x.Gramos).HasColumnType("numeric(7,1)");
            e.Property(x => x.Fuente).HasMaxLength(120);
            e.HasIndex(x => x.AlimentoId);
            e.HasOne(x => x.Alimento).WithMany().HasForeignKey(x => x.AlimentoId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Alergeno>(e =>
        {
            e.ToTable("Alergenos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(40);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        mb.Entity<AlimentoAlergeno>(e =>
        {
            e.ToTable("AlimentoAlergeno");
            e.HasKey(x => new { x.AlimentoId, x.AlergenoId });
            e.HasOne(x => x.Alimento).WithMany().HasForeignKey(x => x.AlimentoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Alergeno).WithMany().HasForeignKey(x => x.AlergenoId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<TipoComida>(e =>
        {
            e.ToTable("TiposComida", t =>
            {
                t.HasCheckConstraint("CK_TiposComida_PorcentajeEnergia", @"""PorcentajeEnergia"" BETWEEN 0 AND 100");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(20);
            e.HasIndex(x => x.Nombre).IsUnique();
            e.Property(x => x.PorcentajeEnergia).HasColumnType("numeric(4,1)");
        });

        mb.Entity<AlimentoTipoComida>(e =>
        {
            e.ToTable("AlimentoTipoComida");
            e.HasKey(x => new { x.AlimentoId, x.TipoComidaId });
            e.HasOne(x => x.Alimento).WithMany().HasForeignKey(x => x.AlimentoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TipoComida).WithMany().HasForeignKey(x => x.TipoComidaId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Plato>(e =>
        {
            e.ToTable("Platos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Nombre).HasMaxLength(200);
            e.Property(x => x.Categoria).HasMaxLength(100);
            e.Property(x => x.PaisOrigen).HasMaxLength(120);
            e.Property(x => x.Clasificacion).HasMaxLength(60);
            e.Property(x => x.KcalPorcion).HasColumnType("numeric(7,1)");
            e.Property(x => x.ProteinaGPorcion).HasColumnType("numeric(7,1)");
            e.Property(x => x.CarbohidratosGPorcion).HasColumnType("numeric(7,1)");
            e.Property(x => x.GrasaGPorcion).HasColumnType("numeric(7,1)");
            e.Property(x => x.FibraGPorcion).HasColumnType("numeric(7,1)");
            e.Property(x => x.AzucaresGPorcion).HasColumnType("numeric(7,1)");
            e.Property(x => x.SodioMgPorcion).HasColumnType("numeric(8,1)");
            e.Property(x => x.CargaGlucemicaPorcion).HasColumnType("numeric(6,1)");
            e.Property(x => x.CalidadNutricional).HasMaxLength(20);
            e.Property(x => x.CoberturaIngredientes).HasColumnType("numeric(4,2)");
            e.Property(x => x.IngredientesSinEquivalente).HasColumnType("text");
            e.Property(x => x.Tiempo).HasMaxLength(10);
            e.Property(x => x.Dificultad).HasMaxLength(20);
            e.Property(x => x.Url).HasColumnType("text");
            e.Property(x => x.ImagenUrl).HasColumnType("text");
        });

        mb.Entity<PlatoIngrediente>(e =>
        {
            e.ToTable("PlatoIngredientes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Texto).HasColumnType("text");
            e.Property(x => x.Gramos).HasColumnType("numeric(8,1)");
            e.Property(x => x.Estado).HasMaxLength(40);
            e.HasIndex(x => x.PlatoId);
            e.HasIndex(x => x.AlimentoId);
            e.HasOne(x => x.Plato).WithMany().HasForeignKey(x => x.PlatoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Alimento).WithMany().HasForeignKey(x => x.AlimentoId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<PlatoTipoComida>(e =>
        {
            e.ToTable("PlatoTipoComida");
            e.HasKey(x => new { x.PlatoId, x.TipoComidaId });
            e.HasOne(x => x.Plato).WithMany().HasForeignKey(x => x.PlatoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TipoComida).WithMany().HasForeignKey(x => x.TipoComidaId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<PerfilUsuario>(e =>
        {
            e.ToTable("PerfilesUsuario", t =>
            {
                t.HasCheckConstraint("CK_PerfilesUsuario_Sexo", @"""Sexo"" IN ('F','M')");
                t.HasCheckConstraint("CK_PerfilesUsuario_Edad", @"""Edad"" BETWEEN 18 AND 80");   // ACT-09.1
                t.HasCheckConstraint("CK_PerfilesUsuario_EstaturaCm", @"""EstaturaCm"" BETWEEN 100 AND 250");
                t.HasCheckConstraint("CK_PerfilesUsuario_PesoKg", @"""PesoKg"" BETWEEN 25 AND 300");
                t.HasCheckConstraint("CK_PerfilesUsuario_FactorActividad", @"""FactorActividad"" BETWEEN 1.2 AND 1.9");
                t.HasCheckConstraint("CK_PerfilesUsuario_Objetivo", @"""Objetivo"" IN ('bajar','mantener','subir')");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.UsuarioId).HasColumnType("text");
            e.HasIndex(x => x.UsuarioId).IsUnique();
            e.Property(x => x.Sexo).HasMaxLength(1).HasColumnType("char(1)");
            e.Property(x => x.Edad);                                               // ACT-09.1: edad en años (integer), no fecha
            e.Property(x => x.EdadRegistradaEn).HasColumnType("date");
            e.Property(x => x.EstaturaCm).HasColumnType("numeric(5,1)");
            e.Property(x => x.PesoKg).HasColumnType("numeric(5,1)");
            e.Property(x => x.FactorActividad).HasColumnType("numeric(4,3)");   // ACT-09: 1,375 y 1,725 necesitan 3 decimales
            e.Property(x => x.Objetivo).HasMaxLength(10);
            e.Property(x => x.ActualizadoEn).HasColumnType("timestamptz");
            e.HasOne<IdentityUser>().WithOne().HasForeignKey<PerfilUsuario>(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<PerfilAlergeno>(e =>
        {
            e.ToTable("PerfilAlergeno");
            e.HasKey(x => new { x.PerfilId, x.AlergenoId });
            e.HasOne(x => x.Perfil).WithMany().HasForeignKey(x => x.PerfilId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Alergeno).WithMany().HasForeignKey(x => x.AlergenoId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<HistorialPeso>(e =>
        {
            e.ToTable("HistorialPeso", t =>
            {
                t.HasCheckConstraint("CK_HistorialPeso_PesoKg", @"""PesoKg"" BETWEEN 25 AND 300");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Fecha).HasColumnType("date");
            e.Property(x => x.PesoKg).HasColumnType("numeric(5,1)");
            e.HasIndex(x => new { x.PerfilId, x.Fecha }).IsUnique();
            e.HasOne(x => x.Perfil).WithMany().HasForeignKey(x => x.PerfilId).OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Recomendacion>(e =>
        {
            e.ToTable("Recomendaciones", t =>
            {
                t.HasCheckConstraint("CK_Recomendaciones_Posicion", @"""Posicion"" BETWEEN 1 AND 5");
                t.HasCheckConstraint("CK_Recomendaciones_AlimentoOPlato", @"(""AlimentoId"" IS NULL) <> (""PlatoId"" IS NULL)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Fecha).HasColumnType("date");
            e.Property(x => x.Similitud).HasColumnType("numeric(5,4)");
            e.Property(x => x.Gramos).HasColumnType("numeric(7,1)");
            e.Property(x => x.Kcal).HasColumnType("numeric(7,1)");
            e.Property(x => x.Explicacion).HasColumnType("text");
            e.Property(x => x.CreadoEn).HasColumnType("timestamptz");
            e.HasIndex(x => new { x.PerfilId, x.Fecha });
            e.HasOne(x => x.Perfil).WithMany().HasForeignKey(x => x.PerfilId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TipoComida).WithMany().HasForeignKey(x => x.TipoComidaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Alimento).WithMany().HasForeignKey(x => x.AlimentoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Plato).WithMany().HasForeignKey(x => x.PlatoId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<RegistroComida>(e =>
        {
            e.ToTable("RegistrosComida", t =>
            {
                t.HasCheckConstraint("CK_RegistrosComida_Gramos", @"""Gramos"" > 0");
                t.HasCheckConstraint("CK_RegistrosComida_AlimentoOPlato", @"(""AlimentoId"" IS NULL) <> (""PlatoId"" IS NULL)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityByDefaultColumn();
            e.Property(x => x.Fecha).HasColumnType("date");
            e.Property(x => x.Gramos).HasColumnType("numeric(7,1)");
            e.Property(x => x.Kcal).HasColumnType("numeric(7,1)");
            e.Property(x => x.ProteinaG).HasColumnType("numeric(7,1)");
            e.Property(x => x.CarbohidratosG).HasColumnType("numeric(7,1)");
            e.Property(x => x.GrasaG).HasColumnType("numeric(7,1)");
            e.Property(x => x.CreadoEn).HasColumnType("timestamptz");
            e.HasIndex(x => new { x.PerfilId, x.Fecha });
            e.HasOne(x => x.Perfil).WithMany().HasForeignKey(x => x.PerfilId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TipoComida).WithMany().HasForeignKey(x => x.TipoComidaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Alimento).WithMany().HasForeignKey(x => x.AlimentoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Plato).WithMany().HasForeignKey(x => x.PlatoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Recomendacion).WithMany().HasForeignKey(x => x.RecomendacionId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
