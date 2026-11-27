using System.Net;
using G3AspNet.Models.Enum;

namespace G3AspNet.Tests.Integration;

public sealed class InvoiceFlowTests : IClassFixture<AppFactory>
{
    private readonly AppFactory factory;

    public InvoiceFlowTests(AppFactory factory) => this.factory = factory;

    [Theory]
    [InlineData("customer", AppFactory.CustomerPassword, true)]
    [InlineData("mechanic", AppFactory.MechanicPassword, true)]
    [InlineData("admin", AppFactory.AdminPassword, true)]
    [InlineData("customer2", AppFactory.CustomerPassword, false)]
    [InlineData("mechanic2", AppFactory.MechanicPassword, false)]
    [InlineData("user", AppFactory.UserPassword, false)]
    public async Task Invoice_IsOnlyForWhoCanSeeTheRepair(string username, string password, bool allowed)
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.Completed, 1m);
        var client = await factory.LoginAsync(username, password);

        var response = await client.GetAsync($"/repairs/invoice/{repair.Id}");

        if (!allowed)
        {
            response.AssertForbidden();
            return;
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.EndsWith(".pdf", response.Content.Headers.ContentDisposition?.FileName);
    }

    [Fact]
    public async Task Invoice_OfAnOpenRepair_RedirectsToTheDetail()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress, 1m);

        var response = await (await factory.AsAdmin()).GetAsync($"/repairs/invoice/{repair.Id}");

        response.AssertRedirectTo($"/Repairs/Details/{repair.Id}");
    }

    [Fact]
    public async Task Invoice_AnonymousOrUnknown_IsNotServed()
    {
        (await factory.CreateAnonymousClient().GetAsync("/repairs/invoice/1")).AssertRedirectTo("/account/login");
        Assert.Equal(HttpStatusCode.NotFound,
            (await (await factory.AsAdmin()).GetAsync("/repairs/invoice/999999")).StatusCode);
    }

    [Theory]
    [InlineData(RepairStatus.Completed, true)]
    [InlineData(RepairStatus.InProgress, false)]
    public async Task Detail_ShowsTheDownloadButtonOnlyForClosedRepairs(RepairStatus status, bool shown)
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", status, 1m);

        var html = await (await (await factory.AsCustomer()).GetAsync($"/repairs/details/{repair.Id}")).HtmlAsync();

        Assert.Equal(shown, html.Contains($"/Repairs/Invoice/{repair.Id}"));
    }
}
