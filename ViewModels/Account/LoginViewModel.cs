using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace G3AspNet.ViewModels.Account;

/// <summary>Campos del formulario de inicio de sesión.</summary>
public sealed class LoginViewModel
{
    //Declaro el rol 
    [Required(ErrorMessage = "El rol es obligatorio.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = string.Empty;

    public List<SelectListItem> RolesDisponibles { get; set; } = new();

    [Required(ErrorMessage = "El usuario o email es obligatorio.")]
    [Display(Name = "Usuario o email")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Recordar sesión")]
    public bool RememberMe { get; set; }

    /// <summary>Ruta local a la que volver después de autenticar correctamente.</summary>
    public string? ReturnUrl { get; set; }

    public bool ShowLoggedOutMessage { get; set; }
}
