using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ArtemisBanking.WebAPI.Filters;

public class SwaggerPublicEndpointFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isAnonymous = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AllowAnonymousAttribute>()
            .Any();

        if (isAnonymous)
        {
            // Sin requisito de seguridad para endpoints públicos
            operation.Security = new List<OpenApiSecurityRequirement>();

            // Añadir nota visual al principio de la descripción
            var note = "🔓 **Endpoint público** — No requiere autenticación.\n\n";
            operation.Description = note + (operation.Description ?? string.Empty);
        }
        else
        {
            // Verificar si tiene [Authorize(Roles = "Admin")]
            var authorizeAttr = context.MethodInfo
                .GetCustomAttributes(true)
                .OfType<AuthorizeAttribute>()
                .FirstOrDefault()
                ?? context.MethodInfo.DeclaringType?
                    .GetCustomAttributes(true)
                    .OfType<AuthorizeAttribute>()
                    .FirstOrDefault();

            if (authorizeAttr != null)
            {
                var roles = authorizeAttr.Roles;
                if (!string.IsNullOrEmpty(roles))
                {
                    var note = $"🔒 **Requiere JWT** — Roles permitidos: `{roles}`.\n\n";
                    operation.Description = note + (operation.Description ?? string.Empty);
                }
                else
                {
                    var note = "🔒 **Requiere JWT**.\n\n";
                    operation.Description = note + (operation.Description ?? string.Empty);
                }
            }
        }
    }
}
