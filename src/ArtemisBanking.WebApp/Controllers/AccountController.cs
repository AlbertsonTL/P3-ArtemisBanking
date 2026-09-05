using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.WebApp.ViewModels.Account;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Application.DTOs.Email;
using ArtemisBanking.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBanking.WebApp.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    // IEmailService
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login()
    {
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

        var user = await _userManager.FindByNameAsync(model.UserName) 
                   ?? await _userManager.FindByEmailAsync(model.UserName);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "El usuario (o correo) y/o la contraseña son incorrectos.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty,
                "Tu cuenta está inactiva. Debes activarla mediante el enlace enviado a tu correo.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, model.Password,
            isPersistent: model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
            return RedirectToRoleHome(user.Role);

        ModelState.AddModelError(string.Empty, "El usuario (o correo) y/o la contraseña son incorrectos.");
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

    // GET /Account/ForgotPassword
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    // POST /Account/ForgotPassword 
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "No existe un usuario registrado con este correo.");
            return View(model);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var resetLink = Url.Action("ResetPassword", "Account",
            new { userId = user.Id, token }, Request.Scheme)!;

        await _emailService.SendAsync(new EmailRequestDto
        {
            To = user.Email!,
            Subject = "Restablecer contraseña — Artemis Banking",
            Body = EmailTemplates.ResetPassword($"{user.FirstName} {user.LastName}", resetLink)
        });

        var enviroment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (enviroment == "Development")
        {
            TempData["Success"] = $"[MODO PRUEBA]: Como no hay servidor de correos, usa este enlace para recuperar: {resetLink}";
        }
        else
        {
            TempData["Success"] = "Se ha enviado un enlace de restablecimiento a tu bandeja de entrada.";
        }
        
        return RedirectToAction(nameof(Login));
    }

    // GET /Account/ResetPassword 
    [HttpGet]
    public IActionResult ResetPassword(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));

        return View(new ResetPasswordViewModel { UserId = userId, Token = token });
    }

    // POST /Account/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null) return RedirectToAction(nameof(Login));

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        if (!user.IsActive)
        {
            user.IsActive = true;
            await _userManager.UpdateAsync(user);
        }

        TempData["Success"] = "Contraseña restablecida correctamente. Ahora puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    // GET /Account/ActivateAccount 
    [HttpGet]
    public async Task<IActionResult> ActivateAccount(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return RedirectToAction(nameof(Login));

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            TempData["Error"] = "El enlace de activación es inválido o ha expirado.";
            return RedirectToAction(nameof(Login));
        }

        user.IsActive = true;
        await _userManager.UpdateAsync(user);

        TempData["Success"] = "Tu cuenta ha sido activada. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToRoleHome(UserRole? role = null)
    {
        if (role == null && _signInManager.IsSignedIn(User))
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Home", new { area = "Admin" });
            if (User.IsInRole("Cajero")) return RedirectToAction("Index", "Home", new { area = "Cashier" });
            if (User.IsInRole("Cliente")) return RedirectToAction("Index", "Home", new { area = "Client" });
        }

        return role switch
        {
            UserRole.Admin => RedirectToAction("Index", "Home", new { area = "Admin" }),
            UserRole.Cajero => RedirectToAction("Index", "Home", new { area = "Cashier" }),
            UserRole.Cliente => RedirectToAction("Index", "Home", new { area = "Client" }),
            _ => RedirectToAction(nameof(Login))
        };

    }
}
