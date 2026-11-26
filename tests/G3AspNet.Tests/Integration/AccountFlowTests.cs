using System.Net;
using G3AspNet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace G3AspNet.Tests.Integration;

/// <summary>Flujo de acceso: login, logout, registro y protección de rutas.</summary>
public sealed class AccountFlowTests : IClassFixture<AppFactory>
{
    private readonly AppFactory factory;

    public AccountFlowTests(AppFactory factory) => this.factory = factory;

    [Fact]
    public async Task HomePage_IsPublic()
    {
        var response = await factory.CreateAnonymousClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/vehicle")]
    [InlineData("/repairs")]
    [InlineData("/spareparts")]
    [InlineData("/profile")]
    [InlineData("/users")]
    public async Task ProtectedPages_RedirectAnonymousToLogin(string url)
    {
        var response = await factory.CreateAnonymousClient().GetAsync(url);

        response.AssertRedirectTo("/account/login");
    }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsHomeAndOpensSession()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = "customer",
            ["Password"] = AppFactory.CustomerPassword
        });

        response.AssertRedirectTo("/");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/vehicle")).StatusCode);
    }

    [Fact]
    public async Task Login_AcceptsEmailInsteadOfUsername()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = "customer@g3.local",
            ["Password"] = AppFactory.CustomerPassword
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsErrorAndNoSession()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = "customer",
            ["Password"] = "mala-contraseña1!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no son válidos", await response.HtmlAsync());
        (await client.GetAsync("/vehicle")).AssertRedirectTo("/account/login");
    }

    [Fact]
    public async Task Login_WithUnknownUser_ShowsGenericError()
    {
        var response = await factory.CreateAnonymousClient().PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = "no-existe",
            ["Password"] = "Whatever123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no son válidos", await response.HtmlAsync());
    }

    [Fact]
    public async Task Login_WithMissingFields_ReturnsValidationErrors()
    {
        var response = await factory.CreateAnonymousClient().PostFormAsync("/account/login", "/account/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("obligatori", await response.HtmlAsync());
    }

    [Fact]
    public async Task Login_HonoursLocalReturnUrl_ButIgnoresExternalOne()
    {
        var local = await factory.CreateAnonymousClient().PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = "customer", ["Password"] = AppFactory.CustomerPassword, ["ReturnUrl"] = "/repairs"
        });
        local.AssertRedirectTo("/repairs");

        var external = await factory.CreateAnonymousClient().PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = "customer", ["Password"] = AppFactory.CustomerPassword, ["ReturnUrl"] = "https://evil.example/"
        });
        Assert.DoesNotContain("evil.example", external.Location());
    }

    [Fact]
    public async Task Login_InactiveUser_IsRejected()
    {
        await CreateUserAsync("inactivo1", "Inactivo123!", RoleNames.User, isActive: false);

        var response = await factory.CreateAnonymousClient().PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = "inactivo1", ["Password"] = "Inactivo123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no son válidos", await response.HtmlAsync());
    }

    [Fact]
    public async Task Login_LocksAccountAfterFiveFailures()
    {
        await CreateUserAsync("bloqueable1", "Bloqueo123!", RoleNames.User);
        var client = factory.CreateAnonymousClient();

        for (var i = 0; i < 5; i++)
        {
            await client.PostFormAsync("/account/login", "/account/login", new()
            {
                ["Username"] = "bloqueable1", ["Password"] = "incorrecta1!"
            });
        }

        // Con la contraseña correcta ya no entra: la cuenta está bloqueada.
        var response = await client.PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = "bloqueable1", ["Password"] = "Bloqueo123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("bloqueado", await response.HtmlAsync());
    }

    [Fact]
    public async Task Login_IsRateLimitedPerIp()
    {
        var client = factory.CreateAnonymousClient("203.0.113.9");
        HttpStatusCode last = default;

        for (var i = 0; i < 12; i++)
        {
            var response = await client.PostFormAsync("/account/login", "/account/login", new()
            {
                ["Username"] = "no-existe", ["Password"] = "Whatever123!"
            });
            last = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last);
    }

    [Fact]
    public async Task Logout_ClosesSession()
    {
        var client = factory.CreateAnonymousClient();
        await client.PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = "user", ["Password"] = AppFactory.UserPassword
        });

        var response = await client.PostForm("/account/logout");

        response.AssertRedirectTo("/account/login");
        (await client.GetAsync("/vehicle")).AssertRedirectTo("/account/login");
    }

    [Fact]
    public async Task Post_WithoutAntiforgeryToken_IsRejected()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "customer", ["Password"] = AppFactory.CustomerPassword
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_CreatesUserAccountAndSignsIn()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PostFormAsync("/account/register", "/account/register", new()
        {
            ["Username"] = "nuevo1",
            ["Email"] = "nuevo1@g3.local",
            ["DisplayName"] = "Nuevo Uno",
            ["Rol"] = RoleNames.User,
            ["Password"] = "Nuevo1234!",
            ["ConfirmPassword"] = "Nuevo1234!"
        });

        response.AssertRedirectTo("/");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/vehicle")).StatusCode);
        var roles = await factory.WithDbAsync(async ctx =>
        {
            var user = await ctx.Users.SingleAsync(u => u.UserName == "nuevo1");
            return await ctx.UserRoles.Where(r => r.UserId == user.Id)
                .Join(ctx.Roles, r => r.RoleId, r => r.Id, (_, role) => role.Name).ToListAsync();
        });
        Assert.Equal([RoleNames.User], roles);
    }

    [Theory]
    [InlineData(RoleNames.Admin)]
    [InlineData(RoleNames.Mechanic)]
    public async Task Register_CannotEscalateToPrivilegedRole(string role)
    {
        var response = await factory.CreateAnonymousClient().PostFormAsync("/account/register", "/account/register", new()
        {
            ["Username"] = $"hacker-{role}",
            ["Email"] = $"hacker-{role}@g3.local",
            ["Rol"] = role,
            ["Password"] = "Hacker1234!",
            ["ConfirmPassword"] = "Hacker1234!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("rol seleccionado no es válido", await response.HtmlAsync());
        Assert.False(await factory.WithDbAsync(ctx => ctx.Users.AnyAsync(u => u.UserName == $"hacker-{role}")));
    }

    [Fact]
    public async Task Register_RejectsMismatchedAndWeakPasswords()
    {
        var mismatch = await factory.CreateAnonymousClient().PostFormAsync("/account/register", "/account/register", new()
        {
            ["Username"] = "mismatch1", ["Email"] = "m1@g3.local", ["Rol"] = RoleNames.User,
            ["Password"] = "Nuevo1234!", ["ConfirmPassword"] = "Otra1234!"
        });
        Assert.Contains("no coinciden", await mismatch.HtmlAsync());

        var weak = await factory.CreateAnonymousClient().PostFormAsync("/account/register", "/account/register", new()
        {
            ["Username"] = "weak1", ["Email"] = "w1@g3.local", ["Rol"] = RoleNames.User,
            ["Password"] = "sinnumeros!", ["ConfirmPassword"] = "sinnumeros!"
        });
        Assert.Equal(HttpStatusCode.OK, weak.StatusCode);
        Assert.False(await factory.WithDbAsync(ctx => ctx.Users.AnyAsync(u => u.UserName == "weak1")));
    }

    [Fact]
    public async Task Register_RejectsDuplicateEmail()
    {
        var response = await factory.CreateAnonymousClient().PostFormAsync("/account/register", "/account/register", new()
        {
            ["Username"] = "duplicado1", ["Email"] = "customer@g3.local", ["Rol"] = RoleNames.User,
            ["Password"] = "Nuevo1234!", ["ConfirmPassword"] = "Nuevo1234!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await factory.WithDbAsync(ctx => ctx.Users.AnyAsync(u => u.UserName == "duplicado1")));
    }

    [Fact]
    public async Task AccessDeniedPage_IsPublic()
    {
        var response = await factory.CreateAnonymousClient().GetAsync("/account/accessdenied");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task CreateUserAsync(string username, string password, string role, bool isActive = true)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = username, Email = $"{username}@g3.local", DisplayName = username,
            EmailConfirmed = true, IsActive = isActive
        };
        Assert.True((await userManager.CreateAsync(user, password)).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, role)).Succeeded);
    }
}
