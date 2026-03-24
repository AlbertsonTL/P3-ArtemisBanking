using ArtemisBanking.Application.DTOs.Email;
using ArtemisBanking.Application.DTOs.Loan;
using ArtemisBanking.Application.Enums;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Shared.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.Infrastructure.Services;

/// <summary>
/// Implementación del servicio de préstamos.
/// </summary>
public class LoanService : ILoanService
{
    private readonly IGenericRepository<Loan, int>              _loanRepo;
    private readonly IGenericRepository<AmortizationEntry, int> _entryRepo;
    private readonly IGenericRepository<SavingsAccount, int>    _accountRepo;
    private readonly IGenericRepository<Transaction, int>       _transactionRepo;
    private readonly IEmailService                              _emailService;
    private readonly UserManager<ApplicationUser>               _userManager;

    public LoanService(
        IGenericRepository<Loan, int>              loanRepo,
        IGenericRepository<AmortizationEntry, int> entryRepo,
        IGenericRepository<SavingsAccount, int>    accountRepo,
        IGenericRepository<Transaction, int>       transactionRepo,
        IEmailService                              emailService,
        UserManager<ApplicationUser>               userManager)
    {
        _loanRepo        = loanRepo;
        _entryRepo       = entryRepo;
        _accountRepo     = accountRepo;
        _transactionRepo = transactionRepo;
        _emailService    = emailService;
        _userManager     = userManager;
    }

    public decimal CalcularCuotaFrancesa(decimal monto, decimal tasaAnual, int meses)
    {
        if (monto <= 0)   throw new ArgumentException("El monto debe ser mayor a cero.", nameof(monto));
        if (meses <= 0)   throw new ArgumentException("El plazo debe ser mayor a cero.", nameof(meses));
        if (tasaAnual < 0) throw new ArgumentException("La tasa no puede ser negativa.", nameof(tasaAnual));

        // Caso especial: tasa 0% cuota = capital / n (sin interés)
        if (tasaAnual == 0m)
            return Math.Round(monto / meses, 2, MidpointRounding.AwayFromZero);

        // Tasa mensual decimal: r = tasaAnual% / 12
        decimal r = tasaAnual / 100m / 12m;

        // (1 + r)^n — calculado en double para precisión del exponente,
        // devuelto a decimal inmediatamente para mantener precisión financiera.
        decimal factor = (decimal)Math.Pow((double)(1m + r), meses);

        // C = P × r × (1+r)^n / [(1+r)^n − 1]
        decimal cuota = monto * r * factor / (factor - 1m);

        return Math.Round(cuota, 2, MidpointRounding.AwayFromZero);
    }

    /// <inheritdoc/>
    public async Task<RiskAssessmentResult> EsClienteAltoRiesgoAsync(
        string clienteId,
        decimal nuevoCapital,
        decimal tasaAnual,
        int meses)
    {
        // 1. Deuda actual del cliente
        var entradasCliente = await _entryRepo.Query()
            .Include(e => e.Loan)
            .Where(e => e.Loan.ClientId == clienteId && !e.IsPaid)
            .ToListAsync();

        decimal deudaActual = entradasCliente.Sum(e => e.QuotaAmount);

        // 2. Promedio de deuda del sistema
        //    = total de cuotas pendientes de TODOS los clientes / número de clientes con préstamos activos
        var todasLasEntradas = await _entryRepo.Query()
            .Include(e => e.Loan)
            .Where(e => !e.IsPaid && e.Loan.IsActive)
            .ToListAsync();

        decimal totalDeudaSistema = todasLasEntradas.Sum(e => e.QuotaAmount);

        // Clientes distintos con al menos un préstamo activo
        int cantidadClientes = todasLasEntradas
            .Select(e => e.Loan.ClientId)
            .Distinct()
            .Count();

        // Evitar división por cero; si no hay datos, el promedio es 0
        decimal promedioSistema = cantidadClientes > 0
            ? totalDeudaSistema / cantidadClientes
            : 0m;

        // 3. Total que generará el nuevo préstamo (capital + intereses)
        decimal cuotaNueva      = CalcularCuotaFrancesa(nuevoCapital, tasaAnual, meses);
        decimal totalNuevoPrest = cuotaNueva * meses;

        // 4. Evaluación de riesgo
        if (deudaActual > promedioSistema)
        {
            return new RiskAssessmentResult
            {
                Level               = RiskLevel.AlreadyHighRisk,
                Message             = "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema.",
                DeudaActualCliente  = deudaActual,
                PromedioSistema     = promedioSistema,
                TotalNuevoPrestamo  = totalNuevoPrest
            };
        }

        if (deudaActual + totalNuevoPrest > promedioSistema)
        {
            return new RiskAssessmentResult
            {
                Level               = RiskLevel.WillBeHighRisk,
                Message             = "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema.",
                DeudaActualCliente  = deudaActual,
                PromedioSistema     = promedioSistema,
                TotalNuevoPrestamo  = totalNuevoPrest
            };
        }

        return new RiskAssessmentResult
        {
            Level               = RiskLevel.NoRisk,
            Message             = string.Empty,
            DeudaActualCliente  = deudaActual,
            PromedioSistema     = promedioSistema,
            TotalNuevoPrestamo  = totalNuevoPrest
        };
    }

    public async Task AssignLoanAsync(CreateLoanDto dto, string adminId)
    {
        //  Validaciones previas
        var client = await _userManager.FindByIdAsync(dto.ClientId)
            ?? throw new InvalidOperationException("Cliente no encontrado.");

        var mainAccount = await _accountRepo.FirstOrDefaultAsync(
            s => s.ClientId == dto.ClientId && s.AccountType == AccountType.Main && s.IsActive)
            ?? throw new InvalidOperationException("El cliente no posee una cuenta principal activa.");

        //  1. Calcular cuota mensual (Sistema Francés, con decimal) 
        decimal cuotaMensual = CalcularCuotaFrancesa(dto.Amount, dto.AnnualInterestRate, dto.TermMonths);

        //  2. Construir entidad Préstamo
        var loan = new Loan
        {
            LoanNumber        = AccountNumberGenerator.Generate9Digits(),
            Amount            = dto.Amount,
            AnnualInterestRate = dto.AnnualInterestRate,
            TermMonths        = dto.TermMonths,
            MonthlyPayment    = cuotaMensual,
            IsActive          = true,
            CreatedAt         = DateTime.UtcNow,
            ClientId          = dto.ClientId,
            AdminId           = adminId
        };

        //  3. Generar tabla de amortización 
        //    Primera cuota: mismo día del mes siguiente a la creación.
        //    Cuotas siguientes: mes a mes hasta completar n cuotas.
        var fechaBase = loan.CreatedAt;
        for (int i = 1; i <= dto.TermMonths; i++)
        {
            loan.AmortizationEntries.Add(new AmortizationEntry
            {
                PaymentDate = fechaBase.AddMonths(i),
                QuotaAmount = cuotaMensual,
                IsPaid      = false,
                IsLate      = false
            });
        }

        //  4. Acreditar monto a la cuenta principal 
        mainAccount.Balance += dto.Amount;

        //  5. Registrar transacción de crédito (LoanDisbursement) 
        //    Usamos el LoanNumber como origen (generado antes del SaveChanges).
        var transaction = new Transaction
        {
            Type             = TransactionType.Credit,
            Amount           = dto.Amount,
            Date             = DateTime.UtcNow,
            Status           = TransactionStatus.Approved,
            Category         = TransactionCategory.LoanDisbursement,
            Origin           = loan.LoanNumber,
            Beneficiary      = mainAccount.AccountNumber,
            SavingsAccountId = mainAccount.Id
        };

        //  6. Persistir todo de forma atómica 
        //    EF Core comparte el mismo DbContext por scope, así que un único
        //    SaveChanges abarca loan + entries + account update + transaction.
        await _loanRepo.AddAsync(loan);
        _accountRepo.Update(mainAccount);
        await _transactionRepo.AddAsync(transaction);
        await _loanRepo.SaveChangesAsync(); // persiste todo en una sola transacción DB

        //  7. Enviar correo de confirmación al cliente
        //    Fuera de la transacción DB; si falla el correo el préstamo ya está guardado.
        try
        {
            var emailBody = EmailTemplates.LoanApproved(
                fullName:       $"{client.FirstName} {client.LastName}",
                amount:         dto.Amount,
                termMonths:     dto.TermMonths,
                rate:           dto.AnnualInterestRate,
                monthlyPayment: cuotaMensual);

            await _emailService.SendAsync(new EmailRequestDto
            {
                To      = client.Email!,
                Subject = $"Préstamo Aprobado - Artemis Banking ({loan.LoanNumber})",
                Body    = emailBody,
                IsHtml  = true
            });
        }
        catch
        {
            // El correo es best-effort; el préstamo ya quedó guardado.
            // En producción se loguearía aquí.
        }
    }

    public async Task UpdateInterestRateAsync(int loanId, decimal nuevaTasaAnual)
    {
        //  1. Cargar préstamo─
        var loan = await _loanRepo.Query()
            .Include(l => l.Client)
            .Include(l => l.AmortizationEntries)
            .FirstOrDefaultAsync(l => l.Id == loanId)
            ?? throw new InvalidOperationException("Préstamo no encontrado.");

        if (!loan.IsActive)
            throw new InvalidOperationException("No se puede modificar la tasa de un préstamo inactivo.");

        //  2. Obtener cuotas futuras pendientes 
        //    FechaPago > hoy  AND  IsPaid = false
        var hoy = DateTime.UtcNow.Date;
        var cuotasFuturas = loan.AmortizationEntries
            .Where(e => e.PaymentDate.Date > hoy && !e.IsPaid)
            .OrderBy(e => e.PaymentDate)
            .ToList();

        if (!cuotasFuturas.Any())
            throw new InvalidOperationException("No hay cuotas futuras pendientes para recalcular.");

        int    n                = cuotasFuturas.Count;
        decimal tasaViejaAnual  = loan.AnnualInterestRate;

        //  3. Calcular capital restante (principal pendiente) 
        //    Usando la fórmula de valor presente de la anualidad con la tasa VIEJA:
        //      P = cuota_vieja × (1 − (1+r)^−n) / r
        //    Esto es matemáticamente correcto: nos da el saldo de capital sin intereses.
        decimal principalRestante;
        decimal cuotaVieja = cuotasFuturas.First().QuotaAmount;

        if (tasaViejaAnual == 0m)
        {
            principalRestante = cuotaVieja * n;
        }
        else
        {
            decimal rVieja   = tasaViejaAnual / 100m / 12m;
            decimal factorVP = 1m - (decimal)Math.Pow((double)(1m + rVieja), -n);
            principalRestante = cuotaVieja * factorVP / rVieja;
        }

        //  4. Recalcular nueva cuota con capital restante y nueva tasa
        decimal nuevaCuota = CalcularCuotaFrancesa(principalRestante, nuevaTasaAnual, n);

        //  5. Actualizar cuotas futuras pendientes
        foreach (var entrada in cuotasFuturas)
        {
            entrada.QuotaAmount = nuevaCuota;
            _entryRepo.Update(entrada);
        }

        //  6. Actualizar tasa y cuota mensual en el préstamo
        loan.AnnualInterestRate = nuevaTasaAnual;
        loan.MonthlyPayment     = nuevaCuota;
        _loanRepo.Update(loan);

        await _loanRepo.SaveChangesAsync();

        //  7. Enviar correo al cliente
        var proximaFecha = cuotasFuturas.First().PaymentDate;
        try
        {
            var emailBody = EmailTemplates.LoanRateUpdated(
                fullName:       $"{loan.Client.FirstName} {loan.Client.LastName}",
                loanNumber:     loan.LoanNumber,
                nuevaTasa:      nuevaTasaAnual,
                nuevaCuota:     nuevaCuota,
                proximaFecha:   proximaFecha);

            await _emailService.SendAsync(new EmailRequestDto
            {
                To      = loan.Client.Email!,
                Subject = $"Actualización de tasa de interés en tu préstamo {loan.LoanNumber}",
                Body    = emailBody,
                IsHtml  = true
            });
        }
        catch
        {
            // Best-effort; los cambios ya están persistidos.
        }
    }

    public async Task<decimal> ProcessSequentialPaymentAsync(int loanId, decimal amount)
    {
        var loan = await _loanRepo.Query()
            .Include(l => l.AmortizationEntries)
            .FirstOrDefaultAsync(l => l.Id == loanId)
            ?? throw new InvalidOperationException("Préstamo no encontrado.");

        if (!loan.IsActive)
            throw new InvalidOperationException("El préstamo ya no está activo.");

        // Obtener cuotas pendientes en orden cronológico
        var pendingEntries = loan.AmortizationEntries
            .Where(e => !e.IsPaid)
            .OrderBy(e => e.PaymentDate)
            .ToList();

        decimal remaining = amount;
        decimal applied = 0;

        // Aplicar pago secuencial cuota por cuota
        foreach (var entry in pendingEntries)
        {
            // Solo cerramos la cuota si el monto remanente cubre el total de la misma
            if (remaining >= entry.QuotaAmount)
            {
                remaining -= entry.QuotaAmount;
                entry.IsPaid = true;
                entry.PaidAt = DateTime.UtcNow;
                _entryRepo.Update(entry);
                applied += entry.QuotaAmount;
            }
            else
            {
                // Si el remanente no alcanza para completar la siguiente cuota, 
                // el resto queda como abono al capital total pero no cierra la cuota.
                // Esto evita el bug de "pagar" cuotas con RD$ 1.00.
                break;
            }
        }

        // Actualizar monto pagado en el préstamo
        loan.AmountPaid += applied;

        // Si no quedan cuotas pendientes, cerrar el préstamo
        bool allPaid = !loan.AmortizationEntries.Any(e => !e.IsPaid);
        if (allPaid)
        {
            loan.Status   = LoanStatus.Completed;
            loan.IsActive = false;
        }

        _loanRepo.Update(loan);
        await _loanRepo.SaveChangesAsync();

        return applied;
    }
}
