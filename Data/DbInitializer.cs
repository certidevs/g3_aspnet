using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using G3AspNet.Models;
using G3AspNet.Models.Enum;

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

        await EnsureVehiclesAsync(context, userManager);
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

    private static async Task EnsureVehiclesAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.Vehicles.AnyAsync())
        {
            return;
        }

        var customerUser = await userManager.FindByNameAsync("customer")
                           ?? (await userManager.GetUsersInRoleAsync(RoleNames.Customer)).FirstOrDefault();

        if (customerUser is null)
        {
            return;
        }

        var demoVehicles = new List<Vehicle>
        {
            new Vehicle
            {
                LicensePlate = "1234ABC",
                VIN = "VF1BB050F12345678",
                Brand = "Renault",
                Model = "Clio 1.5 dCi",
                Year = 2018,
                FuelType = FuelType.Diesel,
                Mileage = 87500,
                OwnerId = customerUser.Id
            },
            new Vehicle
            {
                LicensePlate = "5678DEF",
                VIN = "WVWZZZ3CZWE098765",
                Brand = "Volkswagen",
                Model = "Golf 1.5 TSI",
                Year = 2021,
                FuelType = FuelType.Petrol,
                Mileage = 42300,
                OwnerId = customerUser.Id
            },
            new Vehicle
            {
                LicensePlate = "9012GHI",
                VIN = "JTDKN36U001234567",
                Brand = "Toyota",
                Model = "Yaris Hybrid",
                Year = 2023,
                FuelType = FuelType.Hybrid,
                Mileage = 15800,
                OwnerId = customerUser.Id
            }
        };

        await context.Vehicles.AddRangeAsync(demoVehicles);
        await context.SaveChangesAsync();
    }
}