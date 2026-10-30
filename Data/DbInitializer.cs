using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using G3AspNet.Models;

namespace G3AspNet.Data;

/// <summary>Aplica migraciones y prepara roles y cuentas demo de forma idempotente.</summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Facilita arrancar en clase sin ejecutar comandos SQL manuales.
        context.Database.Migrate();

        var configuration = services.GetRequiredService<IConfiguration>();
        if (!configuration.GetValue("SeedData:Enabled", true))
        {
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, RoleNames.User);
        await EnsureRoleAsync(roleManager, RoleNames.Admin);
        await EnsureRoleAsync(roleManager, RoleNames.Customer);
        await EnsureRoleAsync(roleManager, RoleNames.Mechanic);

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(
            userManager,
            username: "admin",
            email: "admin@g3.local",
            displayName: "Administrador",
            password: "Admin123!",
            role: RoleNames.Admin);

        await EnsureUserAsync(
            userManager,
            username: "user",
            email: "user@g3.local",
            displayName: "Usuario de demo",
            password: "User123!",
            role: RoleNames.User);

        await EnsureUserAsync(
            userManager,
            username: "customer",
            email: "customer@g3.local",
            displayName: "Cliente Demo",
            password: "Customer123!",
            role: RoleNames.Customer);

        await EnsureUserAsync(
            userManager,
            username: "mechanic",
            email: "mechanic@g3.local",
            displayName: "Mecánico Demo",
            password: "Mechanic123!",
            role: RoleNames.Mechanic);

        if (!await context.Vehicles.AnyAsync())
        {
            var customerUser = await userManager.FindByNameAsync("customer");
            if (customerUser != null)
            {
                context.Vehicles.Add(new Vehicle
                {
                    LicensePlate = "1234-BBB",
                    VIN = "VF11234567890ABCD",
                    Brand = "Seat",
                    Model = "Ibiza",
                    Year = 2020,
                    FuelType = G3AspNet.Models.Enum.FuelType.Petrol,
                    Mileage = 45000,
                    OwnerId = customerUser.Id
                });
                await context.SaveChangesAsync();
            }
        }

        if (!await context.SpareParts.AnyAsync())
        {
            context.SpareParts.AddRange(
                new SparePart { Name = "Filtro de aceite", Brand = "Bosch", UnitPrice = 14.50m, Stock = 20, MinStock = 5 },
                new SparePart { Name = "Pastillas de freno", Brand = "Brembo", UnitPrice = 45.00m, Stock = 12, MinStock = 4 },
                new SparePart { Name = "Aceite 5W30 5L", Brand = "Castrol", UnitPrice = 38.90m, Stock = 15, MinStock = 3 }
            );
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el rol {roleName}.");
            }
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string username,
        string email,
        string displayName,
        string password,
        string role)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                DisplayName = displayName,
                EmailConfirmed = true,
                IsActive = true
            };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo crear el usuario demo {username}.");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"No se pudo asignar el rol {role} a {username}.");
            }
        }
    }
}