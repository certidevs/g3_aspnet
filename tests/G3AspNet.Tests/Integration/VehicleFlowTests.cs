using System.Net;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Tests.Integration;

/// <summary>Flujo de vehículos: visibilidad por rol, alta, edición y baja.</summary>
public sealed class VehicleFlowTests : IClassFixture<AppFactory>
{
    private readonly AppFactory factory;

    public VehicleFlowTests(AppFactory factory) => this.factory = factory;

    private async Task<Dictionary<string, string>> VehicleFormAsync(string ownerUsername, string plate, int id = 0) => new()
    {
        ["Id"] = id.ToString(),
        ["LicensePlate"] = plate,
        ["VIN"] = Guid.NewGuid().ToString("N")[..17].ToUpperInvariant(),
        ["Brand"] = "Renault",
        ["Model"] = "Clio",
        ["Year"] = "2019",
        ["FuelType"] = nameof(FuelType.Diesel),
        ["Mileage"] = "12000",
        ["OwnerId"] = await factory.UserIdAsync(ownerUsername)
    };

    [Fact]
    public async Task Customer_OnlySeesOwnVehicles()
    {
        var mine = await factory.CreateVehicleAsync("customer", "MINE-001");
        var other = await factory.CreateVehicleAsync("customer2", "OTHR-001");
        var client = await factory.AsCustomer();

        var html = await (await client.GetAsync("/vehicle")).HtmlAsync();

        Assert.Contains(mine.LicensePlate, html);
        Assert.DoesNotContain(other.LicensePlate, html);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("mechanic")]
    public async Task StaffSeesAllVehicles(string username)
    {
        var a = await factory.CreateVehicleAsync("customer");
        var b = await factory.CreateVehicleAsync("customer2");
        var client = await factory.LoginAsync(username, username == "admin" ? AppFactory.AdminPassword : AppFactory.MechanicPassword);

        var html = await (await client.GetAsync("/vehicle")).HtmlAsync();

        Assert.Contains(a.LicensePlate, html);
        Assert.Contains(b.LicensePlate, html);
    }

    [Fact]
    public async Task Details_OwnerCanSee_OtherCustomerCannot()
    {
        var vehicle = await factory.CreateVehicleAsync("customer");

        var owner = await (await factory.AsCustomer()).GetAsync($"/vehicle/details/{vehicle.Id}");
        var stranger = await (await factory.AsCustomer2()).GetAsync($"/vehicle/details/{vehicle.Id}");

        Assert.Equal(HttpStatusCode.OK, owner.StatusCode);
        stranger.AssertForbidden();
    }

    [Fact]
    public async Task Details_UnknownVehicle_Returns404()
    {
        var response = await (await factory.AsAdmin()).GetAsync("/vehicle/details/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/vehicle/create")]
    [InlineData("/vehicle/edit/1")]
    [InlineData("/vehicle/confirmdelete/1")]
    public async Task Customer_CannotOpenManagementPages(string url)
    {
        var response = await (await factory.AsCustomer()).GetAsync(url);

        response.AssertForbidden();
    }

    [Fact]
    public async Task Customer_CannotCreateVehicle()
    {
        var client = await factory.AsCustomer();

        var response = await client.PostForm("/vehicle/save", await VehicleFormAsync("customer", "HACK-001"));

        response.AssertForbidden();
        Assert.False(await factory.WithDbAsync(ctx => ctx.Vehicles.AnyAsync(v => v.LicensePlate == "HACK-001")));
    }

    [Fact]
    public async Task Admin_CreatesEditsAndDeletesVehicle()
    {
        var client = await factory.AsAdmin();

        // Alta
        var create = await client.PostForm("/vehicle/save", await VehicleFormAsync("customer", "FLOW-001"));
        create.AssertRedirectTo("/Vehicle/Details/");
        var created = await factory.WithDbAsync(ctx => ctx.Vehicles.SingleAsync(v => v.LicensePlate == "FLOW-001"));
        Assert.Equal("Renault", created.Brand);

        // Edición
        var form = await VehicleFormAsync("customer", "FLOW-002", created.Id);
        form["Mileage"] = "34567";
        (await client.PostForm("/vehicle/save", form)).AssertRedirectTo("/Vehicle/Details/");
        var edited = await factory.WithDbAsync(ctx => ctx.Vehicles.AsNoTracking().SingleAsync(v => v.Id == created.Id));
        Assert.Equal("FLOW-002", edited.LicensePlate);
        Assert.Equal(34567, edited.Mileage);

        // Baja
        (await client.PostForm($"/vehicle/delete/{created.Id}")).AssertRedirectTo("/Vehicle");
        Assert.False(await factory.WithDbAsync(ctx => ctx.Vehicles.AnyAsync(v => v.Id == created.Id)));
    }

    [Fact]
    public async Task CreatedVehicle_AppearsInListAndDetails_ForStaffAndOwner()
    {
        var admin = await factory.AsAdmin();
        var create = await admin.PostForm("/vehicle/save", await VehicleFormAsync("customer", "VISIBLE-1"));
        create.AssertRedirectTo("/Vehicle/Details/");

        // Tras el alta se redirige al detalle, que muestra el vehículo.
        var details = await admin.GetAsync(create.Location());
        Assert.Equal(HttpStatusCode.OK, details.StatusCode);
        Assert.Contains("VISIBLE-1", await details.HtmlAsync());

        // Aparece en el listado del admin y en el de su propietario, pero no en el de otro cliente.
        Assert.Contains("VISIBLE-1", await (await admin.GetAsync("/vehicle")).HtmlAsync());
        Assert.Contains("VISIBLE-1", await (await (await factory.AsCustomer()).GetAsync("/vehicle")).HtmlAsync());
        Assert.DoesNotContain("VISIBLE-1", await (await (await factory.AsCustomer2()).GetAsync("/vehicle")).HtmlAsync());
    }

    [Fact]
    public async Task Mechanic_CanCreateVehicle()
    {
        var client = await factory.AsMechanic();

        var response = await client.PostForm("/vehicle/save", await VehicleFormAsync("customer", "MECH-001"));

        response.AssertRedirectTo("/Vehicle/Details/");
    }

    [Fact]
    public async Task Save_WithInvalidData_ReturnsFormWithoutSaving()
    {
        var client = await factory.AsAdmin();
        var form = await VehicleFormAsync("customer", "BAD-0001");
        form["Year"] = "1800";
        form["Brand"] = "";

        var response = await client.PostForm("/vehicle/save", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await factory.WithDbAsync(ctx => ctx.Vehicles.AnyAsync(v => v.LicensePlate == "BAD-0001")));
    }

    [Fact]
    public async Task Save_WithUnknownOwner_IsRejected()
    {
        var client = await factory.AsAdmin();
        var form = await VehicleFormAsync("customer", "NOOWN-01");
        form["OwnerId"] = "no-existe";

        var response = await client.PostForm("/vehicle/save", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await factory.WithDbAsync(ctx => ctx.Vehicles.AnyAsync(v => v.LicensePlate == "NOOWN-01")));
    }

    [Fact]
    public async Task Delete_VehicleWithRepairs_IsBlocked()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var client = await factory.AsAdmin();

        var response = await client.PostForm($"/vehicle/delete/{repair.VehicleId}");

        response.AssertRedirectTo("/Vehicle/Details/");
        Assert.True(await factory.WithDbAsync(ctx => ctx.Vehicles.AnyAsync(v => v.Id == repair.VehicleId)));
    }
}
