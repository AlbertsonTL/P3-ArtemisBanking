using ArtemisBanking.Domain.Entities;

namespace ArtemisBanking.Application.Interfaces.Services;

public interface IJwtService
{
    /// <summary>
    /// Genera un JWT token para un usuario autenticado
    /// </summary>
    string GenerateToken(ApplicationUser user);
    
    /// <summary>
    /// Valida un JWT token y retorna los claims
    /// </summary>
    Dictionary<string, object>? ValidateToken(string token);
}
