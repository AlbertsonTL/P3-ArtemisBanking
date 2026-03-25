using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.WebApp.ViewModels.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtemisBanking.WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = "Cliente")]
public class TransactionsController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly IGenericRepository<CreditCard, int> _cardRepository;
    private readonly IGenericRepository<Loan, int> _loanRepository;
    private readonly IGenericRepository<Beneficiary, int> _beneficiaryRepository;

    public TransactionsController(
        ITransactionService transactionService,
        IGenericRepository<SavingsAccount, int> savingsRepository,
        IGenericRepository<CreditCard, int> cardRepository,
        IGenericRepository<Loan, int> loanRepository,
        IGenericRepository<Beneficiary, int> beneficiaryRepository)
    {
        _transactionService = transactionService;
        _savingsRepository = savingsRepository;
        _cardRepository = cardRepository;
        _loanRepository = loanRepository;
        _beneficiaryRepository = beneficiaryRepository;
    }

    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> OwnAccounts()
    {
        var model = new OwnAccountsTransferViewModel { MyAccounts = await GetMyAccountsList() };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OwnAccounts(OwnAccountsTransferViewModel model)
    {
        if (!ModelState.IsValid) { model.MyAccounts = await GetMyAccountsList(); return View(model); }
        if (model.SourceAccountNumber == model.DestinationAccountNumber)
        {
            ModelState.AddModelError("", "La cuenta de origen y destino no pueden ser la misma.");
            model.MyAccounts = await GetMyAccountsList(); return View(model);
        }

        var success = await _transactionService.TransferBetweenOwnAccountsAsync(GetClientId(), model.SourceAccountNumber, model.DestinationAccountNumber, model.Amount);
        if (success) { TempData["Success"] = "Transferencia entre cuentas propias realizada."; return RedirectToAction(nameof(Index)); }
        
        TempData["Error"] = "Fondos insuficientes o cuenta inválida.";
        model.MyAccounts = await GetMyAccountsList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Express()
    {
        var model = new ExpressTransferViewModel { MyAccounts = await GetMyAccountsList() };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Express(ExpressTransferViewModel model)
    {
        if (!ModelState.IsValid) { model.MyAccounts = await GetMyAccountsList(); return View(model); }

        var success = await _transactionService.ExpressTransactionAsync(GetClientId(), model.SourceAccountNumber, model.DestinationAccountNumber, model.Amount);
        if (success) { TempData["Success"] = "Transferencia Express realizada con éxito."; return RedirectToAction(nameof(Index)); }
        
        TempData["Error"] = "Error al procesar transferencia Express. Verifique saldo y cuenta destino.";
        model.MyAccounts = await GetMyAccountsList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Beneficiaries()
    {
        var model = new BeneficiaryTransferViewModel { 
            MyAccounts = await GetMyAccountsList(),
            Beneficiaries = await GetBeneficiariesList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Beneficiaries(BeneficiaryTransferViewModel model)
    {
        if (!ModelState.IsValid) { 
            model.MyAccounts = await GetMyAccountsList(); 
            model.Beneficiaries = await GetBeneficiariesList();
            return View(model); 
        }

        var success = await _transactionService.TransferToBeneficiaryAsync(GetClientId(), model.SourceAccountNumber, model.DestinationAccountNumber, model.Amount);
        if (success) { TempData["Success"] = "Transferencia a beneficiario enviada."; return RedirectToAction(nameof(Index)); }
        
        TempData["Error"] = "No se pudo realizar la transferencia al beneficiario.";
        model.MyAccounts = await GetMyAccountsList();
        model.Beneficiaries = await GetBeneficiariesList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> PayCreditCard()
    {
        var model = new CreditCardPaymentViewModel { 
            MyAccounts = await GetMyAccountsList(),
            MyCards = await GetMyCardsList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayCreditCard(CreditCardPaymentViewModel model)
    {
        if (!ModelState.IsValid) { 
            model.MyAccounts = await GetMyAccountsList(); 
            model.MyCards = await GetMyCardsList();
            return View(model); 
        }

        var success = await _transactionService.PayCreditCardAsync(GetClientId(), model.SourceAccountNumber, model.CreditCardId, model.Amount);
        if (success) { TempData["Success"] = "Pago de tarjeta de crédito acreditado."; return RedirectToAction(nameof(Index)); }
        
        TempData["Error"] = "Error al pagar tarjeta. Verifique fondos o deuda.";
        model.MyAccounts = await GetMyAccountsList();
        model.MyCards = await GetMyCardsList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> PayLoan()
    {
        var model = new LoanPaymentViewModel { 
            MyAccounts = await GetMyAccountsList(),
            MyLoans = await GetMyLoansList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayLoan(LoanPaymentViewModel model)
    {
        if (!ModelState.IsValid) { 
            model.MyAccounts = await GetMyAccountsList(); 
            model.MyLoans = await GetMyLoansList();
            return View(model); 
        }

        var success = await _transactionService.PayLoanAsync(GetClientId(), model.SourceAccountNumber, model.LoanId, model.Amount);
        if (success) { TempData["Success"] = "Pago de préstamo procesado secuencialmente."; return RedirectToAction(nameof(Index)); }
        
        TempData["Error"] = "Error al pagar préstamo. El monto debe cubrir al menos una cuota.";
        model.MyAccounts = await GetMyAccountsList();
        model.MyLoans = await GetMyLoansList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CashAdvance()
    {
        var model = new CashAdvanceViewModel { 
            MyCards = await GetMyCardsList(),
            MyAccounts = await GetMyAccountsList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CashAdvance(CashAdvanceViewModel model)
    {
        if (!ModelState.IsValid) { 
            model.MyCards = await GetMyCardsList();
            model.MyAccounts = await GetMyAccountsList();
            return View(model); 
        }

        var success = await _transactionService.CashAdvanceAsync(GetClientId(), model.CreditCardId, model.DestinationAccountNumber, model.Amount);
        if (success) { TempData["Success"] = "Avance de efectivo depositado en su cuenta."; return RedirectToAction(nameof(Index)); }
        
        TempData["Error"] = "Avance rechazado por límite de crédito insuficiente.";
        model.MyCards = await GetMyCardsList();
        model.MyAccounts = await GetMyAccountsList();
        return View(model);
    }

    private string GetClientId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task<List<SelectListItem>> GetMyAccountsList()
    {
        var id = GetClientId();
        var accounts = await _savingsRepository.Query().Where(s => s.ClientId == id && s.IsActive).ToListAsync();
        return accounts.Select(a => new SelectListItem {
            Value = a.AccountNumber,
            Text = $"{a.AccountNumber} - {(a.AccountType == Domain.Enums.AccountType.Main ? "Principal" : "Secundaria")} (RD$ {a.Balance:N2})"
        }).ToList();
    }

    private async Task<List<SelectListItem>> GetMyCardsList()
    {
        var id = GetClientId();
        var cards = await _cardRepository.Query().Where(c => c.ClientId == id && c.IsActive).ToListAsync();
        return cards.Select(c => new SelectListItem {
            Value = c.Id.ToString(),
            Text = $"**** {c.CardNumber.Substring(c.CardNumber.Length - 4)} (Deuda: RD$ {c.DebtAmount:N2})"
        }).ToList();
    }

    private async Task<List<SelectListItem>> GetMyLoansList()
    {
        var id = GetClientId();
        var loans = await _loanRepository.Query().Where(l => l.ClientId == id && l.IsActive).ToListAsync();
        return loans.Select(l => new SelectListItem {
            Value = l.Id.ToString(),
            Text = $"#{l.LoanNumber} (Cuota: RD$ {l.MonthlyPayment:N2})"
        }).ToList();
    }

    private async Task<List<SelectListItem>> GetBeneficiariesList()
    {
        var clientId = GetClientId();

        // JOIN en un único query — evita N+1 queries (un subquery por beneficiario)
        var result = await (
            from b in _beneficiaryRepository.Query().Where(b => b.ClientId == clientId)
            join s in _savingsRepository.Query().Include(s => s.Client)
                on b.AccountNumber equals s.AccountNumber into joined
            from s in joined.DefaultIfEmpty()
            orderby b.CreatedAt
            select new
            {
                b.AccountNumber,
                FirstName = s != null ? s.Client.FirstName : null,
                LastName  = s != null ? s.Client.LastName  : null
            }
        ).ToListAsync();

        return result.Select(x => new SelectListItem
        {
            Value = x.AccountNumber,
            Text  = x.FirstName != null
                ? $"{x.FirstName} {x.LastName} ({x.AccountNumber})"
                : $"Abono a {x.AccountNumber}"
        }).ToList();
    }
}
