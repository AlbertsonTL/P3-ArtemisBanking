using ArtemisBanking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace ArtemisBanking.WebApp.Filters;

public class RoleAuthorizeAttribute : AuthorizeAttribute
{
    public RoleAuthorizeAttribute(params UserRole[] roles)
    {
        Roles = string.Join(",", roles.Select(r => r.ToString()));
    }
}
