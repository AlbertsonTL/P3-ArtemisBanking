using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.WebApp.ViewModels.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtemisBanking.WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = "Cliente")]
public class HomeController : Controller
{
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly IGenericRepository<CreditCard, int> _cardRepository;
    private readonly IGenericRepository<Loan, int> _loanRepository;
    private readonly IGenericRepository<Transaction, int> _transactionRepository;
    private readonly IGenericRepository<CardConsumption, int> _consumptionRepository;
    private readonly IGenericRepository<AmortizationEntry, int> _amortizationRepository;

    public HomeController(
        IGenericRepository<SavingsAccount, int> savingsRepository,
        IGenericRepository<CreditCard, int> cardRepository,
        IGenericRepository<Loan, int> loanRepository,
        IGenericRepository<Transaction, int> transactionRepository,
        IGenericRepository<CardConsumption, int> consumptionRepository,
        IGenericRepository<AmortizationEntry, int> amortizationRepository)
    {
        _savingsRepository = savingsRepository;
        _cardRepository = cardRepository;
        _loanRepository = loanRepository;
        _transactionRepository = transactionRepository;
        _consumptionRepository = consumptionRepository;
        _amortizationRepository = amortizationRepository;
    }

    public async Task<IActionResult> Index()
    {
        var clientId = GetClientId();

        var model = new ClientHomeViewModel
        {
            SavingsAccounts = await _savingsRepository.Query()
                .Where(s => s.ClientId == clientId && s.IsActive)
                .OrderBy(s => s.AccountType)
                .ThenByDescending(s => s.Balance)
                .ToListAsync(),
                
            CreditCards = await _cardRepository.Query()
                .Where(c => c.ClientId == clientId && c.IsActive)
                .ToListAsync(),
                
            Loans = await _loanRepository.Query()
                .Where(l => l.ClientId == clientId && l.IsActive)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SavingsDetails(int id)
    {
        var clientId = GetClientId();
        var account = await _savingsRepository.Query()
            .FirstOrDefaultAsync(s => s.Id == id && s.ClientId == clientId);

        if (account == null) return NotFound();

        var transactions = await _transactionRepository.Query()
            .Where(t => t.SavingsAccountId == id)
            .OrderByDescending(t => t.Date)
            .ToListAsync();

        ViewBag.AccountNumber = account.AccountNumber;
        ViewBag.Balance = account.Balance;
        
        return View(transactions);
    }

    [HttpGet]
    public async Task<IActionResult> CardDetails(int id)
    {
        var clientId = GetClientId();
        var card = await _cardRepository.Query()
            .FirstOrDefaultAsync(c => c.Id == id && c.ClientId == clientId);

        if (card == null) return NotFound();

        var consumptions = await _consumptionRepository.Query()
            .Where(c => c.CreditCardId == id)
            .OrderByDescending(c => c.Date)
            .ToListAsync();

        ViewBag.CardNumber = card.CardNumber;
        ViewBag.Debt = card.DebtAmount;
        ViewBag.Limit = card.CreditLimit;

        return View(consumptions);
    }

    [HttpGet]
    public async Task<IActionResult> LoanDetails(int id)
    {
        var clientId = GetClientId();
        var loan = await _loanRepository.Query()
            .FirstOrDefaultAsync(l => l.Id == id && l.ClientId == clientId);

        if (loan == null) return NotFound();

        var entries = await _amortizationRepository.Query()
            .Where(e => e.LoanId == id)
            .OrderBy(e => e.PaymentDate)
            .ToListAsync();

        ViewBag.LoanNumber = loan.LoanNumber;
        ViewBag.Amount = loan.Amount;

        return View(entries);
    }

    private string GetClientId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
}

