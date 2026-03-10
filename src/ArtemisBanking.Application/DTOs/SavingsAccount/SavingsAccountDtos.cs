using ArtemisBanking.Domain.Enums;

namespace ArtemisBanking.Application.DTOs.SavingsAccount;

public class SavingsAccountDto
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public AccountType AccountType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientFullName { get; set; } = string.Empty;
    public string? AdminId { get; set; }
}

public class CreateSavingsAccountDto
{
    public decimal InitialBalance { get; set; } = 0m;
    public string ClientId { get; set; } = string.Empty;
    public string? AdminId { get; set; }
}