using System.Security.Claims;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Shared.Helpers;
using ArtemisBanking.WebApp.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AccountsController : Controller
{
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly IGenericRepository<Loan, int> _loanRepository;
    private readonly IGenericRepository<Transaction, int> _transactionRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountsController(
        IGenericRepository<SavingsAccount, int> savingsRepository,
        IGenericRepository<Loan, int> loanRepository,
        IGenericRepository<Transaction, int> transactionRepository,
        UserManager<ApplicationUser> userManager)
    {
        _savingsRepository = savingsRepository;
        _loanRepository = loanRepository;
        _transactionRepository = transactionRepository;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var accounts = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(_savingsRepository.Query(), a => a.Client).ToListAsync();

        var model = accounts.Select(a => new AccountListViewModel
        {
            Id = a.Id,
            AccountNumber = a.AccountNumber,
            ClientName = $"{a.Client.FirstName} {a.Client.LastName}",
            IdentityCard = a.Client.IdentityCard,
            AccountType = a.AccountType,
            Balance = a.Balance,
            IsActive = a.IsActive
        }).OrderByDescending(a => a.AccountType == AccountType.Main) // Listar principales primero
          .ThenByDescending(a => a.IsActive)
          .ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        // Solo clientes activos
        var clients = _userManager.Users
                                  .Where(u => u.Role == UserRole.Cliente && u.IsActive)
                                  .OrderBy(u => u.FirstName)
                                  .Select(u => new SelectListItem
                                  {
                                      Value = u.Id,
                                      Text = $"{u.FirstName} {u.LastName} ({u.IdentityCard})"
                                  }).ToList();

        return View(new CreateAccountViewModel { Clients = clients });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAccountViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Clients = _userManager.Users.Where(u => u.Role == UserRole.Cliente && u.IsActive)
                 .Select(u => new SelectListItem { Value = u.Id, Text = $"{u.FirstName} {u.LastName}" }).ToList();
            return View(model);
        }

        var client = await _userManager.FindByIdAsync(model.ClientId);
        if (client == null || client.Role != UserRole.Cliente) return BadRequest();

        // 1. Número único que no choque ni en Savings ni en Loans
        string newAccountNumber;
        do
        {
            newAccountNumber = AccountNumberGenerator.Generate9Digits();
        }
        while (await _savingsRepository.ExistsAsync(s => s.AccountNumber == newAccountNumber) ||
               await _loanRepository.ExistsAsync(l => l.LoanNumber == newAccountNumber));

        var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 2. Crear cuenta como SECUNDARIA. La cuenta PRINCIPAL solo se crea de forma automática al registrar usuario.
        var account = new SavingsAccount
        {
            AccountNumber = newAccountNumber,
            Balance = 0m,
            AccountType = AccountType.Secondary, // Solo el registro original genera la Main
            IsActive = true,
            ClientId = client.Id,
            AdminId = currentAdminId
        };

        await _savingsRepository.AddAsync(account);
        await _savingsRepository.SaveChangesAsync();

        TempData["Success"] = $"Cuenta Secundaria #{newAccountNumber} generada exitosamente, balance de RD$ 0.00.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var account = await _savingsRepository.GetByIdAsync(id);
        if (account == null) return NotFound();

        // Regla: La cuenta principal NUNCA puede ser eliminada. 
        if (account.AccountType == AccountType.Main)
        {
            TempData["Error"] = "La cuenta principal del usuario NO puede ser eliminada, bloqueada o cancelada estructuralmente bajo ninguna circunstancia.";
            return RedirectToAction(nameof(Index));
        }

        // Si es secundaria y tiene dinero, sumar todo algebraicamente a la cuenta Principal
        if (account.Balance > 0)
        {
            var mainAccount = await _savingsRepository.FirstOrDefaultAsync(
                a => a.ClientId == account.ClientId && a.AccountType == AccountType.Main);

            if (mainAccount != null)
            {
                mainAccount.Balance += account.Balance;
                _savingsRepository.Update(mainAccount);

                var amountTransferred = account.Balance;
                var sourceAccountNumber = account.AccountNumber;
                
                var transaction = new Transaction
                {
                    Type = TransactionType.Credit,
                    Amount = amountTransferred,
                    Date = DateTime.UtcNow,
                    Status = TransactionStatus.Approved,
                    Category = TransactionCategory.SavingsTransfer,
                    Origin = $"Cierre Cta {sourceAccountNumber}",
                    Beneficiary = mainAccount.AccountNumber,
                    SavingsAccountId = mainAccount.Id
                };
                await _transactionRepository.AddAsync(transaction);
                await _transactionRepository.SaveChangesAsync();

                TempData["Success"] = $"Cuenta {account.AccountNumber} cancelada. Los RD$ {amountTransferred:N2} sobrantes fueron transferidos a su Cuenta Principal (#{mainAccount.AccountNumber}).";
            }
        }
        else
        {
            TempData["Success"] = $"Cuenta Secundaria {account.AccountNumber} cancelada satisfactoriamente.";
        }

        // En ArtemisBanking las cancelaciones se manejan por un soft-delete (IsActive = false).
        // Si el PRD pedía un Delete físico:
        // _savingsRepository.DeleteAsync(account);
        // Usaremos Soft-delete por seguridad bancaria
        account.IsActive = false;
        account.Balance = 0; // Balance queda en 0 tras mover a la principal.
        
        _savingsRepository.Update(account);
        await _savingsRepository.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
