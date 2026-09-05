using ArtemisBanking.Domain.Common;
using ArtemisBanking.Domain.Enums;

namespace ArtemisBanking.Domain.Entities;

public class CardConsumption : BaseEntity<int>
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string CommerceName { get; set; } = string.Empty;
    public ConsumptionStatus Status { get; set; }

    public int CreditCardId { get; set; }
    public int? CommerceId { get; set; }

    public CreditCard CreditCard { get; set; } = null!;
    public Commerce? Commerce { get; set; }
}
