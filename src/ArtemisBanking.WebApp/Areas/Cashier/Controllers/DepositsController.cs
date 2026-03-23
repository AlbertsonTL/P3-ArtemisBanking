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
public class DepositsController : Controller
{
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly IGenericRepository<Transaction, int> _transactionRepository;
    private readonly IEmailService _emailService;

    public DepositsController(
        IGenericRepository<SavingsAccount, int> savingsRepository,
        IGenericRepository<Transaction, int> transactionRepository,
        IEmailService emailService)
    {
        _savingsRepository = savingsRepository;
        _transactionRepository = transactionRepository;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Deposit()
    {
        return View(new DepositViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deposit(DepositViewModel model)
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

        HttpContext.Session.SetString("DepositAccountNumber", model.AccountNumber);
        HttpContext.Session.SetDecimal("DepositAmount", model.Amount);
        HttpContext.Session.SetString("DepositAccountHolderName", $"{account.Client.FirstName} {account.Client.LastName}");
        HttpContext.Session.SetDecimal("DepositCurrentBalance", account.Balance);

        return RedirectToAction(nameof(Confirm));
    }

    [HttpGet]
    public IActionResult Confirm()
    {
        var accountNumber = HttpContext.Session.GetString("DepositAccountNumber");
        var amount = HttpContext.Session.GetDecimal("DepositAmount");
        var holderName = HttpContext.Session.GetString("DepositAccountHolderName");
        var currentBalance = HttpContext.Session.GetDecimal("DepositCurrentBalance");

        if (string.IsNullOrEmpty(accountNumber))
            return RedirectToAction(nameof(Deposit));

        var model = new DepositConfirmationViewModel
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
    public async Task<IActionResult> Confirm(DepositConfirmationViewModel model)
    {
        var accountNumber = HttpContext.Session.GetString("DepositAccountNumber");
        var amount = HttpContext.Session.GetDecimal("DepositAmount");

        if (string.IsNullOrEmpty(accountNumber) || amount <= 0)
            return RedirectToAction(nameof(Deposit));

        var account = await _savingsRepository.Query()
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber && a.IsActive);

        if (account == null)
        {
            TempData["Error"] = "La cuenta no existe o está inactiva.";
            return RedirectToAction("Index", "Home");
        }

        var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            account.Balance += amount;
            _savingsRepository.Update(account);

            await _transactionRepository.AddAsync(new Transaction
            {
                Type = TransactionType.Credit,
                Amount = amount,
                Category = TransactionCategory.CashierDeposit,
                Status = TransactionStatus.Approved,
                Origin = "DEPÓSITO",
                Beneficiary = accountNumber,
                SavingsAccountId = account.Id,
                CashierId = cashierId,
                Date = DateTime.UtcNow
            });

            await _savingsRepository.SaveChangesAsync();

            var last4Digits = accountNumber.Substring(accountNumber.Length - 4);
            await _emailService.SendAsync(new EmailRequestDto
            {
                To = account.Client.Email!,
                Subject = $"Depósito realizado a su cuenta {last4Digits}",
                Body = EmailTemplates.DepositNotification(
                    $"{account.Client.FirstName} {account.Client.LastName}",
                    amount,
                    accountNumber,
                    DateTime.UtcNow)
            });

            TempData["Success"] = $"Depósito de RD$ {amount:N2} realizado exitosamente a la cuenta {accountNumber}.";

            HttpContext.Session.Remove("DepositAccountNumber");
            HttpContext.Session.Remove("DepositAmount");
            HttpContext.Session.Remove("DepositAccountHolderName");
            HttpContext.Session.Remove("DepositCurrentBalance");

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error procesando el depósito: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel()
    {
        HttpContext.Session.Remove("DepositAccountNumber");
        HttpContext.Session.Remove("DepositAmount");
        HttpContext.Session.Remove("DepositAccountHolderName");
        HttpContext.Session.Remove("DepositCurrentBalance");

        return RedirectToAction("Index", "Home");
    }
}