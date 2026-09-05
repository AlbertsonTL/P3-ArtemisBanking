using ArtemisBanking.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SavingsAccount> SavingsAccounts => Set<SavingsAccount>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<AmortizationEntry> AmortizationEntries => Set<AmortizationEntry>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<CardConsumption> CardConsumptions => Set<CardConsumption>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<Commerce> Commerces => Set<Commerce>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser
        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.Property(u => u.IdentityCard).HasMaxLength(20).IsRequired();
            e.HasIndex(u => u.IdentityCard).IsUnique();
            e.Property(u => u.Role).IsRequired();
            e.Property(u => u.CommerceId).IsRequired(false);
            e.HasOne<Commerce>()
             .WithMany()
             .HasForeignKey(u => u.CommerceId)
             .OnDelete(DeleteBehavior.SetNull)
             .IsRequired(false);
        });

        // SavingsAccount
        builder.Entity<SavingsAccount>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.AccountNumber).HasMaxLength(9).IsRequired();
            e.HasIndex(s => s.AccountNumber).IsUnique();
            e.Property(s => s.Balance).HasColumnType("decimal(18,2)");
            e.HasOne(s => s.Client).WithMany(u => u.SavingsAccounts)
             .HasForeignKey(s => s.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Admin).WithMany()
             .HasForeignKey(s => s.AdminId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        });

        // Loan
        builder.Entity<Loan>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.LoanNumber).HasMaxLength(9).IsRequired();
            e.HasIndex(l => l.LoanNumber).IsUnique();
            e.Property(l => l.Amount).HasColumnType("decimal(18,2)");
            e.Property(l => l.AnnualInterestRate).HasColumnType("decimal(5,2)");
            e.Property(l => l.MonthlyPayment).HasColumnType("decimal(18,2)");
            e.Property(l => l.AmountPaid).HasColumnType("decimal(18,2)");
            e.HasOne(l => l.Client).WithMany(u => u.Loans)
             .HasForeignKey(l => l.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Admin).WithMany()
             .HasForeignKey(l => l.AdminId).OnDelete(DeleteBehavior.Restrict);
        });

        // AmortizationEntry
        builder.Entity<AmortizationEntry>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.QuotaAmount).HasColumnType("decimal(18,2)");
            e.HasOne(a => a.Loan).WithMany(l => l.AmortizationEntries)
             .HasForeignKey(a => a.LoanId).OnDelete(DeleteBehavior.Cascade);
        });

        // CreditCard
        builder.Entity<CreditCard>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.CardNumber).HasMaxLength(16).IsRequired();
            e.HasIndex(c => c.CardNumber).IsUnique();
            e.Property(c => c.CreditLimit).HasColumnType("decimal(18,2)");
            e.Property(c => c.DebtAmount).HasColumnType("decimal(18,2)");
            e.Ignore(c => c.CurrentDebt); // computed alias, not a DB column
            e.Property(c => c.ExpirationDate).HasMaxLength(5);
            e.Property(c => c.CVCHashed).HasMaxLength(64);
            e.HasOne(c => c.Client).WithMany(u => u.CreditCards)
             .HasForeignKey(c => c.ClientId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Admin).WithMany()
             .HasForeignKey(c => c.AdminId).OnDelete(DeleteBehavior.Restrict);
        });

        // CardConsumption
        builder.Entity<CardConsumption>(e =>
        {
            e.HasKey(cc => cc.Id);
            e.Property(cc => cc.Amount).HasColumnType("decimal(18,2)");
            e.Property(cc => cc.CommerceName).HasMaxLength(200);
            e.HasOne(cc => cc.CreditCard).WithMany(c => c.Consumptions)
             .HasForeignKey(cc => cc.CreditCardId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cc => cc.Commerce).WithMany(cm => cm.CardConsumptions)
             .HasForeignKey(cc => cc.CommerceId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
        });

        // Transaction
        builder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.Origin).HasMaxLength(200);
            e.Property(t => t.Beneficiary).HasMaxLength(200);
            e.HasOne(t => t.SavingsAccount).WithMany(s => s.Transactions)
             .HasForeignKey(t => t.SavingsAccountId).OnDelete(DeleteBehavior.Cascade);
        });

        // Beneficiary
        builder.Entity<Beneficiary>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.AccountNumber).HasMaxLength(9);
            e.HasOne(b => b.Client).WithMany(u => u.Beneficiaries)
             .HasForeignKey(b => b.ClientId).OnDelete(DeleteBehavior.Cascade);
        });

        // Commerce
        builder.Entity<Commerce>(e =>
        {
            e.HasKey(cm => cm.Id);
            e.Property(cm => cm.Name).HasMaxLength(200);
        });
    }
}
