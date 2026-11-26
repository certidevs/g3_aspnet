using G3AspNet.Models;
using G3AspNet.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Tests.Integration;

/// <summary>Regresiones sobre los datos demo, la portada y la coherencia del inventario.</summary>
public sealed class SeedAndHomeRegressionTests : IClassFixture<AppFactory>
{
    private readonly AppFactory factory;

    public SeedAndHomeRegressionTests(AppFactory factory) => this.factory = factory;

    [Fact]
    public async Task Seed_CreatesTheFourRolesAndAtLeastOneUserOfEach()
    {
        var roles = await factory.WithDbAsync(ctx => ctx.Roles.Select(r => r.Name!).ToListAsync());

        Assert.Contains(RoleNames.Admin, roles);
        Assert.Contains(RoleNames.Mechanic, roles);
        Assert.Contains(RoleNames.Customer, roles);
        Assert.Contains(RoleNames.User, roles);
        foreach (var user in new[] { "admin", "mechanic", "customer", "user" })
        {
            Assert.NotEqual(0, (await factory.UserIdAsync(user)).Length);
        }
    }

    [Fact]
    public async Task Seed_VehiclesAndRepairsHaveValidReferences_AndNoNegativeStock()
    {
        await factory.WithDbAsync(async ctx =>
        {
            Assert.Empty(await ctx.Vehicles.Where(v => !ctx.Users.Any(u => u.Id == v.OwnerId)).ToListAsync());
            Assert.Empty(await ctx.Repairs.Where(r => !ctx.Vehicles.Any(v => v.Id == r.VehicleId)).ToListAsync());
            Assert.Empty(await ctx.Repairs.Where(r => !ctx.Users.Any(u => u.Id == r.MechanicId)).ToListAsync());
            Assert.Empty(await ctx.SpareParts.Where(s => s.Stock < 0).ToListAsync());
            Assert.Empty(await ctx.RepairParts.Where(p => p.Quantity <= 0).ToListAsync());
        });
    }

    [Fact]
    public async Task Seed_ClosedRepairsHaveCompletionDate_OpenOnesDoNot()
    {
        await factory.WithDbAsync(async ctx =>
        {
            var closed = await ctx.Repairs.Where(r => r.Status == RepairStatus.Completed || r.Status == RepairStatus.Delivered).ToListAsync();
            var open = await ctx.Repairs.Where(r => r.Status == RepairStatus.Pending || r.Status == RepairStatus.InProgress).ToListAsync();

            Assert.All(closed, r => Assert.NotNull(r.CompletedAt));
            Assert.All(open, r => Assert.Null(r.CompletedAt));
        });
    }

    [Fact]
    public async Task Seed_ReferencesAreUnique()
    {
        var duplicates = await factory.WithDbAsync(ctx => ctx.SpareParts
            .GroupBy(s => s.Reference).Where(g => g.Count() > 1).Select(g => g.Key).ToListAsync());

        Assert.Empty(duplicates);
    }

    [Fact]
    public async Task Database_RejectsDuplicateSparePartReference()
    {
        var existing = await factory.CreateSparePartAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() => factory.WithDbAsync(async ctx =>
        {
            ctx.SpareParts.Add(new SparePart { Reference = existing.Reference, Name = "dup" });
            await ctx.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task Home_ShowsCountersThatReflectTheDatabase()
    {
        var before = await factory.WithDbAsync(ctx => ctx.Vehicles.CountAsync());
        await factory.CreateVehicleAsync("customer");

        var html = await (await factory.CreateAnonymousClient().GetAsync("/")).HtmlAsync();

        Assert.Contains((before + 1).ToString(), html);
    }

    [Fact]
    public async Task Home_DoesNotExposePersonalData()
    {
        var html = await (await factory.CreateAnonymousClient().GetAsync("/")).HtmlAsync();

        Assert.DoesNotContain("@g3.local", html);
        Assert.DoesNotContain("admin@", html);
    }

    [Fact]
    public async Task StockInvariant_AddRemoveDeleteCycle_NeverLosesOrCreatesUnits()
    {
        var part = await factory.CreateSparePartAsync(stock: 20);
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress);
        var mechanic = await factory.AsMechanic();
        var admin = await factory.AsAdmin();

        await mechanic.PostForm($"/repairs/addpart/{repair.Id}", new() { ["sparePartId"] = part.Id.ToString(), ["quantity"] = "5" });
        await mechanic.PostForm($"/repairs/addpart/{repair.Id}", new() { ["sparePartId"] = part.Id.ToString(), ["quantity"] = "7" });
        Assert.Equal(8, (await factory.GetSparePartAsync(part.Id)).Stock);

        var firstLine = (await factory.GetRepairAsync(repair.Id)).RepairParts.OrderBy(p => p.Id).First();
        await mechanic.PostForm($"/repairs/removepart/{repair.Id}", new() { ["repairPartId"] = firstLine.Id.ToString() });
        Assert.Equal(13, (await factory.GetSparePartAsync(part.Id)).Stock);

        await admin.PostForm($"/repairs/delete/{repair.Id}");
        Assert.Equal(20, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Fact]
    public async Task Regression_CompletedRepairCompletedAtIsUtcAndNotInTheFuture()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress, laborHours: 1m);

        await (await factory.AsMechanic()).PostForm($"/repairs/complete/{repair.Id}");

        var completed = (await factory.GetRepairAsync(repair.Id)).CompletedAt!.Value;
        Assert.True(completed <= DateTime.UtcNow.AddMinutes(1));
        Assert.True(completed >= DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public async Task Regression_ReopeningACompletedRepairIsNotPossible()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.Completed, 1m);
        var admin = await factory.AsAdmin();

        await admin.PostForm($"/repairs/start/{repair.Id}");
        await admin.PostForm($"/repairs/waitforparts/{repair.Id}");

        Assert.Equal(RepairStatus.Completed, (await factory.GetRepairAsync(repair.Id)).Status);
    }

    [Fact]
    public async Task Regression_StateActionsOnMissingRepair_Return404()
    {
        var admin = await factory.AsAdmin();
        foreach (var action in new[] { "start", "waitforparts", "complete", "deliver", "delete" })
        {
            Assert.Equal(System.Net.HttpStatusCode.NotFound, (await admin.PostForm($"/repairs/{action}/999999")).StatusCode);
        }
    }
}
