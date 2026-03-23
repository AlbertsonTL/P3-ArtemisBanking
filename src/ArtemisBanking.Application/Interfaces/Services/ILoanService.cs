using ArtemisBanking.Application.DTOs.Loan;

namespace ArtemisBanking.Application.Interfaces.Services;

/// <summary>
/// Contrato del servicio de préstamos.
/// Implementado en Infrastructure.Services.LoanService.
/// </summary>
public interface ILoanService
{
    /// <summary>
    /// Calcula la cuota mensual constante usando el sistema francés de amortización.
    /// Fórmula: C = P × [r(1+r)^n] / [(1+r)^n − 1]
    /// Usa decimal para precisión financiera (no float/double).
    /// </summary>
    /// <param name="monto">Capital (P).</param>
    /// <param name="tasaAnual">Tasa de interés anual en porcentaje</param>
    /// <param name="meses">Número de cuotas (n).</param>
    /// <returns>Cuota mensual redondeada a 2 decimales.</returns>
    decimal CalcularCuotaFrancesa(decimal monto, decimal tasaAnual, int meses);

    /// <summary>
    /// Evalúa si asignar un nuevo préstamo convierte al cliente en alto riesgo.
    /// Compara la deuda actual/futura contra el promedio de deuda del sistema.
    /// </summary>
    /// <param name="clienteId">ID del cliente (ApplicationUser.Id).</param>
    /// <param name="nuevoCapital">Monto del nuevo préstamo.</param>
    /// <param name="tasaAnual">Tasa anual del nuevo préstamo.</param>
    /// <param name="meses">Plazo en meses del nuevo préstamo.</param>
    /// <returns>RiskAssessmentResult con nivel de riesgo y mensaje descriptivo.</returns>
    Task<RiskAssessmentResult> EsClienteAltoRiesgoAsync(
        string clienteId,
        decimal nuevoCapital,
        decimal tasaAnual,
        int meses);

    /// <summary>
    /// Proceso completo de asignación de préstamo:
    ///   1. Guarda entidad Préstamo.
    ///   2. Genera tabla de amortización (n cuotas con fechas mensuales).
    ///   3. Acredita monto a cuenta principal del cliente.
    ///   4. Registra transacción tipo CRÉDITO / LoanDisbursement.
    ///   5. Envía correo de confirmación al cliente.
    /// </summary>
    Task AssignLoanAsync(CreateLoanDto dto, string adminId);

    /// <summary>
    /// Actualiza la tasa de interés de un préstamo activo y recalcula las
    /// cuotas futuras pendientes usando el capital restante y la nueva tasa.
    /// Las cuotas ya pagadas o vencidas NO se modifican.
    /// Envía correo al cliente notificando el cambio.
    /// </summary>
    /// <param name="loanId">ID del préstamo a modificar.</param>
    /// <param name="nuevaTasaAnual">Nueva tasa de interés anual (%).</param>
    Task UpdateInterestRateAsync(int loanId, decimal nuevaTasaAnual);

    /// <summary>
    /// Aplica un pago secuencial a las cuotas pendientes de un préstamo,
    /// marcando cuotas como pagadas en orden cronológico hasta agotar el monto.
    /// Actualiza AmountPaid y Status del préstamo si queda saldado.
    /// </summary>
    /// <param name="loanId">ID del préstamo.</param>
    /// <param name="amount">Monto a aplicar al préstamo.</param>
    /// <returns>El monto efectivamente aplicado.</returns>
    Task<decimal> ProcessSequentialPaymentAsync(int loanId, decimal amount);
}
