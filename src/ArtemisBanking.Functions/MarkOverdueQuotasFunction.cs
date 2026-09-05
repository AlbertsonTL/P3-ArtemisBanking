using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArtemisBanking.Functions;

public class MarkOverdueQuotasFunction
{
    private readonly IGenericRepository<AmortizationEntry, int> _entryRepo;
    private readonly IGenericRepository<Loan, int>              _loanRepo;
    private readonly ILogger<MarkOverdueQuotasFunction>         _logger;

    public MarkOverdueQuotasFunction(
        IGenericRepository<AmortizationEntry, int> entryRepo,
        IGenericRepository<Loan, int>              loanRepo,
        ILogger<MarkOverdueQuotasFunction>         logger)
    {
        _entryRepo = entryRepo;
        _loanRepo  = loanRepo;
        _logger    = logger;
    }

    [Function(nameof(MarkOverdueQuotasFunction))]
    public async Task Run(
        [TimerTrigger("0 0 0 * * *", RunOnStartup = false)] TimerInfo timerInfo)
    {
        var hoy   = DateTime.UtcNow.Date;
        var ahora = DateTime.UtcNow;

        _logger.LogInformation(
            "[MarkOverdueQuotas] Iniciando job — {Timestamp}. Próxima ejecución: {Next}",
            ahora,
            timerInfo.ScheduleStatus?.Next);

        // 1. Cargar SOLO las cuotas de préstamos activos 
        //    Cargamos únicamente las que necesitan evaluación para minimizar
        //    la carga sobre la DB.
        var todasLasCuotas = await _entryRepo.Query()
            .Include(e => e.Loan)
            .Where(e => e.Loan.IsActive)
            .ToListAsync();

        int marcadasComoAtrasadas = 0;
        int corregidas            = 0;   // pagadas que tenían IsLate=true (inconsistencia)
        int sinCambios            = 0;

        foreach (var cuota in todasLasCuotas)
        {
            bool cambio = false;

            // Caso A: cuota PAGADA  nunca debe estar marcada como atrasada
            if (cuota.IsPaid)
            {
                if (cuota.IsLate)
                {
                    cuota.IsLate = false;
                    _entryRepo.Update(cuota);
                    corregidas++;
                    cambio = true;
                }
            }
            // Caso B: cuota PENDIENTE y ya venció marcar como atrasada
            else if (cuota.PaymentDate.Date < hoy && !cuota.IsLate)
            {
                cuota.IsLate = true;
                _entryRepo.Update(cuota);
                marcadasComoAtrasadas++;
                cambio = true;
            }
            // Caso C: cuota pendiente pero aún no venció (o ya estaba bien marcada)
            else if (!cuota.IsPaid && cuota.PaymentDate.Date < hoy && cuota.IsLate)
            {
                // Ya estaba correctamente marcada; nada que hacer.
                sinCambios++;
            }

            if (!cambio) sinCambios++;
        }

        // 2. Persistir todos los cambios de una vez 
        if (marcadasComoAtrasadas > 0 || corregidas > 0)
            await _entryRepo.SaveChangesAsync();

        _logger.LogInformation(
            "[MarkOverdueQuotas] Completado — Atrasadas nuevas: {Nuevas} | " +
            "Inconsistencias corregidas: {Corregidas} | Sin cambios: {SinCambios} | " +
            "Total evaluadas: {Total}",
            marcadasComoAtrasadas, corregidas, sinCambios, todasLasCuotas.Count);

        // 3. Verificar si algún préstamo ha quedado completamente pagado 
        //    (monitoreo de consistencia  no actualiza IsActive aquí)
        await LogLoansWithAllQuotasPaid();
    }

    private async Task LogLoansWithAllQuotasPaid()
    {
        var prestamosActivos = await _loanRepo.Query()
            .Include(l => l.AmortizationEntries)
            .Where(l => l.IsActive)
            .ToListAsync();

        var inconsistentes = prestamosActivos
            .Where(l => l.AmortizationEntries.Any()
                     && l.AmortizationEntries.All(e => e.IsPaid))
            .ToList();

        if (inconsistentes.Any())
        {
            _logger.LogWarning(
                "[MarkOverdueQuotas] {Count} préstamo(s) con todas las cuotas pagadas " +
                "pero aún marcados como activos: {Ids}",
                inconsistentes.Count,
                string.Join(", ", inconsistentes.Select(l => l.LoanNumber)));
        }
    }
}
