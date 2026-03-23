using ArtemisBanking.Domain.Common;
using ArtemisBanking.Domain.Enums;

namespace ArtemisBanking.Domain.Entities;

public class Transaction : BaseEntity<int>
{
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public TransactionStatus Status { get; set; }
    public TransactionCategory Category { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Beneficiary { get; set; } = string.Empty;
    public string? CashierId { get; set; } // Null si es operación del cliente, UserId si es operación del cajero

    public int SavingsAccountId { get; set; }
    public SavingsAccount SavingsAccount { get; set; } = null!;
}