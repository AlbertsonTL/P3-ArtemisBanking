using ArtemisBanking.Domain.Common;

namespace ArtemisBanking.Domain.Entities;

public class Beneficiary : BaseEntity<int>
{
    public string AccountNumber { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string ClientId { get; set; } = string.Empty;
    public ApplicationUser Client { get; set; } = null!;
}