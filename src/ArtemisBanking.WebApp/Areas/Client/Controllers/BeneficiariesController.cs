using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.WebApp.ViewModels.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtemisBanking.WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = "Cliente")]
public class BeneficiariesController : Controller
{
    private readonly IGenericRepository<Beneficiary, int> _beneficiaryRepository;
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public BeneficiariesController(
        IGenericRepository<Beneficiary, int> beneficiaryRepository,
        IGenericRepository<SavingsAccount, int> savingsRepository,
        UserManager<ApplicationUser> userManager)
    {
        _beneficiaryRepository = beneficiaryRepository;
        _savingsRepository = savingsRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var beneficiaries = await _beneficiaryRepository.Query()
            .Where(b => b.ClientId == clientId)
            .ToListAsync();

        var model = new List<BeneficiaryListViewModel>();

        foreach (var b in beneficiaries)
        {
            var account = await _savingsRepository.Query()
                .Include(s => s.Client)
                .FirstOrDefaultAsync(s => s.AccountNumber == b.AccountNumber);
            
            model.Add(new BeneficiaryListViewModel
            {
                Id = b.Id,
                AccountNumber = b.AccountNumber,
                BeneficiaryName = account != null ? $"{account.Client.FirstName} {account.Client.LastName}" : "Desconocido",
                CreatedAt = b.CreatedAt
            });
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Create() => View(new AddBeneficiaryViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AddBeneficiaryViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Validar que la cuenta existe
        var targetAccount = await _savingsRepository.Query()
            .Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.AccountNumber == model.AccountNumber);

        if (targetAccount == null)
        {
            ModelState.AddModelError(nameof(model.AccountNumber), "El número de cuenta no existe.");
            return View(model);
        }

        // 2. No se puede agregar a sí mismo
        if (targetAccount.ClientId == clientId)
        {
            ModelState.AddModelError(nameof(model.AccountNumber), "No puedes agregarte a ti mismo como beneficiario.");
            return View(model);
        }

        // 3. Validar si ya existe en su lista
        var exists = await _beneficiaryRepository.ExistsAsync(b => b.ClientId == clientId && b.AccountNumber == model.AccountNumber);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.AccountNumber), "Esta cuenta ya está en tu lista de beneficiarios.");
            return View(model);
        }

        var beneficiary = new Beneficiary
        {
            AccountNumber = model.AccountNumber,
            ClientId = clientId!,
            CreatedAt = DateTime.UtcNow
        };

        await _beneficiaryRepository.AddAsync(beneficiary);
        await _beneficiaryRepository.SaveChangesAsync();

        TempData["Success"] = $"Beneficiario {targetAccount.Client.FirstName} {targetAccount.Client.LastName} agregado con éxito.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var beneficiary = await _beneficiaryRepository.GetByIdAsync(id);

        if (beneficiary == null || beneficiary.ClientId != clientId)
        {
            TempData["Error"] = "Beneficiario no encontrado.";
            return RedirectToAction(nameof(Index));
        }

        _beneficiaryRepository.Remove(beneficiary);
        await _beneficiaryRepository.SaveChangesAsync();

        TempData["Success"] = "Beneficiario eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
