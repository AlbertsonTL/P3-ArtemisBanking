using ArtemisBanking.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = "Cliente")]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.Users
            .Include(u => u.SavingsAccounts)
            .Include(u => u.CreditCards)
            .Include(u => u.Loans)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);

        if (user == null) return NotFound();

        return View(user);
    }
}
