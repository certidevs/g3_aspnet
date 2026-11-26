using System.Net;
using G3AspNet.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Tests.Integration;

/// <summary>Flujo completo de reparaciones: alta, máquina de estados, piezas, permisos y baja.</summary>
public sealed class RepairWorkflowTests : IClassFixture<AppFactory>
{
    private readonly AppFactory factory;

    public RepairWorkflowTests(AppFactory factory) => this.factory = factory;

    private async Task<Dictionary<string, string>> RepairFormAsync(int vehicleId, string mechanicUsername, int id = 0) => new()
    {
        ["Id"] = id.ToString(),
        ["VehicleId"] = vehicleId.ToString(),
        ["MechanicId"] = await factory.UserIdAsync(mechanicUsername),
        ["RepairType"] = nameof(RepairType.Tires),
        ["Description"] = "  Cambio de neumáticos  ",
        ["OpenedAt"] = "2026-01-15T10:00",
        ["MileageAtEntry"] = "45000",
        ["LaborHours"] = "2",
        ["HourlyRate"] = "60"
    };

    // ---------- Consulta ----------

    [Fact]
    public async Task Index_ShowsRepairsAccordingToRole()
    {
        var mine = await factory.CreateRepairAsync("customer", "mechanic");
        var otherCustomer = await factory.CreateRepairAsync("customer2", "mechanic2");

        var customerHtml = await (await (await factory.AsCustomer()).GetAsync("/repairs")).HtmlAsync();
        var mechanicHtml = await (await (await factory.AsMechanic()).GetAsync("/repairs")).HtmlAsync();
        var adminHtml = await (await (await factory.AsAdmin()).GetAsync("/repairs")).HtmlAsync();

        Assert.Contains($"/Repairs/Details/{mine.Id}", customerHtml);
        Assert.DoesNotContain($"/Repairs/Details/{otherCustomer.Id}\"", customerHtml);
        Assert.Contains($"/Repairs/Details/{mine.Id}", mechanicHtml);
        Assert.DoesNotContain($"/Repairs/Details/{otherCustomer.Id}\"", mechanicHtml);
        Assert.Contains($"/Repairs/Details/{mine.Id}", adminHtml);
        Assert.Contains($"/Repairs/Details/{otherCustomer.Id}", adminHtml);
    }

    [Theory]
    [InlineData("customer", true)]
    [InlineData("customer2", false)]
    [InlineData("mechanic", true)]
    [InlineData("mechanic2", false)]
    [InlineData("admin", true)]
    public async Task Details_RespectsOwnershipAndAssignment(string username, bool allowed)
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var password = username.StartsWith("admin") ? AppFactory.AdminPassword
            : username.StartsWith("mechanic") ? AppFactory.MechanicPassword
            : AppFactory.CustomerPassword;
        var client = await factory.LoginAsync(username, password);

        var response = await client.GetAsync($"/repairs/details/{repair.Id}");

        if (allowed)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        else
        {
            response.AssertForbidden();
        }
    }

    [Fact]
    public async Task Details_UnknownRepair_Returns404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await (await factory.AsAdmin()).GetAsync("/repairs/details/999999")).StatusCode);
    }

    // ---------- Alta y edición ----------

    [Fact]
    public async Task Customer_CannotCreateRepair()
    {
        var vehicle = await factory.CreateVehicleAsync("customer");
        var client = await factory.AsCustomer();

        (await client.GetAsync("/repairs/create")).AssertForbidden();
        (await client.PostForm("/repairs/create", await RepairFormAsync(vehicle.Id, "mechanic"))).AssertForbidden();
    }

    [Fact]
    public async Task Admin_CreatesRepair_StartsPendingAndKeepsChosenMechanicAndRate()
    {
        var vehicle = await factory.CreateVehicleAsync("customer");
        var client = await factory.AsAdmin();

        var response = await client.PostForm("/repairs/create", await RepairFormAsync(vehicle.Id, "mechanic2"));

        response.AssertRedirectTo("/Repairs/Details/");
        var repair = await factory.WithDbAsync(ctx => ctx.Repairs.SingleAsync(r => r.VehicleId == vehicle.Id));
        Assert.Equal(RepairStatus.Pending, repair.Status);
        Assert.Equal(await factory.UserIdAsync("mechanic2"), repair.MechanicId);
        Assert.Equal(60m, repair.HourlyRate);
        Assert.Equal("Cambio de neumáticos", repair.Description);
        Assert.Null(repair.CompletedAt);
    }

    [Fact]
    public async Task Mechanic_CreatesRepair_ForcedToSelfAndStandardRate()
    {
        var vehicle = await factory.CreateVehicleAsync("customer");
        var client = await factory.AsMechanic();
        var form = await RepairFormAsync(vehicle.Id, "mechanic2"); // intenta asignarla a otro y cobrar 60 €/h

        (await client.PostForm("/repairs/create", form)).AssertRedirectTo("/Repairs/Details/");

        var repair = await factory.WithDbAsync(ctx => ctx.Repairs.SingleAsync(r => r.VehicleId == vehicle.Id));
        Assert.Equal(await factory.UserIdAsync("mechanic"), repair.MechanicId);
        Assert.Equal(45m, repair.HourlyRate);
    }

    [Fact]
    public async Task Create_WithUnknownVehicle_ReturnsFormWithError()
    {
        var response = await (await factory.AsAdmin()).PostForm("/repairs/create", await RepairFormAsync(999999, "mechanic"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_AdminWithoutMechanic_IsRejected()
    {
        var vehicle = await factory.CreateVehicleAsync("customer");
        var form = await RepairFormAsync(vehicle.Id, "mechanic");
        form["MechanicId"] = "";

        var response = await (await factory.AsAdmin()).PostForm("/repairs/create", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await factory.WithDbAsync(ctx => ctx.Repairs.AnyAsync(r => r.VehicleId == vehicle.Id)));
    }

    [Fact]
    public async Task Create_WithoutDescription_IsRejected()
    {
        var vehicle = await factory.CreateVehicleAsync("customer");
        var form = await RepairFormAsync(vehicle.Id, "mechanic");
        form["Description"] = "";

        var response = await (await factory.AsAdmin()).PostForm("/repairs/create", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await factory.WithDbAsync(ctx => ctx.Repairs.AnyAsync(r => r.VehicleId == vehicle.Id)));
    }

    [Fact]
    public async Task Admin_EditsRepair_IncludingMechanicAndRate()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var form = await RepairFormAsync(repair.VehicleId, "mechanic2", repair.Id);

        (await (await factory.AsAdmin()).PostForm($"/repairs/edit/{repair.Id}", form)).AssertRedirectTo("/Repairs/Details/");

        var edited = await factory.GetRepairAsync(repair.Id);
        Assert.Equal(await factory.UserIdAsync("mechanic2"), edited.MechanicId);
        Assert.Equal(60m, edited.HourlyRate);
        Assert.Equal(2m, edited.LaborHours);
        Assert.Equal(RepairType.Tires, edited.RepairType);
    }

    [Fact]
    public async Task Mechanic_EditsOwnRepair_ButCannotChangeMechanicOrRate()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var form = await RepairFormAsync(repair.VehicleId, "mechanic2", repair.Id);

        (await (await factory.AsMechanic()).PostForm($"/repairs/edit/{repair.Id}", form)).AssertRedirectTo("/Repairs/Details/");

        var edited = await factory.GetRepairAsync(repair.Id);
        Assert.Equal(await factory.UserIdAsync("mechanic"), edited.MechanicId);
        Assert.Equal(45m, edited.HourlyRate);
        Assert.Equal(2m, edited.LaborHours);
    }

    [Fact]
    public async Task Mechanic_CannotEditRepairAssignedToAnother()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic2");
        var form = await RepairFormAsync(repair.VehicleId, "mechanic", repair.Id);

        var response = await (await factory.AsMechanic()).PostForm($"/repairs/edit/{repair.Id}", form);

        response.AssertForbidden();
        Assert.Equal(0m, (await factory.GetRepairAsync(repair.Id)).LaborHours);
    }

    [Theory]
    [InlineData(RepairStatus.Completed)]
    [InlineData(RepairStatus.Delivered)]
    public async Task Mechanic_CannotEditClosedRepair(RepairStatus status)
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", status);

        var response = await (await factory.AsMechanic()).GetAsync($"/repairs/edit/{repair.Id}");

        response.AssertForbidden();
    }

    [Fact]
    public async Task Edit_WithNonexistentId_ReturnsNotFound()
    {
        // El formulario envía "Id" y el value provider de formulario gana a la ruta,
        // así que un Id inexistente en el cuerpo acaba en 404 (el guard BadRequest no se alcanza por esta vía).
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var form = await RepairFormAsync(repair.VehicleId, "mechanic", repair.Id + 1000);

        var response = await (await factory.AsAdmin()).PostForm($"/repairs/edit/{repair.Id}", form);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Máquina de estados ----------

    [Fact]
    public async Task FullLifecycle_Pending_InProgress_WaitingParts_InProgress_Completed_Delivered()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", laborHours: 2m);
        var mechanic = await factory.AsMechanic();
        var admin = await factory.AsAdmin();

        (await mechanic.PostForm($"/repairs/start/{repair.Id}")).AssertRedirectTo($"/Repairs/Details/{repair.Id}");
        Assert.Equal(RepairStatus.InProgress, (await factory.GetRepairAsync(repair.Id)).Status);

        await mechanic.PostForm($"/repairs/waitforparts/{repair.Id}");
        Assert.Equal(RepairStatus.WaitingParts, (await factory.GetRepairAsync(repair.Id)).Status);

        await mechanic.PostForm($"/repairs/start/{repair.Id}"); // retoma
        Assert.Equal(RepairStatus.InProgress, (await factory.GetRepairAsync(repair.Id)).Status);

        await mechanic.PostForm($"/repairs/complete/{repair.Id}");
        var completed = await factory.GetRepairAsync(repair.Id);
        Assert.Equal(RepairStatus.Completed, completed.Status);
        Assert.NotNull(completed.CompletedAt);

        await admin.PostForm($"/repairs/deliver/{repair.Id}");
        Assert.Equal(RepairStatus.Delivered, (await factory.GetRepairAsync(repair.Id)).Status);
    }

    [Theory]
    [InlineData("start", RepairStatus.InProgress)]
    [InlineData("start", RepairStatus.Completed)]
    [InlineData("start", RepairStatus.Delivered)]
    [InlineData("waitforparts", RepairStatus.Pending)]
    [InlineData("waitforparts", RepairStatus.WaitingParts)]
    [InlineData("complete", RepairStatus.Pending)]
    [InlineData("complete", RepairStatus.WaitingParts)]
    [InlineData("deliver", RepairStatus.Pending)]
    [InlineData("deliver", RepairStatus.InProgress)]
    public async Task InvalidTransitions_AreRejectedAndStatusIsKept(string action, RepairStatus current)
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", current, laborHours: 2m);
        var client = action == "deliver" ? await factory.AsAdmin() : await factory.AsAdmin(); // admin puede ejecutar todas

        var response = await client.PostForm($"/repairs/{action}/{repair.Id}");

        // Las transiciones de mecánico sobre estados cerrados devuelven Forbid; las demás redirigen con error.
        Assert.True(response.StatusCode == HttpStatusCode.Redirect);
        Assert.Equal(current, (await factory.GetRepairAsync(repair.Id)).Status);
    }

    [Fact]
    public async Task Complete_WithoutLaborHours_IsRejected()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress, laborHours: 0m);

        await (await factory.AsMechanic()).PostForm($"/repairs/complete/{repair.Id}");

        var after = await factory.GetRepairAsync(repair.Id);
        Assert.Equal(RepairStatus.InProgress, after.Status);
        Assert.Null(after.CompletedAt);
    }

    [Fact]
    public async Task Mechanic_CannotDeliver_OnlyAdminCan()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.Completed, laborHours: 1m);

        (await (await factory.AsMechanic()).PostForm($"/repairs/deliver/{repair.Id}")).AssertForbidden();
        Assert.Equal(RepairStatus.Completed, (await factory.GetRepairAsync(repair.Id)).Status);
    }

    [Fact]
    public async Task Mechanic_CannotStartRepairOfAnotherMechanic()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic2");

        (await (await factory.AsMechanic()).PostForm($"/repairs/start/{repair.Id}")).AssertForbidden();
        Assert.Equal(RepairStatus.Pending, (await factory.GetRepairAsync(repair.Id)).Status);
    }

    [Fact]
    public async Task Customer_CannotChangeStatus()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic");

        (await (await factory.AsCustomer()).PostForm($"/repairs/start/{repair.Id}")).AssertForbidden();
        Assert.Equal(RepairStatus.Pending, (await factory.GetRepairAsync(repair.Id)).Status);
    }

    // ---------- Piezas y stock ----------

    [Fact]
    public async Task AddPart_DecrementsStock_AndRemovePart_RestoresIt()
    {
        var part = await factory.CreateSparePartAsync(stock: 10, price: 20m);
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress);
        var mechanic = await factory.AsMechanic();

        (await mechanic.PostForm($"/repairs/addpart/{repair.Id}", new()
        {
            ["sparePartId"] = part.Id.ToString(), ["quantity"] = "3"
        })).AssertRedirectTo($"/Repairs/Details/{repair.Id}");

        Assert.Equal(7, (await factory.GetSparePartAsync(part.Id)).Stock);
        var withPart = await factory.GetRepairAsync(repair.Id);
        var line = Assert.Single(withPart.RepairParts);
        Assert.Equal(3, line.Quantity);
        Assert.Equal(20m, line.UnitPrice);

        (await mechanic.PostForm($"/repairs/removepart/{repair.Id}", new() { ["repairPartId"] = line.Id.ToString() }))
            .AssertRedirectTo($"/Repairs/Details/{repair.Id}");

        Assert.Equal(10, (await factory.GetSparePartAsync(part.Id)).Stock);
        Assert.Empty((await factory.GetRepairAsync(repair.Id)).RepairParts);
    }

    [Fact]
    public async Task AddPart_BeyondStock_IsRejectedAndStockIsKept()
    {
        var part = await factory.CreateSparePartAsync(stock: 2);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");

        await (await factory.AsMechanic()).PostForm($"/repairs/addpart/{repair.Id}", new()
        {
            ["sparePartId"] = part.Id.ToString(), ["quantity"] = "5"
        });

        Assert.Equal(2, (await factory.GetSparePartAsync(part.Id)).Stock);
        Assert.Empty((await factory.GetRepairAsync(repair.Id)).RepairParts);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task AddPart_WithNonPositiveQuantity_IsRejected(string quantity)
    {
        var part = await factory.CreateSparePartAsync(stock: 5);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");

        await (await factory.AsMechanic()).PostForm($"/repairs/addpart/{repair.Id}", new()
        {
            ["sparePartId"] = part.Id.ToString(), ["quantity"] = quantity
        });

        Assert.Equal(5, (await factory.GetSparePartAsync(part.Id)).Stock);
        Assert.Empty((await factory.GetRepairAsync(repair.Id)).RepairParts);
    }

    [Fact]
    public async Task AddPart_UnknownSparePart_Returns404()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic");

        var response = await (await factory.AsMechanic()).PostForm($"/repairs/addpart/{repair.Id}", new()
        {
            ["sparePartId"] = "999999", ["quantity"] = "1"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(RepairStatus.Completed)]
    [InlineData(RepairStatus.Delivered)]
    public async Task AddPart_ToClosedRepair_WithoutConfirmation_IsRejectedForAdmin(RepairStatus status)
    {
        var part = await factory.CreateSparePartAsync(stock: 5);
        var repair = await factory.CreateRepairAsync("customer", "mechanic", status);

        var response = await (await factory.AsAdmin()).PostForm($"/repairs/addpart/{repair.Id}", new()
        {
            ["sparePartId"] = part.Id.ToString(), ["quantity"] = "1"
        });

        response.AssertRedirectTo($"/Repairs/Details/{repair.Id}");
        Assert.Equal(5, (await factory.GetSparePartAsync(part.Id)).Stock);
        Assert.Empty((await factory.GetRepairAsync(repair.Id)).RepairParts);
    }

    [Fact]
    public async Task AddPart_ByOtherMechanic_IsForbidden()
    {
        var part = await factory.CreateSparePartAsync(stock: 5);
        var repair = await factory.CreateRepairAsync("customer", "mechanic2");

        var response = await (await factory.AsMechanic()).PostForm($"/repairs/addpart/{repair.Id}", new()
        {
            ["sparePartId"] = part.Id.ToString(), ["quantity"] = "1"
        });

        response.AssertForbidden();
        Assert.Equal(5, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Fact]
    public async Task RemovePart_FromOtherRepair_Returns404()
    {
        var part = await factory.CreateSparePartAsync(stock: 5);
        var repairA = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress);
        var repairB = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress);
        var mechanic = await factory.AsMechanic();
        await mechanic.PostForm($"/repairs/addpart/{repairA.Id}", new() { ["sparePartId"] = part.Id.ToString(), ["quantity"] = "1" });
        var lineId = (await factory.GetRepairAsync(repairA.Id)).RepairParts.Single().Id;

        var response = await mechanic.PostForm($"/repairs/removepart/{repairB.Id}", new() { ["repairPartId"] = lineId.ToString() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(4, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Fact]
    public async Task TotalCost_IsLaborPlusParts_AfterAddingParts()
    {
        var part = await factory.CreateSparePartAsync(stock: 10, price: 15m);
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress, laborHours: 2m); // 2 × 45 = 90
        await (await factory.AsMechanic()).PostForm($"/repairs/addpart/{repair.Id}", new()
        {
            ["sparePartId"] = part.Id.ToString(), ["quantity"] = "2" // 30
        });

        var total = (await factory.GetRepairAsync(repair.Id)).TotalCost;

        Assert.Equal(120m, total);
    }

    // ---------- Baja ----------

    [Fact]
    public async Task Admin_DeletesRepair_AndStockReturnsToWarehouse()
    {
        var part = await factory.CreateSparePartAsync(stock: 10);
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress);
        var admin = await factory.AsAdmin();
        await admin.PostForm($"/repairs/addpart/{repair.Id}", new() { ["sparePartId"] = part.Id.ToString(), ["quantity"] = "4" });
        Assert.Equal(6, (await factory.GetSparePartAsync(part.Id)).Stock);

        (await admin.PostForm($"/repairs/delete/{repair.Id}")).AssertRedirectTo("/Repairs");

        Assert.False(await factory.WithDbAsync(ctx => ctx.Repairs.AnyAsync(r => r.Id == repair.Id)));
        Assert.Equal(10, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Fact]
    public async Task OnlyAdmin_CanDeleteRepairs()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic");

        (await (await factory.AsMechanic()).GetAsync($"/repairs/delete/{repair.Id}")).AssertForbidden();
        (await (await factory.AsMechanic()).PostForm($"/repairs/delete/{repair.Id}")).AssertForbidden();
        (await (await factory.AsCustomer()).PostForm($"/repairs/delete/{repair.Id}")).AssertForbidden();
        Assert.True(await factory.WithDbAsync(ctx => ctx.Repairs.AnyAsync(r => r.Id == repair.Id)));
    }
}
