using System.Net;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Tests.Integration;

/// <summary>Una reparación cerrada solo la modifica un Admin que lo confirma expresamente.</summary>
public sealed class ClosedRepairCostTests(AppFactory factory) : IClassFixture<AppFactory>
{
    private const string ConfirmationField = "name=\"confirmClosed\"";

    private static Dictionary<string, string> EditForm(Repair repair) => new()
    {
        ["Id"] = repair.Id.ToString(),
        ["VehicleId"] = repair.VehicleId.ToString(),
        ["MechanicId"] = repair.MechanicId,
        ["RepairType"] = nameof(RepairType.OilChange),
        ["Description"] = "Cambio sobre reparación cerrada",
        ["OpenedAt"] = "2026-01-15T10:00",
        ["MileageAtEntry"] = "50000",
        ["LaborHours"] = "10",
        ["HourlyRate"] = "100",
        ["Status"] = nameof(RepairStatus.InProgress)
    };

    private static Dictionary<string, string> PartForm(int repairId, int sparePartId, int quantity, int id = 0) => new()
    {
        ["Id"] = id.ToString(),
        ["RepairId"] = repairId.ToString(),
        ["SparePartId"] = sparePartId.ToString(),
        ["Quantity"] = quantity.ToString(),
        ["UnitPrice"] = "10"
    };

    private static Dictionary<string, string> Confirmed(Dictionary<string, string>? fields = null) =>
        new(fields ?? new()) { ["confirmClosed"] = "true" };

    private async Task<(Repair Repair, SparePart Part, RepairPart Line)> ClosedRepairWithPartAsync(RepairStatus status)
    {
        var part = await factory.CreateSparePartAsync(stock: 10);
        var repair = await factory.CreateRepairAsync("customer", "mechanic", status, laborHours: 2m);
        var line = await factory.AddPartAsync(repair.Id, part.Id, 2);
        return (repair, part, line);
    }

    private Task<bool> RepairExistsAsync(int id) => factory.WithDbAsync(ctx => ctx.Repairs.AnyAsync(r => r.Id == id));

    [Theory]
    [InlineData(RepairStatus.Completed)]
    [InlineData(RepairStatus.Delivered)]
    public async Task Admin_EditWithoutConfirmation_KeepsCost(RepairStatus status)
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", status, laborHours: 2m);
        var admin = await factory.AsAdmin();

        var response = await admin.PostForm($"/repairs/edit/{repair.Id}", EditForm(repair));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(ConfirmationField, await response.HtmlAsync());
        Assert.Equal(90m, (await factory.GetRepairAsync(repair.Id)).TotalCost);
    }

    [Theory]
    [InlineData(RepairStatus.Completed)]
    [InlineData(RepairStatus.Delivered)]
    public async Task Admin_EditWithConfirmation_ChangesCostAndKeepsStatus(RepairStatus status)
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", status, laborHours: 2m);
        var admin = await factory.AsAdmin();

        Assert.Contains(ConfirmationField, await admin.GetStringAsync($"/repairs/edit/{repair.Id}"));
        var response = await admin.PostForm($"/repairs/edit/{repair.Id}", Confirmed(EditForm(repair)));

        response.AssertRedirectTo($"/Repairs/Details/{repair.Id}");
        var after = await factory.GetRepairAsync(repair.Id);
        Assert.Equal(1000m, after.TotalCost);
        Assert.Equal(status, after.Status);
    }

    [Theory]
    [InlineData(RepairStatus.Completed)]
    [InlineData(RepairStatus.Delivered)]
    public async Task Mechanic_CannotModifyEvenWithConfirmation(RepairStatus status)
    {
        var (repair, part, line) = await ClosedRepairWithPartAsync(status);
        var mechanic = await factory.AsMechanic();

        (await mechanic.GetAsync($"/repairs/edit/{repair.Id}")).AssertForbidden();
        (await mechanic.PostForm($"/repairs/edit/{repair.Id}", Confirmed(EditForm(repair)))).AssertForbidden();
        (await mechanic.PostForm($"/repairs/addpart/{repair.Id}",
            Confirmed(new() { ["sparePartId"] = part.Id.ToString(), ["quantity"] = "1" }))).AssertForbidden();
        (await mechanic.PostForm($"/repairs/removepart/{repair.Id}",
            Confirmed(new() { ["repairPartId"] = line.Id.ToString() }))).AssertForbidden();
        (await mechanic.PostForm("/repairparts/create", Confirmed(PartForm(repair.Id, part.Id, 1)))).AssertForbidden();
        (await mechanic.PostForm($"/repairparts/edit/{line.Id}",
            Confirmed(PartForm(repair.Id, part.Id, 5, line.Id)))).AssertForbidden();
        (await mechanic.PostForm($"/repairparts/delete/{line.Id}", Confirmed())).AssertForbidden();

        Assert.Equal(110m, (await factory.GetRepairAsync(repair.Id)).TotalCost);
        Assert.Equal(8, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Fact]
    public async Task Admin_PartChangesWithoutConfirmation_AreRejected()
    {
        var (repair, part, line) = await ClosedRepairWithPartAsync(RepairStatus.Delivered);
        var admin = await factory.AsAdmin();

        await admin.PostForm($"/repairs/removepart/{repair.Id}", new() { ["repairPartId"] = line.Id.ToString() });
        await admin.PostForm($"/repairparts/edit/{line.Id}", PartForm(repair.Id, part.Id, 5, line.Id));
        await admin.PostForm($"/repairparts/delete/{line.Id}");
        await admin.PostForm($"/repairs/delete/{repair.Id}");

        Assert.Equal(2, (await factory.GetRepairPartAsync(line.Id)).Quantity);
        Assert.Equal(8, (await factory.GetSparePartAsync(part.Id)).Stock);
        Assert.True(await RepairExistsAsync(repair.Id));
    }

    [Fact]
    public async Task Admin_MovingPartIntoClosedRepair_RequiresConfirmation()
    {
        var part = await factory.CreateSparePartAsync(stock: 10);
        var open = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress);
        var closed = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.Completed);
        var line = await factory.AddPartAsync(open.Id, part.Id, 1);
        var admin = await factory.AsAdmin();

        await admin.PostForm($"/repairparts/edit/{line.Id}", PartForm(closed.Id, part.Id, 1, line.Id));
        Assert.Equal(open.Id, (await factory.GetRepairPartAsync(line.Id)).RepairId);

        await admin.PostForm($"/repairparts/edit/{line.Id}", Confirmed(PartForm(closed.Id, part.Id, 1, line.Id)));
        Assert.Equal(closed.Id, (await factory.GetRepairPartAsync(line.Id)).RepairId);
    }

    [Fact]
    public async Task Admin_PartChangesWithConfirmation_AreApplied()
    {
        var (repair, part, line) = await ClosedRepairWithPartAsync(RepairStatus.Delivered);
        var admin = await factory.AsAdmin();

        await admin.PostForm($"/repairs/addpart/{repair.Id}",
            Confirmed(new() { ["sparePartId"] = part.Id.ToString(), ["quantity"] = "1" }));
        await admin.PostForm("/repairparts/create", Confirmed(PartForm(repair.Id, part.Id, 1)));
        Assert.Equal(6, (await factory.GetSparePartAsync(part.Id)).Stock);

        await admin.PostForm($"/repairparts/edit/{line.Id}", Confirmed(PartForm(repair.Id, part.Id, 3, line.Id)));
        Assert.Equal(3, (await factory.GetRepairPartAsync(line.Id)).Quantity);

        await admin.PostForm($"/repairs/removepart/{repair.Id}",
            Confirmed(new() { ["repairPartId"] = line.Id.ToString() }));
        var remaining = (await factory.GetRepairAsync(repair.Id)).RepairParts.First();
        await admin.PostForm($"/repairparts/delete/{remaining.Id}", Confirmed());

        Assert.Single((await factory.GetRepairAsync(repair.Id)).RepairParts);
        Assert.Equal(9, (await factory.GetSparePartAsync(part.Id)).Stock);

        await admin.PostForm($"/repairs/delete/{repair.Id}", Confirmed());
        Assert.False(await RepairExistsAsync(repair.Id));
        Assert.Equal(10, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Fact]
    public async Task Admin_SeesConfirmationOnEveryFormOfAClosedRepair()
    {
        var (repair, _, line) = await ClosedRepairWithPartAsync(RepairStatus.Completed);
        var admin = await factory.AsAdmin();

        foreach (var url in new[]
                 {
                     $"/repairs/details/{repair.Id}", $"/repairs/edit/{repair.Id}", $"/repairs/delete/{repair.Id}",
                     $"/repairparts/create?repairId={repair.Id}", $"/repairparts/edit/{line.Id}",
                     $"/repairparts/delete/{line.Id}"
                 })
        {
            Assert.Contains(ConfirmationField, await admin.GetStringAsync(url));
        }

        var editPart = await admin.GetStringAsync($"/repairparts/edit/{line.Id}");
        Assert.Contains($"value=\"{repair.Id}\"", editPart);
    }

    [Fact]
    public async Task OpenRepair_DoesNotAskForConfirmation()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress);
        var admin = await factory.AsAdmin();

        foreach (var url in new[]
                 {
                     $"/repairs/details/{repair.Id}", $"/repairs/edit/{repair.Id}",
                     $"/repairs/delete/{repair.Id}", $"/repairparts/create?repairId={repair.Id}"
                 })
        {
            Assert.DoesNotContain(ConfirmationField, await admin.GetStringAsync(url));
        }
    }
}
