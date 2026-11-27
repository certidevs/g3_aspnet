using System.Net;
using G3AspNet.Data;
using G3AspNet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace G3AspNet.Tests.Integration;

public sealed class UserDeletionFactory : AppFactory
{
    public BeforeSaveInterceptor Interceptor { get; } = new();

    protected override void ConfigureExtraServices(IServiceCollection services) =>
        services.ConfigureDbContext<ApplicationDbContext>(options => options.AddInterceptors(Interceptor));
}

/// <summary>
/// E1 (docs/planning.md): una cuenta con vehículos o reparaciones no se elimina, y se explica
/// por qué en lugar de responder con un error 500.
/// </summary>
public sealed class UserDeletionTests : IClassFixture<UserDeletionFactory>
{
    private readonly UserDeletionFactory factory;

    public UserDeletionTests(UserDeletionFactory factory) => this.factory = factory;

    private async Task AssertBlockedAsync(ApplicationUser user, HttpResponseMessage response)
    {
        Assert.True((int)response.StatusCode < 500, $"Respuesta {(int)response.StatusCode}");
        Assert.Contains("desactivar", await (await factory.AsAdmin()).FollowErrorAsync(response));
        Assert.NotNull(await factory.FindUserAsync(user.Id));
    }

    [Fact]
    public async Task OwnerWithVehicles_IsNotDeleted()
    {
        var user = await factory.CreateAccountAsync(RoleNames.Customer);
        await factory.CreateVehicleAsync(user.UserName!);

        var response = await (await factory.AsAdmin()).PostForm($"/users/delete/{user.Id}");

        await AssertBlockedAsync(user, response);
    }

    [Fact]
    public async Task MechanicWithRepairs_IsNotDeleted()
    {
        var user = await factory.CreateAccountAsync(RoleNames.Mechanic);
        await factory.CreateRepairAsync("customer", user.UserName!);

        var response = await (await factory.AsAdmin()).PostForm($"/users/delete/{user.Id}");

        await AssertBlockedAsync(user, response);
    }

    [Fact]
    public async Task AccountWithoutData_IsDeleted()
    {
        var user = await factory.CreateAccountAsync(RoleNames.Customer);

        var response = await (await factory.AsAdmin()).PostForm($"/users/delete/{user.Id}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Null(await factory.FindUserAsync(user.Id));
    }

    [Fact]
    public async Task VehicleAssignedBetweenTheCheckAndTheDelete_ShowsTheWarning()
    {
        var user = await factory.CreateAccountAsync(RoleNames.Customer);
        var admin = await factory.AsAdmin();
        var token = await admin.GetAntiforgeryTokenAsync("/profile");
        // Se asigna el vehículo cuando la comprobación ya ha pasado, justo antes de borrar.
        factory.Interceptor.OnNextSave(() => factory.CreateVehicleAsync(user.UserName!));

        var response = await admin.PostAsync($"/users/delete/{user.Id}", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        await AssertBlockedAsync(user, response);
    }
}
