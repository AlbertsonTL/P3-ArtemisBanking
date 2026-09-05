using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArtemisBanking.WebApp.Filters;

public class AlreadyLoggedInFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true) return;

        if (user.IsInRole("Admin"))
            context.Result = new RedirectToActionResult("Index", "Home", new { area = "Admin" });
        else if (user.IsInRole("Cajero"))
            context.Result = new RedirectToActionResult("Index", "Home", new { area = "Cajero" });
        else if (user.IsInRole("Cliente"))
            context.Result = new RedirectToActionResult("Index", "Home", new { area = "Cliente" });
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
