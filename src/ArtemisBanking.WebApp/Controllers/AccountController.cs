using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.WebApp.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBanking.WebApp.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser>  _userManager;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser>  userManager)
    {
        _signInManager = signInManager;
        _userManager   = userManager;
    }

    // GET /Account/Login
    [HttpGet]
    public IActionResult Login()
    {
        // Si ya está logueado, redirigir a su Home según rol
        if (_signInManager.IsSignedIn(User))
            return RedirectToRoleHome();

        return View(new LoginViewModel());
    }

    // POST /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByNameAsync(model.UserName);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View(model);
        }

        // Cuenta inactiva
        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty,
                "Tu cuenta está inactiva. Debes activarla mediante el enlace enviado a tu correo.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.UserName, model.Password,
            isPersistent: model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
            return RedirectToRoleHome(user.Role);

        ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
        return View(model);
    }

    // GET /Account/Logout
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    // GET /Account/AccessDenied
    public IActionResult AccessDenied() => View();

    // Helpers
    private IActionResult RedirectToRoleHome(UserRole? role = null)
    {
        if (role == null && _signInManager.IsSignedIn(User))
        {
            // Leer el rol del usuario actual desde los claims
            if (User.IsInRole("Admin"))   return RedirectToAction("Index", "Home", new { area = "Admin" });
            if (User.IsInRole("Cajero"))  return RedirectToAction("Index", "Home", new { area = "Cajero" });
            if (User.IsInRole("Cliente")) return RedirectToAction("Index", "Home", new { area = "Cliente" });
        }

        return role switch
        {
            UserRole.Admin   => RedirectToAction("Index", "Home", new { area = "Admin" }),
            UserRole.Cajero  => RedirectToAction("Index", "Home", new { area = "Cajero" }),
            UserRole.Cliente => RedirectToAction("Index", "Home", new { area = "Cliente" }),
            _                => RedirectToAction(nameof(Login))
        };
    }
}