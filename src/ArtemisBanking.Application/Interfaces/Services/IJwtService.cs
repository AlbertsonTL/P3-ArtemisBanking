using ArtemisBanking.Domain.Entities;

namespace ArtemisBanking.Application.Interfaces.Services;

public interface IJwtService
{
    string GenerateToken(ApplicationUser user);
    
    Dictionary<string, object>? ValidateToken(string token);
}
