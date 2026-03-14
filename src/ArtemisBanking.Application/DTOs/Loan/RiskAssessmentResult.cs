using ArtemisBanking.Application.Enums;

namespace ArtemisBanking.Application.DTOs.Loan;

/// <summary>
/// Resultado del análisis de riesgo de un cliente antes de asignar un préstamo.
/// </summary>
public class RiskAssessmentResult
{
    /// <summary>Nivel de riesgo detectado.</summary>
    public RiskLevel Level { get; set; }

    /// <summary>Mensaje descriptivo del riesgo para mostrar al admin.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Deuda actual acumulada del cliente (suma de cuotas pendientes de préstamos activos).</summary>
    public decimal DeudaActualCliente { get; set; }

    /// <summary>Promedio de deuda del sistema (total deuda pendiente / clientes con préstamos activos).</summary>
    public decimal PromedioSistema { get; set; }

    /// <summary>Total que generará el nuevo préstamo (cuota × meses).</summary>
    public decimal TotalNuevoPrestamo { get; set; }

    /// <summary>Indica si hay riesgo que requiere confirmación del admin.</summary>
    public bool TieneRiesgo => Level != RiskLevel.NoRisk;
}
