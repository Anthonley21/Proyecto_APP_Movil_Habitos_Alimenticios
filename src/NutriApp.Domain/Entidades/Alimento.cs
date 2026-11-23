namespace NutriApp.Domain.Entidades;

/// <summary>
/// Catálogo de 997 alimentos de Ecuador (tabla USFQ 2021 limpia + azúcares + IG). Valores por 100 g comestibles.
/// Tabla: "Alimentos" (ACT-03).
/// </summary>
public class Alimento
{
    /// <summary>Identificador (EC0001 → 1)</summary>
    public int Id { get; set; }

    /// <summary>Código estable del alimento (EC0001…)</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Nombre en español</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Nombre en inglés (para cruzar con USDA)</summary>
    public string? NombreIngles { get; set; }

    /// <summary>Grupo del alimento</summary>
    public string GrupoCodigo { get; set; } = string.Empty;

    /// <summary>'100 g' o '100 ml' (bebidas)</summary>
    public string UnidadBase { get; set; } = string.Empty;

    /// <summary>Energía [kcal]</summary>
    public decimal KcalPor100g { get; set; }

    /// <summary>Proteína [g]</summary>
    public decimal ProteinaG { get; set; }

    /// <summary>Carbohidratos totales (incluyen fibra) [g]</summary>
    public decimal CarbohidratosG { get; set; }

    /// <summary>Grasa total [g]</summary>
    public decimal GrasaG { get; set; }

    /// <summary>Fibra dietética [g]</summary>
    public decimal? FibraG { get; set; }

    /// <summary>Azúcares totales (ver CalidadAzucar) [g]</summary>
    public decimal? AzucaresG { get; set; }

    /// <summary>Carbohidratos - fibra [g]</summary>
    public decimal CarbDisponibleG { get; set; }

    /// <summary>Sodio [mg]</summary>
    public decimal? SodioMg { get; set; }

    /// <summary>Ácidos grasos saturados [g]</summary>
    public decimal? GrasaSaturadaG { get; set; }

    /// <summary>Colesterol [mg]</summary>
    public decimal? ColesterolMg { get; set; }

    /// <summary>Calcio (micronutriente, no lo usa el motor) [mg]</summary>
    public decimal? CalcioMg { get; set; }

    /// <summary>Hierro [mg]</summary>
    public decimal? HierroMg { get; set; }

    /// <summary>Potasio [mg]</summary>
    public decimal? PotasioMg { get; set; }

    /// <summary>Zinc [mg]</summary>
    public decimal? ZincMg { get; set; }

    /// <summary>Vitamina C [mg]</summary>
    public decimal? VitaminaCMg { get; set; }

    /// <summary>Vitamina A [µg ERE]</summary>
    public decimal? VitaminaAUg { get; set; }

    /// <summary>Folatos [µg]</summary>
    public decimal? FolatosUg { get; set; }

    /// <summary>Vitamina B12 [µg]</summary>
    public decimal? VitaminaB12Ug { get; set; }

    /// <summary>IG (glucosa = 100)</summary>
    public int? IndiceGlucemico { get; set; }

    /// <summary>bajo · medio · alto · no aplica · sin dato</summary>
    public string CategoriaIG { get; set; } = string.Empty;

    /// <summary>IG × CarbDisponibleG / 100</summary>
    public decimal? CargaGlucemica100g { get; set; }

    /// <summary>alta · media · estimada · sin dato</summary>
    public string CalidadAzucar { get; set; } = string.Empty;

    /// <summary>alta · media · baja · no aplica · sin dato</summary>
    public string CalidadIG { get; set; } = string.Empty;

    /// <summary>Producto empacado (dato de etiqueta)</summary>
    public bool EsProductoDeMarca { get; set; }

    /// <summary>False si el dato de la fuente es imposible</summary>
    public bool AptoMotor { get; set; }

    /// <summary>Pendiente de revisión humana</summary>
    public bool Revisar { get; set; }

    /// <summary>Fuente original del dato</summary>
    public string Fuente { get; set; } = string.Empty;

    /// <summary>Correcciones aplicadas por el script</summary>
    public string? NotaLimpieza { get; set; }

    // Navegación
    public GrupoAlimento Grupo { get; set; } = null!;
}
