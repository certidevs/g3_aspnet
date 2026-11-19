using System.ComponentModel.DataAnnotations;

namespace G3AspNet.ViewModels.Account;

/// <summary>Verificación de segundo factor TOTP durante el inicio de sesión.</summary>
public sealed class LoginWith2faViewModel
{
    [Required(ErrorMessage = "Introduce el código de verificación.")]
    [StringLength(7, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos.")]
    [DataType(DataType.Text)]
    [Display(Name = "Código de autenticación")]
    public string TwoFactorCode { get; set; } = string.Empty;

    [Display(Name = "Recordar este dispositivo")]
    public bool RememberMachine { get; set; }

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
