using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
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
    private readonly UserManager<ApplicationUser> _userManager;

    public LoansController(
        IGenericRepository<Loan, int> loanRepository,
        UserManager<ApplicationUser> userManager)
    {
        _loanRepository = loanRepository;
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
            // Suma del "Principal" pendiente en los entries NO pagados
            RemainingDebt = l.AmortizationEntries
                             .Where(e => !e.IsPaid)
                             .Sum(e => e.Principal)
        }).OrderByDescending(l => l.CreatedAt).ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult Assign()
    {
        // Este wizard depende del ILoanService y lógica matemática de Albertson.
        // Mientras, entregamos la vista que indica el bloqueo temporal para no romper el programa.
        return View();
    }
}
