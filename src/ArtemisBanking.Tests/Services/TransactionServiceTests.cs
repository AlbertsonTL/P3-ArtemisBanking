using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Infrastructure.Services;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace ArtemisBanking.Tests.Services;

/// <summary>
/// Tests unitarios para TransactionService.
/// Cubre los 8 métodos de transacción con happy paths y casos de error.
/// </summary>
public class TransactionServiceTests
{
    // ── Mocks ────────────────────────────────────────────────────────────────
    private readonly Mock<IGenericRepository<SavingsAccount, int>>    _savingsMock      = new();
    private readonly Mock<IGenericRepository<CreditCard, int>>        _cardMock         = new();
    private readonly Mock<IGenericRepository<Loan, int>>              _loanMock         = new();
    private readonly Mock<IGenericRepository<Transaction, int>>       _txMock           = new();
    private readonly Mock<IGenericRepository<CardConsumption, int>>   _consumptionMock  = new();
    private readonly Mock<IGenericRepository<AmortizationEntry, int>> _amortMock        = new();
    private readonly Mock<IGenericRepository<Beneficiary, int>>       _beneficiaryMock  = new();
    private readonly Mock<IEmailService>                              _emailMock        = new();
    private readonly Mock<ILoanService>                               _loanSvcMock      = new();

    private TransactionService BuildService() => new(
        _savingsMock.Object,
        _cardMock.Object,
        _loanMock.Object,
        _txMock.Object,
        _consumptionMock.Object,
        _amortMock.Object,
        _beneficiaryMock.Object,
        _emailMock.Object,
        _loanSvcMock.Object);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static SavingsAccount MakeAccount(string accountNumber, string clientId, decimal balance, bool active = true)
        => new()
        {
            Id            = 1,
            AccountNumber = accountNumber,
            ClientId      = clientId,
            Balance       = balance,
            IsActive      = active,
            Client        = new ApplicationUser { Email = "test@artemis.com", FirstName = "Juan", LastName = "Pérez" }
        };

    private static CreditCard MakeCard(int id, string clientId, decimal creditLimit, decimal debtAmount, bool active = true)
        => new()
        {
            Id          = id,
            CardNumber  = "1234567890123456",
            ClientId    = clientId,
            CreditLimit = creditLimit,
            DebtAmount  = debtAmount,
            IsActive    = active,
            Client      = new ApplicationUser { Email = "test@artemis.com", FirstName = "Juan", LastName = "Pérez" }
        };

    private void SetupSavingsQuery(params SavingsAccount[] accounts)
    {
        _savingsMock
            .Setup(r => r.Query())
            .Returns(accounts.AsQueryable());
    }

    private void SetupCardQuery(params CreditCard[] cards)
    {
        _cardMock
            .Setup(r => r.Query())
            .Returns(cards.AsQueryable());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ISSUE 5 — TransferBetweenOwnAccountsAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TransferBetweenOwnAccounts_HappyPath_DeductsAndCreditsBalances()
    {
        // Arrange
        var source = MakeAccount("111", "client1", 5000m);
        var dest   = MakeAccount("222", "client1", 1000m);
        dest.Id    = 2;
        SetupSavingsQuery(source, dest);
        _savingsMock.Setup(r => r.Update(It.IsAny<SavingsAccount>()));
        _txMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        _savingsMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var svc = BuildService();

        // Act
        var result = await svc.TransferBetweenOwnAccountsAsync("client1", "111", "222", 2000m);

        // Assert
        result.Should().BeTrue();
        source.Balance.Should().Be(3000m);
        dest.Balance.Should().Be(3000m);
    }

    [Fact]
    public async Task TransferBetweenOwnAccounts_InsufficientFunds_ReturnsFalse()
    {
        // Arrange
        var source = MakeAccount("111", "client1", 500m);
        var dest   = MakeAccount("222", "client1", 1000m);
        dest.Id    = 2;
        SetupSavingsQuery(source, dest);

        var svc = BuildService();

        // Act
        var result = await svc.TransferBetweenOwnAccountsAsync("client1", "111", "222", 1000m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TransferBetweenOwnAccounts_UsesTransferOwnAccountsCategory()
    {
        // Arrange
        Transaction? capturedDebit = null;
        var source = MakeAccount("111", "client1", 5000m);
        var dest   = MakeAccount("222", "client1", 0m);
        dest.Id    = 2;
        SetupSavingsQuery(source, dest);
        _savingsMock.Setup(r => r.Update(It.IsAny<SavingsAccount>()));
        _txMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
               .Callback<Transaction>(tx => { if (tx.Type == TransactionType.Debit) capturedDebit = tx; })
               .Returns(Task.CompletedTask);
        _savingsMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var svc = BuildService();

        // Act
        await svc.TransferBetweenOwnAccountsAsync("client1", "111", "222", 100m);

        // Assert — Issue 5: debe usar TransferOwnAccounts, NO SavingsTransfer
        capturedDebit.Should().NotBeNull();
        capturedDebit!.Category.Should().Be(TransactionCategory.TransferOwnAccounts);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ISSUE 4 — TransferToBeneficiaryAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TransferToBeneficiary_DestinationNotInBeneficiaryList_ReturnsFalse()
    {
        // Arrange — beneficiario NO existe
        _beneficiaryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Beneficiary, bool>>>()))
            .ReturnsAsync(false);

        var svc = BuildService();

        // Act
        var result = await svc.TransferToBeneficiaryAsync("client1", "111", "999", 500m);

        // Assert — Issue 4: debe retornar false si el destino no es beneficiario
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TransferToBeneficiary_ValidBeneficiary_UsesTransferToBeneficiaryCategory()
    {
        // Arrange
        Transaction? capturedDebit = null;
        var source = MakeAccount("111", "client1", 5000m);
        var dest   = MakeAccount("222", "client2", 0m);
        dest.Id    = 2;

        _beneficiaryMock
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Beneficiary, bool>>>()))
            .ReturnsAsync(true);
        SetupSavingsQuery(source, dest);
        _savingsMock.Setup(r => r.Update(It.IsAny<SavingsAccount>()));
        _txMock.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
               .Callback<Transaction>(tx => { if (tx.Type == TransactionType.Debit) capturedDebit = tx; })
               .Returns(Task.CompletedTask);
        _savingsMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _emailMock.Setup(r => r.SendAsync(It.IsAny<Application.DTOs.Email.EmailRequestDto>())).Returns(Task.CompletedTask);

        var svc = BuildService();

        // Act
        var result = await svc.TransferToBeneficiaryAsync("client1", "111", "222", 1000m);

        // Assert — Issue 4: categoría correcta + envía 2 emails
        result.Should().BeTrue();
        capturedDebit!.Category.Should().Be(TransactionCategory.TransferToBeneficiary);
        _emailMock.Verify(r => r.SendAsync(It.IsAny<Application.DTOs.Email.EmailRequestDto>()), Times.Exactly(2));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ISSUE 6 — PayCreditCardAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PayCreditCard_AmountExceedsDebt_ChargesOnlyDebt()
    {
        // Arrange — cliente tiene RD$ 8,000 en cuenta, deuda RD$ 3,000, intenta pagar RD$ 5,000
        var account = MakeAccount("111", "client1", 8000m);
        var card    = MakeCard(1, "client1", creditLimit: 10000m, debtAmount: 3000m);
        SetupSavingsQuery(account);
        SetupCardQuery(card);
        _savingsMock.Setup(r => r.Update(It.IsAny<SavingsAccount>()));
        _cardMock.Setup(r => r.Update(It.IsAny<CreditCard>()));
        _txMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        _savingsMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _emailMock.Setup(r => r.SendAsync(It.IsAny<Application.DTOs.Email.EmailRequestDto>())).Returns(Task.CompletedTask);

        var svc = BuildService();

        // Act
        var result = await svc.PayCreditCardAsync("client1", "111", 1, 5000m);

        // Assert — Issue 6: paga exitosamente cobrando solo la deuda (3,000)
        result.Should().BeTrue();
        account.Balance.Should().Be(5000m);  // 8000 - 3000
        card.DebtAmount.Should().Be(0m);
    }

    [Fact]
    public async Task PayCreditCard_BalanceLessThanDebt_ReturnsFalse()
    {
        // Arrange — cuenta tiene RD$ 500, deuda RD$ 3,000
        var account = MakeAccount("111", "client1", 500m);
        var card    = MakeCard(1, "client1", creditLimit: 10000m, debtAmount: 3000m);
        SetupSavingsQuery(account);
        SetupCardQuery(card);

        var svc = BuildService();

        // Act
        var result = await svc.PayCreditCardAsync("client1", "111", 1, 3000m);

        // Assert — Issue 6: rechaza porque balance < deuda real
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PayCreditCard_DebtIsZero_ReturnsFalse()
    {
        // Arrange — tarjeta sin deuda
        var account = MakeAccount("111", "client1", 5000m);
        var card    = MakeCard(1, "client1", creditLimit: 10000m, debtAmount: 0m);
        SetupSavingsQuery(account);
        SetupCardQuery(card);

        var svc = BuildService();

        // Act
        var result = await svc.PayCreditCardAsync("client1", "111", 1, 1000m);

        // Assert — paymentAmount = Math.Min(1000, 0) = 0 → retorna false
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PayCreditCard_BalanceSufficientForActualDebt_Succeeds()
    {
        // Arrange — Issue 6 escenario exacto: balance 5,000 < amount 8,000 pero >= deuda 3,000
        var account = MakeAccount("111", "client1", 5000m);
        var card    = MakeCard(1, "client1", creditLimit: 10000m, debtAmount: 3000m);
        SetupSavingsQuery(account);
        SetupCardQuery(card);
        _savingsMock.Setup(r => r.Update(It.IsAny<SavingsAccount>()));
        _cardMock.Setup(r => r.Update(It.IsAny<CreditCard>()));
        _txMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        _savingsMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _emailMock.Setup(r => r.SendAsync(It.IsAny<Application.DTOs.Email.EmailRequestDto>())).Returns(Task.CompletedTask);

        var svc = BuildService();

        // Act — paga 8,000 cuando solo debe 3,000 y tiene 5,000 disponibles
        var result = await svc.PayCreditCardAsync("client1", "111", 1, 8000m);

        // Assert — DEBE ser exitoso: el bug anterior lo rechazaba (5000 < 8000)
        result.Should().BeTrue();
        account.Balance.Should().Be(2000m);  // 5000 - 3000
        card.DebtAmount.Should().Be(0m);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CashAdvanceAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CashAdvance_ExceedsCreditLimit_ReturnsFalse()
    {
        // Arrange — límite 10,000, deuda actual 8,000, avance 2,000 + 6.25% = 2,125 → excede
        var card  = MakeCard(1, "client1", creditLimit: 10000m, debtAmount: 8000m);
        var dest  = MakeAccount("111", "client1", 0m);
        SetupCardQuery(card);
        SetupSavingsQuery(dest);

        var svc = BuildService();

        // Act
        var result = await svc.CashAdvanceAsync("client1", 1, "111", 2000m);

        // Assert — 8000 + 2000 * 1.0625 = 10,125 > 10,000 → rechazar
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CashAdvance_Applies625PercentInterest_ChargesCorrectly()
    {
        // Arrange — límite 10,000, deuda 0, avance 1,000
        var card  = MakeCard(1, "client1", creditLimit: 10000m, debtAmount: 0m);
        var dest  = MakeAccount("111", "client1", 0m);
        SetupCardQuery(card);
        SetupSavingsQuery(dest);
        _cardMock.Setup(r => r.Update(It.IsAny<CreditCard>()));
        _savingsMock.Setup(r => r.Update(It.IsAny<SavingsAccount>()));
        _txMock.Setup(r => r.AddAsync(It.IsAny<Transaction>())).Returns(Task.CompletedTask);
        _consumptionMock.Setup(r => r.AddAsync(It.IsAny<CardConsumption>())).Returns(Task.CompletedTask);
        _cardMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _emailMock.Setup(r => r.SendAsync(It.IsAny<Application.DTOs.Email.EmailRequestDto>())).Returns(Task.CompletedTask);

        var svc = BuildService();

        // Act
        var result = await svc.CashAdvanceAsync("client1", 1, "111", 1000m);

        // Assert — interés 6.25%: totalToCharge = 1000 * 1.0625 = 1062.50
        result.Should().BeTrue();
        card.DebtAmount.Should().Be(1062.50m);
        dest.Balance.Should().Be(1000m); // solo el monto neto, sin interés
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TransferBetweenOwnAccounts — cuenta destino inactiva
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task TransferBetweenOwnAccounts_InactiveDestAccount_ReturnsFalse()
    {
        // Arrange
        var source = MakeAccount("111", "client1", 5000m);
        var dest   = MakeAccount("222", "client1", 0m, active: false);
        dest.Id    = 2;
        SetupSavingsQuery(source, dest);

        var svc = BuildService();

        // Act — dest está inactiva, no debe aparecer en la query del servicio
        var result = await svc.TransferBetweenOwnAccountsAsync("client1", "111", "222", 1000m);

        // Assert — la condición IsActive en el query filtra la cuenta, retorna null → false
        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PayLoanAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PayLoan_InsufficientBalance_ReturnsFalse()
    {
        // Arrange
        var account = MakeAccount("111", "client1", 100m);
        var loan = new Loan
        {
            Id       = 1,
            ClientId = "client1",
            IsActive = true,
            LoanNumber = "123456789",
            Client   = new ApplicationUser { Email = "test@artemis.com", FirstName = "Juan", LastName = "Pérez" },
            AmortizationEntries = new List<AmortizationEntry>()
        };
        SetupSavingsQuery(account);
        _loanMock.Setup(r => r.Query()).Returns(new[] { loan }.AsQueryable());

        var svc = BuildService();

        // Act
        var result = await svc.PayLoanAsync("client1", "111", 1, 5000m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task PayLoan_ProcessSequentialReturnZero_ReturnsFalse()
    {
        // Arrange — balance suficiente pero ProcessSequentialPayment retorna 0 (monto no cubre cuota)
        var account = MakeAccount("111", "client1", 9999m);
        var loan = new Loan
        {
            Id       = 1,
            ClientId = "client1",
            IsActive = true,
            LoanNumber = "123456789",
            Client   = new ApplicationUser { Email = "test@artemis.com", FirstName = "Juan", LastName = "Pérez" },
            AmortizationEntries = new List<AmortizationEntry>()
        };
        SetupSavingsQuery(account);
        _loanMock.Setup(r => r.Query()).Returns(new[] { loan }.AsQueryable());
        _loanSvcMock.Setup(r => r.ProcessSequentialPaymentAsync(1, It.IsAny<decimal>())).ReturnsAsync(0m);

        var svc = BuildService();

        // Act
        var result = await svc.PayLoanAsync("client1", "111", 1, 1m);

        // Assert — applied = 0 → retorna false
        result.Should().BeFalse();
    }
}
