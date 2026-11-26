using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriApp.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class EdadEnLugarDeFecha : Migration
    {
        // ACT-09.1 · Reemplaza SOLO los métodos Up() y Down() de la migración que generó
// "dotnet ef migrations add EdadEnLugarDeFecha". No toques el archivo .Designer.cs
// ni NutriAppDbContextModelSnapshot.cs: esos los genera EF y ya quedan bien.

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Columnas nuevas (con valor temporal para las filas que ya existen)
            migrationBuilder.AddColumn<int>(
                name: "Edad",
                table: "PerfilesUsuario",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EdadRegistradaEn",
                table: "PerfilesUsuario",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // 2. Convertir los perfiles existentes: edad cumplida hoy, registrada hoy (siempre dentro de 18–80)
            migrationBuilder.Sql(@"
                UPDATE ""PerfilesUsuario""
                SET ""Edad"" = LEAST(80, GREATEST(18, date_part('year', age(CURRENT_DATE, ""FechaNacimiento""))::int)),
                    ""EdadRegistradaEn"" = CURRENT_DATE;");

            // 3. Ya no se guarda la fecha de nacimiento (minimización de datos, LOPDP)
            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "PerfilesUsuario");

            // 4. Recién ahora la restricción: todas las filas ya tienen una edad válida
            migrationBuilder.AddCheckConstraint(
                name: "CK_PerfilesUsuario_Edad",
                table: "PerfilesUsuario",
                sql: "\"Edad\" BETWEEN 18 AND 80");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PerfilesUsuario_Edad",
                table: "PerfilesUsuario");

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaNacimiento",
                table: "PerfilesUsuario",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // Aproximación: la fecha exacta no se guardó; se reconstruye una fecha con la misma edad
            migrationBuilder.Sql(@"
                UPDATE ""PerfilesUsuario""
                SET ""FechaNacimiento"" = (""EdadRegistradaEn"" - make_interval(years => ""Edad""))::date;");

            migrationBuilder.DropColumn(
                name: "Edad",
                table: "PerfilesUsuario");

            migrationBuilder.DropColumn(
                name: "EdadRegistradaEn",
                table: "PerfilesUsuario");
        }

    }
}
