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
public class PaymentsController : Controller
{
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly IGenericRepository<CreditCard, int> _cardRepository;
    private readonly IGenericRepository<Loan, int> _loanRepository;
    private readonly IGenericRepository<Transaction, int> _transactionRepository;
    private readonly IEmailService _emailService;
    private readonly ILoanService _loanService;

    public PaymentsController(
        IGenericRepository<SavingsAccount, int> savingsRepository,
        IGenericRepository<CreditCard, int> cardRepository,
        IGenericRepository<Loan, int> loanRepository,
        IGenericRepository<Transaction, int> transactionRepository,
        IEmailService emailService,
        ILoanService loanService)
    {
        _savingsRepository = savingsRepository;
        _cardRepository = cardRepository;
        _loanRepository = loanRepository;
        _transactionRepository = transactionRepository;
        _emailService = emailService;
        _loanService = loanService;
    }

    [HttpGet]
    public IActionResult PayCreditCard()
    {
        return View(new CreditCardPaymentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayCreditCard(CreditCardPaymentViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var sourceAccount = await _savingsRepository.Query()
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == model.AccountNumber && a.IsActive);

        if (sourceAccount == null)
        {
            ModelState.AddModelError("AccountNumber", "La cuenta origen no existe o esta inactiva.");
            return View(model);
        }

        var creditCard = await _cardRepository.Query()
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.CardNumber == model.CardNumber && c.IsActive);

        if (creditCard == null)
        {
            ModelState.AddModelError("CardNumber", "La tarjeta de credito no existe o esta inactiva.");
            return View(model);
        }

        if (sourceAccount.Balance < model.Amount)
        {
            ModelState.AddModelError("Amount", $"Fondos insuficientes. Disponible: RD$ {sourceAccount.Balance:N2}");
            return View(model);
        }

        decimal actualAmountToCharge = Math.Min(model.Amount, creditCard.CurrentDebt);

        HttpContext.Session.SetString("PaymentSourceAccountNumber", model.AccountNumber);
        HttpContext.Session.SetString("PaymentCardNumber", model.CardNumber);
        HttpContext.Session.SetDecimal("PaymentAmount", model.Amount);
        HttpContext.Session.SetDecimal("PaymentActualAmountToCharge", actualAmountToCharge);
        HttpContext.Session.SetString("PaymentType", "CreditCard");
        HttpContext.Session.SetString("PaymentCardHolderName", $"{creditCard.Client.FirstName} {creditCard.Client.LastName}");
        HttpContext.Session.SetDecimal("PaymentCardCurrentDebt", creditCard.CurrentDebt);

        return RedirectToAction(nameof(ConfirmPayment));
    }

    [HttpGet]
    public IActionResult PayLoan()
    {
        return View(new LoanPaymentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayLoan(LoanPaymentViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var sourceAccount = await _savingsRepository.Query()
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == model.AccountNumber && a.IsActive);

        if (sourceAccount == null)
        {
            ModelState.AddModelError("AccountNumber", "La cuenta origen no existe o esta inactiva.");
            return View(model);
        }

        var loan = await _loanRepository.Query()
            .Include(l => l.Client)
            .FirstOrDefaultAsync(l => l.LoanNumber == model.LoanNumber && l.Status == LoanStatus.Active);

        if (loan == null)
        {
            ModelState.AddModelError("LoanNumber", "El prestamo no existe o no esta activo.");
            return View(model);
        }

        if (sourceAccount.Balance < model.Amount)
        {
            ModelState.AddModelError("Amount", $"Fondos insuficientes. Disponible: RD$ {sourceAccount.Balance:N2}");
            return View(model);
        }

        decimal remainingDebt = loan.Amount - loan.AmountPaid;
        decimal actualAmountToCharge = Math.Min(model.Amount, remainingDebt);
        decimal excessAmount = model.Amount - actualAmountToCharge;

        int pendingQuotas = loan.AmortizationEntries?.Count(q => !q.IsPaid) ?? 0;

        HttpContext.Session.SetString("PaymentSourceAccountNumber", model.AccountNumber);
        HttpContext.Session.SetString("PaymentLoanNumber", model.LoanNumber);
        HttpContext.Session.SetDecimal("PaymentAmount", model.Amount);
        HttpContext.Session.SetDecimal("PaymentActualAmountToCharge", actualAmountToCharge);
        HttpContext.Session.SetDecimal("PaymentExcessAmount", excessAmount);
        HttpContext.Session.SetString("PaymentType", "Loan");
        HttpContext.Session.SetString("PaymentLoanHolderName", $"{loan.Client.FirstName} {loan.Client.LastName}");
        HttpContext.Session.SetDecimal("PaymentRemainingDebt", remainingDebt);
        HttpContext.Session.SetInt("PaymentPendingQuotas", pendingQuotas);

        return RedirectToAction(nameof(ConfirmPayment));
    }

    [HttpGet]
    public IActionResult ConfirmPayment()
    {
        var paymentType = HttpContext.Session.GetString("PaymentType");

        if (string.IsNullOrEmpty(paymentType))
            return RedirectToAction(nameof(PayCreditCard));

        if (paymentType == "CreditCard")
        {
            var cardNumber    = HttpContext.Session.GetString("PaymentCardNumber") ?? string.Empty;
            var amount        = HttpContext.Session.GetDecimal("PaymentAmount");
            var actualAmount  = HttpContext.Session.GetDecimal("PaymentActualAmountToCharge");
            var cardHolderName = HttpContext.Session.GetString("PaymentCardHolderName") ?? string.Empty;
            var accountNumber = HttpContext.Session.GetString("PaymentSourceAccountNumber") ?? string.Empty;
            var currentDebt   = HttpContext.Session.GetDecimal("PaymentCardCurrentDebt");

            var model = new CreditCardPaymentConfirmationViewModel
            {
                AccountNumber       = accountNumber,
                CardNumber          = cardNumber,
                CardHolderName      = cardHolderName,
                Amount              = amount,
                CurrentCardDebt     = currentDebt,
                ActualAmountToCharge = actualAmount
            };

            return View("ConfirmCreditCardPayment", model);
        }
        else
        {
            var loanNumber    = HttpContext.Session.GetString("PaymentLoanNumber") ?? string.Empty;
            var amount        = HttpContext.Session.GetDecimal("PaymentAmount");
            var actualAmount  = HttpContext.Session.GetDecimal("PaymentActualAmountToCharge");
            var loanHolderName = HttpContext.Session.GetString("PaymentLoanHolderName") ?? string.Empty;
            var accountNumber = HttpContext.Session.GetString("PaymentSourceAccountNumber") ?? string.Empty;
            var remainingDebt = HttpContext.Session.GetDecimal("PaymentRemainingDebt");
            var excessAmount  = HttpContext.Session.GetDecimal("PaymentExcessAmount");
            var pendingQuotas = HttpContext.Session.GetInt("PaymentPendingQuotas");

            var model = new LoanPaymentConfirmationViewModel
            {
                AccountNumber        = accountNumber,
                LoanNumber           = loanNumber,
                LoanHolderName       = loanHolderName,
                Amount               = amount,
                RemainingDebt        = remainingDebt,
                ActualAmountToCharge = actualAmount,
                ExcessAmount         = excessAmount,
                PendingQuotas        = pendingQuotas
            };

            return View("ConfirmLoanPayment", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(IFormCollection form)
    {
        var paymentType = HttpContext.Session.GetString("PaymentType");

        if (paymentType == "CreditCard")
            return await ConfirmCreditCardPayment();
        else
            return await ConfirmLoanPayment();
    }

    private async Task<IActionResult> ConfirmCreditCardPayment()
    {
        var accountNumber        = HttpContext.Session.GetString("PaymentSourceAccountNumber");
        var cardNumber           = HttpContext.Session.GetString("PaymentCardNumber");
        var actualAmountToCharge = HttpContext.Session.GetDecimal("PaymentActualAmountToCharge");

        if (string.IsNullOrEmpty(accountNumber) || actualAmountToCharge <= 0)
            return RedirectToAction(nameof(PayCreditCard));

        var sourceAccount = await _savingsRepository.Query()
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber && a.IsActive);

        var creditCard = await _cardRepository.Query()
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.CardNumber == cardNumber && c.IsActive);

        if (sourceAccount == null || creditCard == null || sourceAccount.Balance < actualAmountToCharge)
        {
            TempData["Error"] = "No se pudo procesar el pago. Datos invalidos o fondos insuficientes.";
            return RedirectToAction("Index", "Home");
        }

        var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        try
        {
            sourceAccount.Balance -= actualAmountToCharge;
            _savingsRepository.Update(sourceAccount);

            creditCard.CurrentDebt -= actualAmountToCharge;
            if (creditCard.CurrentDebt < 0) creditCard.CurrentDebt = 0;
            _cardRepository.Update(creditCard);

            await _transactionRepository.AddAsync(new Transaction
            {
                Type             = TransactionType.Debit,
                Amount           = actualAmountToCharge,
                Category         = TransactionCategory.CashierCreditCardPayment,
                Status           = TransactionStatus.Approved,
                Origin           = accountNumber,
                Beneficiary      = cardNumber ?? string.Empty,
                SavingsAccountId = sourceAccount.Id,
                CashierId        = cashierId,
                Date             = DateTime.UtcNow
            });

            await _savingsRepository.SaveChangesAsync();

            var last4CardDigits = (cardNumber ?? string.Empty).Length >= 4
                ? cardNumber!.Substring(cardNumber.Length - 4)
                : cardNumber ?? string.Empty;

            var clientEmail = sourceAccount.Client?.Email ?? string.Empty;
            if (!string.IsNullOrEmpty(clientEmail))
            {
                var clientName = $"{sourceAccount.Client!.FirstName} {sourceAccount.Client.LastName}";
                await _emailService.SendAsync(new EmailRequestDto
                {
                    To      = clientEmail,
                    Subject = $"Pago realizado a la tarjeta {last4CardDigits}",
                    Body    = EmailTemplates.CreditCardPaymentNotification(
                        clientName,
                        actualAmountToCharge,
                        accountNumber,
                        last4CardDigits,
                        DateTime.UtcNow)
                });
            }

            TempData["Success"] = $"Pago de RD$ {actualAmountToCharge:N2} a tarjeta {last4CardDigits} realizado exitosamente.";

            HttpContext.Session.Remove("PaymentSourceAccountNumber");
            HttpContext.Session.Remove("PaymentCardNumber");
            HttpContext.Session.Remove("PaymentAmount");
            HttpContext.Session.Remove("PaymentActualAmountToCharge");
            HttpContext.Session.Remove("PaymentType");
            HttpContext.Session.Remove("PaymentCardHolderName");
            HttpContext.Session.Remove("PaymentCardCurrentDebt");

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error procesando el pago: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }

    private async Task<IActionResult> ConfirmLoanPayment()
    {
        var accountNumber        = HttpContext.Session.GetString("PaymentSourceAccountNumber");
        var loanNumber           = HttpContext.Session.GetString("PaymentLoanNumber");
        var actualAmountToCharge = HttpContext.Session.GetDecimal("PaymentActualAmountToCharge");
        var excessAmount         = HttpContext.Session.GetDecimal("PaymentExcessAmount");

        if (string.IsNullOrEmpty(accountNumber) || actualAmountToCharge <= 0)
            return RedirectToAction(nameof(PayLoan));

        var sourceAccount = await _savingsRepository.Query()
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber && a.IsActive);

        var loan = await _loanRepository.Query()
            .Include(l => l.Client)
            .Include(l => l.AmortizationEntries)
            .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber && l.Status == LoanStatus.Active);

        if (sourceAccount == null || loan == null || sourceAccount.Balance < actualAmountToCharge)
        {
            TempData["Error"] = "No se pudo procesar el pago. Datos invalidos o fondos insuficientes.";
            return RedirectToAction("Index", "Home");
        }

        var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        try
        {
            await _loanService.ProcessSequentialPaymentAsync(loan.Id, actualAmountToCharge);

            sourceAccount.Balance -= actualAmountToCharge;
            _savingsRepository.Update(sourceAccount);

            if (excessAmount > 0)
            {
                sourceAccount.Balance += excessAmount;
                _savingsRepository.Update(sourceAccount);
            }

            await _transactionRepository.AddAsync(new Transaction
            {
                Type             = TransactionType.Debit,
                Amount           = actualAmountToCharge,
                Category         = TransactionCategory.CashierLoanPayment,
                Status           = TransactionStatus.Approved,
                Origin           = accountNumber,
                Beneficiary      = loanNumber ?? string.Empty,
                SavingsAccountId = sourceAccount.Id,
                CashierId        = cashierId,
                Date             = DateTime.UtcNow
            });

            await _savingsRepository.SaveChangesAsync();

            var last4AccountDigits = accountNumber.Length >= 4
                ? accountNumber.Substring(accountNumber.Length - 4)
                : accountNumber;

            var clientEmail = sourceAccount.Client?.Email ?? string.Empty;
            if (!string.IsNullOrEmpty(clientEmail))
            {
                var clientName   = $"{sourceAccount.Client!.FirstName} {sourceAccount.Client.LastName}";
                var safeLoanNumber = loanNumber ?? string.Empty;
                await _emailService.SendAsync(new EmailRequestDto
                {
                    To      = clientEmail,
                    Subject = $"Pago realizado al prestamo {safeLoanNumber}",
                    Body    = EmailTemplates.LoanPaymentNotification(
                        clientName,
                        actualAmountToCharge,
                        last4AccountDigits,
                        safeLoanNumber,
                        DateTime.UtcNow,
                        excessAmount)
                });
            }

            string message = $"Pago de RD$ {actualAmountToCharge:N2} al prestamo {loanNumber} realizado exitosamente.";
            if (excessAmount > 0)
                message += $" Excedente de RD$ {excessAmount:N2} retornado a la cuenta.";

            TempData["Success"] = message;

            HttpContext.Session.Remove("PaymentSourceAccountNumber");
            HttpContext.Session.Remove("PaymentLoanNumber");
            HttpContext.Session.Remove("PaymentAmount");
            HttpContext.Session.Remove("PaymentActualAmountToCharge");
            HttpContext.Session.Remove("PaymentExcessAmount");
            HttpContext.Session.Remove("PaymentType");
            HttpContext.Session.Remove("PaymentLoanHolderName");
            HttpContext.Session.Remove("PaymentRemainingDebt");
            HttpContext.Session.Remove("PaymentPendingQuotas");

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error procesando el pago: {ex.Message}";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CancelPayment()
    {
        HttpContext.Session.Remove("PaymentSourceAccountNumber");
        HttpContext.Session.Remove("PaymentCardNumber");
        HttpContext.Session.Remove("PaymentLoanNumber");
        HttpContext.Session.Remove("PaymentAmount");
        HttpContext.Session.Remove("PaymentActualAmountToCharge");
        HttpContext.Session.Remove("PaymentExcessAmount");
        HttpContext.Session.Remove("PaymentType");
        HttpContext.Session.Remove("PaymentCardHolderName");
        HttpContext.Session.Remove("PaymentCardCurrentDebt");
        HttpContext.Session.Remove("PaymentLoanHolderName");
        HttpContext.Session.Remove("PaymentRemainingDebt");
        HttpContext.Session.Remove("PaymentPendingQuotas");

        return RedirectToAction("Index", "Home");
    }
}
