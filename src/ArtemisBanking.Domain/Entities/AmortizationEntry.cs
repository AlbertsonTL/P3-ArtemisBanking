using ArtemisBanking.Domain.Common;

namespace ArtemisBanking.Domain.Entities;

public class AmortizationEntry : BaseEntity<int>
{
    public DateTime PaymentDate { get; set; }
    public decimal QuotaAmount { get; set; }
    public bool IsPaid { get; set; } = false;
    public bool IsLate { get; set; } = false;
    public DateTime? PaidAt { get; set; }

    public int LoanId { get; set; }
    public Loan Loan { get; set; } = null!;
}