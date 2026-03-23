using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.WebApp.ViewModels.Cashier;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtemisBanking.WebApp.Areas.Cashier.Controllers;

[Area("Cashier")]
[Authorize(Roles = "Cajero")]
public class HomeController : Controller
{
    private readonly IGenericRepository<Transaction, int> _transactionRepository;
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;

    public HomeController(
        IGenericRepository<Transaction, int> transactionRepository,
        IGenericRepository<SavingsAccount, int> savingsRepository)
    {
        _transactionRepository = transactionRepository;
        _savingsRepository = savingsRepository;
    }

    public async Task<IActionResult> Index()
    {
        var cashierId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var today = DateTime.UtcNow.Date;

        // Obtener transacciones del cajero logueado del día actual
        var todayTransactions = await _transactionRepository.Query()
            .Where(t => t.CashierId == cashierId && t.Date.Date == today)
            .ToListAsync();

        // Contar operaciones por tipo
        var depositsCount = todayTransactions.Count(t => t.Category == TransactionCategory.CashierDeposit);
        var withdrawalsCount = todayTransactions.Count(t => t.Category == TransactionCategory.CashierWithdrawal);
        var creditCardPaymentsCount = todayTransactions.Count(t => t.Category == TransactionCategory.CreditCardPayment && t.CashierId != null);
        var loanPaymentsCount = todayTransactions.Count(t => t.Category == TransactionCategory.LoanPayment && t.CashierId != null);

        // Calcular montos totales
        var totalDeposited = todayTransactions
            .Where(t => t.Category == TransactionCategory.CashierDeposit)
            .Sum(t => t.Amount);

        var totalWithdrawn = todayTransactions
            .Where(t => t.Category == TransactionCategory.CashierWithdrawal)
            .Sum(t => t.Amount);

        var model = new CashierDashboardViewModel
        {
            DepositsCount = depositsCount,
            WithdrawalsCount = withdrawalsCount,
            CreditCardPaymentsCount = creditCardPaymentsCount,
            LoanPaymentsCount = loanPaymentsCount,
            TotalDeposited = totalDeposited,
            TotalWithdrawn = totalWithdrawn
        };

        return View(model);
    }
}