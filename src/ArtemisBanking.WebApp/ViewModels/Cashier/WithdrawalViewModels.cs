using System.ComponentModel.DataAnnotations;

namespace ArtemisBanking.WebApp.ViewModels.Cashier;

public class WithdrawalViewModel
{
    [Required(ErrorMessage = "El número de cuenta es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe tener exactamente 9 dígitos.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "El número de cuenta debe contener solo dígitos.")]
    [Display(Name = "Número de Cuenta Origen")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto es requerido.")]
    [Range(0.01, 1000000, ErrorMessage = "El monto debe estar entre 0.01 y 1,000,000.")]
    [Display(Name = "Monto a Retirar (RD$)")]
    public decimal Amount { get; set; }
}

public class WithdrawalConfirmationViewModel
{
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AccountHolderName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
}