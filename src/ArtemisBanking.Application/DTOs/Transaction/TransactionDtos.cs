using ArtemisBanking.Domain.Enums;

namespace ArtemisBanking.Application.DTOs.Transaction;

public class TransactionDto
{
    public int Id { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public TransactionStatus Status { get; set; }
    public TransactionCategory Category { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Beneficiary { get; set; } = string.Empty;
    public int SavingsAccountId { get; set; }
}

public class CreateTransactionDto
{
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public TransactionCategory Category { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Beneficiary { get; set; } = string.Empty;
    public int SavingsAccountId { get; set; }
}