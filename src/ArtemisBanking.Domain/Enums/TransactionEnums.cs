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
    SavingsTransfer    = 1,
    CreditCardPayment  = 2,
    LoanPayment        = 3,
    CashAdvance        = 4,
    LoanDisbursement   = 5,
    CashierDeposit     = 6,
    CashierWithdrawal  = 7
}

public enum AccountType
{
    Main      = 1,
    Secondary = 2
}