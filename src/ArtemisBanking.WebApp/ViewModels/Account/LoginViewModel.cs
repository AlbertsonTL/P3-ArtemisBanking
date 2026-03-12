using System.ComponentModel.DataAnnotations;

namespace ArtemisBanking.WebApp.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "El usuario es requerido")]
    [Display(Name = "Usuario")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}