using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriApp.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class FactorActividadTresDecimales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "FactorActividad",
                table: "PerfilesUsuario",
                type: "numeric(4,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(3,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "FactorActividad",
                table: "PerfilesUsuario",
                type: "numeric(3,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(4,3)");
        }
    }
}
