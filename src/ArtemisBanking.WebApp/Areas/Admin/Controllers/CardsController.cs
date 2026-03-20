using System.Security.Claims;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Shared.Helpers;
using ArtemisBanking.WebApp.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.WebApp.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CardsController : Controller
{
    private readonly IGenericRepository<CreditCard, int> _cardRepository;
    private readonly IGenericRepository<CardConsumption, int> _consumptionRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public CardsController(
        IGenericRepository<CreditCard, int> cardRepository,
        IGenericRepository<CardConsumption, int> consumptionRepository,
        UserManager<ApplicationUser> userManager)
    {
        _cardRepository = cardRepository;
        _consumptionRepository = consumptionRepository;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cards = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(_cardRepository.Query(), c => c.Client).ToListAsync();
        
        var model = cards.Select(c => new CardListViewModel
        {
            Id = c.Id,
            CardNumber = c.CardNumber,
            ClientName = $"{c.Client.FirstName} {c.Client.LastName}",
            IdentityCard = c.Client.IdentityCard,
            CreditLimit = c.CreditLimit,
            DebtAmount = c.DebtAmount,
            ExpirationDate = c.ExpirationDate,
            IsActive = c.IsActive
        }).OrderByDescending(x => x.IsActive).ThenBy(x => x.ClientName).ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult Assign()
    {
        var clients = _userManager.Users
                                  .Where(u => u.Role == UserRole.Cliente && u.IsActive)
                                  .OrderBy(u => u.FirstName)
                                  .Select(u => new SelectListItem
                                  {
                                      Value = u.Id,
                                      Text = $"{u.FirstName} {u.LastName} ({u.IdentityCard})"
                                  }).ToList();

        return View(new AssignCardViewModel { Clients = clients });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignCardViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Clients = _userManager.Users.Where(u => u.Role == UserRole.Cliente && u.IsActive)
                .Select(u => new SelectListItem { Value = u.Id, Text = $"{u.FirstName} {u.LastName}" }).ToList();
            return View(model);
        }

        var client = await _userManager.FindByIdAsync(model.ClientId);
        if (client == null || client.Role != UserRole.Cliente) return BadRequest();

        // 1. Generar nuevo número de 16 dígitos asegurando que no exista
        string newCardNumber;
        do {
            newCardNumber = AccountNumberGenerator.Generate16Digits();
        } while (await _cardRepository.ExistsAsync(c => c.CardNumber == newCardNumber));

        // 2. Generar CVC crudo y Encriptarlo usando CryptoHelper de Dev 1
        var rawCvc = new Random().Next(100, 999).ToString();
        var hashedCvc = CryptoHelper.HashSHA256(rawCvc);

        // 3. Crear Entidad
        var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var card = new CreditCard
        {
            CardNumber = newCardNumber,
            CreditLimit = model.CreditLimit,
            DebtAmount = 0m,
            ExpirationDate = AccountNumberGenerator.GetExpirationDate(3), // Expira en 3 años
            CVCHashed = hashedCvc,
            IsActive = true,
            ClientId = client.Id,
            AdminId = currentAdminId!
        };

        await _cardRepository.AddAsync(card);
        await _cardRepository.SaveChangesAsync();

        // Mandar el CVC CRUDO temporalmente a TempData para mostrarlo al Admin SOLO ESTA VEZ
        TempData["Success"] = $"Tarjeta generada con éxito. Número: {newCardNumber.Substring(0,4)} XXXX XXXX {newCardNumber.Substring(12)} - CVC: {rawCvc} (NOTA AL ADMIN: Copie el CVC entregado al cliente, no se mostrará de nuevo).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var card = await _cardRepository.GetByIdAsync(id);
        if (card == null) return NotFound();

        // Regla Crítica: No se puede cancelar ni desactivar una tarjeta que tenga deuda
        if (card.IsActive && card.DebtAmount > 0)
        {
            TempData["Error"] = $"No se puede desactivar o cancelar la tarjeta bloqueada ({card.CardNumber}). Debe pagar RD$ {card.DebtAmount:N2} pendiente primero.";
            return RedirectToAction(nameof(Index));
        }

        card.IsActive = !card.IsActive;
        _cardRepository.Update(card);
        await _cardRepository.SaveChangesAsync();

        TempData["Success"] = card.IsActive ? "Tarjeta activada." : "Tarjeta desactivada y cancelada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLimit(EditLimitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "El límite nuevo es inválido.";
            return RedirectToAction(nameof(Index));
        }

        var card = await _cardRepository.GetByIdAsync(model.Id);
        if (card == null) return NotFound();

        // La deuda no puede ser mayor que el nuevo límite (lógica de protección)
        if (card.DebtAmount > model.NewLimit)
        {
            TempData["Error"] = $"El nuevo límite (RD$ {model.NewLimit:N2}) no puede ser menor a la deuda actual (RD$ {card.DebtAmount:N2}).";
            return RedirectToAction(nameof(Index));
        }

        card.CreditLimit = model.NewLimit;
        _cardRepository.Update(card);
        await _cardRepository.SaveChangesAsync();

        TempData["Success"] = $"El límite de crédito se actualizó a RD$ {model.NewLimit:N2} satisfactoriamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Consumptions(int id)
    {
        var card = await _cardRepository.Query()
            .Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (card == null) return NotFound();

        var consumptions = await _consumptionRepository.Query()
            .Where(c => c.CreditCardId == id)
            .OrderByDescending(c => c.Date)
            .ToListAsync();

        var model = new CardConsumptionViewModel
        {
            CardNumber = card.CardNumber,
            ClientName = $"{card.Client.FirstName} {card.Client.LastName}",
            Consumptions = consumptions.Select(c => new ConsumptionItemViewModel
            {
                Amount = c.Amount,
                Date = c.Date,
                CommerceName = c.CommerceName,
                Status = c.Status
            }).ToList()
        };

        return View(model);
    }
}
