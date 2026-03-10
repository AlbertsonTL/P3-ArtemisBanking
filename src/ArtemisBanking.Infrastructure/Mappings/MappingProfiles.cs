using ArtemisBanking.Application.DTOs.Account;
using ArtemisBanking.Application.DTOs.Beneficiary;
using ArtemisBanking.Application.DTOs.Commerce;
using ArtemisBanking.Application.DTOs.CreditCard;
using ArtemisBanking.Application.DTOs.Loan;
using ArtemisBanking.Application.DTOs.SavingsAccount;
using ArtemisBanking.Application.DTOs.Transaction;
using ArtemisBanking.Domain.Entities;
using AutoMapper;

namespace ArtemisBanking.Infrastructure.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<ApplicationUser, UserDto>();

        CreateMap<CreateUserDto, ApplicationUser>()
            .ForMember(d => d.IsActive, opt => opt.MapFrom(_ => false))
            .ForMember(d => d.PasswordHash, opt => opt.Ignore());

        CreateMap<UpdateUserDto, ApplicationUser>()
            .ForMember(d => d.PasswordHash, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}

public class SavingsAccountMappingProfile : Profile
{
    public SavingsAccountMappingProfile()
    {
        CreateMap<SavingsAccount, SavingsAccountDto>()
            .ForMember(d => d.ClientFullName,
                opt => opt.MapFrom(s => s.Client != null
                    ? $"{s.Client.FirstName} {s.Client.LastName}" : string.Empty));

        CreateMap<CreateSavingsAccountDto, SavingsAccount>()
            .ForMember(d => d.Balance,        opt => opt.MapFrom(s => s.InitialBalance))
            .ForMember(d => d.IsActive,       opt => opt.MapFrom(_ => true))
            .ForMember(d => d.CreatedAt,      opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.AccountNumber,  opt => opt.Ignore());
    }
}

public class LoanMappingProfile : Profile
{
    public LoanMappingProfile()
    {
        CreateMap<Loan, LoanDto>()
            .ForMember(d => d.ClientFullName,
                opt => opt.MapFrom(s => s.Client != null
                    ? $"{s.Client.FirstName} {s.Client.LastName}" : string.Empty))
            .ForMember(d => d.PaidQuotas,
                opt => opt.MapFrom(s => s.AmortizationEntries.Count(a => a.IsPaid)))
            .ForMember(d => d.PendingAmount,
                opt => opt.MapFrom(s => s.AmortizationEntries.Where(a => !a.IsPaid).Sum(a => a.QuotaAmount)))
            .ForMember(d => d.IsInDefault,
                opt => opt.MapFrom(s => s.AmortizationEntries.Any(a => a.IsLate)));

        CreateMap<CreateLoanDto, Loan>()
            .ForMember(d => d.IsActive,       opt => opt.MapFrom(_ => true))
            .ForMember(d => d.CreatedAt,      opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.LoanNumber,     opt => opt.Ignore())
            .ForMember(d => d.MonthlyPayment, opt => opt.Ignore());

        CreateMap<AmortizationEntry, AmortizationEntryDto>();
    }
}

public class CreditCardMappingProfile : Profile
{
    public CreditCardMappingProfile()
    {
        CreateMap<CreditCard, CreditCardDto>()
            .ForMember(d => d.ClientFullName,
                opt => opt.MapFrom(s => s.Client != null
                    ? $"{s.Client.FirstName} {s.Client.LastName}" : string.Empty));

        CreateMap<CreateCreditCardDto, CreditCard>()
            .ForMember(d => d.IsActive,        opt => opt.MapFrom(_ => true))
            .ForMember(d => d.DebtAmount,      opt => opt.MapFrom(_ => 0m))
            .ForMember(d => d.CreatedAt,       opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.CardNumber,      opt => opt.Ignore())
            .ForMember(d => d.ExpirationDate,  opt => opt.Ignore())
            .ForMember(d => d.CVCHashed,       opt => opt.Ignore());

        CreateMap<CardConsumption, CardConsumptionDto>();
    }
}

public class TransactionMappingProfile : Profile
{
    public TransactionMappingProfile()
    {
        CreateMap<Transaction, TransactionDto>();
        CreateMap<CreateTransactionDto, Transaction>()
            .ForMember(d => d.Date,   opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.Status, opt => opt.Ignore());
    }
}

public class BeneficiaryMappingProfile : Profile
{
    public BeneficiaryMappingProfile()
    {
        CreateMap<Beneficiary, BeneficiaryDto>()
            .ForMember(d => d.OwnerFullName, opt => opt.Ignore());
        CreateMap<CreateBeneficiaryDto, Beneficiary>()
            .ForMember(d => d.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}

public class CommerceMappingProfile : Profile
{
    public CommerceMappingProfile()
    {
        CreateMap<Commerce, CommerceDto>();
        CreateMap<CreateCommerceDto, Commerce>()
            .ForMember(d => d.IsActive,  opt => opt.MapFrom(_ => true))
            .ForMember(d => d.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}