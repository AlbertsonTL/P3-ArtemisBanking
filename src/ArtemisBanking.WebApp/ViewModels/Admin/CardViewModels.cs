using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ArtemisBanking.Domain.Enums;

namespace ArtemisBanking.WebApp.ViewModels.Admin;

public class CardListViewModel
{
    public int Id { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string IdentityCard { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal DebtAmount { get; set; }
    public string ExpirationDate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AssignCardViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un cliente.")]
    [Display(Name = "Cliente")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El límite de crédito es requerido.")]
    [Range(1000, 10000000, ErrorMessage = "El límite debe ser entre RD$1,000 y RD$10,000,000")]
    [Display(Name = "Límite de Crédito (RD$)")]
    public decimal CreditLimit { get; set; }

    // Lista para el select
    public List<SelectListItem> Clients { get; set; } = new();
}

public class EditLimitViewModel
{
    public int Id { get; set; }
    
    [Required]
    [Range(1000, 10000000, ErrorMessage = "Límite inválido")]
    public decimal NewLimit { get; set; }
}

public class CardConsumptionViewModel
{
    public string CardNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public List<ConsumptionItemViewModel> Consumptions { get; set; } = new();
}

public class ConsumptionItemViewModel
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string CommerceName { get; set; } = string.Empty;
    public ConsumptionStatus Status { get; set; }
}
