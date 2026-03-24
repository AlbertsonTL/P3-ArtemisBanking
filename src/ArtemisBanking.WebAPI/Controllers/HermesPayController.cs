using ArtemisBanking.Application.DTOs.Email;
using ArtemisBanking.Application.DTOs.Transaction;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Shared.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtemisBanking.WebAPI.Controllers;

/// <summary>
/// Hermes Pay — Procesador de pagos con tarjeta de crédito.
/// Acceso: Admin (ve todos los comercios) y Commerce (solo el propio).
/// </summary>
[ApiController]
[Route("pay")]
[Authorize(Roles = "Admin,Comercio")]
[Tags("Hermes Pay")]
public class HermesPayController : ControllerBase
{
    private readonly IGenericRepository<Commerce, int> _commerceRepo;
    private readonly IGenericRepository<CreditCard, int> _cardRepo;
    private readonly IGenericRepository<CardConsumption, int> _consumptionRepo;
    private readonly IGenericRepository<SavingsAccount, int> _accountRepo;
    private readonly IGenericRepository<Transaction, int> _transactionRepo;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;

    public HermesPayController(
        IGenericRepository<Commerce, int> commerceRepo,
        IGenericRepository<CreditCard, int> cardRepo,
        IGenericRepository<CardConsumption, int> consumptionRepo,
        IGenericRepository<SavingsAccount, int> accountRepo,
        IGenericRepository<Transaction, int> transactionRepo,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager)
    {
        _commerceRepo = commerceRepo;
        _cardRepo = cardRepo;
        _consumptionRepo = consumptionRepo;
        _accountRepo = accountRepo;
        _transactionRepo = transactionRepo;
        _emailService = emailService;
        _userManager = userManager;
    }

    /// <summary>
    /// Listado paginado de transacciones de un comercio.
    /// Si el rol es Comercio → usa commerceId del JWT (ignora URL).
    /// Si el rol es Admin → usa commerceId de la URL.
    /// </summary>
    [HttpGet("get-transactions/{commerceId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTransactions(int commerceId, [FromQuery] int page = 1)
    {
        if (page < 1) page = 1;
        const int pageSize = 20;

        // Determinar commerceId efectivo según el rol
        int effectiveCommerceId = commerceId;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Comercio")
        {
            // Rol Comercio: ignorar URL param, usar el del JWT
            var commerceIdClaim = User.FindFirst("CommerceId")?.Value;
            if (string.IsNullOrEmpty(commerceIdClaim) || !int.TryParse(commerceIdClaim, out effectiveCommerceId))
                return Unauthorized(new { message = "El usuario no tiene un comercio asociado" });
        }

        // Obtener consumos del comercio
        var query = _consumptionRepo.Query()
            .Include(c => c.CreditCard)
            .Where(c => c.CommerceId == effectiveCommerceId)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var consumptions = await query
            .OrderByDescending(c => c.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = consumptions.Select(c => new
        {
            id = c.Id,
            amount = c.Amount,
            date = c.Date,
            commerceName = c.CommerceName,
            status = c.Status.ToString(),
            cardLastFour = c.CreditCard.CardNumber.Length >= 4 ? c.CreditCard.CardNumber[^4..] : c.CreditCard.CardNumber
        });

        return Ok(new
        {
            data = dtos,
            page,
            pageSize,
            totalCount,
            totalPages,
            hasPreviousPage = page > 1,
            hasNextPage = page < totalPages
        });
    }

    /// <summary>
    /// Procesar pago con tarjeta de crédito.
    /// Valida tarjeta, comercio, crédito disponible. Acredita al comercio y registra consumo.
    /// Envía 2 correos: al cliente y al comercio.
    /// </summary>
    [HttpPost("process-payment/{commerceId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ProcessPayment(int commerceId, [FromBody] ProcessPaymentDto request)
    {
        // ── 1. Validar campos obligatorios ────────────────────────────────────
        if (string.IsNullOrWhiteSpace(request.CardNumber)
            || request.MonthExpirationCard <= 0
            || request.YearExpirationCard <= 0
            || string.IsNullOrWhiteSpace(request.CVC)
            || request.TransactionAmount <= 0)
            return BadRequest(new { message = "Todos los campos son obligatorios y el monto debe ser mayor a cero" });

        // ── 2. Buscar tarjeta por número ──────────────────────────────────────
        var card = await _cardRepo.Query()
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.CardNumber == request.CardNumber);

        if (card == null)
            return BadRequest(new { message = "Tarjeta no encontrada" });

        if (!card.IsActive)
            return BadRequest(new { message = "La tarjeta está cancelada" });

        // ── 3. Validar fecha de expiración ─────────────────────────────────────
        // ExpirationDate format: "MM/yy"
        if (!TryParseExpiration(card.ExpirationDate, out var expMonth, out var expYear))
            return BadRequest(new { message = "Formato de expiración de tarjeta inválido en el sistema" });

        var now = DateTime.UtcNow;
        var cardExpiry = new DateTime(expYear, expMonth, DateTime.DaysInMonth(expYear, expMonth));
        if (cardExpiry < now)
            return BadRequest(new { message = "La tarjeta está vencida" });

        // Validar mes/año del request contra la tarjeta
        int reqYear = request.YearExpirationCard < 100
            ? 2000 + request.YearExpirationCard
            : request.YearExpirationCard;
        if (request.MonthExpirationCard != expMonth || reqYear != expYear)
            return BadRequest(new { message = "Fecha de expiración incorrecta" });

        // ── 4. Validar CVC (SHA-256) ──────────────────────────────────────────
        if (!CryptoHelper.VerifySHA256(request.CVC, card.CVCHashed))
            return BadRequest(new { message = "CVC incorrecto" });

        // ── 5. Buscar y validar comercio ──────────────────────────────────────
        var commerce = await _commerceRepo.Query().FirstOrDefaultAsync(c => c.Id == commerceId);
        if (commerce == null)
            return BadRequest(new { message = "Comercio no encontrado" });

        if (!commerce.IsActive)
            return BadRequest(new { message = "El comercio está inactivo" });

        // ── 6. Validar crédito disponible ──────────────────────────────────────
        decimal available = card.CreditLimit - card.DebtAmount;
        if (available < request.TransactionAmount)
            return BadRequest(new
            {
                message = $"Crédito disponible insuficiente. Disponible: {available:C}, Solicitado: {request.TransactionAmount:C}"
            });

        // ── 7. Buscar cuenta principal del comercio ───────────────────────────
        // El comercio tiene usuarios asociados vía CommerceId en ApplicationUser
        var commerceUser = await _userManager.Users
            .FirstOrDefaultAsync(u => u.CommerceId == commerceId && u.Role == UserRole.Comercio);

        SavingsAccount? commerceAccount = null;
        if (commerceUser != null)
        {
            commerceAccount = await _accountRepo.Query()
                .FirstOrDefaultAsync(a => a.ClientId == commerceUser.Id && a.AccountType == AccountType.Main && a.IsActive);
        }

        // ── 8. Registrar consumo en la tarjeta ────────────────────────────────
        card.DebtAmount += request.TransactionAmount;
        _cardRepo.Update(card);

        var consumption = new CardConsumption
        {
            Amount = request.TransactionAmount,
            Date = DateTime.UtcNow,
            CommerceName = commerce.Name,
            Status = ConsumptionStatus.Approved,
            CreditCardId = card.Id,
            CommerceId = commerce.Id
        };
        await _consumptionRepo.AddAsync(consumption);

        // ── 9. Acreditar monto a la cuenta principal del comercio ─────────────
        if (commerceAccount != null)
        {
            commerceAccount.Balance += request.TransactionAmount;
            _accountRepo.Update(commerceAccount);

            var tx = new Transaction
            {
                Type = TransactionType.Credit,
                Amount = request.TransactionAmount,
                Date = DateTime.UtcNow,
                Status = TransactionStatus.Approved,
                Category = TransactionCategory.CreditCardPayment,
                Origin = $"Tarjeta *{card.CardNumber[^4..]}",
                Beneficiary = commerceAccount.AccountNumber,
                SavingsAccountId = commerceAccount.Id
            };
            await _transactionRepo.AddAsync(tx);
        }

        await _consumptionRepo.SaveChangesAsync();

        var lastFour = card.CardNumber[^4..];
        var clientFullName = $"{card.Client.FirstName} {card.Client.LastName}";

        // ── 10. Enviar correo al CLIENTE ──────────────────────────────────────
        try
        {
            await _emailService.SendAsync(new EmailRequestDto
            {
                To = card.Client.Email!,
                Subject = $"Consumo realizado con la tarjeta *{lastFour} - Artemis Banking",
                Body = BuildClientPaymentEmail(clientFullName, lastFour, request.TransactionAmount, commerce.Name),
                IsHtml = true
            });
        }
        catch { /* best-effort */ }

        // ── 11. Enviar correo al COMERCIO ─────────────────────────────────────
        if (commerceUser != null && !string.IsNullOrWhiteSpace(commerceUser.Email))
        {
            try
            {
                await _emailService.SendAsync(new EmailRequestDto
                {
                    To = commerceUser.Email,
                    Subject = $"Pago recibido a través de tarjeta *{lastFour} - Artemis Banking",
                    Body = BuildCommercePaymentEmail(commerce.Name, lastFour, request.TransactionAmount),
                    IsHtml = true
                });
            }
            catch { /* best-effort */ }
        }

        return NoContent();
    }

    // ── Helpers privados ─────────────────────────────────────────────────────

    private static bool TryParseExpiration(string expDate, out int month, out int year)
    {
        month = 0; year = 0;
        if (string.IsNullOrWhiteSpace(expDate)) return false;
        var parts = expDate.Split('/');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out month)) return false;
        if (!int.TryParse(parts[1], out int yy)) return false;
        year = yy < 100 ? 2000 + yy : yy;
        return true;
    }

    private static string BuildClientPaymentEmail(string clientName, string lastFour, decimal amount, string commerceName) =>
        $@"<!DOCTYPE html><html><head><meta charset='utf-8'></head><body style='font-family:Arial,sans-serif;'>
        <div style='max-width:600px;margin:40px auto;background:#fff;border-radius:8px;overflow:hidden;'>
          <div style='background:#1a3c5e;padding:30px;text-align:center;'>
            <h1 style='color:#fff;margin:0;'>Artemis Banking</h1>
          </div>
          <div style='padding:30px;color:#333;'>
            <p>Hola <strong>{clientName}</strong>,</p>
            <p>Se realizó un consumo con tu tarjeta terminada en <strong>*{lastFour}</strong>:</p>
            <table style='width:100%;border-collapse:collapse;margin:20px 0;'>
              <tr><td style='padding:8px;border:1px solid #ddd;'><strong>Comercio</strong></td><td style='padding:8px;border:1px solid #ddd;'>{commerceName}</td></tr>
              <tr><td style='padding:8px;border:1px solid #ddd;'><strong>Monto</strong></td><td style='padding:8px;border:1px solid #ddd;'>{amount:C}</td></tr>
              <tr><td style='padding:8px;border:1px solid #ddd;'><strong>Fecha</strong></td><td style='padding:8px;border:1px solid #ddd;'>{DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC</td></tr>
            </table>
            <p>Si no reconoces este cargo, contáctanos de inmediato.</p>
            <p>Atentamente,<br><strong>Equipo Artemis Banking</strong></p>
          </div>
          <div style='background:#f0f0f0;padding:15px;text-align:center;font-size:12px;color:#888;'>© 2025 Artemis Banking</div>
        </div></body></html>";

    private static string BuildCommercePaymentEmail(string commerceName, string lastFour, decimal amount) =>
        $@"<!DOCTYPE html><html><head><meta charset='utf-8'></head><body style='font-family:Arial,sans-serif;'>
        <div style='max-width:600px;margin:40px auto;background:#fff;border-radius:8px;overflow:hidden;'>
          <div style='background:#27ae60;padding:30px;text-align:center;'>
            <h1 style='color:#fff;margin:0;'>Artemis Banking</h1>
          </div>
          <div style='padding:30px;color:#333;'>
            <p>Hola <strong>{commerceName}</strong>,</p>
            <p>Has recibido un pago a través de tarjeta terminada en <strong>*{lastFour}</strong>:</p>
            <table style='width:100%;border-collapse:collapse;margin:20px 0;'>
              <tr><td style='padding:8px;border:1px solid #ddd;'><strong>Monto acreditado</strong></td><td style='padding:8px;border:1px solid #ddd;'>{amount:C}</td></tr>
              <tr><td style='padding:8px;border:1px solid #ddd;'><strong>Fecha</strong></td><td style='padding:8px;border:1px solid #ddd;'>{DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC</td></tr>
            </table>
            <p>El monto ha sido acreditado a tu cuenta principal.</p>
            <p>Atentamente,<br><strong>Equipo Artemis Banking</strong></p>
          </div>
          <div style='background:#f0f0f0;padding:15px;text-align:center;font-size:12px;color:#888;'>© 2025 Artemis Banking</div>
        </div></body></html>";
}

// ── DTOs del módulo ───────────────────────────────────────────────────────────

/// <summary>Body para POST /pay/process-payment/{commerceId}</summary>
public class ProcessPaymentDto
{
    public string CardNumber { get; set; } = string.Empty;
    public int MonthExpirationCard { get; set; }
    public int YearExpirationCard { get; set; }
    public string CVC { get; set; } = string.Empty;
    public decimal TransactionAmount { get; set; }
}
