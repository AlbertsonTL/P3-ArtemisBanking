namespace ArtemisBanking.WebApp.ViewModels.Cashier;

public class CashierDashboardViewModel
{
    public int DepositsCount { get; set; }

    public int WithdrawalsCount { get; set; }

    public int CreditCardPaymentsCount { get; set; }

    public int LoanPaymentsCount { get; set; }

    public decimal TotalDeposited { get; set; }

    public decimal TotalWithdrawn { get; set; }
}
