namespace ArtemisBanking.Application.DTOs.Loan;

public class LoanDto
{
    public int Id { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public int TermMonths { get; set; }
    public decimal MonthlyPayment { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientFullName { get; set; } = string.Empty;
    public string AdminId { get; set; } = string.Empty;
    public int TotalQuotas => TermMonths;
    public int PaidQuotas { get; set; }
    public decimal PendingAmount { get; set; }
    public bool IsInDefault { get; set; }
}

public class CreateLoanDto
{
    public string ClientId { get; set; } = string.Empty;
    public string AdminId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public int TermMonths { get; set; }
}

public class UpdateLoanInterestDto
{
    public decimal NewAnnualInterestRate { get; set; }
}

public class AmortizationEntryDto
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal  QuotaAmount { get; set; }
    public bool IsPaid { get; set; }
    public bool IsLate { get; set; }
    public DateTime? PaidAt { get; set; }
}
