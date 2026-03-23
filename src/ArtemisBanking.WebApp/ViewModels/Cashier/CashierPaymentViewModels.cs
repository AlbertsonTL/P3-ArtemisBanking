using System.ComponentModel.DataAnnotations;

namespace ArtemisBanking.WebApp.ViewModels.Cashier;

public class CreditCardPaymentViewModel
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe tener exactamente 9 dígitos.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "El número de cuenta debe contener solo dígitos.")]
    [Display(Name = "Número de Cuenta Origen")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de tarjeta es requerido.")]
    [StringLength(16, MinimumLength = 16, ErrorMessage = "El número de tarjeta debe tener exactamente 16 dígitos.")]
    [RegularExpression(@"^\d{16}$", ErrorMessage = "El número de tarjeta debe contener solo dígitos.")]
    [Display(Name = "Número de Tarjeta de Crédito")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto es requerido.")]
    [Range(0.01, 1000000, ErrorMessage = "El monto debe estar entre 0.01 y 1,000,000.")]
    [Display(Name = "Monto a Pagar (RD$)")]
    public decimal Amount { get; set; }
}

public class CreditCardPaymentConfirmationViewModel
{
    public string AccountNumber { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal CurrentCardDebt { get; set; }
    public decimal ActualAmountToCharge { get; set; }  // Si deuda < monto, se cobra solo la deuda
}

public class LoanPaymentViewModel
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe tener exactamente 9 dígitos.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "El número de cuenta debe contener solo dígitos.")]
    [Display(Name = "Número de Cuenta Origen")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número del préstamo es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número del préstamo debe tener exactamente 9 dígitos.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "El número del préstamo debe contener solo dígitos.")]
    [Display(Name = "Número del Préstamo")]
    public string LoanNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto es requerido.")]
    [Range(0.01, 1000000, ErrorMessage = "El monto debe estar entre 0.01 y 1,000,000.")]
    [Display(Name = "Monto a Pagar (RD$)")]
    public decimal Amount { get; set; }
}

public class LoanPaymentConfirmationViewModel
{
    public string AccountNumber { get; set; } = string.Empty;
    public string LoanNumber { get; set; } = string.Empty;
    public string LoanHolderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal RemainingDebt { get; set; }
    public int PendingQuotas { get; set; }
    public decimal ActualAmountToCharge { get; set; }
    public decimal ExcessAmount { get; set; }  // Excedente a retornar
}

public class ThirdPartyTransferViewModel
{
    [Required(ErrorMessage = "El número de cuenta origen es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe tener exactamente 9 dígitos.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "El número de cuenta debe contener solo dígitos.")]
    [Display(Name = "Número de Cuenta Origen")]
    public string SourceAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe tener exactamente 9 dígitos.")]
    [RegularExpression(@"^\d{9}$", ErrorMessage = "El número de cuenta debe contener solo dígitos.")]
    [Display(Name = "Número de Cuenta Destino")]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto es requerido.")]
    [Range(0.01, 1000000, ErrorMessage = "El monto debe estar entre 0.01 y 1,000,000.")]
    [Display(Name = "Monto a Transferir (RD$)")]
    public decimal Amount { get; set; }
}

public class ThirdPartyTransferConfirmationViewModel
{
    public string SourceAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountHolderName { get; set; } = string.Empty;
    public string SourceAccountHolderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal SourceAccountBalance { get; set; }
    public decimal DestinationAccountBalance { get; set; }
}