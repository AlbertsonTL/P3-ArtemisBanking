using ArtemisBanking.Domain.Common;
using ArtemisBanking.Domain.Enums;

namespace ArtemisBanking.Domain.Entities;

public class SavingsAccount : BaseEntity<int>
{
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 0m;
    public AccountType AccountType { get; set; } = AccountType.Main;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string  ClientId { get; set; } = string.Empty;
    public string? AdminId  { get; set; }

    public ApplicationUser Client { get; set; } = null!;
    public ApplicationUser? Admin { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}