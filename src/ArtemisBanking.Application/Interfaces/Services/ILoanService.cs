using ArtemisBanking.Application.DTOs.Loan;

namespace ArtemisBanking.Application.Interfaces.Services;

public interface ILoanService
{
    decimal CalcularCuotaFrancesa(decimal monto, decimal tasaAnual, int meses);

    Task<RiskAssessmentResult> EsClienteAltoRiesgoAsync(
        string clienteId,
        decimal nuevoCapital,
        decimal tasaAnual,
        int meses);

    Task AssignLoanAsync(CreateLoanDto dto, string adminId);

    Task UpdateInterestRateAsync(int loanId, decimal nuevaTasaAnual);

    Task<decimal> ProcessSequentialPaymentAsync(int loanId, decimal amount);
}
