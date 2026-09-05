using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArtemisBanking.WebApp.ViewModels.Client;

public class TransactionMainViewModel
{
    // Para mostrar un selector de tipo de transacción en la vista principal
}

public class BaseTransactionViewModel
{
    [Required(ErrorMessage = "Seleccione una cuenta de origen.")]
    [Display(Name = "Cuenta de Origen")]
    public string SourceAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto es requerido.")]
    [Range(1, 1000000, ErrorMessage = "El monto debe ser entre 1 y 1,000,000.")]
    [Display(Name = "Monto (RD$)")]
    public decimal Amount { get; set; }

    public List<SelectListItem> MyAccounts { get; set; } = new();
}

public class OwnAccountsTransferViewModel : BaseTransactionViewModel
{
    [Required(ErrorMessage = "Seleccione una cuenta de destino.")]
    [Display(Name = "Cuenta de Destino")]
    public string DestinationAccountNumber { get; set; } = string.Empty;
}

public class BeneficiaryTransferViewModel : BaseTransactionViewModel
{
    [Required(ErrorMessage = "Seleccione un beneficiario.")]
    [Display(Name = "Beneficiario")]
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public List<SelectListItem> Beneficiaries { get; set; } = new();
}

public class ExpressTransferViewModel : BaseTransactionViewModel
{
    [Required(ErrorMessage = "El número de cuenta de destino es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe tener 9 dígitos.")]
    [Display(Name = "Número de Cuenta de Destino")]
    public string DestinationAccountNumber { get; set; } = string.Empty;
}

public class CreditCardPaymentViewModel : BaseTransactionViewModel
{
    [Required(ErrorMessage = "Seleccione la tarjeta a pagar.")]
    [Display(Name = "Tarjeta de Crédito")]
    public int CreditCardId { get; set; }
    public List<SelectListItem> MyCards { get; set; } = new();
}

public class LoanPaymentViewModel : BaseTransactionViewModel
{
    [Required(ErrorMessage = "Seleccione el préstamo a pagar.")]
    [Display(Name = "Préstamo")]
    public int LoanId { get; set; }
    public List<SelectListItem> MyLoans { get; set; } = new();
}

public class CashAdvanceViewModel
{
    [Required(ErrorMessage = "Seleccione la tarjeta de crédito.")]
    [Display(Name = "Tarjeta de Crédito")]
    public int CreditCardId { get; set; }

    [Required(ErrorMessage = "Seleccione la cuenta de destino.")]
    [Display(Name = "Cuenta de Ahorro Destino")]
    public string DestinationAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "El monto es requerido.")]
    [Range(1, 1000000, ErrorMessage = "Monto inválido.")]
    [Display(Name = "Monto a Retirar (RD$)")]
    public decimal Amount { get; set; }

    public List<SelectListItem> MyCards { get; set; } = new();
    public List<SelectListItem> MyAccounts { get; set; } = new();
}
