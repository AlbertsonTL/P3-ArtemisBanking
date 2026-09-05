namespace ArtemisBanking.Application.DTOs.Beneficiary;

public class BeneficiaryDto
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string OwnerFullName { get; set; } = string.Empty;
}

public class CreateBeneficiaryDto
{
    public string AccountNumber { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}
