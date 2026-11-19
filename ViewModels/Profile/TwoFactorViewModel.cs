using System.ComponentModel.DataAnnotations;

namespace G3AspNet.ViewModels.Profile;

/// <summary>Configuración TOTP de doble factor en el perfil de usuario.</summary>
public sealed class TwoFactorViewModel
{
    public bool Is2faEnabled { get; set; }

    public string SharedKey { get; set; } = string.Empty;

    public string AuthenticatorUri { get; set; } = string.Empty;

    [Required(ErrorMessage = "Introduce el código de 6 dígitos generado por tu app autenticadora.")]
    [StringLength(7, MinimumLength = 6, ErrorMessage = "El código debe tener 6 dígitos.")]
    [Display(Name = "Código de verificación")]
    public string Code { get; set; } = string.Empty;
}
