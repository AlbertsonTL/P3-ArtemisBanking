using ArtemisBanking.Application.Enums;

namespace ArtemisBanking.Application.DTOs.Loan;

public class RiskAssessmentResult
{
    public RiskLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal DeudaActualCliente { get; set; }
    public decimal PromedioSistema { get; set; }
    public decimal TotalNuevoPrestamo { get; set; }
    public bool TieneRiesgo => Level != RiskLevel.NoRisk;
}
