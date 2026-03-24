namespace ArtemisBanking.Application.DTOs.Account;

/// <summary>
/// DTO para login en la API
/// </summary>
public class LoginDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta de login exitoso
/// </summary>
public class LoginResponseDto
{
    public string Jwt { get; set; } = string.Empty;
    public UserDto? User { get; set; }
}

/// <summary>
/// DTO para confirmar usuario
/// </summary>
public class ConfirmAccountDto
{
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// DTO para obtener token de reset password
/// </summary>
public class GetResetTokenDto
{
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// DTO para resetear contraseña
/// </summary>
public class ResetPasswordDto
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO para cambiar estado de usuario
/// </summary>
public class ChangeUserStatusDto
{
    public bool Status { get; set; }
}

/// <summary>
/// Respuesta paginada genérica para usuarios
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
