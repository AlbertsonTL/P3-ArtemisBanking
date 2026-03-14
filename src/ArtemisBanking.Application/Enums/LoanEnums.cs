namespace ArtemisBanking.Application.Enums;

/// <summary>
/// Nivel de riesgo detectado al analizar la deuda del cliente vs el promedio del sistema.
/// </summary>
public enum RiskLevel
{
    /// <summary>Deuda actual + nuevo préstamo ≤ promedio del sistema sin riesgo.</summary>
    NoRisk = 0,

    /// <summary>La deuda actual del cliente YA supera el promedio del sistema.</summary>
    AlreadyHighRisk = 1,

    /// <summary>La deuda actual no supera el promedio, pero sumando el nuevo préstamo sí.</summary>
    WillBeHighRisk = 2
}
