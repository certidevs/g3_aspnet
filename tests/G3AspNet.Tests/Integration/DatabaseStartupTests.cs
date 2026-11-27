using G3AspNet.Data;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace G3AspNet.Tests.Integration;

/// <summary>
/// Arranque de la base de datos: persistencia, roles, administrador inicial y datos demo.
/// Cada prueba usa su propia base y puede "reiniciar" la aplicación sobre ella.
/// </summary>
public sealed class DatabaseStartupTests : IDisposable
{
    private const string AdminPassword = "Inicial123!";

    private readonly string dbPath = Path.Combine(Path.GetTempPath(), $"g3-startup-{Guid.NewGuid():N}.db");

    private static readonly Dictionary<string, string> AdminSettings = new()
    {
        ["AdminAccount:UserName"] = "jefa",
        ["AdminAccount:Email"] = "jefa@taller.local",
        ["AdminAccount:Password"] = AdminPassword
    };

    private WebApplicationFactory<Program> Start(
        string environment = "Production",
        Dictionary<string, string>? settings = null,
        Action<IServiceCollection>? services = null)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            if (services is not null)
            {
                builder.ConfigureTestServices(services);
            }

            builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath}");
            foreach (var (key, value) in settings ?? [])
            {
                builder.UseSetting(key, value);
            }
        });

        _ = factory.Services; // Fuerza el arranque y, con él, DbInitializer.
        return factory;
    }

    private static async Task<T> InScopeAsync<T>(
        WebApplicationFactory<Program> factory,
        Func<ApplicationDbContext, UserManager<ApplicationUser>, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        return await action(
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
    }

    private static Task AddSparePartAsync(WebApplicationFactory<Program> factory) =>
        InScopeAsync(factory, async (db, _) =>
        {
            db.SpareParts.Add(new SparePart { Reference = "PERSISTE-1", Name = "Dato real" });
            return await db.SaveChangesAsync();
        });

    private static Task<bool> SparePartExistsAsync(WebApplicationFactory<Program> factory) =>
        InScopeAsync(factory, (db, _) => db.SpareParts.AnyAsync(part => part.Reference == "PERSISTE-1"));

    [Fact]
    public async Task Production_CreatesTheRoles_ButNoAccountsNorDemoData()
    {
        using var app = Start();

        await InScopeAsync(app, async (db, _) =>
        {
            Assert.Equal(
                [RoleNames.Admin, RoleNames.Customer, RoleNames.Mechanic, RoleNames.User],
                await db.Roles.Select(role => role.Name!).OrderBy(name => name).ToListAsync());
            Assert.Empty(await db.Users.ToListAsync());
            Assert.Empty(await db.Vehicles.ToListAsync());
            return true;
        });
    }

    [Fact]
    public async Task Production_KeepsTheDataAfterRestarting()
    {
        using (var first = Start())
        {
            await AddSparePartAsync(first);
        }

        using var second = Start();

        Assert.True(await SparePartExistsAsync(second));
    }

    [Fact]
    public async Task Development_RecreatesTheDatabaseAndLoadsDemoData()
    {
        using (var first = Start("Development"))
        {
            await AddSparePartAsync(first);
        }

        using var second = Start("Development");

        Assert.False(await SparePartExistsAsync(second));
        Assert.True(await InScopeAsync(second, (db, _) => db.Users.AnyAsync(user => user.UserName == "admin")));
    }

    [Fact]
    public async Task Registration_WorksWithoutDemoData()
    {
        using var app = Start();
        var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostFormAsync("/account/register", "/account/register", new()
        {
            ["Username"] = "primera",
            ["Email"] = "primera@taller.local",
            ["Password"] = "Nuevo1234!",
            ["ConfirmPassword"] = "Nuevo1234!"
        });

        response.AssertRedirectTo("/");
    }

    [Fact]
    public async Task AdminAccount_IsCreatedWithTheAdminRole_AndNothingElse()
    {
        using var app = Start(settings: AdminSettings);

        await InScopeAsync(app, async (db, users) =>
        {
            var admin = Assert.Single(await db.Users.ToListAsync());
            Assert.Equal("jefa", admin.UserName);
            Assert.True(admin.IsActive);
            Assert.True(admin.EmailConfirmed);
            Assert.Equal([RoleNames.Admin], await users.GetRolesAsync(admin));
            Assert.True(await users.CheckPasswordAsync(admin, AdminPassword));
            return true;
        });
    }

    [Fact]
    public async Task AdminAccount_IsIgnoredOnceAnAdminExists()
    {
        using (Start(settings: AdminSettings))
        {
        }

        using var second = Start(settings: new()
        {
            ["AdminAccount:UserName"] = "otra",
            ["AdminAccount:Email"] = "otra@taller.local",
            ["AdminAccount:Password"] = "Cambiada123!"
        });

        await InScopeAsync(second, async (db, users) =>
        {
            var admin = Assert.Single(await db.Users.ToListAsync());
            Assert.Equal("jefa", admin.UserName);
            Assert.True(await users.CheckPasswordAsync(admin, AdminPassword));
            return true;
        });
    }

    [Theory]
    [InlineData("jefa", "distinta@taller.local")]
    [InlineData("distinta", "jefa@taller.local")]
    public async Task AdminAccount_DoesNotPromoteAnExistingAccount(string username, string email)
    {
        using (var first = Start())
        {
            await InScopeAsync(first, async (_, users) =>
            {
                var intruder = new ApplicationUser { UserName = username, Email = email, EmailConfirmed = true };
                Assert.True((await users.CreateAsync(intruder, "Intruso123!")).Succeeded);
                return await users.AddToRoleAsync(intruder, RoleNames.User);
            });
        }

        using var second = Start(settings: AdminSettings);

        await InScopeAsync(second, async (_, users) =>
        {
            Assert.Empty(await users.GetUsersInRoleAsync(RoleNames.Admin));
            return true;
        });
    }

    [Theory]
    [InlineData("AdminAccount:UserName")]
    [InlineData("AdminAccount:Email")]
    [InlineData("AdminAccount:Password")]
    public void AdminAccount_Incomplete_StopsTheStartup(string missing)
    {
        var settings = new Dictionary<string, string>(AdminSettings) { [missing] = "" };

        var error = Assert.ThrowsAny<Exception>(() => Start(settings: settings));

        Assert.Contains("hacen falta", error.ToString());
    }

    [Fact]
    public void AdminAccount_WeakPassword_StopsTheStartupWithoutLeakingIt()
    {
        const string weak = "debil";
        var settings = new Dictionary<string, string>(AdminSettings) { ["AdminAccount:Password"] = weak };

        var error = Assert.ThrowsAny<Exception>(() => Start(settings: settings));

        Assert.Contains("No se pudo crear el administrador inicial", error.ToString());
        Assert.DoesNotContain(weak, error.Message);
    }

    [Fact]
    public async Task DemoData_IsNotAddedToADatabaseThatAlreadyHasAccounts()
    {
        using (Start(settings: AdminSettings))
        {
        }

        using var second = Start(settings: new() { ["SeedData:Enabled"] = "true" });

        await InScopeAsync(second, async (db, _) =>
        {
            Assert.Equal("jefa", Assert.Single(await db.Users.ToListAsync()).UserName);
            Assert.Empty(await db.Vehicles.ToListAsync());
            return true;
        });
    }

    [Fact]
    public async Task DemoData_SurvivesARestartWithoutDuplicates()
    {
        var demo = new Dictionary<string, string> { ["SeedData:Enabled"] = "true" };
        using (var first = Start(settings: demo))
        {
            await InScopeAsync(first, (db, _) => db.Repairs
                .Where(repair => repair.Status == RepairStatus.Pending)
                .ExecuteUpdateAsync(set => set.SetProperty(repair => repair.Status, RepairStatus.InProgress)));
        }

        using var second = Start(settings: demo);

        await InScopeAsync(second, async (db, _) =>
        {
            Assert.Equal(8, await db.Users.CountAsync());
            Assert.Equal(7, await db.Vehicles.CountAsync());
            Assert.Equal(13, await db.Repairs.CountAsync());
            Assert.False(await db.Repairs.AnyAsync(repair => repair.Status == RepairStatus.Pending));
            return true;
        });
    }

    /// <summary>Acepta crear la cuenta y rechaza cualquier cambio posterior, como asignarle un rol.</summary>
    private sealed class RejectChanges(ApplicationDbContext db) : IUserValidator<ApplicationUser>
    {
        public async Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user) =>
            await db.Users.AnyAsync(stored => stored.Id == user.Id)
                ? IdentityResult.Failed(new IdentityError { Description = "Rechazado por la prueba." })
                : IdentityResult.Success;
    }

    private sealed class RejectAll : IUserValidator<ApplicationUser>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError { Description = "Rechazado por la prueba." }));
    }

    [Fact]
    public async Task AdminAccount_WhenTheRoleCannotBeAssigned_LeavesNoAccountBehind()
    {
        var error = Assert.ThrowsAny<Exception>(() => Start(
            settings: AdminSettings,
            services: s => s.AddScoped<IUserValidator<ApplicationUser>, RejectChanges>()));

        Assert.Contains("rol Admin", error.ToString());
        using var restarted = Start();
        Assert.Empty(await InScopeAsync(restarted, (db, _) => db.Users.ToListAsync()));
    }

    [Fact]
    public void DemoData_WhenAnAccountCannotBeCreated_StopsTheStartup()
    {
        var error = Assert.ThrowsAny<Exception>(() => Start(
            settings: new() { ["SeedData:Enabled"] = "true" },
            services: s => s.AddScoped<IUserValidator<ApplicationUser>, RejectAll>()));

        Assert.Contains("usuario demo", error.ToString());
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(dbPath) + "*"))
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }
}
