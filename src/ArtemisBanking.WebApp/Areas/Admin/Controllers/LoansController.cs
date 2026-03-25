using System.Security.Claims;
using System.Text.Json;
using ArtemisBanking.Application.DTOs.Loan;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
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
    private readonly ILoanService                            _loanService;
    private readonly IGenericRepository<Loan, int>           _loanRepository;
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly UserManager<ApplicationUser>            _userManager;

    private const string PendingLoanKey = "PendingLoanDto";

    public LoansController(
        ILoanService                             loanService,
        IGenericRepository<Loan, int>            loanRepository,
        IGenericRepository<SavingsAccount, int>  savingsRepository,
        UserManager<ApplicationUser>             userManager)
    {
        _loanService       = loanService;
        _loanRepository    = loanRepository;
        _savingsRepository = savingsRepository;
        _userManager       = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string searchCedula, string filterState, int page = 1, int pageSize = 5)
    {
        var query = _loanRepository.Query()
            .Include(l => l.Client)
            .Include(l => l.AmortizationEntries)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchCedula))
        {
            query = query.Where(l => l.Client.IdentityCard.Contains(searchCedula));
        }

        var loans = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

        var model = loans.Select(l => new LoanListViewModel
        {
            Id                 = l.Id,
            LoanNumber         = l.LoanNumber,
            ClientName         = $"{l.Client.FirstName} {l.Client.LastName}",
            IdentityCard       = l.Client.IdentityCard,
            Amount             = l.Amount,
            AnnualInterestRate = l.AnnualInterestRate,
            TermMonths         = l.TermMonths,
            MonthlyPayment     = l.MonthlyPayment,
            IsActive           = l.IsActive,
            CreatedAt          = l.CreatedAt,
            RemainingDebt      = l.AmortizationEntries.Where(e => !e.IsPaid).Sum(e => e.QuotaAmount),
            HasLateEntries     = l.AmortizationEntries.Any(e => e.IsLate && !e.IsPaid)
        }).ToList();

        if (!string.IsNullOrEmpty(filterState))
        {
            if (filterState == "Pagado")
            {
                model = model.Where(l => l.RemainingDebt <= 0 && l.Amount > 0).ToList();
            }
            else if (filterState == "AlDia")
            {
                model = model.Where(l => l.IsActive && l.RemainingDebt > 0).ToList();
            }
            else if (filterState == "EnMora")
            {
                // Un préstamo en mora tiene cuotas activas marcadas como IsLate por el job de background
                model = model.Where(l => l.HasLateEntries).ToList();
            }
        }

        var totalItems = model.Count;
        var pagedModel = model
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.SearchCedula = searchCedula;
        ViewBag.FilterState  = filterState;
        ViewBag.CurrentPage  = page;
        ViewBag.TotalPages   = (int)Math.Ceiling((double)totalItems / pageSize);

        return View(pagedModel);
    }

    // ASSIGN 
    [HttpGet]
    public IActionResult Assign() => View(new AssignLoanViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignLoanViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var client = await _userManager.FindByIdAsync(model.ClientId);
        if (client == null || client.Role != UserRole.Cliente || !client.IsActive)
        {
            TempData["Error"] = "El cliente seleccionado no es válido o está inactivo.";
            return View(model);
        }

        var mainAccount = await _savingsRepository.FirstOrDefaultAsync(
            s => s.ClientId == client.Id && s.AccountType == AccountType.Main && s.IsActive);
        if (mainAccount == null)
        {
            TempData["Error"] = "El cliente no posee una Cuenta de Ahorro Principal activa.";
            return View(model);
        }

        // Validación alto riesgo
        var risk = await _loanService.EsClienteAltoRiesgoAsync(
            model.ClientId, model.Amount, model.AnnualInterestRate, model.TermMonths);

        if (risk.TieneRiesgo)
        {
            // Serializar datos del préstamo para recuperarlos tras confirmación
            TempData[PendingLoanKey] = JsonSerializer.Serialize(new CreateLoanDto
            {
                ClientId           = model.ClientId,
                Amount             = model.Amount,
                AnnualInterestRate = model.AnnualInterestRate,
                TermMonths         = model.TermMonths
            });

            return View("RiskWarning", new RiskWarningViewModel
            {
                ClientId            = model.ClientId,
                ClientName          = $"{client.FirstName} {client.LastName}",
                Amount              = model.Amount,
                AnnualInterestRate  = model.AnnualInterestRate,
                TermMonths          = model.TermMonths,
                RiskMessage         = risk.Message,
                DeudaActualCliente  = risk.DeudaActualCliente,
                PromedioSistema     = risk.PromedioSistema,
                TotalNuevoPrestamo  = risk.TotalNuevoPrestamo
            });
        }

        return await DoAssignLoan(model.ClientId, model.Amount, model.AnnualInterestRate, model.TermMonths);
    }

    // RISK WARNING CONFIRM / CANCEL 

    /// <summary>
    /// Admin confirma préstamo de alto riesgo 
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmAssign()
    {
        var json = TempData[PendingLoanKey] as string;
        if (string.IsNullOrEmpty(json))
        {
            TempData["Error"] = "Sesión expirada. Intente nuevamente.";
            return RedirectToAction(nameof(Index));
        }

        var dto = JsonSerializer.Deserialize<CreateLoanDto>(json);
        if (dto is null)
        {
            TempData["Error"] = "Error al recuperar datos del préstamo.";
            return RedirectToAction(nameof(Index));
        }

        return await DoAssignLoan(dto.ClientId, dto.Amount, dto.AnnualInterestRate, dto.TermMonths);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CancelAssign()
    {
        TempData.Remove(PendingLoanKey);
        TempData["Info"] = "Asignación de préstamo cancelada.";
        return RedirectToAction(nameof(Index));
    }

    // EDIT RATE

    [HttpGet]
    public async Task<IActionResult> EditRate(int id)
    {
        var loan = await _loanRepository.Query()
            .Include(l => l.Client)
            .Include(l => l.AmortizationEntries)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan is null) { TempData["Error"] = "Préstamo no encontrado."; return RedirectToAction(nameof(Index)); }
        if (!loan.IsActive) { TempData["Error"] = "No se puede modificar un préstamo inactivo."; return RedirectToAction(nameof(Index)); }

        var hoy = DateTime.UtcNow.Date;
        return View(new EditLoanRateViewModel
        {
            LoanId                = loan.Id,
            LoanNumber            = loan.LoanNumber,
            ClientName            = $"{loan.Client.FirstName} {loan.Client.LastName}",
            CurrentRate           = loan.AnnualInterestRate,
            CurrentMonthly        = loan.MonthlyPayment,
            RemainingQuotas       = loan.AmortizationEntries.Count(e => e.PaymentDate.Date > hoy && !e.IsPaid),
            NewAnnualInterestRate = loan.AnnualInterestRate   // precargado
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRate(EditLoanRateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _loanService.UpdateInterestRateAsync(model.LoanId, model.NewAnnualInterestRate);
            TempData["Success"] = $"Tasa actualizada a {model.NewAnnualInterestRate:N2}%. Cuotas futuras recalculadas y cliente notificado.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // HELPER 
    private async Task<IActionResult> DoAssignLoan(string clienteId, decimal amount, decimal rate, int months)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        try
        {
            await _loanService.AssignLoanAsync(new CreateLoanDto
            {
                ClientId           = clienteId,
                AdminId            = adminId,
                Amount             = amount,
                AnnualInterestRate = rate,
                TermMonths         = months
            }, adminId);

            decimal cuota = _loanService.CalcularCuotaFrancesa(amount, rate, months);
            TempData["Success"] = $"Préstamo de RD$ {amount:N2} aprobado y desembolsado exitosamente. Cuota mensual: RD$ {cuota:N2}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al procesar el préstamo: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
