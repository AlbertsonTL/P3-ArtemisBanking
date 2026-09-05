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
    
    // Indica si el préstamo tiene cuotas vencidas (mora real según IsLate de AmortizationEntry)
    public bool HasLateEntries { get; set; }
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

public class RiskWarningViewModel
{
    // Datos del préstamo pendiente (para re-enviar al confirmar)
    public string ClientId            { get; set; } = string.Empty;
    public string ClientName          { get; set; } = string.Empty;
    public decimal Amount             { get; set; }
    public decimal AnnualInterestRate { get; set; }
    public int    TermMonths          { get; set; }

    // Datos del análisis de riesgo (para mostrar en pantalla)
    public string  RiskMessage        { get; set; } = string.Empty;
    public decimal DeudaActualCliente { get; set; }
    public decimal PromedioSistema    { get; set; }
    public decimal TotalNuevoPrestamo { get; set; }
}

public class EditLoanRateViewModel
{
    public int    LoanId             { get; set; }
    public string LoanNumber         { get; set; } = string.Empty;
    public string ClientName         { get; set; } = string.Empty;
    public decimal CurrentRate       { get; set; }
    public decimal CurrentMonthly    { get; set; }
    public int    RemainingQuotas    { get; set; }

    [Required(ErrorMessage = "La tasa de interés es obligatoria.")]
    [Range(0.01, 50.0, ErrorMessage = "La tasa debe ser mayor a 0 y menor o igual a 50%.")]
    [Display(Name = "Nueva Tasa de Interés Anual (%)")]
    public decimal NewAnnualInterestRate { get; set; }
}

