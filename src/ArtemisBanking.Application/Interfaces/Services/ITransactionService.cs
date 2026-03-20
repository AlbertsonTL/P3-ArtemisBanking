using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;

namespace ArtemisBanking.Application.Interfaces.Services;

public interface ITransactionService
{
    Task<bool> TransferBetweenOwnAccountsAsync(string senderClientId, string sourceAccountNumber, string destinationAccountNumber, decimal amount);
    
    Task<bool> TransferToBeneficiaryAsync(string senderClientId, string sourceAccountNumber, string destinationAccountNumber, decimal amount);
    
    Task<bool> ExpressTransactionAsync(string senderClientId, string sourceAccountNumber, string destinationAccountNumber, decimal amount);
    
    Task<bool> PayCreditCardAsync(string clientId, string sourceAccountNumber, int creditCardId, decimal amount);
    
    Task<bool> PayLoanAsync(string clientId, string sourceAccountNumber, int loanId, decimal amount);
    
    Task<bool> CashAdvanceAsync(string clientId, int creditCardId, string destinationAccountNumber, decimal amount);
}
