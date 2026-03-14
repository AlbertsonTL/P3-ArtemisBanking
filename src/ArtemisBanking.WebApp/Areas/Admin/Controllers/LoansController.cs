using System.Security.Claims;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.WebApp.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class LoansController : Controller
{
    private readonly IGenericRepository<Loan, int> _loanRepository;
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoansController(
        IGenericRepository<Loan, int> loanRepository,
        IGenericRepository<SavingsAccount, int> savingsRepository,
        UserManager<ApplicationUser> userManager)
    {
        _loanRepository = loanRepository;
        _savingsRepository = savingsRepository;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // 1. Obtener préstamos con sus clientes y tabla de amortización para calcular deuda
        var query = _loanRepository.Query()
            .Include(l => l.Client)
            .Include(l => l.AmortizationEntries);
            
        var loans = await query.ToListAsync();

        var model = loans.Select(l => new LoanListViewModel
        {
            Id = l.Id,
            LoanNumber = l.LoanNumber,
            ClientName = $"{l.Client.FirstName} {l.Client.LastName}",
            IdentityCard = l.Client.IdentityCard,
            Amount = l.Amount,
            AnnualInterestRate = l.AnnualInterestRate,
            TermMonths = l.TermMonths,
            MonthlyPayment = l.MonthlyPayment,
            IsActive = l.IsActive,
            CreatedAt = l.CreatedAt,
            // Suma de Cuotas (QuotaAmount) pendientes en la tabla de amortización
            RemainingDebt = l.AmortizationEntries
                             .Where(e => !e.IsPaid)
                             .Sum(e => e.QuotaAmount)
        }).OrderByDescending(l => l.CreatedAt).ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult Assign()
    {
        var model = new AssignLoanViewModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignLoanViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var client = await _userManager.FindByIdAsync(model.ClientId);
        if (client == null || client.Role != UserRole.Cliente || !client.IsActive)
        {
            TempData["Error"] = "El cliente seleccionado no es válido o está inactivo.";
            return View(model);
        }

        // Buscar cuenta principal para desembolsar el monto
        var mainAccount = await _savingsRepository.FirstOrDefaultAsync(s => s.ClientId == client.Id && s.AccountType == AccountType.Main);
        if (mainAccount == null)
        {
            TempData["Error"] = "El cliente no posee una Cuenta de Ahorro Principal. No se puede desembolsar el préstamo.";
            return View(model);
        }

        // 1. Calcular Cuota Fija (Sistema Francés)
        double r = (double)(model.AnnualInterestRate / 100m / 12m); // Tasa mensual decimal
        int n = model.TermMonths;
        double p = (double)model.Amount;
        
        double cuotaFija = 0;
        if (r > 0)
        {
            cuotaFija = p * r / (1 - Math.Pow(1 + r, -n));
        }
        else
        {
            cuotaFija = p / n; // En caso de que la tasa sea 0%
        }

        decimal montlyPayment = Math.Round((decimal)cuotaFija, 2);

        // 2. Crear Préstamo
        var adminId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        var loan = new Loan
        {
            LoanNumber = ArtemisBanking.Shared.Helpers.AccountNumberGenerator.Generate9Digits(),
            Amount = model.Amount,
            AnnualInterestRate = model.AnnualInterestRate,
            TermMonths = model.TermMonths,
            MonthlyPayment = montlyPayment,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ClientId = client.Id,
            AdminId = adminId!
        };

        // 3. Generar Tabla de Amortización Mensual
        for (int i = 1; i <= n; i++)
        {
            loan.AmortizationEntries.Add(new AmortizationEntry
            {
                PaymentDate = DateTime.UtcNow.AddMonths(i),
                QuotaAmount = montlyPayment,
                IsPaid = false,
                IsLate = false
            });
        }

        // 4. Desembolsar en Cuenta Principal
        mainAccount.Balance += model.Amount;

        // 5. Guardar en Base de Datos (Transacción Implícita de EF)
        await _loanRepository.AddAsync(loan);
        _savingsRepository.Update(mainAccount);
        await _loanRepository.SaveChangesAsync();
        await _savingsRepository.SaveChangesAsync();

        TempData["Success"] = $"Préstamo de RD$ {model.Amount:N2} aprobado y desembolsado con éxito a la cuenta del titular. Cuota: RD$ {montlyPayment:N2}";
        return RedirectToAction(nameof(Index));
    }
}
