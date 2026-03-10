using ArtemisBanking.Domain.Common;

namespace ArtemisBanking.Domain.Entities;

public class Loan : BaseEntity<int>
{
    public string LoanNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AnnualInterestRate{ get; set; }
    public int TermMonths { get; set; }
    public decimal MonthlyPayment { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string ClientId { get; set; } = string.Empty;
    public string AdminId { get; set; } = string.Empty;

    public ApplicationUser Client { get; set; } = null!;
    public ApplicationUser Admin { get; set; } = null!;
    public ICollection<AmortizationEntry> AmortizationEntries { get; set; } = new List<AmortizationEntry>();
}