using System.Net;
using G3AspNet.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Tests.Integration;

/// <summary>Líneas de pieza (RepairParts): alta, edición con contabilidad de stock, baja y permisos.</summary>
public sealed class RepairPartFlowTests : IClassFixture<AppFactory>
{
    private readonly AppFactory factory;

    public RepairPartFlowTests(AppFactory factory) => this.factory = factory;

    private static Dictionary<string, string> Form(int repairId, int sparePartId, int quantity, int id = 0, string unitPrice = "0") => new()
    {
        ["Id"] = id.ToString(),
        ["RepairId"] = repairId.ToString(),
        ["SparePartId"] = sparePartId.ToString(),
        ["Quantity"] = quantity.ToString(),
        ["UnitPrice"] = unitPrice
    };

    [Fact]
    public async Task Create_AddsLineAndDecrementsStock()
    {
        var part = await factory.CreateSparePartAsync(stock: 10, price: 12m);
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress);

        var response = await (await factory.AsMechanic()).PostForm("/repairparts/create", Form(repair.Id, part.Id, 4));

        response.AssertRedirectTo("/RepairParts/Details/");
        Assert.Equal(6, (await factory.GetSparePartAsync(part.Id)).Stock);
        var line = Assert.Single((await factory.GetRepairAsync(repair.Id)).RepairParts);
        Assert.Equal(12m, line.UnitPrice);
    }

    [Fact]
    public async Task Create_BeyondStock_ReturnsFormAndKeepsStock()
    {
        var part = await factory.CreateSparePartAsync(stock: 2);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");

        var response = await (await factory.AsMechanic()).PostForm("/repairparts/create", Form(repair.Id, part.Id, 3));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Fact]
    public async Task Create_ExactlyAllStock_LeavesZero()
    {
        var part = await factory.CreateSparePartAsync(stock: 3);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");

        (await (await factory.AsMechanic()).PostForm("/repairparts/create", Form(repair.Id, part.Id, 3)))
            .AssertRedirectTo("/RepairParts/Details/");

        Assert.Equal(0, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(1001)]
    public async Task Create_QuantityOutOfRange_IsRejected(int quantity)
    {
        var part = await factory.CreateSparePartAsync(stock: 5000);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");

        var response = await (await factory.AsMechanic()).PostForm("/repairparts/create", Form(repair.Id, part.Id, quantity));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5000, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Fact]
    public async Task Create_ClosedRepair_WithoutConfirmation_IsRejected()
    {
        var part = await factory.CreateSparePartAsync(stock: 5);
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.Completed);

        var response = await (await factory.AsAdmin()).PostForm("/repairparts/create", Form(repair.Id, part.Id, 1));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Fact]
    public async Task Create_UnknownRepairOrPart_Returns404()
    {
        var part = await factory.CreateSparePartAsync();
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var mechanic = await factory.AsMechanic();

        Assert.Equal(HttpStatusCode.NotFound, (await mechanic.PostForm("/repairparts/create", Form(999999, part.Id, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await mechanic.PostForm("/repairparts/create", Form(repair.Id, 999999, 1))).StatusCode);
    }

    [Fact]
    public async Task Customer_CannotCreateEditOrDeleteLines()
    {
        var part = await factory.CreateSparePartAsync(stock: 5);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var line = await factory.AddPartAsync(repair.Id, part.Id, 1);
        var customer = await factory.AsCustomer();

        (await customer.PostForm("/repairparts/create", Form(repair.Id, part.Id, 1))).AssertForbidden();
        (await customer.PostForm($"/repairparts/edit/{line.Id}", Form(repair.Id, part.Id, 2, line.Id))).AssertForbidden();
        (await customer.PostForm($"/repairparts/delete/{line.Id}")).AssertForbidden();
        Assert.Equal(4, (await factory.GetSparePartAsync(part.Id)).Stock);
        Assert.Equal(1, (await factory.GetRepairPartAsync(line.Id)).Quantity);
    }

    [Fact]
    public async Task Details_OwnerSees_StrangerCannot()
    {
        var part = await factory.CreateSparePartAsync(stock: 5);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var line = await factory.AddPartAsync(repair.Id, part.Id, 1);

        Assert.Equal(HttpStatusCode.OK, (await (await factory.AsCustomer()).GetAsync($"/repairparts/details/{line.Id}")).StatusCode);
        (await (await factory.AsCustomer2()).GetAsync($"/repairparts/details/{line.Id}")).AssertForbidden();
        (await (await factory.AsMechanic2()).GetAsync($"/repairparts/details/{line.Id}")).AssertForbidden();
    }

    [Fact]
    public async Task Edit_SamePart_IncreaseAndDecreaseAdjustsStockByDifference()
    {
        var part = await factory.CreateSparePartAsync(stock: 10);
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress);
        var line = await factory.AddPartAsync(repair.Id, part.Id, 3); // stock 7
        var mechanic = await factory.AsMechanic();

        (await mechanic.PostForm($"/repairparts/edit/{line.Id}", Form(repair.Id, part.Id, 5, line.Id)))
            .AssertRedirectTo("/RepairParts/Details/");
        Assert.Equal(5, (await factory.GetSparePartAsync(part.Id)).Stock);

        await mechanic.PostForm($"/repairparts/edit/{line.Id}", Form(repair.Id, part.Id, 1, line.Id));
        Assert.Equal(9, (await factory.GetSparePartAsync(part.Id)).Stock);
        Assert.Equal(1, (await factory.GetRepairPartAsync(line.Id)).Quantity);
    }

    [Fact]
    public async Task Edit_SamePart_UpToTotalAvailableIncludingOwnUnits_IsAllowed()
    {
        var part = await factory.CreateSparePartAsync(stock: 5);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var line = await factory.AddPartAsync(repair.Id, part.Id, 2); // stock 3, disponible real 5

        (await (await factory.AsMechanic()).PostForm($"/repairparts/edit/{line.Id}", Form(repair.Id, part.Id, 5, line.Id)))
            .AssertRedirectTo("/RepairParts/Details/");

        Assert.Equal(0, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Fact]
    public async Task Edit_SamePart_BeyondTotalAvailable_IsRejectedAndNothingChanges()
    {
        var part = await factory.CreateSparePartAsync(stock: 5);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var line = await factory.AddPartAsync(repair.Id, part.Id, 2);

        var response = await (await factory.AsMechanic()).PostForm($"/repairparts/edit/{line.Id}", Form(repair.Id, part.Id, 6, line.Id));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, (await factory.GetSparePartAsync(part.Id)).Stock);
        Assert.Equal(2, (await factory.GetRepairPartAsync(line.Id)).Quantity);
    }

    [Fact]
    public async Task Edit_SwitchingPart_RestoresOldStockAndConsumesNew()
    {
        var oldPart = await factory.CreateSparePartAsync(stock: 10);
        var newPart = await factory.CreateSparePartAsync(stock: 10);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var line = await factory.AddPartAsync(repair.Id, oldPart.Id, 4); // old 6

        (await (await factory.AsMechanic()).PostForm($"/repairparts/edit/{line.Id}", Form(repair.Id, newPart.Id, 2, line.Id)))
            .AssertRedirectTo("/RepairParts/Details/");

        Assert.Equal(10, (await factory.GetSparePartAsync(oldPart.Id)).Stock);
        Assert.Equal(8, (await factory.GetSparePartAsync(newPart.Id)).Stock);
    }

    [Fact]
    public async Task Edit_MovingLineToRepairOfAnotherMechanic_IsForbidden()
    {
        var part = await factory.CreateSparePartAsync(stock: 10);
        var mine = await factory.CreateRepairAsync("customer", "mechanic");
        var theirs = await factory.CreateRepairAsync("customer", "mechanic2");
        var line = await factory.AddPartAsync(mine.Id, part.Id, 1);

        var response = await (await factory.AsMechanic()).PostForm($"/repairparts/edit/{line.Id}", Form(theirs.Id, part.Id, 1, line.Id));

        response.AssertForbidden();
        Assert.Equal(mine.Id, (await factory.GetRepairPartAsync(line.Id)).RepairId);
    }

    [Fact]
    public async Task Edit_PriceOnlyAdminCanChange()
    {
        var part = await factory.CreateSparePartAsync(stock: 10, price: 10m);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var line = await factory.AddPartAsync(repair.Id, part.Id, 1);

        await (await factory.AsMechanic()).PostForm($"/repairparts/edit/{line.Id}", Form(repair.Id, part.Id, 1, line.Id, "99"));
        Assert.Equal(10m, (await factory.GetRepairPartAsync(line.Id)).UnitPrice);

        await (await factory.AsAdmin()).PostForm($"/repairparts/edit/{line.Id}", Form(repair.Id, part.Id, 1, line.Id, "99"));
        Assert.Equal(99m, (await factory.GetRepairPartAsync(line.Id)).UnitPrice);
    }

    [Fact]
    public async Task Edit_ClosedRepair_IsForbiddenForMechanic()
    {
        var part = await factory.CreateSparePartAsync(stock: 10);
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress);
        var line = await factory.AddPartAsync(repair.Id, part.Id, 1);
        await factory.WithDbAsync(async ctx =>
        {
            (await ctx.Repairs.FindAsync(repair.Id))!.Status = RepairStatus.Completed;
            await ctx.SaveChangesAsync();
        });

        (await (await factory.AsMechanic()).PostForm($"/repairparts/edit/{line.Id}", Form(repair.Id, part.Id, 3, line.Id))).AssertForbidden();
        Assert.Equal(1, (await factory.GetRepairPartAsync(line.Id)).Quantity);
    }

    [Fact]
    public async Task Edit_UnknownLine_Returns404()
    {
        var part = await factory.CreateSparePartAsync();
        var repair = await factory.CreateRepairAsync("customer", "mechanic");

        var response = await (await factory.AsAdmin()).PostForm("/repairparts/edit/999999", Form(repair.Id, part.Id, 1, 999999));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RestoresStock()
    {
        var part = await factory.CreateSparePartAsync(stock: 10);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var line = await factory.AddPartAsync(repair.Id, part.Id, 6);

        (await (await factory.AsMechanic()).PostForm($"/repairparts/delete/{line.Id}")).AssertRedirectTo("/RepairParts");

        Assert.Equal(10, (await factory.GetSparePartAsync(part.Id)).Stock);
        Assert.False(await factory.WithDbAsync(ctx => ctx.RepairParts.AnyAsync(p => p.Id == line.Id)));
    }

    [Fact]
    public async Task Delete_UnknownLine_Returns404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await (await factory.AsAdmin()).PostForm("/repairparts/delete/999999")).StatusCode);
    }
}
