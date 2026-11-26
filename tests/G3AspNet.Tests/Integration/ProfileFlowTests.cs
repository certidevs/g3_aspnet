using System.Net;
using G3AspNet.Models;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Tests.Integration;

/// <summary>Perfil propio: edición, cambio de contraseña y 2FA.</summary>
public sealed class ProfileFlowTests : IClassFixture<AppFactory>
{
    private readonly AppFactory factory;

    public ProfileFlowTests(AppFactory factory) => this.factory = factory;

    private async Task<(HttpClient Client, string Username)> NewLoggedInUserAsync(string suffix)
    {
        var username = $"perfil-{suffix}";
        var admin = await factory.AsAdmin();
        await admin.PostForm("/users/create", new()
        {
            ["Username"] = username, ["Email"] = $"{username}@g3.local", ["DisplayName"] = "Perfil",
            ["Password"] = "Inicial123!", ["ConfirmPassword"] = "Inicial123!",
            ["Role"] = RoleNames.User, ["IsActive"] = "true"
        });
        var client = factory.CreateAnonymousClient();
        await client.PostFormAsync("/account/login", "/account/login", new() { ["Username"] = username, ["Password"] = "Inicial123!" });
        return (client, username);
    }

    [Theory]
    [InlineData("/profile")]
    [InlineData("/profile/edit")]
    [InlineData("/profile/changepassword")]
    [InlineData("/profile/twofactor")]
    public async Task ProfilePages_OpenForAuthenticatedUser(string url)
    {
        var response = await (await factory.AsCustomer()).GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Edit_UpdatesDisplayNameAndEmail()
    {
        var (client, username) = await NewLoggedInUserAsync("edit");

        var response = await client.PostForm("/profile/edit", new()
        {
            ["DisplayName"] = "Nombre Nuevo", ["Email"] = "nuevo-correo@g3.local"
        });

        response.AssertRedirectTo("/Profile");
        var user = await factory.WithDbAsync(ctx => ctx.Users.AsNoTracking().SingleAsync(u => u.UserName == username));
        Assert.Equal("Nombre Nuevo", user.DisplayName);
        Assert.Equal("nuevo-correo@g3.local", user.Email);
    }

    [Fact]
    public async Task Edit_WithInvalidEmail_ReturnsFormWithoutSaving()
    {
        var (client, username) = await NewLoggedInUserAsync("bademail");

        var response = await client.PostForm("/profile/edit", new() { ["DisplayName"] = "X", ["Email"] = "no-es-un-email" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await factory.WithDbAsync(ctx => ctx.Users.AsNoTracking().SingleAsync(u => u.UserName == username));
        Assert.Equal("Perfil", user.DisplayName);
    }

    [Fact]
    public async Task ChangePassword_NewPasswordWorks_OldOneDoesNot()
    {
        var (client, username) = await NewLoggedInUserAsync("pwd");

        var response = await client.PostForm("/profile/changepassword", new()
        {
            ["CurrentPassword"] = "Inicial123!", ["NewPassword"] = "Cambiada456!", ["ConfirmPassword"] = "Cambiada456!"
        });
        response.AssertRedirectTo("/Profile");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/profile")).StatusCode); // la sesión sigue viva

        var withNew = factory.CreateAnonymousClient();
        var okLogin = await withNew.PostFormAsync("/account/login", "/account/login", new() { ["Username"] = username, ["Password"] = "Cambiada456!" });
        Assert.Equal(HttpStatusCode.Redirect, okLogin.StatusCode);

        var withOld = factory.CreateAnonymousClient();
        var badLogin = await withOld.PostFormAsync("/account/login", "/account/login", new() { ["Username"] = username, ["Password"] = "Inicial123!" });
        Assert.Equal(HttpStatusCode.OK, badLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_IsRejected()
    {
        var (client, _) = await NewLoggedInUserAsync("wrongpwd");

        var response = await client.PostForm("/profile/changepassword", new()
        {
            ["CurrentPassword"] = "Incorrecta1!", ["NewPassword"] = "Cambiada456!", ["ConfirmPassword"] = "Cambiada456!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithMismatchedConfirmation_IsRejected()
    {
        var (client, _) = await NewLoggedInUserAsync("mismatchpwd");

        var response = await client.PostForm("/profile/changepassword", new()
        {
            ["CurrentPassword"] = "Inicial123!", ["NewPassword"] = "Cambiada456!", ["ConfirmPassword"] = "Otra456!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
