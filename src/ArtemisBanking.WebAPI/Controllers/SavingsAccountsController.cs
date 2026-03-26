using ArtemisBanking.Application.DTOs.Account;
using ArtemisBanking.Application.DTOs.SavingsAccount;
using ArtemisBanking.Application.DTOs.Transaction;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Shared.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.WebAPI.Controllers;

[ApiController]
[Route("api/savings-account")]
[Authorize(Roles = "Admin")]
[Tags("Savings Accounts")]
public class SavingsAccountsController : ControllerBase
{
    private readonly IGenericRepository<SavingsAccount, int> _accountRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    public SavingsAccountsController(
        IGenericRepository<SavingsAccount, int> accountRepo,
        UserManager<ApplicationUser> userManager)
    {
        _accountRepo = accountRepo;
        _userManager = userManager;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<SavingsAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<SavingsAccountDto>>> GetAccounts(
        [FromQuery] int page = 1,
        [FromQuery] string? cedula = null,
        [FromQuery] bool? activa = null,
        [FromQuery] AccountType? tipo = null)
    {
        if (page < 1) page = 1;
        const int pageSize = 20;

        var query = _accountRepo.Query()
            .Include(a => a.Client)
            .AsQueryable();

        // Filtro por estado
        if (activa.HasValue)
            query = query.Where(a => a.IsActive == activa.Value);

        // Filtro por tipo
        if (tipo.HasValue)
            query = query.Where(a => a.AccountType == tipo.Value);

        // Filtro por cédula
        if (!string.IsNullOrWhiteSpace(cedula))
        {
            var clientIds = _userManager.Users
                .Where(u => u.IdentityCard == cedula)
                .Select(u => u.Id);
            query = query.Where(a => clientIds.Contains(a.ClientId));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var accounts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = accounts.Select(a => new SavingsAccountDto
        {
            Id = a.Id,
            AccountNumber = a.AccountNumber,
            Balance = a.Balance,
            AccountType = a.AccountType,
            IsActive = a.IsActive,
            CreatedAt = a.CreatedAt,
            ClientId = a.ClientId,
            ClientFullName = a.Client != null ? $"{a.Client.FirstName} {a.Client.LastName}" : string.Empty,
            AdminId = a.AdminId
        }).ToList();

        return Ok(new PaginatedResponse<SavingsAccountDto>
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

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateSavingsAccountApiDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ClienteId))
            return BadRequest(new { message = "clienteId es requerido" });

        var client = await _userManager.FindByIdAsync(request.ClienteId);
        if (client == null)
            return BadRequest(new { message = "Cliente no encontrado" });

        // Verificar que el cliente tiene cuenta principal (requisito base)
        var hasMain = await _accountRepo.Query()
            .AnyAsync(a => a.ClientId == request.ClienteId && a.AccountType == AccountType.Main);
        if (!hasMain)
            return BadRequest(new { message = "El cliente no tiene cuenta principal" });

        // Generar número único de 9 dígitos
        string accountNumber;
        do
        {
            accountNumber = AccountNumberGenerator.Generate9Digits();
        } while (await _accountRepo.Query().AnyAsync(a => a.AccountNumber == accountNumber));

        var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var account = new SavingsAccount
        {
            AccountNumber = accountNumber,
            AccountType = AccountType.Secondary,
            ClientId = request.ClienteId,
            AdminId = adminId,
            IsActive = true,
            Balance = request.SaldoInicial,
            CreatedAt = DateTime.UtcNow
        };

        await _accountRepo.AddAsync(account);
        await _accountRepo.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Cuenta de ahorro creada exitosamente",
            accountId = account.Id,
            accountNumber = account.AccountNumber
        });
    }

    [HttpGet("{accountNumber}/transactions")]
    [ProducesResponseType(typeof(List<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(string accountNumber)
    {
        var account = await _accountRepo.Query()
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

        if (account == null)
            return NotFound(new { message = "Cuenta no encontrada" });

        var txDtos = account.Transactions
            .OrderByDescending(t => t.Date)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                Date = t.Date,
                Status = t.Status,
                Category = t.Category,
                Origin = t.Origin,
                Beneficiary = t.Beneficiary,
                SavingsAccountId = t.SavingsAccountId
            }).ToList();

        return Ok(txDtos);
    }
}

// ── DTOs exclusivos de la API ─────────────────────────────────────────────────

public class CreateSavingsAccountApiDto
{
    public string ClienteId { get; set; } = string.Empty;
    public decimal SaldoInicial { get; set; } = 0m;
}
