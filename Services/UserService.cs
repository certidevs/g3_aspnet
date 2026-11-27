using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using G3AspNet.Data;
using G3AspNet.Models;
using G3AspNet.ViewModels.Profile;
using G3AspNet.ViewModels.Users;

namespace G3AspNet.Services;

/// <summary>
/// Casos de uso comunes de usuarios. Es la pieza que todos los proyectos de grupo
/// reciben ya resuelta antes de crear las entidades de su dominio.
/// </summary>
public sealed class UserService
{
    private readonly ApplicationDbContext context;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly RoleManager<IdentityRole> roleManager;
    private readonly ImageStorage images;
    private readonly ILogger<UserService> logger;

    public UserService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ImageStorage images,
        ILogger<UserService> logger)
    {
        this.context = context;
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.images = images;
        this.logger = logger;
    }

    /// <summary>Busca usuarios y añade a cada fila su único rol.</summary>
    public async Task<List<UserListItemViewModel>> SearchAsync(string? search)
    {
        var query = context.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(user =>
                (user.UserName != null && EF.Functions.Like(user.UserName, $"%{value}%")) ||
                (user.Email != null && EF.Functions.Like(user.Email, $"%{value}%")) ||
                (user.DisplayName != null && EF.Functions.Like(user.DisplayName, $"%{value}%")));
        }

        var users = query.OrderBy(user => user.UserName).ToList();
        var rows = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            rows.Add(new UserListItemViewModel
            {
                User = user,
                Role = roles.FirstOrDefault() ?? RoleNames.User
            });
        }

        return rows;
    }

    /// <summary>Carga los datos de perfil seguros para mostrar o administrar una cuenta.</summary>
    public ProfileViewModel? GetProfile(string userId)
    {
        var user = context.Users.AsNoTracking().SingleOrDefault(item => item.Id == userId);
        return user is null ? null : new ProfileViewModel { User = user };
    }

    /// <summary>Prepara el formulario del perfil de la cuenta autenticada.</summary>
    public ProfileEditViewModel? GetProfileEditModel(string userId)
    {
        var user = context.Users.AsNoTracking().SingleOrDefault(item => item.Id == userId);
        if (user is null)
        {
            return null;
        }

        return new ProfileEditViewModel
        {
            DisplayName = user.DisplayName ?? user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            CurrentAvatarFileName = user.AvatarFileName
        };
    }

    /// <summary>Actualiza datos personales y sustituye o elimina el avatar si procede.</summary>
    public async Task<IdentityResult> UpdateProfileAsync(string userId, ProfileEditViewModel model)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Failure("El usuario no existe.");
        }

        var displayName = model.DisplayName.Trim();
        var email = model.Email.Trim();
        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(email))
        {
            return Failure("El nombre visible y el email son obligatorios.");
        }

        var oldAvatarFileName = user.AvatarFileName;
        var avatarFileName = model.RemoveAvatar ? null : oldAvatarFileName;
        string? newAvatarFileName = null;
        if (model.Avatar is not null)
        {
            var upload = images.Save(model.Avatar);
            if (!upload.Succeeded)
            {
                return Failure(upload.Error!);
            }

            newAvatarFileName = upload.FileName;
            avatarFileName = newAvatarFileName;
        }

        user.DisplayName = displayName;
        user.Email = email;
        user.AvatarFileName = avatarFileName;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            images.Delete(newAvatarFileName);
            return updateResult;
        }

        if (!string.Equals(oldAvatarFileName, user.AvatarFileName, StringComparison.Ordinal))
        {
            images.Delete(oldAvatarFileName);
        }

        return updateResult;
    }

    /// <summary>Cambia la contraseña usando la verificación interna de Identity.</summary>
    public async Task<IdentityResult> ChangePasswordAsync(string userId, ChangePasswordViewModel model)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return Failure("El usuario no existe.");
        }

        return await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
    }

    /// <summary>Prepara el formulario administrativo de una cuenta.</summary>
    public async Task<EditUserViewModel?> GetEditModelAsync(string id)
    {
        var user = context.Users.AsNoTracking().SingleOrDefault(item => item.Id == id);
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return new EditUserViewModel
        {
            Id = user.Id,
            CurrentAvatarFileName = user.AvatarFileName,
            DisplayName = user.DisplayName ?? user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            Role = roles.FirstOrDefault() ?? RoleNames.User
        };
    }

    /// <summary>Actualiza una cuenta, revoca sesiones al cambiar seguridad y protege al último administrador.</summary>
    public async Task<IdentityResult> UpdateAsync(EditUserViewModel model, string currentAdminId)
    {
        if (!await roleManager.RoleExistsAsync(model.Role))
        {
            return Failure("El rol seleccionado no existe.");
        }

        if (model.Id == currentAdminId && (model.Role != RoleNames.Admin || !model.IsActive))
        {
            return Failure("No puedes quitarte el rol de administrador ni desactivar tu propia cuenta.");
        }

        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return Failure("El usuario no existe.");
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var wasAdmin = currentRoles.Contains(RoleNames.Admin);
        var wasActive = user.IsActive;
        if (wasAdmin && (model.Role != RoleNames.Admin || !model.IsActive))
        {
            var admins = await userManager.GetUsersInRoleAsync(RoleNames.Admin);
            var activeOtherAdmins = admins.Count(a => a.Id != user.Id && a.IsActive);
            if (activeOtherAdmins == 0)
            {
                return Failure("Debe quedar al menos un administrador activo en el sistema.");
            }
        }

        user.DisplayName = model.DisplayName.Trim();
        user.Email = model.Email.Trim();
        user.IsActive = model.IsActive;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return updateResult;
        }

        var securityChanged = wasActive != model.IsActive;

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);
            if (!passwordResult.Succeeded)
            {
                return passwordResult;
            }

            securityChanged = true;
            logger.LogInformation("Contraseña restablecida para usuario {TargetUserId} por admin {AdminId}.", user.Id,
                currentAdminId);
        }

        if (currentRoles.Count != 1 || currentRoles[0] != model.Role)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return removeResult;
            }

            var addResult = await userManager.AddToRoleAsync(user, model.Role);
            if (!addResult.Succeeded)
            {
                return addResult;
            }

            securityChanged = true;
            logger.LogInformation(
                "Cambio de rol para usuario {TargetUserId}: [{OldRoles}] -> {NewRole} por admin {AdminId}.",
                user.Id, string.Join(",", currentRoles), model.Role, currentAdminId);
        }

        if (securityChanged)
        {
            await userManager.UpdateSecurityStampAsync(user);
            if (wasActive != model.IsActive)
            {
                logger.LogInformation(
                    "Estado IsActive de usuario {TargetUserId} cambiado a {IsActive} por admin {AdminId}.",
                    user.Id, model.IsActive, currentAdminId);
            }
        }

        return IdentityResult.Success;
    }

    /// <summary>Crea una cuenta, asigna rol y guarda su avatar opcional.</summary>
    public async Task<IdentityResult> CreateAsync(CreateUserViewModel model)
    {
        if (!await roleManager.RoleExistsAsync(model.Role))
        {
            return Failure("El rol seleccionado no existe.");
        }

        var user = new ApplicationUser
        {
            UserName = model.Username.Trim(),
            Email = model.Email.Trim(),
            DisplayName = model.DisplayName.Trim(),
            IsActive = model.IsActive,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            return createResult;
        }

        var roleResult = await userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return roleResult;
        }

        logger.LogInformation("Usuario {UserId} ({UserName}) creado con rol {Role}.", user.Id, user.UserName,
            model.Role);

        if (model.Avatar is null)
        {
            return IdentityResult.Success;
        }

        var upload = images.Save(model.Avatar);
        if (!upload.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return Failure(upload.Error!);
        }

        user.AvatarFileName = upload.FileName;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            images.Delete(user.AvatarFileName);
            await userManager.DeleteAsync(user);
        }

        return updateResult;
    }

    /// <summary>Elimina una cuenta sin permitir borrar la propia ni el último admin.</summary>
    public async Task<IdentityResult> DeleteAsync(
        string id,
        string currentAdminId)
    {
        // 1. No permitir eliminar la propia cuenta
        if (id == currentAdminId)
        {
            return Failure(
                "No puedes eliminar tu propia cuenta desde la administración.");
        }

        // 2. Comprobar que el usuario existe
        var user = await userManager.FindByIdAsync(id);

        if (user is null)
        {
            return Failure("El usuario no existe.");
        }

        // 3. Proteger al último administrador
        if (await userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            var administrators =
                await userManager.GetUsersInRoleAsync(RoleNames.Admin);

            if (administrators.Count <= 1)
            {
                return Failure(
                    "No se puede eliminar al último administrador.");
            }
        }

        // 4. Comprobar vehículos relacionados
        var tieneVehiculos = await context.Vehicles
            .AnyAsync(v => v.OwnerId == id);

        // 5. Comprobar reparaciones asignadas al mecánico
        var tieneReparaciones = await context.Repairs
            .AnyAsync(r => r.MechanicId == id);

        // 6. Impedir el borrado si existen dependencias
        if (tieneVehiculos || tieneReparaciones)
        {
            return Failure(
                "No se puede eliminar el usuario porque tiene " +
                "vehículos o reparaciones asociados. " +
                "Puedes desactivar su cuenta.");
        }

        var avatarFileName = user.AvatarFileName;

        try
        {
            // 7. Eliminar usuario
            var result = await userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                // 8. Eliminar avatar
                try
                {
                    images.Delete(avatarFileName);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "No se pudo eliminar el avatar del usuario {UserId}.",
                        id);
                }

                logger.LogInformation(
                    "Usuario {TargetUserId} ({UserName}) eliminado por admin {AdminId}.",
                    user.Id,
                    user.UserName,
                    currentAdminId);
            }

            return result;
        }
        // Respaldo por si se asignan datos entre la comprobación y el borrado. SQLite usa
        // 1811 cuando una relación Restrict impide borrar y 787 en el resto de claves ajenas.
        catch (DbUpdateException ex) when (
            ex.InnerException is Microsoft.Data.Sqlite.SqliteException sqlite
            && sqlite.SqliteExtendedErrorCode is 787 or 1811)
        {
            logger.LogWarning(
                ex,
                "No se pudo eliminar el usuario {UserId} por una clave foránea.",
                id);

            return Failure(
                "No se puede eliminar el usuario porque existen " +
                "registros relacionados. Puedes desactivar su cuenta.");
        }
    }

    private static IdentityResult Failure(string message) =>
        IdentityResult.Failed(new IdentityError { Description = message });
}
