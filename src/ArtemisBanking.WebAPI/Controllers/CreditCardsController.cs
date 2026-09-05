using ArtemisBanking.Application.DTOs.Account;
using ArtemisBanking.Application.DTOs.CreditCard;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Shared.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.WebAPI.Controllers;

[ApiController]
[Route("api/credit-card")]
[Authorize(Roles = "Admin")]
[Tags("Credit Cards")]
public class CreditCardsController : ControllerBase
{
    private readonly IGenericRepository<CreditCard, int> _cardRepo;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreditCardsController(
        IGenericRepository<CreditCard, int> cardRepo,
        UserManager<ApplicationUser> userManager)
    {
        _cardRepo = cardRepo;
        _userManager = userManager;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<CreditCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<CreditCardDto>>> GetCards(
        [FromQuery] int page = 1,
        [FromQuery] string? cedula = null,
        [FromQuery] string? estado = null)
    {
        if (page < 1) page = 1;
        const int pageSize = 20;

        var query = _cardRepo.Query()
            .Include(c => c.Client)
            .AsQueryable();

        // Filtro por estado
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (estado.Equals("activa", StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => c.IsActive);
            else if (estado.Equals("cancelada", StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => !c.IsActive);
        }

        // Filtro por cédula
        if (!string.IsNullOrWhiteSpace(cedula))
        {
            var clientIds = _userManager.Users
                .Where(u => u.IdentityCard == cedula)
                .Select(u => u.Id);
            query = query.Where(c => clientIds.Contains(c.ClientId));
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var cards = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = cards.Select(MapCardToDto).ToList();

        return Ok(new PaginatedResponse<CreditCardDto>
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

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CreditCardDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreditCardDetailDto>> GetCardById(int id)
    {
        var card = await _cardRepo.Query()
            .Include(c => c.Client)
            .Include(c => c.Consumptions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (card == null)
            return NotFound(new { message = "Tarjeta no encontrada" });

        var dto = new CreditCardDetailDto
        {
            Card = MapCardToDto(card),
            Consumptions = card.Consumptions
                .OrderByDescending(c => c.Date)
                .Select(c => new CardConsumptionDto
                {
                    Id = c.Id,
                    Amount = c.Amount,
                    Date = c.Date,
                    CommerceName = c.CommerceName,
                    Status = c.Status,
                    CreditCardId = c.CreditCardId,
                    CommerceId = c.CommerceId
                }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignCard([FromBody] CreateCreditCardApiDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ClienteId))
            return BadRequest(new { message = "clienteId es requerido" });
        if (request.Limite <= 0)
            return BadRequest(new { message = "El límite debe ser mayor a cero" });

        var client = await _userManager.FindByIdAsync(request.ClienteId);
        if (client == null)
            return BadRequest(new { message = "Cliente no encontrado" });

        // Verificar que no tenga ya una tarjeta activa
        var existing = await _cardRepo.Query()
            .AnyAsync(c => c.ClientId == request.ClienteId && c.IsActive);
        if (existing)
            return Conflict(new { message = "El cliente ya tiene una tarjeta activa" });

        // Generar número de tarjeta único
        string cardNumber;
        do
        {
            cardNumber = AccountNumberGenerator.Generate16Digits();
        } while (await _cardRepo.Query().AnyAsync(c => c.CardNumber == cardNumber));

        // Generar CVC (3 dígitos) y hashearlo
        var cvcPlain = new Random().Next(100, 1000).ToString();
        var cvcHashed = CryptoHelper.HashSHA256(cvcPlain);

        var adminId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        var card = new CreditCard
        {
            CardNumber = cardNumber,
            CreditLimit = request.Limite,
            DebtAmount = 0m,
            ExpirationDate = AccountNumberGenerator.GetExpirationDate(3),
            CVCHashed = cvcHashed,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ClientId = request.ClienteId,
            AdminId = adminId
        };

        await _cardRepo.AddAsync(card);
        await _cardRepo.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Tarjeta asignada exitosamente",
            cardId = card.Id,
            cardNumber = card.CardNumber,
            expirationDate = card.ExpirationDate
        });
    }

    [HttpPatch("{id:int}/limit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCardLimit(int id, [FromBody] UpdateCreditCardLimitApiDto request)
    {
        var card = await _cardRepo.Query().FirstOrDefaultAsync(c => c.Id == id);
        if (card == null)
            return NotFound(new { message = "Tarjeta no encontrada" });

        if (request.NuevoLimite < card.DebtAmount)
            return BadRequest(new
            {
                message = $"El nuevo límite ({request.NuevoLimite:C}) no puede ser menor a la deuda actual ({card.DebtAmount:C})"
            });

        card.CreditLimit = request.NuevoLimite;
        _cardRepo.Update(card);
        await _cardRepo.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelCard(int id)
    {
        var card = await _cardRepo.Query().FirstOrDefaultAsync(c => c.Id == id);
        if (card == null)
            return NotFound(new { message = "Tarjeta no encontrada" });

        if (!card.IsActive)
            return BadRequest(new { message = "La tarjeta ya está cancelada" });

        if (card.DebtAmount > 0)
            return BadRequest(new
            {
                message = $"No se puede cancelar la tarjeta con deuda pendiente ({card.DebtAmount:C})"
            });

        card.IsActive = false;
        _cardRepo.Update(card);
        await _cardRepo.SaveChangesAsync();

        return NoContent();
    }

    // ── Helper privado ────────────────────────────────────────────────────────
    private static CreditCardDto MapCardToDto(CreditCard c) => new()
    {
        Id = c.Id,
        CardNumber = c.CardNumber,
        CreditLimit = c.CreditLimit,
        DebtAmount = c.DebtAmount,
        ExpirationDate = c.ExpirationDate,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        ClientId = c.ClientId,
        ClientFullName = c.Client != null ? $"{c.Client.FirstName} {c.Client.LastName}" : string.Empty,
        AdminId = c.AdminId
    };
}

// ── DTOs exclusivos de la API ─────────────────────────────────────────────────

public class CreateCreditCardApiDto
{
    public string ClienteId { get; set; } = string.Empty;
    public decimal Limite { get; set; }
}

public class UpdateCreditCardLimitApiDto
{
    public decimal NuevoLimite { get; set; }
}

public class CreditCardDetailDto
{
    public CreditCardDto Card { get; set; } = null!;
    public List<CardConsumptionDto> Consumptions { get; set; } = new();
}
