using System.Net;
using G3AspNet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace G3AspNet.Tests.Integration;

/// <summary>Administración de usuarios (solo Admin).</summary>
public sealed class UserAdminFlowTests : IClassFixture<AppFactory>
{
    private readonly AppFactory factory;

    public UserAdminFlowTests(AppFactory factory) => this.factory = factory;

    private static Dictionary<string, string> NewUserForm(string username, string role = RoleNames.Customer) => new()
    {
        ["Username"] = username,
        ["Email"] = $"{username}@g3.local",
        ["DisplayName"] = $"Usuario {username}",
        ["Password"] = "Secreta123!",
        ["ConfirmPassword"] = "Secreta123!",
        ["Role"] = role,
        ["IsActive"] = "true"
    };

    [Theory]
    [InlineData("/users")]
    [InlineData("/users/create")]
    public async Task NonAdmins_CannotOpenUserAdministration(string url)
    {
        (await (await factory.AsMechanic()).GetAsync(url)).AssertForbidden();
        (await (await factory.AsCustomer()).GetAsync(url)).AssertForbidden();
        (await (await factory.AsUser()).GetAsync(url)).AssertForbidden();
    }

    [Fact]
    public async Task NonAdmin_CannotCreateUsers()
    {
        var response = await (await factory.AsMechanic()).PostForm("/users/create", NewUserForm("colado1"));

        response.AssertForbidden();
        Assert.False(await factory.WithDbAsync(ctx => ctx.Users.AnyAsync(u => u.UserName == "colado1")));
    }

    [Fact]
    public async Task Admin_ListsAndSearchesUsers()
    {
        var admin = await factory.AsAdmin();

        var all = await (await admin.GetAsync("/users")).HtmlAsync();
        var search = await (await admin.GetAsync("/users?search=mechanic3")).HtmlAsync();

        Assert.Contains("customer@g3.local", all);
        Assert.Contains("mechanic3@g3.local", search);
        Assert.DoesNotContain("customer@g3.local", search);
    }

    [Fact]
    public async Task Admin_CreatesUser_WhoCanThenLogIn()
    {
        var admin = await factory.AsAdmin();

        (await admin.PostForm("/users/create", NewUserForm("creado1", RoleNames.Mechanic))).AssertRedirectTo("/Users");

        var login = factory.CreateAnonymousClient();
        var response = await login.PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = "creado1", ["Password"] = "Secreta123!"
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await login.GetAsync("/repairs/create")).StatusCode); // rol Mechanic
    }

    [Fact]
    public async Task Create_RejectsPasswordMismatchAndDuplicates()
    {
        var admin = await factory.AsAdmin();
        var form = NewUserForm("mismatch2");
        form["ConfirmPassword"] = "Distinta123!";
        Assert.Equal(HttpStatusCode.OK, (await admin.PostForm("/users/create", form)).StatusCode);
        Assert.False(await factory.WithDbAsync(ctx => ctx.Users.AnyAsync(u => u.UserName == "mismatch2")));

        var duplicated = NewUserForm("customer"); // ya existe
        Assert.Equal(HttpStatusCode.OK, (await admin.PostForm("/users/create", duplicated)).StatusCode);
    }

    [Fact]
    public async Task Admin_EditsUser_ChangesRoleAndDeactivates()
    {
        var admin = await factory.AsAdmin();
        await admin.PostForm("/users/create", NewUserForm("editable1"));
        var id = await factory.UserIdAsync("editable1");

        var response = await admin.PostForm($"/users/edit/{id}", new()
        {
            ["Id"] = id,
            ["DisplayName"] = "Editado",
            ["Email"] = "editable1@g3.local",
            ["Role"] = RoleNames.Mechanic,
            ["IsActive"] = "false"
        });

        response.AssertRedirectTo("/Users/Details/");
        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await manager.FindByIdAsync(id))!;
        Assert.Equal("Editado", user.DisplayName);
        Assert.False(user.IsActive);
        Assert.True(await manager.IsInRoleAsync(user, RoleNames.Mechanic));
        Assert.False(await manager.IsInRoleAsync(user, RoleNames.Customer));
    }

    [Fact]
    public async Task DeactivatedUser_LosesAccess()
    {
        var admin = await factory.AsAdmin();
        await admin.PostForm("/users/create", NewUserForm("desactivar1"));
        var login = factory.CreateAnonymousClient();
        await login.PostFormAsync("/account/login", "/account/login", new() { ["Username"] = "desactivar1", ["Password"] = "Secreta123!" });
        Assert.Equal(HttpStatusCode.OK, (await login.GetAsync("/vehicle")).StatusCode);
        var id = await factory.UserIdAsync("desactivar1");

        await admin.PostForm($"/users/edit/{id}", new()
        {
            ["Id"] = id, ["DisplayName"] = "x", ["Email"] = "desactivar1@g3.local",
            ["Role"] = RoleNames.Customer, ["IsActive"] = "false"
        });

        (await login.GetAsync("/vehicle")).AssertRedirectTo("/account/login");
    }

    [Fact]
    public async Task Admin_DeletesUser()
    {
        var admin = await factory.AsAdmin();
        await admin.PostForm("/users/create", NewUserForm("borrar1"));
        var id = await factory.UserIdAsync("borrar1");

        (await admin.PostForm($"/users/delete/{id}")).AssertRedirectTo("/Users");

        Assert.False(await factory.WithDbAsync(ctx => ctx.Users.AnyAsync(u => u.Id == id)));
    }

    [Fact]
    public async Task Admin_CannotDeleteOwnAccount()
    {
        var admin = await factory.AsAdmin();
        var id = await factory.UserIdAsync("admin");

        var response = await admin.PostForm($"/users/delete/{id}");

        response.AssertRedirectTo("/Users/Details/");
        Assert.True(await factory.WithDbAsync(ctx => ctx.Users.AnyAsync(u => u.Id == id)));
    }

    [Fact]
    public async Task Details_UnknownUser_Returns404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await (await factory.AsAdmin()).GetAsync("/users/details/no-existe")).StatusCode);
    }
}
