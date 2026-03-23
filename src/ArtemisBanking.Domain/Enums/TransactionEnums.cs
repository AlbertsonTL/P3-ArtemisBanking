namespace ArtemisBanking.Domain.Enums;

public enum TransactionType
{
    Debit  = 1,
    Credit = 2
}

public enum TransactionStatus
{
    Approved = 1,
    Rejected = 2
}

public enum ConsumptionStatus
{
    Approved = 1,
    Rejected = 2
}

public enum TransactionCategory
{
    // Cliente - Operaciones propias
    TransferOwnAccounts = 1,
    TransferToBeneficiary = 2,
    TransferExpress = 3,
    CreditCardPayment = 4,
    LoanPayment = 5,
    CashAdvance = 6,

    // Cajero - Operaciones en nombre de cliente
    CashierDeposit = 10,
    CashierWithdrawal = 11,
    CashierCreditCardPayment = 12,
    CashierLoanPayment = 13,
    CashierThirdPartyTransfer = 14,
    CashierThirdPartyTransferReceived = 15
}

public enum AccountType
{
    Main      = 1,
    Secondary = 2
}