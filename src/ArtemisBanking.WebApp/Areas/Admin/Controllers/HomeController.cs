using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.WebApp.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HomeController : Controller
{
    private readonly IGenericRepository<ApplicationUser, string> _userRepository;
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly IGenericRepository<CreditCard, int> _cardRepository;
    private readonly IGenericRepository<Loan, int> _loanRepository;
    private readonly IGenericRepository<Transaction, int> _transactionRepository;

    public HomeController(
        IGenericRepository<ApplicationUser, string> userRepository,
        IGenericRepository<SavingsAccount, int> savingsRepository,
        IGenericRepository<CreditCard, int> cardRepository,
        IGenericRepository<Loan, int> loanRepository,
        IGenericRepository<Transaction, int> transactionRepository)
    {
        _userRepository = userRepository;
        _savingsRepository = savingsRepository;
        _cardRepository = cardRepository;
        _loanRepository = loanRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<IActionResult> Index()
    {
        var usersQuery = _userRepository.Query();
        var savingsQuery = _savingsRepository.Query();
        var loansQuery = _loanRepository.Query();
        var cardsQuery = _cardRepository.Query();
        var transactionsQuery = _transactionRepository.Query();

        var activeClientsCount = await usersQuery.CountAsync(u => u.Role == UserRole.Cliente && u.IsActive);
        var inactiveClientsCount = await usersQuery.CountAsync(u => u.Role == UserRole.Cliente && !u.IsActive);
        
        var totalSavings = await savingsQuery.CountAsync();
        var totalLoans = await loansQuery.CountAsync();
        var totalCards = await cardsQuery.CountAsync();

        var activeLoansCount = await loansQuery.CountAsync(l => l.IsActive);
        var totalTransactionsCount = await transactionsQuery.CountAsync();

        var activeSavingsQuery = savingsQuery.Where(s => s.IsActive);
        var activeSavingsBalance = await activeSavingsQuery.SumAsync(s => s.Balance);

        var today = DateTime.UtcNow.Date;
        var todayPaymentsCount = await transactionsQuery.CountAsync(t => t.Date.Date == today);
        
        var activeCardsCount = await cardsQuery.CountAsync(c => c.IsActive);

        var model = new DashboardViewModel
        {
            // 1 & 2. Clientes (Activos e Inactivos)
            TotalActiveClients = activeClientsCount,
            TotalInactiveClients = inactiveClientsCount,
            
            // 3. Productos (Cuentas + Préstamos + TC)
            TotalAssignedProducts = totalSavings + totalLoans + totalCards,
            
            // 4. Préstamos activos
            TotalActiveLoans = activeLoansCount,
            
            // 5. Transacciones Totales
            TotalTransactions = totalTransactionsCount,
            
            // 6. Balance total (Cuentas activas)
            TotalSavingsBalance = activeSavingsBalance,
            
            // 7. Pagos realizados hoy
            TodayPayments = todayPaymentsCount,
            
            // 8. Tarjetas de crédito activas
            TotalActiveCreditCards = activeCardsCount
        };

        return View(model);
    }
}
