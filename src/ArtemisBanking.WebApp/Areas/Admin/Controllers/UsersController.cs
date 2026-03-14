using System.Security.Claims;
using ArtemisBanking.Application.DTOs.Email;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Shared.Helpers;
using ArtemisBanking.WebApp.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly IGenericRepository<Loan, int> _loanRepository;
    private readonly IGenericRepository<Transaction, int> _transactionRepository;
    private readonly IEmailService _emailService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        IGenericRepository<SavingsAccount, int> savingsRepository,
        IGenericRepository<Loan, int> loanRepository,
        IGenericRepository<Transaction, int> transactionRepository,
        IEmailService emailService)
    {
        _userManager = userManager;
        _savingsRepository = savingsRepository;
        _loanRepository = loanRepository;
        _transactionRepository = transactionRepository;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(UserRole? filterRole, int pg = 1)
    {
        var usersQuery = _userManager.Users.AsQueryable();

        if (filterRole.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.Role == filterRole.Value);
        }

        const int pageSize = 5;
        if (pg < 1) pg = 1;

        int totalUsers = await usersQuery.CountAsync();
        int totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

        var users = await usersQuery
            .Skip((pg - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModels = users.Select(u => new UserListViewModel
        {
            Id = u.Id,
            FullName = $"{u.FirstName} {u.LastName}",
            IdentityCard = u.IdentityCard,
            Email = u.Email!,
            UserName = u.UserName!,
            IsActive = u.IsActive,
            Role = u.Role
        }).ToList();

        ViewBag.CurrentFilter = filterRole;
        ViewBag.CurrentPage = pg;
        ViewBag.TotalPages = totalPages;
        
        return View(viewModels);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _userManager.FindByNameAsync(model.UserName) != null)
        {
            ModelState.AddModelError(string.Empty, "El nombre de usuario ya existe.");
            return View(model);
        }

        // 1. Crear usuario desactivado y sin confirmar (hasta que active vía email)
        var user = new ApplicationUser
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            IdentityCard = model.IdentityCard,
            Email = model.Email,
            UserName = model.UserName,
            Role = model.Role,
            IsActive = false, 
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        // Asignar rol Identity
        await _userManager.AddToRoleAsync(user, model.Role.ToString());

        // 2. Si es Cliente, crearle su cuenta principal (9 dígitos)
        if (model.Role == UserRole.Cliente)
        {
            string newAccountNumber;
            do
            {
                newAccountNumber = AccountNumberGenerator.Generate9Digits();
            }
            while (await _savingsRepository.ExistsAsync(s => s.AccountNumber == newAccountNumber) ||
                   await _loanRepository.ExistsAsync(l => l.LoanNumber == newAccountNumber));

            var adminClaimId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var mainAccount = new SavingsAccount
            {
                AccountNumber = newAccountNumber,
                Balance = 0m,
                AccountType = AccountType.Main,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ClientId = user.Id,
                AdminId = adminClaimId
            };
            
            await _savingsRepository.AddAsync(mainAccount);
            await _savingsRepository.SaveChangesAsync();
        }

        // 3. Generar token y mandar correo
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var activationLink = Url.Action("ActivateAccount", "Account", 
            new { area = "", userId = user.Id, token }, Request.Scheme)!;

        // Se requiere template genérico o texto plano si Albertson no dejó un template de activación prearmado
        await _emailService.SendAsync(new EmailRequestDto
        {
            To = user.Email,
            Subject = "Bienvenido a Artemis Banking — Activa tu cuenta",
            Body = $"<h1>Hola {user.FirstName}</h1><p>Tu cuenta ha sido creada. Click aquí para activarla: <a href='{activationLink}'>Activar Cuenta</a></p>"
        });

        TempData["Success"] = $"Usuario {user.UserName} creado correctamente. Se le envió correo de activación.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IdentityCard = user.IdentityCard,
            Email = user.Email!,
            UserName = user.UserName!,
            Role = user.Role,
            MontoAdicional = 0m // Siempre inicia en 0
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Regla de Negocio: Admin no puede editarse a sí mismo
        if (model.Id == currentAdminId)
        {
            TempData["Error"] = "Operación denegada. No puedes editar tu propio usuario.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        // Actualizar datos personales
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.IdentityCard = model.IdentityCard;
        user.Email = model.Email;

        await _userManager.UpdateAsync(user);

        // Si es cliente, aplicar MontoAdicional sumando al balance de su cuenta PRINCIPAL
        if (user.Role == UserRole.Cliente && model.MontoAdicional > 0)
        {
            var mainAccount = await _savingsRepository.FirstOrDefaultAsync(
                s => s.ClientId == user.Id && s.AccountType == AccountType.Main);

            if (mainAccount != null)
            {
                mainAccount.Balance += model.MontoAdicional;
                _savingsRepository.Update(mainAccount);
                await _savingsRepository.SaveChangesAsync();
                
                var transaction = new Transaction
                {
                    Type = TransactionType.Credit,
                    Amount = model.MontoAdicional,
                    Date = DateTime.UtcNow,
                    Status = TransactionStatus.Approved,
                    Category = TransactionCategory.SavingsTransfer,
                    Origin = "Ajuste de Admin",
                    Beneficiary = mainAccount.AccountNumber,
                    SavingsAccountId = mainAccount.Id
                };
                await _transactionRepository.AddAsync(transaction);
                await _transactionRepository.SaveChangesAsync();
                
                TempData["Success"] = $"Usuario editado. Se acreditaron RD$ {model.MontoAdicional:N2} a su cuenta principal.";
            }
            else
            {
                TempData["Error"] = "Usuario editado, pero fallo sumando fondos (el cliente no posee cuenta principal).";
            }
        }
        else
        {
            TempData["Success"] = "Usuario editado correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest();

        var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Regla: Admin no puede activarse/desactivarse a sí mismo
        if (id == currentAdminId)
        {
            TempData["Error"] = "Operación denegada. No puedes cambiar el estado de tu propio usuario.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        var status = user.IsActive ? "activado" : "desactivado";
        TempData["Success"] = $"El usuario {user.UserName} ha sido {status}.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route("Api/Clients")]
    public async Task<IActionResult> GetClientsApi()
    {
        var users = await _userManager.GetUsersInRoleAsync("Cliente");
        var activeClients = users.Where(u => u.IsActive).Select(u => new
        {
            id = u.Id,
            fullName = $"{u.FirstName} {u.LastName}",
            identityCard = u.IdentityCard
        }).ToList();

        return Json(activeClients);
    }
}
