using ArtemisBanking.Domain.Common;

namespace ArtemisBanking.Domain.Entities;

public class Commerce : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CardConsumption> CardConsumptions { get; set; } = new List<CardConsumption>();
}