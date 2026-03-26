using ArtemisBanking.Domain.Enums;

namespace ArtemisBanking.Application.DTOs.CreditCard;

public class CreditCardDto
{
    public int Id { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string LastFourDigits => CardNumber.Length >= 4 ? CardNumber[^4..] : CardNumber;
    public decimal CreditLimit { get; set; }
    public decimal DebtAmount { get; set; }
    public decimal AvailableCredit => CreditLimit - DebtAmount;
    public string ExpirationDate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientFullName { get; set; } = string.Empty;
    public string AdminId { get; set; } = string.Empty;
}

public class CreateCreditCardDto
{
    public string ClientId { get; set; } = string.Empty;
    public string AdminId { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
}

public class UpdateCreditCardLimitDto
{
    public decimal NewCreditLimit { get; set; }
}

public class CardConsumptionDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string CommerceName { get; set; } = string.Empty;
    public ConsumptionStatus Status { get; set; }
    public int CreditCardId { get; set; }
    public int? CommerceId { get; set; }
}
