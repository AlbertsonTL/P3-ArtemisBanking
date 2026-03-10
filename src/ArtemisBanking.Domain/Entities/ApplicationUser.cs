using ArtemisBanking.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace ArtemisBanking.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdentityCard { get; set; } = string.Empty;
    public bool IsActive { get; set; } = false;
    public UserRole Role { get; set; }

    public ICollection<SavingsAccount> SavingsAccounts { get; set; } = new List<SavingsAccount>();
    public ICollection<CreditCard> CreditCards { get; set; } = new List<CreditCard>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
}