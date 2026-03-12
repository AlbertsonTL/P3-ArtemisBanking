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

public class CreateAccountViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un cliente.")]
    [Display(Name = "Cliente")]
    public string ClientId { get; set; } = string.Empty;

    public List<SelectListItem> Clients { get; set; } = new();
}
