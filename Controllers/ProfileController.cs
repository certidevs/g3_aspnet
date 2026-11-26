using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using G3AspNet.Models;
using G3AspNet.Services;
using G3AspNet.Utilities;
using G3AspNet.ViewModels.Profile;

namespace G3AspNet.Controllers;

[Authorize]
/// <summary>Consulta y edición del perfil del usuario autenticado y configuración 2FA.</summary>
public sealed class ProfileController : Controller
{
    private readonly UserService userService;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UrlEncoder urlEncoder;
    private readonly ILogger<ProfileController> logger;

    public ProfileController(
        UserService userService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        UrlEncoder urlEncoder,
        ILogger<ProfileController> logger)
    {
        this.userService = userService;
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.urlEncoder = urlEncoder;
        this.logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = userService.GetProfile(User.GetRequiredUserId());
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Edit()
    {
        var model = userService.GetProfileEditModel(User.GetRequiredUserId());
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProfileEditViewModel model)
    {
        var userId = User.GetRequiredUserId();
        if (!ModelState.IsValid)
        {
            RestoreCurrentAvatar(model, userId);
            return View(model);
        }

        var result = await userService.UpdateProfileAsync(userId, model);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            RestoreCurrentAvatar(model, userId);
            return View(model);
        }

        await RefreshSignInAsync(userId);
        TempData["Message"] = "Perfil actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.GetRequiredUserId();
        var result = await userService.ChangePasswordAsync(userId, model);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        await RefreshSignInAsync(userId);
        TempData["Message"] = "Contraseña cambiada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> TwoFactor()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        return View(await BuildTwoFactorModelAsync(user));
    }

    [HttpPost]
    public async Task<IActionResult> EnableTwoFactor(TwoFactorViewModel model)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(TwoFactor), await BuildTwoFactorModelAsync(user));
        }

        var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var isTokenValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            verificationCode);

        if (!isTokenValid)
        {
            ModelState.AddModelError(nameof(model.Code), "El código de verificación no es válido.");
            return View(nameof(TwoFactor), await BuildTwoFactorModelAsync(user));
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        logger.LogInformation("2FA TOTP activado para usuario {UserId}.", user.Id);
        TempData["Message"] = "Autenticación en dos pasos (2FA) activada correctamente.";
        return RedirectToAction(nameof(TwoFactor));
    }

    [HttpPost]
    public async Task<IActionResult> DisableTwoFactor()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        logger.LogWarning("2FA TOTP desactivado para usuario {UserId}.", user.Id);
        TempData["Message"] = "Autenticación en dos pasos desactivada.";
        return RedirectToAction(nameof(TwoFactor));
    }

    private async Task<TwoFactorViewModel> BuildTwoFactorModelAsync(ApplicationUser user)
    {
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user) ?? string.Empty;
        }

        var email = await userManager.GetEmailAsync(user) ?? user.UserName ?? "usuario";
        var formattedKey = FormatKey(key);
        var authenticatorUri = string.Format(
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            urlEncoder.Encode("TallerNet"),
            urlEncoder.Encode(email),
            key);

        var isEnabled = await userManager.GetTwoFactorEnabledAsync(user);

        return new TwoFactorViewModel
        {
            Is2faEnabled = isEnabled,
            SharedKey = formattedKey,
            AuthenticatorUri = authenticatorUri,
            QrCodeImage = isEnabled ? string.Empty : QrCodeDataUri.FromText(authenticatorUri)
        };
    }

    private static string FormatKey(string unformattedKey)
    {
        var result = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + 4 < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition, 4)).Append(' ');
            currentPosition += 4;
        }

        if (currentPosition < unformattedKey.Length)
        {
            result.Append(unformattedKey.AsSpan(currentPosition));
        }

        return result.ToString().ToLowerInvariant();
    }

    private void RestoreCurrentAvatar(ProfileEditViewModel model, string userId)
    {
        model.CurrentAvatarFileName = userService.GetProfileEditModel(userId)?.CurrentAvatarFileName;
    }

    private async Task RefreshSignInAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            await signInManager.RefreshSignInAsync(user);
        }
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
