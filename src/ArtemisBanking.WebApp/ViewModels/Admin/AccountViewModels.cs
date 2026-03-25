using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ArtemisBanking.Domain.Enums;

namespace ArtemisBanking.WebApp.ViewModels.Admin;

public class AccountListViewModel
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string IdentityCard { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
}

public class AccountDetailsViewModel
{
    public string AccountNumber { get; set; } = string.Empty;
    public string ClientName    { get; set; } = string.Empty;
    public string IdentityCard  { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
    public decimal Balance       { get; set; }
    public bool IsActive         { get; set; }
    public List<TransactionItemViewModel> Transactions { get; set; } = new();
}

public class TransactionItemViewModel
{
    public DateTime Date              { get; set; }
    public TransactionType Type       { get; set; }
    public decimal Amount             { get; set; }
    public TransactionCategory Category { get; set; }
    public string Origin              { get; set; } = string.Empty;
    public string Beneficiary         { get; set; } = string.Empty;
    public TransactionStatus Status   { get; set; }
}

public class CreateAccountViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un cliente.")]
    [Display(Name = "Cliente")]
    public string ClientId { get; set; } = string.Empty;

    public List<SelectListItem> Clients { get; set; } = new();
}
