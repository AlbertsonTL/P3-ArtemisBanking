using ArtemisBanking.Domain.Entities;

namespace ArtemisBanking.WebApp.ViewModels.Client;

public class ClientHomeViewModel
{
    public List<SavingsAccount> SavingsAccounts { get; set; } = new();
    public List<CreditCard> CreditCards { get; set; } = new();
    public List<Loan> Loans { get; set; } = new();
}
