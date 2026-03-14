using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.WebApp.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        // Usuarios (Clientes)
        var allUsers = await _userRepository.GetAllAsync();
        var clients = allUsers.Where(u => u.Role == UserRole.Cliente).ToList();
        
        // Productos
        var savings = await _savingsRepository.GetAllAsync();
        var loans = await _loanRepository.GetAllAsync();
        var cards = await _cardRepository.GetAllAsync();
        
        // Transacciones
        var transactions = await _transactionRepository.GetAllAsync();

        var model = new DashboardViewModel
        {
            // 1 & 2. Clientes (Activos e Inactivos)
            TotalActiveClients = clients.Count(c => c.IsActive),
            TotalInactiveClients = clients.Count(c => !c.IsActive),
            
            // 3. Productos (Cuentas + Préstamos + TC)
            TotalAssignedProducts = savings.Count() + loans.Count() + cards.Count(),
            
            // 4. Préstamos activos
            TotalActiveLoans = loans.Count(l => l.IsActive),
            
            // 5. Transacciones Totales
            TotalTransactions = transactions.Count(),
            
            // 6. Balance total (Cuentas activas)
            TotalSavingsBalance = savings.Where(s => s.IsActive).Sum(s => s.Balance),
            
            // 7. Pagos realizados hoy (Transacciones de ingreso al banco - simulamos con fecha de hoy)
            TodayPayments = transactions.Count(t => t.Date.Date == DateTime.UtcNow.Date),
            
            // 8. Tarjetas de crédito activas
            TotalActiveCreditCards = cards.Count(c => c.IsActive)
        };

        return View(model);
    }
}
