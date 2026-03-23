namespace ArtemisBanking.WebApp.ViewModels.Cashier;

public class CashierDashboardViewModel
{
    /// <summary>
    /// Cantidad de depósitos realizados hoy por el cajero logueado
    /// </summary>
    public int DepositsCount { get; set; }

    /// <summary>
    /// Cantidad de retiros realizados hoy por el cajero logueado
    /// </summary>
    public int WithdrawalsCount { get; set; }

    /// <summary>
    /// Cantidad de pagos a tarjetas realizados hoy por el cajero logueado
    /// </summary>
    public int CreditCardPaymentsCount { get; set; }

    /// <summary>
    /// Cantidad de pagos a préstamos realizados hoy por el cajero logueado
    /// </summary>
    public int LoanPaymentsCount { get; set; }

    /// <summary>
    /// Monto total depositado hoy
    /// </summary>
    public decimal TotalDeposited { get; set; }

    /// <summary>
    /// Monto total retirado hoy
    /// </summary>
    public decimal TotalWithdrawn { get; set; }
}