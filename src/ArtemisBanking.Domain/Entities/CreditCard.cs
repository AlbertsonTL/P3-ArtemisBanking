using ArtemisBanking.Domain.Common;

namespace ArtemisBanking.Domain.Entities;

public class CreditCard : BaseEntity<int>
{
    public string CardNumber { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal DebtAmount { get; set; } = 0m;

    /// <summary>Alias de DebtAmount para compatibilidad con operaciones de cajero.</summary>
    public decimal CurrentDebt
    {
        get => DebtAmount;
        set => DebtAmount = value;
    }
    public string ExpirationDate { get; set; } = string.Empty;
    public string CVCHashed { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string ClientId { get; set; } = string.Empty;
    public string AdminId { get; set; } = string.Empty;

    public ApplicationUser Client { get; set; } = null!;
    public ApplicationUser Admin { get; set; } = null!;
    public ICollection<CardConsumption> Consumptions { get; set; } = new List<CardConsumption>();
}