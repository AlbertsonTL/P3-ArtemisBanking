using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArtemisBanking.WebApp.ViewModels.Admin;

public class LoanListViewModel
{
    public int Id { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string IdentityCard { get; set; } = string.Empty;
    
    // Configuración del préstamo
    public decimal Amount { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public int TermMonths { get; set; }
    public decimal MonthlyPayment { get; set; }
    
    // Estado
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Deuda Restante (calculada)
    public decimal RemainingDebt { get; set; } 
}

public class AssignLoanViewModel
{
    [Required(ErrorMessage = "Debe buscar y seleccionar un cliente.")]
    [Display(Name = "Cliente")]
    public string ClientId { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;
    public string IdentityCard { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto es obligatorio.")]
    [Range(5000, 5000000, ErrorMessage = "El préstamo debe rondar entre RD$ 5,000 y RD$ 5,000,000.")]
    [Display(Name = "Monto del Préstamo (RD$)")]
    public decimal Amount { get; set; }

    [Required]
    [Range(0.01, 50.0, ErrorMessage = "Tasa ilógica (permite entre 0.01% y 50%)")]
    [Display(Name = "Tasa de Interés Anual (%)")]
    public decimal AnnualInterestRate { get; set; }

    [Required]
    [Range(6, 360, ErrorMessage = "Plazo debe ser entre 6 y 360 meses (30 años).")]
    [Display(Name = "Plazo de Financiamiento (Meses)")]
    public int TermMonths { get; set; }
}
