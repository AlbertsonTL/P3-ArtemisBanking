namespace ArtemisBanking.Application.DTOs.Commerce;

public class CommerceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCommerceDto
{
    public string Name { get; set; } = string.Empty;
}
