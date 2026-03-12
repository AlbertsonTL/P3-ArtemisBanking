using System.ComponentModel.DataAnnotations;

namespace ArtemisBanking.WebApp.ViewModels.Account;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "El usuario es requerido")]
    [Display(Name = "Nombre de usuario")]
    public string UserName { get; set; } = string.Empty;
}