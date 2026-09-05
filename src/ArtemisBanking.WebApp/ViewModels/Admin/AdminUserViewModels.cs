using System.ComponentModel.DataAnnotations;
using ArtemisBanking.Domain.Enums;

namespace ArtemisBanking.WebApp.ViewModels.Admin;

public class UserListViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string IdentityCard { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public UserRole Role { get; set; }
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "El nombre es requerido")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula es requerida")]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "La cédula debe tener 11 dígitos (formato 000-0000000-0)")]
    public string IdentityCard { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress(ErrorMessage = "Formato de email incorrecto")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es requerido")]
    public string UserName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "La contraseña es requerida")]
    [MinLength(8, ErrorMessage = "Mínimo 8 caracteres")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El rol es requerido")]
    public UserRole Role { get; set; }
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es requerido")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es requerido")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "La cédula es requerida")]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "La cédula debe tener 11 dígitos (formato 000-0000000-0)")]
    public string IdentityCard { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es requerido")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    // Campo especial para sumar fondos si es Cliente
    [DataType(DataType.Currency)]
    [Display(Name = "Añadir Fondos a Cuenta Principal")]
    public decimal MontoAdicional { get; set; } = 0m;
}
