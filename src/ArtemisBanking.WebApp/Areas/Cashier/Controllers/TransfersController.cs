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
public class TransfersController : Controller
{
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly IGenericRepository<Transaction, int> _transactionRepository;
    private readonly IEmailService _emailService;

    public TransfersController(
        IGenericRepository<SavingsAccount, int> savingsRepository,
        IGenericRepository<Transaction, int> transactionRepository,
        IEmailService emailService)
    {
        _savingsRepository = savingsRepository;
        _transactionRepository = transactionRepository;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult TransferThirdParty()
    {
        return View(new ThirdPartyTransferViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferThirdParty(ThirdPartyTransferViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // No se puede transferir a la misma cuenta
        if (model.SourceAccountNumber == model.DestinationAccountNumber)
        {
            ModelState.AddModelError("DestinationAccountNumber", "No puedes transferir a la misma cuenta de origen.");
            return View(model);
        }

        // Validar cuenta origen
        var sourceAccount = await _savingsRepository.Query()
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == model.SourceAccountNumber && a.IsActive);

        if (sourceAccount == null)
        {
            ModelState.AddModelError("SourceAccountNumber", "La cuenta origen no existe o está inactiva.");
            return View(model);
        }

        // Validar cuenta destino
        var destinationAccount = await _savingsRepository.Query()
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == model.DestinationAccountNumber && a.IsActive);

        if (destinationAccount == null)
        {
            ModelState.AddModelError("DestinationAccountNumber", "La cuenta destino no existe o está inactiva.");
            return View(model);
        }

        // Validar fondos
        if (sourceAccount.Balance < model.Amount)
        {
            ModelState.AddModelError("Amount", $"Fondos insuficientes. Disponible: RD$ {sourceAccount.Balance:N2}");
            return View(model);
        }

        // Guardar en sesión
        HttpContext.Session.SetString("TransferSourceAccountNumber", model.SourceAccountNumber);
        HttpContext.Session.SetString("TransferDestinationAccountNumber", model.DestinationAccountNumber);
        HttpContext.Session.SetDecimal("TransferAmount", model.Amount);
        HttpContext.Session.SetString("TransferDestinationHolderName", $"{destinationAccount.Client.FirstName} {destinationAccount.Client.LastName}");
        HttpContext.Session.SetString("TransferSourceHolderName", $"{sourceAccount.Client.FirstName} {sourceAccount.Client.LastName}");
        HttpContext.Session.SetDecimal("TransferSourceBalance", sourceAccount.Balance);
        HttpContext.Session.SetDecimal("TransferDestinationBalance", destinationAccount.Balance);

        return RedirectToAction(nameof(ConfirmTransfer));
    }

    [HttpGet]
    public IActionResult ConfirmTransfer()
    {
        var sourceAccountNumber = HttpContext.Session.GetString("TransferSourceAccountNumber");
        var destinationAccountNumber = HttpContext.Session.GetString("TransferDestinationAccountNumber");
        var amount = HttpContext.Session.GetDecimal("TransferAmount");
        var destinationHolderName = HttpContext.Session.GetString("TransferDestinationHolderName");
        var sourceHolderName = HttpContext.Session.GetString("TransferSourceHolderName");
        var sourceBalance = HttpContext.Session.GetDecimal("TransferSourceBalance");
        var destinationBalance = HttpContext.Session.GetDecimal("TransferDestinationBalance");

        if (string.IsNullOrEmpty(sourceAccountNumber) || string.IsNullOrEmpty(destinationAccountNumber))
            return RedirectToAction(nameof(TransferThirdParty));

        var model = new ThirdPartyTransferConfirmationViewModel
        {
            SourceAccountNumber = sourceAccountNumber,
            DestinationAccountNumber = destinationAccountNumber,
            DestinationAccountHolderName = destinationHolderName ?? string.Empty,
            SourceAccountHolderName = sourceHolderName ?? string.Empty,
            Amount = amount,
            SourceAccountBalance = sourceBalance,
            DestinationAccountBalance = destinationBalance
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmTransfer(ThirdPartyTransferConfirmationViewModel model)
    {
        var sourceAccountNumber = HttpContext.Session.GetString("TransferSourceAccountNumber");
        var destinationAccountNumber = HttpContext.Session.GetString("TransferDestinationAccountNumber");
        var amount = HttpContext.Session.GetDecimal("TransferAmount");

        if (string.IsNullOrEmpty(sourceAccountNumber) || string.IsNullOrEmpty(destinationAccountNumber) || amount <= 0)
            return RedirectToAction(nameof(TransferThirdParty));

        var sourceAccount = await _savingsRepository.Query()
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == sourceAccountNumber && a.IsActive);

        var destinationAccount = await _savingsRepository.Query()
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == destinationAccountNumber && a.IsActive);

        if (sourceAccount == null || destinationAccount == null || sourceAccount.Balance < amount)
        {
            TempData["Error"] = "No se pudo procesar la transferencia. Datos inválidos o fondos insuficientes.";
            return RedirectToAction("Index", "Home");
        }

        var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            // Debitar cuenta origen
            sourceAccount.Balance -= amount;
            _savingsRepository.Update(sourceAccount);

            // Acreditar cuenta destino
            destinationAccount.Balance += amount;
            _savingsRepository.Update(destinationAccount);

            // Registrar transacción en cuenta ORIGEN (DÉBITO)
            await _transactionRepository.AddAsync(new Transaction
            {
                Type = TransactionType.Debit,
                Amount = amount,
                Category = TransactionCategory.CashierThirdPartyTransfer,
                Status = TransactionStatus.Approved,
                Origin = sourceAccountNumber,
                Beneficiary = destinationAccountNumber,
                SavingsAccountId = sourceAccount.Id,
                CashierId = cashierId,
                Date = DateTime.UtcNow
            });

            // Registrar transacción en cuenta DESTINO (CRÉDITO)
            await _transactionRepository.AddAsync(new Transaction
            {
                Type = TransactionType.Credit,
                Amount = amount,
                Category = TransactionCategory.CashierThirdPartyTransferReceived,
                Status = TransactionStatus.Approved,
                Origin = sourceAccountNumber,
                Beneficiary = destinationAccountNumber,
                SavingsAccountId = destinationAccount.Id,
                CashierId = cashierId,
                Date = DateTime.UtcNow
            });

            await _savingsRepository.SaveChangesAsync();

            // Email 1: Para titular de cuenta ORIGEN
            var last4DestinationDigits = destinationAccountNumber.Substring(destinationAccountNumber.Length - 4);
            await _emailService.SendAsync(new EmailRequestDto
            {
                To = sourceAccount.Client.Email!,
                Subject = $"Transacción realizada a la cuenta {last4DestinationDigits}",
                Body = EmailTemplates.ThirdPartyTransferSentNotification(
                    $"{sourceAccount.Client.FirstName} {sourceAccount.Client.LastName}",
                    amount,
                    last4DestinationDigits,
                    DateTime.UtcNow)
            });

            // Email 2: Para titular de cuenta DESTINO
            var last4SourceDigits = sourceAccountNumber.Substring(sourceAccountNumber.Length - 4);
            await _emailService.SendAsync(new EmailRequestDto
            {
                To = destinationAccount.Client.Email!,
                Subject = $"Transacción enviada desde la cuenta {last4SourceDigits}",
                Body = EmailTemplates.ThirdPartyTransferReceivedNotification(
                    $"{destinationAccount.Client.FirstName} {destinationAccount.Client.LastName}",
                    amount,
                    last4SourceDigits,
                    DateTime.UtcNow)
            });

            TempData["Success"] = $"Transferencia de RD$ {amount:N2} a la cuenta {destinationAccountNumber} realizada exitosamente.";

            // Limpiar sesión
            HttpContext.Session.Remove("TransferSourceAccountNumber");
            HttpContext.Session.Remove("TransferDestinationAccountNumber");
            HttpContext.Session.Remove("TransferAmount");
            HttpContext.Session.Remove("TransferDestinationHolderName");
            HttpContext.Session.Remove("TransferSourceHolderName");
            HttpContext.Session.Remove("TransferSourceBalance");
            HttpContext.Session.Remove("TransferDestinationBalance");

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error procesando la transferencia: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CancelTransfer()
    {
        HttpContext.Session.Remove("TransferSourceAccountNumber");
        HttpContext.Session.Remove("TransferDestinationAccountNumber");
        HttpContext.Session.Remove("TransferAmount");
        HttpContext.Session.Remove("TransferDestinationHolderName");
        HttpContext.Session.Remove("TransferSourceHolderName");
        HttpContext.Session.Remove("TransferSourceBalance");
        HttpContext.Session.Remove("TransferDestinationBalance");

        return RedirectToAction("Index", "Home");
    }
}