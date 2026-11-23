namespace NutriApp.Domain.Entidades;

/// <summary>
/// 471 recetas con nutrientes por porción calculados desde sus ingredientes.
/// Tabla: "Platos" (ACT-03).
/// </summary>
public class Plato
{
    /// <summary>Id de la receta (SomosNLP)</summary>
    public int Id { get; set; }

    /// <summary>Nombre del plato</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Categoría de la fuente</summary>
    public string? Categoria { get; set; }

    /// <summary>País de origen según la fuente</summary>
    public string? PaisOrigen { get; set; }

    /// <summary>Origen ecuatoriano</summary>
    public bool EsEcuatoriana { get; set; }

    /// <summary>preparable en Ecuador · preparable con sustituto · excluida</summary>
    public string Clasificacion { get; set; } = string.Empty;

    /// <summary>Raciones de la receta</summary>
    public int? Raciones { get; set; }

    /// <summary>Peso comestible por porción [g]</summary>
    public int? PorcionG { get; set; }

    /// <summary>Energía por porción [kcal]</summary>
    public decimal? KcalPorcion { get; set; }

    /// <summary>Proteína por porción [g]</summary>
    public decimal? ProteinaGPorcion { get; set; }

    /// <summary>Carbohidratos por porción [g]</summary>
    public decimal? CarbohidratosGPorcion { get; set; }

    /// <summary>Grasa por porción [g]</summary>
    public decimal? GrasaGPorcion { get; set; }

    /// <summary>Fibra por porción [g]</summary>
    public decimal? FibraGPorcion { get; set; }

    /// <summary>Azúcares por porción [g]</summary>
    public decimal? AzucaresGPorcion { get; set; }

    /// <summary>Sodio por porción (sin sal de mesa) [mg]</summary>
    public decimal? SodioMgPorcion { get; set; }

    /// <summary>IG ponderado del plato</summary>
    public int? IndiceGlucemico { get; set; }

    /// <summary>Carga glucémica por porción (≤10 baja, ≥20 alta)</summary>
    public decimal? CargaGlucemicaPorcion { get; set; }

    /// <summary>confiable · aceptable · baja</summary>
    public string CalidadNutricional { get; set; } = string.Empty;

    /// <summary>Proporción de ingredientes resueltos</summary>
    public decimal CoberturaIngredientes { get; set; }

    /// <summary>Energía muy distinta a la estimación de la fuente</summary>
    public bool Revisar { get; set; }

    /// <summary>Para definir sustitutos</summary>
    public string? IngredientesSinEquivalente { get; set; }

    /// <summary>Tiempo de preparación [hh:mm]</summary>
    public string? Tiempo { get; set; }

    /// <summary>Fácil · Media · Difícil</summary>
    public string? Dificultad { get; set; }

    /// <summary>Receta original</summary>
    public string? Url { get; set; }

    /// <summary>Foto del plato</summary>
    public string? ImagenUrl { get; set; }
}
