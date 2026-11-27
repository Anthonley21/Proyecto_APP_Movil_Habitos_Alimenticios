namespace NutriApp.Api.Contratos;

// ---------- ACT-10 · Filtro por reglas ----------

/// <summary>Candidatos de cada comida para el perfil del usuario.</summary>
/// <param name="KcalDia">Requerimiento diario (ACT-08)</param>
/// <param name="Restricciones">Lo que se aplicó: diabetes, hipertensión y alérgenos</param>
public sealed record CandidatosRespuesta(
    double KcalDia,
    RestriccionesRespuesta Restricciones,
    IReadOnlyList<CandidatosComidaRespuesta> Comidas);

public sealed record RestriccionesRespuesta(bool Diabetes, bool Hipertension, IReadOnlyList<string> Alergenos);

/// <param name="ToleranciaPorcentaje">10 normalmente; 15 si con ±10 % quedaban menos de 3 candidatos</param>
/// <param name="TotalCandidatos">Cuántos cumplen todas las reglas (la lista trae como máximo "limite")</param>
/// <param name="Suficiente">false si hay menos de 3 (entonces Aviso explica por qué)</param>
/// <param name="Descartes">Cuántos alimentos y platos se descartaron por cada regla</param>
public sealed record CandidatosComidaRespuesta(
    int TipoComidaId,
    string Comida,
    double MetaKcal,
    double ToleranciaPorcentaje,
    double MinKcal,
    double MaxKcal,
    int TotalCandidatos,
    bool Suficiente,
    string? Aviso,
    IReadOnlyDictionary<string, int> Descartes,
    IReadOnlyList<CandidatoRespuesta> Candidatos);

/// <param name="Tipo">"alimento" o "plato"</param>
/// <param name="Cantidad">Porción ya ajustada: "2 × 1/2 taza", "1 porción (331 g)"</param>
/// <param name="PorcentajeMeta">kcal del candidato ÷ meta de la comida × 100</param>
/// <param name="Reglas">Reglas que cumple, en palabras (base de la explicación de ACT-12)</param>
public sealed record CandidatoRespuesta(
    string Tipo,
    int Id,
    string Nombre,
    string Cantidad,
    double Gramos,
    double Kcal,
    double PorcentajeMeta,
    double ProteinaG,
    double CarbohidratosG,
    double GrasaG,
    double? AzucaresG,
    double? SodioMg,
    int? IndiceGlucemico,
    IReadOnlyList<string> Reglas);
