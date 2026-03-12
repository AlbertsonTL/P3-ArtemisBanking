using ArtemisBanking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace ArtemisBanking.WebApp.Filters;

/// <summary>
/// Shortcut para aplicar [Authorize(Roles = "...")] con el enum UserRole.
/// Uso: [RoleAuthorize(UserRole.Admin)] o [RoleAuthorize(UserRole.Admin, UserRole.Cajero)]
/// </summary>
public class RoleAuthorizeAttribute : AuthorizeAttribute
{
    public RoleAuthorizeAttribute(params UserRole[] roles)
    {
        Roles = string.Join(",", roles.Select(r => r.ToString()));
    }
}