using System.ComponentModel.DataAnnotations;

namespace ArtemisBanking.WebApp.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El correo electrónico es requerido")]
    [EmailAddress(ErrorMessage = "Formato de correo inválido")]
    [Display(Name = "Correo Electrónico")]
    public string Email { get; set; } = string.Empty;
}
