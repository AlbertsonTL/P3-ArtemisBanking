using ArtemisBanking.Application.DTOs.Loan;
using ArtemisBanking.Application.Enums;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Infrastructure.Persistence;
using ArtemisBanking.Infrastructure.Repositories;
using ArtemisBanking.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ArtemisBanking.Tests.Services;

public class LoanServiceCalculationTests
{
    
    // Helpers: instanciar LoanService con mocks mínimos
    

    private static LoanService BuildServiceWithContext(AppDbContext ctx)
    {
        var loanRepo        = new GenericRepository<Loan, int>(ctx);
        var entryRepo       = new GenericRepository<AmortizationEntry, int>(ctx);
        var accountRepo     = new GenericRepository<SavingsAccount, int>(ctx);
        var transactionRepo = new GenericRepository<Transaction, int>(ctx);

        var emailMock  = new Mock<ArtemisBanking.Application.Interfaces.Services.IEmailService>();
        var userMgrMock = MockUserManager();

        return new LoanService(
            loanRepo, entryRepo, accountRepo, transactionRepo,
            emailMock.Object, userMgrMock.Object);
    }

    private static AppDbContext BuildInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }    

    [Fact]
    public void CalcularCuotaFrancesa_PrestamoEstandar_RetornaValorCorrecto()
    {
        // Arrange
        using var ctx     = BuildInMemoryContext(nameof(CalcularCuotaFrancesa_PrestamoEstandar_RetornaValorCorrecto));
        var service       = BuildServiceWithContext(ctx);
        decimal monto     = 100_000m;
        decimal tasa      = 12m;   // 12% anual 1% mensual
        int     meses     = 12;

        // Act
        decimal cuota = service.CalcularCuotaFrancesa(monto, tasa, meses);

        // Assert - tolerancia de ±RD$ 1 por redondeo financiero
        cuota.Should().BeApproximately(8_884.88m, 1.00m,
            because: "la fórmula francesa para 100k/12%/12m da aprox. 8884.88");
        cuota.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void CalcularCuotaFrancesa_PlazoLargTasaAlta_RetornaValorCorrecto()
    {
        // Arrange
        using var ctx = BuildInMemoryContext(nameof(CalcularCuotaFrancesa_PlazoLargTasaAlta_RetornaValorCorrecto));
        var service   = BuildServiceWithContext(ctx);

        // Act
        decimal cuota = service.CalcularCuotaFrancesa(500_000m, 24m, 60);

        // Assert — tolerancia ±RD$ 1
        cuota.Should().BeApproximately(13_247.20m, 1.00m,
            because: "la fórmula francesa para 500k/24%/60m da aprox. 13247.20");
    }

    [Fact]
    public void CalcularCuotaFrancesa_TasaCero_RetornaMontoDivididoEntreMeses()
    {
        using var ctx = BuildInMemoryContext(nameof(CalcularCuotaFrancesa_TasaCero_RetornaMontoDivididoEntreMeses));
        var service   = BuildServiceWithContext(ctx);

        decimal cuota = service.CalcularCuotaFrancesa(120_000m, 0m, 12);

        cuota.Should().Be(10_000m, because: "sin interés la cuota es capital / meses");
    }

    [Fact]
    public void CalcularCuotaFrancesa_MontoNegativo_LanzaArgumentException()
    {
        using var ctx = BuildInMemoryContext(nameof(CalcularCuotaFrancesa_MontoNegativo_LanzaArgumentException));
        var service   = BuildServiceWithContext(ctx);

        var act = () => service.CalcularCuotaFrancesa(-1000m, 12m, 12);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("monto");
    }    

    [Fact]
    public async Task EsClienteAltoRiesgo_SinDeuda_RetornaNoRisk()
    {
        // Arrange: DB vacía (sin cuotas)
        using var ctx = BuildInMemoryContext(nameof(EsClienteAltoRiesgo_SinDeuda_RetornaNoRisk));
        var service   = BuildServiceWithContext(ctx);

        // Act
        var result = await service.EsClienteAltoRiesgoAsync("cliente-1", 50_000m, 12m, 12);

        // Assert
        result.Level.Should().Be(RiskLevel.NoRisk,
            because: "sin deuda en el sistema el promedio es 0 y 0+nuevo≥0, se evalúa correctamente");
        result.TieneRiesgo.Should().BeFalse();
    }

    [Fact]
    public async Task EsClienteAltoRiesgo_DeudaActualSuperaPromedio_RetornaAlreadyHighRisk()
    {
        // Arrange: crear un préstamo con cuotas pendientes para el cliente
        using var ctx   = BuildInMemoryContext(nameof(EsClienteAltoRiesgo_DeudaActualSuperaPromedio_RetornaAlreadyHighRisk));
        var clienteId   = "cliente-alto-riesgo";

        // Añadir cuotas pendientes para el cliente: 20 cuotas de RD$ 15,000 = RD$ 300,000 de deuda
        var loan = new Loan
        {
            LoanNumber         = "111111111",
            Amount             = 200_000m,
            AnnualInterestRate = 12m,
            TermMonths         = 20,
            MonthlyPayment     = 15_000m,
            IsActive           = true,
            CreatedAt          = DateTime.UtcNow,
            ClientId           = clienteId,
            AdminId            = "admin-1"
        };
        for (int i = 1; i <= 20; i++)
            loan.AmortizationEntries.Add(new AmortizationEntry
            {
                PaymentDate = DateTime.UtcNow.AddMonths(i),
                QuotaAmount = 15_000m,
                IsPaid      = false,
                IsLate      = false
            });

        // Otro cliente con UNA sola cuota de RD$ 1,000 promedio ≈ (300,000+1,000)/2 = 150,500
        var loanOtro = new Loan
        {
            LoanNumber         = "222222222",
            Amount             = 10_000m,
            AnnualInterestRate = 12m,
            TermMonths         = 1,
            MonthlyPayment     = 1_000m,
            IsActive           = true,
            CreatedAt          = DateTime.UtcNow,
            ClientId           = "otro-cliente",
            AdminId            = "admin-1"
        };
        loanOtro.AmortizationEntries.Add(new AmortizationEntry
        {
            PaymentDate = DateTime.UtcNow.AddMonths(1),
            QuotaAmount = 1_000m,
            IsPaid      = false,
            IsLate      = false
        });

        ctx.Loans.AddRange(loan, loanOtro);
        await ctx.SaveChangesAsync();

        var service = BuildServiceWithContext(ctx);

        // Act
        // Deuda del cliente = 20 × 15,000 = 300,000
        // Promedio sistema = (300,000 + 1,000) / 2 clientes = 150,500
        // 300,000 > 150,500 AlreadyHighRisk
        var result = await service.EsClienteAltoRiesgoAsync(clienteId, 50_000m, 12m, 12);

        // Assert
        result.Level.Should().Be(RiskLevel.AlreadyHighRisk);
        result.TieneRiesgo.Should().BeTrue();
        result.DeudaActualCliente.Should().Be(300_000m);
    }

    [Fact]
    public async Task EsClienteAltoRiesgo_NuevoPrestamoSuperaPromedio_RetornaWillBeHighRisk()
    {
        // Arrange: un cliente sin deuda. El sistema tiene un promedio de RD$ 5,000
        using var ctx   = BuildInMemoryContext(nameof(EsClienteAltoRiesgo_NuevoPrestamoSuperaPromedio_RetornaWillBeHighRisk));
        var clienteId   = "cliente-sin-deuda";

        // Préstamo de otro cliente con cuota de RD$ 5,000 (promedio = 5,000)
        var loanRef = new Loan
        {
            LoanNumber         = "333333333",
            Amount             = 50_000m,
            AnnualInterestRate = 12m,
            TermMonths         = 1,
            MonthlyPayment     = 5_000m,
            IsActive           = true,
            CreatedAt          = DateTime.UtcNow,
            ClientId           = "cliente-referencia",
            AdminId            = "admin-1"
        };
        loanRef.AmortizationEntries.Add(new AmortizationEntry
        {
            PaymentDate = DateTime.UtcNow.AddMonths(1),
            QuotaAmount = 5_000m,
            IsPaid      = false,
            IsLate      = false
        });

        ctx.Loans.Add(loanRef);
        await ctx.SaveChangesAsync();

        var service = BuildServiceWithContext(ctx);

        // Act
        // Deuda cliente-sin-deuda = 0 no supera promedio (5,000)
        // Nuevo préstamo: 100,000 / 12% / 12 meses cuota ≈ 8,885 × 12 ≈ 106,620
        // 0 + 106,620 > 5,000 WillBeHighRisk
        var result = await service.EsClienteAltoRiesgoAsync(clienteId, 100_000m, 12m, 12);

        // Assert
        result.Level.Should().Be(RiskLevel.WillBeHighRisk);
        result.TieneRiesgo.Should().BeTrue();
        result.DeudaActualCliente.Should().Be(0m);
    }
}
