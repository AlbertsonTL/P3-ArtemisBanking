using ArtemisBanking.Application.DTOs.Account;
using ArtemisBanking.Application.DTOs.Loan;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtemisBanking.WebAPI.Controllers;

/// <summary>
/// Gestión de Préstamos — Admin only.
/// </summary>
[ApiController]
[Route("api/loan")]
[Authorize(Roles = "Admin")]
[Tags("Loans")]
public class LoansController : ControllerBase
{
    private readonly IGenericRepository<Loan, int> _loanRepo;
    private readonly IGenericRepository<AmortizationEntry, int> _entryRepo;
    private readonly ILoanService _loanService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoansController(
        IGenericRepository<Loan, int> loanRepo,
        IGenericRepository<AmortizationEntry, int> entryRepo,
        ILoanService loanService,
        UserManager<ApplicationUser> userManager)
    {
        _loanRepo = loanRepo;
        _entryRepo = entryRepo;
        _loanService = loanService;
        _userManager = userManager;
    }

    /// <summary>
    /// Listado paginado de préstamos con filtros opcionales.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<LoanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<LoanDto>>> GetLoans(
        [FromQuery] int page = 1,
        [FromQuery] string? estado = null,
        [FromQuery] string? cedula = null)
    {
        if (page < 1) page = 1;
        const int pageSize = 20;

        var query = _loanRepo.Query()
            .Include(l => l.Client)
            .Include(l => l.AmortizationEntries)
            .AsQueryable();

        // Filtro por estado
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (estado.Equals("activos", StringComparison.OrdinalIgnoreCase))
                query = query.Where(l => l.IsActive);
            else if (estado.Equals("completados", StringComparison.OrdinalIgnoreCase))
                query = query.Where(l => !l.IsActive);
        }

        // Filtro por cédula del cliente
        if (!string.IsNullOrWhiteSpace(cedula))
        {
            var clientIds = _userManager.Users
                .Where(u => u.IdentityCard == cedula)
                .Select(u => u.Id);
            query = query.Where(l => clientIds.Contains(l.ClientId));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var loans = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = loans.Select(l => MapLoanToDto(l)).ToList();

        return Ok(new PaginatedResponse<LoanDto>
        {
            Data = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        });
    }

    /// <summary>
    /// Detalle de un préstamo con su tabla de amortización.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LoanDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoanDetailDto>> GetLoanById(int id)
    {
        var loan = await _loanRepo.Query()
            .Include(l => l.Client)
            .Include(l => l.AmortizationEntries)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (loan == null)
            return NotFound(new { message = "Préstamo no encontrado" });

        var dto = new LoanDetailDto
        {
            Loan = MapLoanToDto(loan),
            AmortizationTable = loan.AmortizationEntries
                .OrderBy(e => e.PaymentDate)
                .Select(e => new AmortizationEntryDto
                {
                    Id = e.Id,
                    LoanId = e.LoanId,
                    PaymentDate = e.PaymentDate,
                    QuotaAmount = e.QuotaAmount,
                    IsPaid = e.IsPaid,
                    IsLate = e.IsLate,
                    PaidAt = e.PaidAt
                }).ToList()
        };

        return Ok(dto);
    }

    /// <summary>
    /// Crear préstamo para un cliente. Verifica riesgo (409 si es alto riesgo).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateLoan([FromBody] CreateLoanApiDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ClienteId))
            return BadRequest(new { message = "clienteId es requerido" });
        if (request.Monto <= 0)
            return BadRequest(new { message = "El monto debe ser mayor a cero" });
        if (request.Plazo <= 0)
            return BadRequest(new { message = "El plazo debe ser mayor a cero" });
        if (request.TasaAnual < 0)
            return BadRequest(new { message = "La tasa no puede ser negativa" });

        var client = await _userManager.FindByIdAsync(request.ClienteId);
        if (client == null)
            return BadRequest(new { message = "Cliente no encontrado" });

        // Verificar que el cliente no tenga préstamo activo
        var loanActivo = await _loanRepo.Query()
            .AnyAsync(l => l.ClientId == request.ClienteId && l.IsActive);
        if (loanActivo)
            return BadRequest(new { message = "El cliente ya tiene un préstamo activo" });

        // Evaluar riesgo
        var riesgo = await _loanService.EsClienteAltoRiesgoAsync(
            request.ClienteId, request.Monto, request.TasaAnual, request.Plazo);

        if (riesgo.TieneRiesgo)
            return Conflict(new
            {
                message = riesgo.Message,
                nivel = riesgo.Level.ToString(),
                deudaActual = riesgo.DeudaActualCliente,
                promedioSistema = riesgo.PromedioSistema,
                totalNuevoPrestamo = riesgo.TotalNuevoPrestamo
            });

        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        await _loanService.AssignLoanAsync(new CreateLoanDto
        {
            ClientId = request.ClienteId,
            AdminId = adminId,
            Amount = request.Monto,
            AnnualInterestRate = request.TasaAnual,
            TermMonths = request.Plazo
        }, adminId);

        return StatusCode(StatusCodes.Status201Created, new { message = "Préstamo creado exitosamente" });
    }

    /// <summary>
    /// Editar tasa de interés y recalcular cuotas futuras pendientes.
    /// </summary>
    [HttpPatch("{id:int}/rate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateLoanRate(int id, [FromBody] UpdateLoanRateApiDto request)
    {
        if (request.TasaAnual < 0)
            return BadRequest(new { message = "La tasa no puede ser negativa" });

        var loan = await _loanRepo.Query().FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null)
            return NotFound(new { message = "Préstamo no encontrado" });

        if (!loan.IsActive)
            return BadRequest(new { message = "No se puede modificar un préstamo inactivo" });

        try
        {
            await _loanService.UpdateInterestRateAsync(id, request.TasaAnual);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ── Helper privado ────────────────────────────────────────────────────────
    private static LoanDto MapLoanToDto(Loan l) => new()
    {
        Id = l.Id,
        LoanNumber = l.LoanNumber,
        Amount = l.Amount,
        AnnualInterestRate = l.AnnualInterestRate,
        TermMonths = l.TermMonths,
        MonthlyPayment = l.MonthlyPayment,
        IsActive = l.IsActive,
        CreatedAt = l.CreatedAt,
        ClientId = l.ClientId,
        ClientFullName = l.Client != null ? $"{l.Client.FirstName} {l.Client.LastName}" : string.Empty,
        AdminId = l.AdminId,
        PaidQuotas = l.AmortizationEntries.Count(a => a.IsPaid),
        PendingAmount = l.AmortizationEntries.Where(a => !a.IsPaid).Sum(a => a.QuotaAmount),
        IsInDefault = l.AmortizationEntries.Any(a => a.IsLate)
    };
}

// ── DTOs exclusivos de la API ─────────────────────────────────────────────────

/// <summary>Body para POST /api/loan</summary>
public class CreateLoanApiDto
{
    public string ClienteId { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public int Plazo { get; set; }
    public decimal TasaAnual { get; set; }
}

/// <summary>Body para PATCH /api/loan/{id}/rate</summary>
public class UpdateLoanRateApiDto
{
    public decimal TasaAnual { get; set; }
}

/// <summary>Detalle completo del préstamo con tabla de amortización</summary>
public class LoanDetailDto
{
    public LoanDto Loan { get; set; } = null!;
    public List<AmortizationEntryDto> AmortizationTable { get; set; } = new();
}
