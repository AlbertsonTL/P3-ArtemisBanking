namespace ArtemisBanking.Application.DTOs.Account;

public class LoginDto
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Jwt { get; set; } = string.Empty;
    public UserDto? User { get; set; }
}

public class ConfirmAccountDto
{
    public string Token { get; set; } = string.Empty;
}

public class GetResetTokenDto
{
    public string UserName { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangeUserStatusDto
{
    public bool Status { get; set; }
}

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
