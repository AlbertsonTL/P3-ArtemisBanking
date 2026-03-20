using System.ComponentModel.DataAnnotations;

namespace ArtemisBanking.WebApp.ViewModels.Client;

public class BeneficiaryListViewModel
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AddBeneficiaryViewModel
{
    [Required(ErrorMessage = "El número de cuenta es requerido.")]
    [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe tener 9 dígitos.")]
    [Display(Name = "Número de Cuenta")]
    public string AccountNumber { get; set; } = string.Empty;
}
