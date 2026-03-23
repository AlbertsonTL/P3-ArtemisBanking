using ArtemisBanking.Application.DTOs.Email;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Infrastructure.Services;
using ArtemisBanking.WebApp.Extensions;
using ArtemisBanking.WebApp.ViewModels.Cashier;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtemisBanking.WebApp.Areas.Cashier.Controllers;

[Area("Cashier")]
[Authorize(Roles = "Cajero")]
public class WithdrawalsController : Controller
{
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly IGenericRepository<Transaction, int> _transactionRepository;
    private readonly IEmailService _emailService;

    public WithdrawalsController(
        IGenericRepository<SavingsAccount, int> savingsRepository,
        IGenericRepository<Transaction, int> transactionRepository,
        IEmailService emailService)
    {
        _savingsRepository = savingsRepository;
        _transactionRepository = transactionRepository;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Withdrawal()
    {
        return View(new WithdrawalViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdrawal(WithdrawalViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var account = await _savingsRepository.Query()
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == model.AccountNumber && a.IsActive);

        if (account == null)
        {
            ModelState.AddModelError("AccountNumber", "La cuenta no existe o está inactiva.");
            return View(model);
        }

        if (account.Balance < model.Amount)
        {
            ModelState.AddModelError("Amount", $"Saldo insuficiente. Disponible: RD$ {account.Balance:N2}");
            return View(model);
        }

        HttpContext.Session.SetString("WithdrawalAccountNumber", model.AccountNumber);
        HttpContext.Session.SetDecimal("WithdrawalAmount", model.Amount);
        HttpContext.Session.SetString("WithdrawalAccountHolderName", $"{account.Client.FirstName} {account.Client.LastName}");
        HttpContext.Session.SetDecimal("WithdrawalCurrentBalance", account.Balance);

        return RedirectToAction(nameof(Confirm));
    }

    [HttpGet]
    public IActionResult Confirm()
    {
        var accountNumber = HttpContext.Session.GetString("WithdrawalAccountNumber");
        var amount = HttpContext.Session.GetDecimal("WithdrawalAmount");
        var holderName = HttpContext.Session.GetString("WithdrawalAccountHolderName");
        var currentBalance = HttpContext.Session.GetDecimal("WithdrawalCurrentBalance");

        if (string.IsNullOrEmpty(accountNumber))
            return RedirectToAction(nameof(Withdrawal));

        var model = new WithdrawalConfirmationViewModel
        {
            AccountNumber = accountNumber,
            Amount = amount,
            AccountHolderName = holderName ?? string.Empty,
            CurrentBalance = currentBalance
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(WithdrawalConfirmationViewModel model)
    {
        var accountNumber = HttpContext.Session.GetString("WithdrawalAccountNumber");
        var amount = HttpContext.Session.GetDecimal("WithdrawalAmount");

        if (string.IsNullOrEmpty(accountNumber) || amount <= 0)
            return RedirectToAction(nameof(Withdrawal));

        var account = await _savingsRepository.Query()
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber && a.IsActive);

        if (account == null || account.Balance < amount)
        {
            TempData["Error"] = "Operación no válida. Fondos insuficientes.";
            return RedirectToAction("Index", "Home");
        }

        var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            account.Balance -= amount;
            _savingsRepository.Update(account);

            await _transactionRepository.AddAsync(new Transaction
            {
                Type = TransactionType.Debit,
                Amount = amount,
                Category = TransactionCategory.CashierWithdrawal,
                Status = TransactionStatus.Approved,
                Origin = accountNumber,
                Beneficiary = "RETIRO",
                SavingsAccountId = account.Id,
                CashierId = cashierId,
                Date = DateTime.UtcNow
            });

            await _savingsRepository.SaveChangesAsync();

            var last4Digits = accountNumber.Substring(accountNumber.Length - 4);
            await _emailService.SendAsync(new EmailRequestDto
            {
                To = account.Client.Email!,
                Subject = $"Retiro realizado de su cuenta {last4Digits}",
                Body = EmailTemplates.WithdrawalNotification(
                    $"{account.Client.FirstName} {account.Client.LastName}",
                    amount,
                    accountNumber,
                    DateTime.UtcNow)
            });

            TempData["Success"] = $"Retiro de RD$ {amount:N2} procesado exitosamente de la cuenta {accountNumber}.";

            HttpContext.Session.Remove("WithdrawalAccountNumber");
            HttpContext.Session.Remove("WithdrawalAmount");
            HttpContext.Session.Remove("WithdrawalAccountHolderName");
            HttpContext.Session.Remove("WithdrawalCurrentBalance");

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error procesando el retiro: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel()
    {
        HttpContext.Session.Remove("WithdrawalAccountNumber");
        HttpContext.Session.Remove("WithdrawalAmount");
        HttpContext.Session.Remove("WithdrawalAccountHolderName");
        HttpContext.Session.Remove("WithdrawalCurrentBalance");

        return RedirectToAction("Index", "Home");
    }
}