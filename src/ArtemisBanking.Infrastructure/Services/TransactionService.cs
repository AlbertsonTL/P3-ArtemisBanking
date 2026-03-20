using ArtemisBanking.Application.DTOs.Email;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly IGenericRepository<CreditCard, int> _cardRepository;
    private readonly IGenericRepository<Loan, int> _loanRepository;
    private readonly IGenericRepository<Transaction, int> _transactionRepository;
    private readonly IGenericRepository<CardConsumption, int> _consumptionRepository;
    private readonly IGenericRepository<AmortizationEntry, int> _amortizationRepository;
    private readonly IEmailService _emailService;

    public TransactionService(
        IGenericRepository<SavingsAccount, int> savingsRepository,
        IGenericRepository<CreditCard, int> cardRepository,
        IGenericRepository<Loan, int> loanRepository,
        IGenericRepository<Transaction, int> transactionRepository,
        IGenericRepository<CardConsumption, int> consumptionRepository,
        IGenericRepository<AmortizationEntry, int> amortizationRepository,
        IEmailService emailService)
    {
        _savingsRepository = savingsRepository;
        _cardRepository = cardRepository;
        _loanRepository = loanRepository;
        _transactionRepository = transactionRepository;
        _consumptionRepository = consumptionRepository;
        _amortizationRepository = amortizationRepository;
        _emailService = emailService;
    }

    public async Task<bool> TransferBetweenOwnAccountsAsync(string senderClientId, string sourceAccountNumber, string destinationAccountNumber, decimal amount)
    {
        var source = await _savingsRepository.Query().Include(s => s.Client).FirstOrDefaultAsync(s => s.AccountNumber == sourceAccountNumber && s.ClientId == senderClientId && s.IsActive);
        var dest = await _savingsRepository.Query().Include(s => s.Client).FirstOrDefaultAsync(s => s.AccountNumber == destinationAccountNumber && s.ClientId == senderClientId && s.IsActive);

        if (source == null || dest == null || source.Balance < amount) return false;

        source.Balance -= amount;
        dest.Balance += amount;

        _savingsRepository.Update(source);
        _savingsRepository.Update(dest);

        await _transactionRepository.AddAsync(new Transaction
        {
            Type = TransactionType.Debit,
            Amount = amount,
            Category = TransactionCategory.SavingsTransfer,
            Status = TransactionStatus.Approved,
            Origin = sourceAccountNumber,
            Beneficiary = destinationAccountNumber,
            SavingsAccountId = source.Id,
            Date = DateTime.UtcNow
        });

        await _transactionRepository.AddAsync(new Transaction
        {
            Type = TransactionType.Credit,
            Amount = amount,
            Category = TransactionCategory.SavingsTransfer,
            Status = TransactionStatus.Approved,
            Origin = sourceAccountNumber,
            Beneficiary = destinationAccountNumber,
            SavingsAccountId = dest.Id,
            Date = DateTime.UtcNow
        });

        await _savingsRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TransferToBeneficiaryAsync(string senderClientId, string sourceAccountNumber, string destinationAccountNumber, decimal amount)
    {
         return await ExpressTransactionAsync(senderClientId, sourceAccountNumber, destinationAccountNumber, amount);
    }

    public async Task<bool> ExpressTransactionAsync(string senderClientId, string sourceAccountNumber, string destinationAccountNumber, decimal amount)
    {
        var source = await _savingsRepository.Query().Include(s => s.Client).FirstOrDefaultAsync(s => s.AccountNumber == sourceAccountNumber && s.ClientId == senderClientId && s.IsActive);
        var dest = await _savingsRepository.Query().Include(s => s.Client).FirstOrDefaultAsync(s => s.AccountNumber == destinationAccountNumber && s.IsActive);

        if (source == null || dest == null || source.Balance < amount) return false;

        source.Balance -= amount;
        dest.Balance += amount;

        _savingsRepository.Update(source);
        _savingsRepository.Update(dest);

        await _transactionRepository.AddAsync(new Transaction
        {
            Type = TransactionType.Debit,
            Amount = amount,
            Category = TransactionCategory.SavingsTransfer,
            Status = TransactionStatus.Approved,
            Origin = sourceAccountNumber,
            Beneficiary = destinationAccountNumber,
            SavingsAccountId = source.Id,
            Date = DateTime.UtcNow
        });

        await _transactionRepository.AddAsync(new Transaction
        {
            Type = TransactionType.Credit,
            Amount = amount,
            Category = TransactionCategory.SavingsTransfer,
            Status = TransactionStatus.Approved,
            Origin = sourceAccountNumber,
            Beneficiary = destinationAccountNumber,
            SavingsAccountId = dest.Id,
            Date = DateTime.UtcNow
        });

        await _savingsRepository.SaveChangesAsync();

        await _emailService.SendAsync(new EmailRequestDto {
            To = source.Client.Email!,
            Subject = "Transferencia Enviada",
            Body = EmailTemplates.TransactionNotification($"{source.Client.FirstName} {source.Client.LastName}", "Debito por Transferencia", amount, destinationAccountNumber)
        });

        await _emailService.SendAsync(new EmailRequestDto {
            To = dest.Client.Email!,
            Subject = "Transferencia Recibida",
            Body = EmailTemplates.TransactionNotification($"{dest.Client.FirstName} {dest.Client.LastName}", "Credito por Transferencia", amount, sourceAccountNumber)
        });

        return true;
    }

    public async Task<bool> PayCreditCardAsync(string clientId, string sourceAccountNumber, int creditCardId, decimal amount)
    {
        var source = await _savingsRepository.Query().FirstOrDefaultAsync(s => s.AccountNumber == sourceAccountNumber && s.ClientId == clientId && s.IsActive);
        var card = await _cardRepository.Query().Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == creditCardId && c.ClientId == clientId && c.IsActive);

        if (source == null || card == null || source.Balance < amount) return false;

        var paymentAmount = Math.Min(amount, card.DebtAmount);
        if (paymentAmount <= 0) return false;

        source.Balance -= paymentAmount;
        card.DebtAmount -= paymentAmount;

        _savingsRepository.Update(source);
        _cardRepository.Update(card);

        await _transactionRepository.AddAsync(new Transaction
        {
            Type = TransactionType.Debit,
            Amount = paymentAmount,
            Category = TransactionCategory.CreditCardPayment,
            Status = TransactionStatus.Approved,
            Origin = sourceAccountNumber,
            Beneficiary = card.CardNumber,
            SavingsAccountId = source.Id,
            Date = DateTime.UtcNow
        });

        await _savingsRepository.SaveChangesAsync();

        await _emailService.SendAsync(new EmailRequestDto {
            To = card.Client.Email!,
            Subject = "Pago de Tarjeta de Crédito",
            Body = EmailTemplates.TransactionNotification($"{card.Client.FirstName} {card.Client.LastName}", "Pago Tarjeta", paymentAmount, card.CardNumber)
        });

        return true;
    }

    public async Task<bool> PayLoanAsync(string clientId, string sourceAccountNumber, int loanId, decimal amount)
    {
        var source = await _savingsRepository.Query().FirstOrDefaultAsync(s => s.AccountNumber == sourceAccountNumber && s.ClientId == clientId && s.IsActive);
        var loan = await _loanRepository.Query().Include(l => l.Client).FirstOrDefaultAsync(l => l.Id == loanId && l.ClientId == clientId && l.IsActive);

        if (source == null || loan == null || source.Balance < amount) return false;

        var entries = await _amortizationRepository.Query()
            .Where(e => e.LoanId == loanId && !e.IsPaid)
            .OrderBy(e => e.PaymentDate)
            .ToListAsync();

        decimal remainingAmount = amount;
        decimal paidTotal = 0;

        foreach (var entry in entries)
        {
            if (remainingAmount >= entry.QuotaAmount)
            {
                remainingAmount -= entry.QuotaAmount;
                paidTotal += entry.QuotaAmount;
                entry.IsPaid = true;
                entry.PaidAt = DateTime.UtcNow;
                _amortizationRepository.Update(entry);
            }
            else break;
        }

        if (paidTotal == 0) return false;

        source.Balance -= paidTotal;
        _savingsRepository.Update(source);

        await _transactionRepository.AddAsync(new Transaction
        {
            Type = TransactionType.Debit,
            Amount = paidTotal,
            Category = TransactionCategory.LoanPayment,
            Status = TransactionStatus.Approved,
            Origin = sourceAccountNumber,
            Beneficiary = loan.LoanNumber,
            SavingsAccountId = source.Id,
            Date = DateTime.UtcNow
        });

        var pending = await _amortizationRepository.ExistsAsync(e => e.LoanId == loanId && !e.IsPaid);
        if (!pending)
        {
            loan.IsActive = false;
            _loanRepository.Update(loan);
        }

        await _savingsRepository.SaveChangesAsync();

        await _emailService.SendAsync(new EmailRequestDto {
            To = loan.Client.Email!,
            Subject = "Pago de Préstamo Realizado",
            Body = EmailTemplates.TransactionNotification($"{loan.Client.FirstName} {loan.Client.LastName}", "Pago Préstamo", paidTotal, loan.LoanNumber)
        });

        return true;
    }

    public async Task<bool> CashAdvanceAsync(string clientId, int creditCardId, string destinationAccountNumber, decimal amount)
    {
        var card = await _cardRepository.Query().Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == creditCardId && c.ClientId == clientId && c.IsActive);
        var dest = await _savingsRepository.Query().FirstOrDefaultAsync(s => s.AccountNumber == destinationAccountNumber && s.ClientId == clientId && s.IsActive);

        if (card == null || dest == null) return false;

        decimal interest = amount * 0.0625m;
        decimal totalToCharge = amount + interest;

        if (card.DebtAmount + totalToCharge > card.CreditLimit) return false;

        card.DebtAmount += totalToCharge;
        dest.Balance += amount;

        _cardRepository.Update(card);
        _savingsRepository.Update(dest);

        await _transactionRepository.AddAsync(new Transaction
        {
            Type = TransactionType.Credit,
            Amount = amount,
            Category = TransactionCategory.CashAdvance,
            Status = TransactionStatus.Approved,
            Origin = card.CardNumber.Length > 4 
                ? card.CardNumber.Substring(card.CardNumber.Length - 4) 
                : card.CardNumber,
            Beneficiary = destinationAccountNumber,
            SavingsAccountId = dest.Id,
            Date = DateTime.UtcNow
        });

        await _consumptionRepository.AddAsync(new CardConsumption
        {
            Amount = totalToCharge,
            Date = DateTime.UtcNow,
            CommerceName = "AVANCE DE EFECTIVO",
            Status = ConsumptionStatus.Approved,
            CreditCardId = card.Id
        });

        await _cardRepository.SaveChangesAsync();

        await _emailService.SendAsync(new EmailRequestDto {
            To = card.Client.Email!,
            Subject = "Avance de Efectivo realizado",
            Body = EmailTemplates.TransactionNotification($"{card.Client.FirstName} {card.Client.LastName}", "Avance de Efectivo", amount, card.CardNumber)
        });

        return true;
    }
}
