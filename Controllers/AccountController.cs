using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.RateLimiting;
using G3AspNet.Models;
using G3AspNet.ViewModels.Account;

namespace G3AspNet.Controllers;

/// <summary>Registro, login, verificación 2FA y logout mediante ASP.NET Core Identity.</summary>
public sealed class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly ILogger<AccountController> logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl, bool loggedOut = false) => View(new LoginViewModel
    {
        ReturnUrl = returnUrl,
        ShowLoggedOutMessage = loggedOut
    });

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    /// <summary>Identity necesita async para validar la contraseña y crear la cookie.</summary>
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var identifier = model.Username.Trim();
        var user = await userManager.FindByNameAsync(identifier)
            ?? await userManager.FindByEmailAsync(identifier);
        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Intento de inicio de sesión fallido para identificador {Identifier}.", identifier);
            ModelState.AddModelError(string.Empty, "El usuario o la contraseña no son válidos.");
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("Inicio de sesión correcto para usuario {UserId}.", user.Id);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            logger.LogInformation("Usuario {UserId} requiere verificación 2FA.", user.Id);
            return RedirectToAction(nameof(LoginWith2fa), new
            {
                returnUrl = model.ReturnUrl,
                rememberMe = model.RememberMe
            });
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("Cuenta {UserId} bloqueada temporalmente por demasiados intentos fallidos.", user.Id);
            ModelState.AddModelError(string.Empty, "La cuenta se ha bloqueado temporalmente por demasiados intentos.");
            return View(model);
        }

        logger.LogWarning("Contraseña incorrecta para usuario {UserId}.", user.Id);
        ModelState.AddModelError(string.Empty, "El usuario o la contraseña no son válidos.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string? returnUrl = null)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToAction(nameof(Login), new { returnUrl });
        }

        return View(new LoginWith2faViewModel
        {
            RememberMe = rememberMe,
            ReturnUrl = returnUrl
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null || !user.IsActive)
        {
            return RedirectToAction(nameof(Login), new { returnUrl = model.ReturnUrl });
        }

        var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);
        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
            authenticatorCode,
            model.RememberMe,
            model.RememberMachine);

        if (result.Succeeded)
        {
            logger.LogInformation("Inicio de sesión 2FA correcto para usuario {UserId}.", user.Id);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("Cuenta {UserId} bloqueada durante verificación 2FA.", user.Id);
            ModelState.AddModelError(string.Empty, "La cuenta se ha bloqueado temporalmente por demasiados intentos.");
            return View(model);
        }

        logger.LogWarning("Código 2FA inválido para usuario {UserId}.", user.Id);
        ModelState.AddModelError(string.Empty, "El código de verificación no es válido.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        var model = new RegisterViewModel();
        CargarRoles(model);
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // Validación en servidor: el valor del <select> se puede manipular desde el navegador.
        if (!RolesPermitidos.Contains(model.Rol))
        {
            ModelState.AddModelError(nameof(model.Rol), "El rol seleccionado no es válido.");
        }

        if (!ModelState.IsValid)
        {
            CargarRoles(model);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Username.Trim(),
            Email = model.Email.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(model.DisplayName)
                ? model.Username.Trim()
                : model.DisplayName.Trim(),
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            CargarRoles(model);
            return View(model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, model.Rol);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            AddIdentityErrors(roleResult);
            CargarRoles(model);
            return View(model);
        }

        logger.LogInformation("Nueva cuenta registrada: {UserId} con rol {Role}.", user.Id, model.Rol);
        await signInManager.SignInAsync(user, isPersistent: false);
        TempData["Message"] = "Tu cuenta se ha creado correctamente.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        var userId = userManager.GetUserId(User);
        await signInManager.SignOutAsync();
        logger.LogInformation("Cierre de sesión para usuario {UserId}.", userId);
        return RedirectToAction(nameof(Login), new { loggedOut = true });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    /// <summary>Evita que returnUrl pueda redirigir a una web externa.</summary>
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl!);
        }

        return RedirectToAction("Index", "Home")!;
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    /// <summary>Roles que un usuario puede elegir al registrarse públicamente. Nunca incluir Admin ni Mechanic.</summary>
    private static string[] RolesPermitidos => new[]
    {
        RoleNames.User
    };

    private static void CargarRoles(RegisterViewModel model)
    {
        model.RolesDisponibles = RolesPermitidos
            .Select(r => new SelectListItem(r, r))
            .ToList();
    }
}
