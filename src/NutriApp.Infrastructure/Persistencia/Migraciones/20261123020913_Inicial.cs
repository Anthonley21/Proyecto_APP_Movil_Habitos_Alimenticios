using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NutriApp.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alergenos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alergenos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GruposAlimento",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposAlimento", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "Platos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Categoria = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaisOrigen = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    EsEcuatoriana = table.Column<bool>(type: "boolean", nullable: false),
                    Clasificacion = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Raciones = table.Column<int>(type: "integer", nullable: true),
                    PorcionG = table.Column<int>(type: "integer", nullable: true),
                    KcalPorcion = table.Column<decimal>(type: "numeric(7,1)", nullable: true),
                    ProteinaGPorcion = table.Column<decimal>(type: "numeric(7,1)", nullable: true),
                    CarbohidratosGPorcion = table.Column<decimal>(type: "numeric(7,1)", nullable: true),
                    GrasaGPorcion = table.Column<decimal>(type: "numeric(7,1)", nullable: true),
                    FibraGPorcion = table.Column<decimal>(type: "numeric(7,1)", nullable: true),
                    AzucaresGPorcion = table.Column<decimal>(type: "numeric(7,1)", nullable: true),
                    SodioMgPorcion = table.Column<decimal>(type: "numeric(8,1)", nullable: true),
                    IndiceGlucemico = table.Column<int>(type: "integer", nullable: true),
                    CargaGlucemicaPorcion = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    CalidadNutricional = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CoberturaIngredientes = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    Revisar = table.Column<bool>(type: "boolean", nullable: false),
                    IngredientesSinEquivalente = table.Column<string>(type: "text", nullable: true),
                    Tiempo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Dificultad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    ImagenUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposComida",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PorcentajeEnergia = table.Column<decimal>(type: "numeric(4,1)", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposComida", x => x.Id);
                    table.CheckConstraint("CK_TiposComida_PorcentajeEnergia", "\"PorcentajeEnergia\" BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfilesUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    Sexo = table.Column<string>(type: "char(1)", maxLength: 1, nullable: false),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    EstaturaCm = table.Column<decimal>(type: "numeric(5,1)", nullable: false),
                    PesoKg = table.Column<decimal>(type: "numeric(5,1)", nullable: false),
                    FactorActividad = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    Objetivo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TieneDiabetes = table.Column<bool>(type: "boolean", nullable: false),
                    TieneHipertension = table.Column<bool>(type: "boolean", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilesUsuario", x => x.Id);
                    table.CheckConstraint("CK_PerfilesUsuario_EstaturaCm", "\"EstaturaCm\" BETWEEN 100 AND 250");
                    table.CheckConstraint("CK_PerfilesUsuario_FactorActividad", "\"FactorActividad\" BETWEEN 1.2 AND 1.9");
                    table.CheckConstraint("CK_PerfilesUsuario_Objetivo", "\"Objetivo\" IN ('bajar','mantener','subir')");
                    table.CheckConstraint("CK_PerfilesUsuario_PesoKg", "\"PesoKg\" BETWEEN 25 AND 300");
                    table.CheckConstraint("CK_PerfilesUsuario_Sexo", "\"Sexo\" IN ('F','M')");
                    table.ForeignKey(
                        name: "FK_PerfilesUsuario_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alimentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NombreIngles = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GrupoCodigo = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    UnidadBase = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    KcalPor100g = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    ProteinaG = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    CarbohidratosG = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    GrasaG = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    FibraG = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    AzucaresG = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    CarbDisponibleG = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    SodioMg = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    GrasaSaturadaG = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    ColesterolMg = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    CalcioMg = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    HierroMg = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    PotasioMg = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    ZincMg = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    VitaminaCMg = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    VitaminaAUg = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    FolatosUg = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    VitaminaB12Ug = table.Column<decimal>(type: "numeric(7,2)", nullable: true),
                    IndiceGlucemico = table.Column<int>(type: "integer", nullable: true),
                    CategoriaIG = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CargaGlucemica100g = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    CalidadAzucar = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CalidadIG = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EsProductoDeMarca = table.Column<bool>(type: "boolean", nullable: false),
                    AptoMotor = table.Column<bool>(type: "boolean", nullable: false),
                    Revisar = table.Column<bool>(type: "boolean", nullable: false),
                    Fuente = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    NotaLimpieza = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alimentos", x => x.Id);
                    table.CheckConstraint("CK_Alimentos_CarbohidratosG", "\"CarbohidratosG\" >= 0");
                    table.CheckConstraint("CK_Alimentos_GrasaG", "\"GrasaG\" >= 0");
                    table.CheckConstraint("CK_Alimentos_KcalPor100g", "\"KcalPor100g\" >= 0");
                    table.CheckConstraint("CK_Alimentos_ProteinaG", "\"ProteinaG\" >= 0");
                    table.CheckConstraint("CK_Alimentos_UnidadBase", "\"UnidadBase\" IN ('100 g','100 ml')");
                    table.ForeignKey(
                        name: "FK_Alimentos_GruposAlimento_GrupoCodigo",
                        column: x => x.GrupoCodigo,
                        principalTable: "GruposAlimento",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatoTipoComida",
                columns: table => new
                {
                    PlatoId = table.Column<int>(type: "integer", nullable: false),
                    TipoComidaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatoTipoComida", x => new { x.PlatoId, x.TipoComidaId });
                    table.ForeignKey(
                        name: "FK_PlatoTipoComida_Platos_PlatoId",
                        column: x => x.PlatoId,
                        principalTable: "Platos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlatoTipoComida_TiposComida_TipoComidaId",
                        column: x => x.TipoComidaId,
                        principalTable: "TiposComida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialPeso",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PerfilId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    PesoKg = table.Column<decimal>(type: "numeric(5,1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPeso", x => x.Id);
                    table.CheckConstraint("CK_HistorialPeso_PesoKg", "\"PesoKg\" BETWEEN 25 AND 300");
                    table.ForeignKey(
                        name: "FK_HistorialPeso_PerfilesUsuario_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PerfilAlergeno",
                columns: table => new
                {
                    PerfilId = table.Column<int>(type: "integer", nullable: false),
                    AlergenoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilAlergeno", x => new { x.PerfilId, x.AlergenoId });
                    table.ForeignKey(
                        name: "FK_PerfilAlergeno_Alergenos_AlergenoId",
                        column: x => x.AlergenoId,
                        principalTable: "Alergenos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PerfilAlergeno_PerfilesUsuario_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlimentoAlergeno",
                columns: table => new
                {
                    AlimentoId = table.Column<int>(type: "integer", nullable: false),
                    AlergenoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlimentoAlergeno", x => new { x.AlimentoId, x.AlergenoId });
                    table.ForeignKey(
                        name: "FK_AlimentoAlergeno_Alergenos_AlergenoId",
                        column: x => x.AlergenoId,
                        principalTable: "Alergenos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlimentoAlergeno_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlimentoTipoComida",
                columns: table => new
                {
                    AlimentoId = table.Column<int>(type: "integer", nullable: false),
                    TipoComidaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlimentoTipoComida", x => new { x.AlimentoId, x.TipoComidaId });
                    table.ForeignKey(
                        name: "FK_AlimentoTipoComida_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlimentoTipoComida_TiposComida_TipoComidaId",
                        column: x => x.TipoComidaId,
                        principalTable: "TiposComida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlatoIngredientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    PlatoId = table.Column<int>(type: "integer", nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    Texto = table.Column<string>(type: "text", nullable: false),
                    AlimentoId = table.Column<int>(type: "integer", nullable: true),
                    Gramos = table.Column<decimal>(type: "numeric(8,1)", nullable: true),
                    Estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatoIngredientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatoIngredientes_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatoIngredientes_Platos_PlatoId",
                        column: x => x.PlatoId,
                        principalTable: "Platos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Porciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AlimentoId = table.Column<int>(type: "integer", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Gramos = table.Column<decimal>(type: "numeric(7,1)", nullable: false),
                    EsPredeterminada = table.Column<bool>(type: "boolean", nullable: false),
                    Fuente = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Porciones", x => x.Id);
                    table.CheckConstraint("CK_Porciones_Gramos", "\"Gramos\" > 0");
                    table.ForeignKey(
                        name: "FK_Porciones_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Recomendaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PerfilId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    TipoComidaId = table.Column<int>(type: "integer", nullable: false),
                    AlimentoId = table.Column<int>(type: "integer", nullable: true),
                    PlatoId = table.Column<int>(type: "integer", nullable: true),
                    Posicion = table.Column<int>(type: "integer", nullable: false),
                    Similitud = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    Gramos = table.Column<decimal>(type: "numeric(7,1)", nullable: false),
                    Kcal = table.Column<decimal>(type: "numeric(7,1)", nullable: false),
                    Explicacion = table.Column<string>(type: "text", nullable: false),
                    Elegida = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recomendaciones", x => x.Id);
                    table.CheckConstraint("CK_Recomendaciones_AlimentoOPlato", "(\"AlimentoId\" IS NULL) <> (\"PlatoId\" IS NULL)");
                    table.CheckConstraint("CK_Recomendaciones_Posicion", "\"Posicion\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_Recomendaciones_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recomendaciones_PerfilesUsuario_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Recomendaciones_Platos_PlatoId",
                        column: x => x.PlatoId,
                        principalTable: "Platos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recomendaciones_TiposComida_TipoComidaId",
                        column: x => x.TipoComidaId,
                        principalTable: "TiposComida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosComida",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PerfilId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    TipoComidaId = table.Column<int>(type: "integer", nullable: false),
                    AlimentoId = table.Column<int>(type: "integer", nullable: true),
                    PlatoId = table.Column<int>(type: "integer", nullable: true),
                    RecomendacionId = table.Column<int>(type: "integer", nullable: true),
                    Gramos = table.Column<decimal>(type: "numeric(7,1)", nullable: false),
                    Kcal = table.Column<decimal>(type: "numeric(7,1)", nullable: false),
                    ProteinaG = table.Column<decimal>(type: "numeric(7,1)", nullable: false),
                    CarbohidratosG = table.Column<decimal>(type: "numeric(7,1)", nullable: false),
                    GrasaG = table.Column<decimal>(type: "numeric(7,1)", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosComida", x => x.Id);
                    table.CheckConstraint("CK_RegistrosComida_AlimentoOPlato", "(\"AlimentoId\" IS NULL) <> (\"PlatoId\" IS NULL)");
                    table.CheckConstraint("CK_RegistrosComida_Gramos", "\"Gramos\" > 0");
                    table.ForeignKey(
                        name: "FK_RegistrosComida_Alimentos_AlimentoId",
                        column: x => x.AlimentoId,
                        principalTable: "Alimentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosComida_PerfilesUsuario_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "PerfilesUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrosComida_Platos_PlatoId",
                        column: x => x.PlatoId,
                        principalTable: "Platos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosComida_Recomendaciones_RecomendacionId",
                        column: x => x.RecomendacionId,
                        principalTable: "Recomendaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RegistrosComida_TiposComida_TipoComidaId",
                        column: x => x.TipoComidaId,
                        principalTable: "TiposComida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alergenos_Nombre",
                table: "Alergenos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlimentoAlergeno_AlergenoId",
                table: "AlimentoAlergeno",
                column: "AlergenoId");

            migrationBuilder.CreateIndex(
                name: "IX_Alimentos_Codigo",
                table: "Alimentos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alimentos_GrupoCodigo",
                table: "Alimentos",
                column: "GrupoCodigo");

            migrationBuilder.CreateIndex(
                name: "IX_AlimentoTipoComida_TipoComidaId",
                table: "AlimentoTipoComida",
                column: "TipoComidaId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPeso_PerfilId_Fecha",
                table: "HistorialPeso",
                columns: new[] { "PerfilId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfilAlergeno_AlergenoId",
                table: "PerfilAlergeno",
                column: "AlergenoId");

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesUsuario_UsuarioId",
                table: "PerfilesUsuario",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatoIngredientes_AlimentoId",
                table: "PlatoIngredientes",
                column: "AlimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatoIngredientes_PlatoId",
                table: "PlatoIngredientes",
                column: "PlatoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatoTipoComida_TipoComidaId",
                table: "PlatoTipoComida",
                column: "TipoComidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Porciones_AlimentoId",
                table: "Porciones",
                column: "AlimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Recomendaciones_AlimentoId",
                table: "Recomendaciones",
                column: "AlimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Recomendaciones_PerfilId_Fecha",
                table: "Recomendaciones",
                columns: new[] { "PerfilId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Recomendaciones_PlatoId",
                table: "Recomendaciones",
                column: "PlatoId");

            migrationBuilder.CreateIndex(
                name: "IX_Recomendaciones_TipoComidaId",
                table: "Recomendaciones",
                column: "TipoComidaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosComida_AlimentoId",
                table: "RegistrosComida",
                column: "AlimentoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosComida_PerfilId_Fecha",
                table: "RegistrosComida",
                columns: new[] { "PerfilId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosComida_PlatoId",
                table: "RegistrosComida",
                column: "PlatoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosComida_RecomendacionId",
                table: "RegistrosComida",
                column: "RecomendacionId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosComida_TipoComidaId",
                table: "RegistrosComida",
                column: "TipoComidaId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposComida_Nombre",
                table: "TiposComida",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlimentoAlergeno");

            migrationBuilder.DropTable(
                name: "AlimentoTipoComida");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "HistorialPeso");

            migrationBuilder.DropTable(
                name: "PerfilAlergeno");

            migrationBuilder.DropTable(
                name: "PlatoIngredientes");

            migrationBuilder.DropTable(
                name: "PlatoTipoComida");

            migrationBuilder.DropTable(
                name: "Porciones");

            migrationBuilder.DropTable(
                name: "RegistrosComida");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Alergenos");

            migrationBuilder.DropTable(
                name: "Recomendaciones");

            migrationBuilder.DropTable(
                name: "Alimentos");

            migrationBuilder.DropTable(
                name: "PerfilesUsuario");

            migrationBuilder.DropTable(
                name: "Platos");

            migrationBuilder.DropTable(
                name: "TiposComida");

            migrationBuilder.DropTable(
                name: "GruposAlimento");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
