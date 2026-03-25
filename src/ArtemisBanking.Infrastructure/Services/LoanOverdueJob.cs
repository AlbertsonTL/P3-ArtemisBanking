using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBanking.Infrastructure.Services;

/// <summary>
/// Job de Hangfire — Se ejecuta diariamente para marcar como vencidas (IsLate = true)
/// todas las cuotas de amortización cuya fecha de pago ya pasó y que aún no están pagadas.
/// Corresponde al Issue #20 del plan de desarrollo.
/// </summary>
public class LoanOverdueJob
{
    private readonly IGenericRepository<AmortizationEntry, int> _entryRepo;
    private readonly ILogger<LoanOverdueJob> _logger;

    public LoanOverdueJob(
        IGenericRepository<AmortizationEntry, int> entryRepo,
        ILogger<LoanOverdueJob> logger)
    {
        _entryRepo = entryRepo;
        _logger    = logger;
    }

    public async Task MarkOverdueEntriesAsync()
    {
        var today = DateTime.UtcNow.Date;

        // Cuotas que ya vencieron (PaymentDate < hoy), no pagadas y aún no marcadas como late
        var overdueEntries = await _entryRepo
            .Query()
            .Where(e => !e.IsPaid && !e.IsLate && e.PaymentDate.Date < today)
            .ToListAsync();

        if (!overdueEntries.Any())
        {
            _logger.LogInformation("[LoanOverdueJob] No hay cuotas vencidas nuevas al {Date}.", today);
            return;
        }

        foreach (var entry in overdueEntries)
        {
            entry.IsLate = true;
            _entryRepo.Update(entry);
        }

        await _entryRepo.SaveChangesAsync();

        _logger.LogInformation("[LoanOverdueJob] {Count} cuota(s) marcadas como vencidas (IsLate=true) el {Date}.",
            overdueEntries.Count, today);
    }
}
