using System.Net;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Tests.Integration;

/// <summary>Flujo del almacén de recambios.</summary>
public sealed class SparePartFlowTests : IClassFixture<AppFactory>
{
    private readonly AppFactory factory;

    public SparePartFlowTests(AppFactory factory) => this.factory = factory;

    private static Dictionary<string, string> Form(string reference, int id = 0, int stock = 10, int minStock = 2) => new()
    {
        ["Id"] = id.ToString(),
        ["Reference"] = reference,
        ["Name"] = "Pastillas de prueba",
        ["Brand"] = "Brembo",
        ["UnitPrice"] = "25",
        ["Stock"] = stock.ToString(),
        ["MinStock"] = minStock.ToString()
    };

    [Fact]
    public async Task AnyAuthenticatedUser_CanBrowseSpareParts()
    {
        var response = await (await factory.AsCustomer()).GetAsync("/spareparts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Filtro de aceite", await response.HtmlAsync());
    }

    [Fact]
    public async Task LowStockFilter_OnlyListsPartsAtOrBelowMinimum()
    {
        var low = await factory.CreateSparePartAsync(stock: 1, minStock: 5);
        var healthy = await factory.CreateSparePartAsync(stock: 50, minStock: 5);
        await factory.WithDbAsync(async ctx =>
        {
            (await ctx.SpareParts.FindAsync(low.Id))!.Name = "BAJO-STOCK-XYZ";
            (await ctx.SpareParts.FindAsync(healthy.Id))!.Name = "STOCK-SANO-XYZ";
            await ctx.SaveChangesAsync();
        });

        var html = await (await (await factory.AsAdmin()).GetAsync("/spareparts?lowStock=true")).HtmlAsync();

        Assert.Contains("BAJO-STOCK-XYZ", html);
        Assert.DoesNotContain("STOCK-SANO-XYZ", html);
    }

    [Theory]
    [InlineData("/spareparts/create")]
    [InlineData("/spareparts/edit/1")]
    public async Task Customer_CannotOpenManagementPages(string url)
    {
        (await (await factory.AsCustomer()).GetAsync(url)).AssertForbidden();
    }

    [Fact]
    public async Task Customer_CannotSaveOrDelete()
    {
        var client = await factory.AsCustomer();

        (await client.PostForm("/spareparts/save", Form("HACK-REF"))).AssertForbidden();
        (await client.PostForm("/spareparts/delete/1")).AssertForbidden();
        Assert.False(await factory.WithDbAsync(ctx => ctx.SpareParts.AnyAsync(s => s.Reference == "HACK-REF")));
    }

    [Fact]
    public async Task Mechanic_CreatesEditsAndDeletesSparePart()
    {
        var client = await factory.AsMechanic();

        (await client.PostForm("/spareparts/save", Form("FLOW-REF-1"))).AssertRedirectTo("/SpareParts/Details/");
        var created = await factory.WithDbAsync(ctx => ctx.SpareParts.SingleAsync(s => s.Reference == "FLOW-REF-1"));
        Assert.Equal(25m, created.UnitPrice);

        var edit = Form("FLOW-REF-1", created.Id, stock: 3, minStock: 5);
        (await client.PostForm("/spareparts/save", edit)).AssertRedirectTo("/SpareParts/Details/");
        var edited = await factory.GetSparePartAsync(created.Id);
        Assert.Equal(3, edited.Stock);
        Assert.True(edited.IsLowStock);

        (await client.PostForm($"/spareparts/delete/{created.Id}")).AssertRedirectTo("/SpareParts");
        Assert.False(await factory.WithDbAsync(ctx => ctx.SpareParts.AnyAsync(s => s.Id == created.Id)));
    }

    [Fact]
    public async Task Save_DuplicateReference_IsRejected()
    {
        var existing = await factory.CreateSparePartAsync();
        var client = await factory.AsAdmin();

        var response = await client.PostForm("/spareparts/save", Form(existing.Reference));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Ya existe un recambio con esa referencia", await response.HtmlAsync());
    }

    [Fact]
    public async Task Save_InvalidValues_ReturnsFormWithoutSaving()
    {
        var form = Form("INVALID-REF", stock: -5);
        var response = await (await factory.AsAdmin()).PostForm("/spareparts/save", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await factory.WithDbAsync(ctx => ctx.SpareParts.AnyAsync(s => s.Reference == "INVALID-REF")));
    }

    [Fact]
    public async Task Delete_SparePartUsedInRepair_IsBlocked()
    {
        var part = await factory.CreateSparePartAsync();
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var admin = await factory.AsAdmin();
        await admin.PostForm($"/repairs/addpart/{repair.Id}", new() { ["sparePartId"] = part.Id.ToString(), ["quantity"] = "1" });

        var response = await admin.PostForm($"/spareparts/delete/{part.Id}");

        response.AssertRedirectTo("/SpareParts/Details/");
        Assert.True(await factory.WithDbAsync(ctx => ctx.SpareParts.AnyAsync(s => s.Id == part.Id)));
    }

    [Fact]
    public async Task Details_UnknownPart_Returns404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await (await factory.AsAdmin()).GetAsync("/spareparts/details/999999")).StatusCode);
    }
}
