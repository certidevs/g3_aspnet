using System.Net;
using G3AspNet.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Tests.Integration;

/// <summary>Regresiones de seguridad: acceso anónimo, CSRF, IDOR, overposting y XSS.</summary>
public sealed class SecurityRegressionTests : IClassFixture<AppFactory>
{
    private readonly AppFactory factory;

    public SecurityRegressionTests(AppFactory factory) => this.factory = factory;

    public static TheoryData<string> PostRoutes => new()
    {
        "/repairs/create", "/repairs/edit/1", "/repairs/delete/1", "/repairs/start/1", "/repairs/waitforparts/1",
        "/repairs/complete/1", "/repairs/deliver/1", "/repairs/addpart/1", "/repairs/removepart/1",
        "/repairparts/create", "/repairparts/edit/1", "/repairparts/delete/1",
        "/vehicle/save", "/vehicle/delete/1",
        "/spareparts/save", "/spareparts/delete/1",
        "/users/create", "/users/edit/x", "/users/delete/x",
        "/profile/edit", "/profile/changepassword", "/profile/enabletwofactor", "/profile/disabletwofactor",
        "/account/logout"
    };

    [Theory]
    [MemberData(nameof(PostRoutes))]
    public async Task AnonymousPost_IsRedirectedToLogin_NeverExecuted(string url)
    {
        var response = await factory.CreateAnonymousClient().PostFormAsync(url, "/account/login");

        response.AssertRedirectTo("/account/login");
    }

    [Theory]
    [MemberData(nameof(PostRoutes))]
    public async Task AuthenticatedPost_WithoutAntiforgeryToken_Returns400(string url)
    {
        var admin = await factory.AsAdmin();

        var response = await admin.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutToken_DoesNotChangeState()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic");

        await (await factory.AsAdmin()).PostAsync($"/repairs/start/{repair.Id}", new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(RepairStatus.Pending, (await factory.GetRepairAsync(repair.Id)).Status);
    }

    [Fact]
    public async Task GetRequests_CannotTriggerStateChangingActions()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var admin = await factory.AsAdmin();

        var response = await admin.GetAsync($"/repairs/start/{repair.Id}");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(RepairStatus.Pending, (await factory.GetRepairAsync(repair.Id)).Status);
    }

    // ---------- IDOR ----------

    [Fact]
    public async Task Customer_CannotReadOrWriteOthersRepairsVehiclesOrParts()
    {
        var part = await factory.CreateSparePartAsync(stock: 5);
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.InProgress);
        var line = await factory.AddPartAsync(repair.Id, part.Id, 1);
        var intruder = await factory.AsCustomer2();

        (await intruder.GetAsync($"/repairs/details/{repair.Id}")).AssertForbidden();
        (await intruder.GetAsync($"/vehicle/details/{repair.VehicleId}")).AssertForbidden();
        (await intruder.GetAsync($"/repairparts/details/{line.Id}")).AssertForbidden();
        (await intruder.GetAsync($"/repairs/edit/{repair.Id}")).AssertForbidden();
        (await intruder.PostForm($"/repairs/removepart/{repair.Id}", new() { ["repairPartId"] = line.Id.ToString() })).AssertForbidden();
        Assert.Equal(4, (await factory.GetSparePartAsync(part.Id)).Stock);
    }

    [Fact]
    public async Task Customer_ListsNeverLeakOthersRepairParts()
    {
        var part = await factory.CreateSparePartAsync(stock: 5);
        var repair = await factory.CreateRepairAsync("customer", "mechanic");
        var line = await factory.AddPartAsync(repair.Id, part.Id, 1);

        var admin = await (await (await factory.AsAdmin()).GetAsync("/repairparts")).HtmlAsync();
        var stranger = await (await (await factory.AsCustomer2()).GetAsync("/repairparts")).HtmlAsync();
        var strangerPart = await (await (await factory.AsCustomer2()).GetAsync($"/spareparts/details/{part.Id}")).HtmlAsync();

        Assert.Contains($"/RepairParts/Details/{line.Id}\"", admin);
        Assert.DoesNotContain($"/RepairParts/Details/{line.Id}\"", stranger);
        Assert.DoesNotContain($"/Repairs/Details/{repair.Id}\"", strangerPart);
    }

    [Fact]
    public async Task Mechanic_CannotDeleteOrDeliverEvenOwnRepairs()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic", RepairStatus.Completed, 1m);
        var mechanic = await factory.AsMechanic();

        (await mechanic.PostForm($"/repairs/delete/{repair.Id}")).AssertForbidden();
        (await mechanic.PostForm($"/repairs/deliver/{repair.Id}")).AssertForbidden();
        Assert.Equal(RepairStatus.Completed, (await factory.GetRepairAsync(repair.Id)).Status);
    }

    // ---------- Overposting ----------

    [Fact]
    public async Task Create_IgnoresStatusAndCompletedAtSentByTheClient()
    {
        var vehicle = await factory.CreateVehicleAsync("customer");

        await (await factory.AsMechanic()).PostForm("/repairs/create", new()
        {
            ["Id"] = "0", ["VehicleId"] = vehicle.Id.ToString(), ["RepairType"] = nameof(RepairType.Tires),
            ["Description"] = "Overposting", ["OpenedAt"] = "2026-03-01T09:00", ["MileageAtEntry"] = "1",
            ["LaborHours"] = "1", ["HourlyRate"] = "1",
            ["Status"] = nameof(RepairStatus.Delivered), ["CompletedAt"] = "2026-03-02T09:00"
        });

        var repair = await factory.WithDbAsync(ctx => ctx.Repairs.SingleAsync(r => r.VehicleId == vehicle.Id));
        Assert.Equal(RepairStatus.Pending, repair.Status);
        Assert.Null(repair.CompletedAt);
    }

    [Fact]
    public async Task Edit_CannotBypassStateMachineBySendingStatus()
    {
        var repair = await factory.CreateRepairAsync("customer", "mechanic");

        await (await factory.AsAdmin()).PostForm($"/repairs/edit/{repair.Id}", new()
        {
            ["Id"] = repair.Id.ToString(), ["VehicleId"] = repair.VehicleId.ToString(),
            ["MechanicId"] = repair.MechanicId, ["RepairType"] = nameof(RepairType.OilChange),
            ["Description"] = "x", ["OpenedAt"] = "2026-03-01T09:00", ["MileageAtEntry"] = "1",
            ["LaborHours"] = "1", ["HourlyRate"] = "45", ["Status"] = nameof(RepairStatus.Delivered)
        });

        Assert.Equal(RepairStatus.Pending, (await factory.GetRepairAsync(repair.Id)).Status);
    }

    // ---------- XSS ----------

    [Fact]
    public async Task UserSuppliedText_IsHtmlEncodedInViews()
    {
        const string payload = "<script>alert('xss')</script>";
        var vehicle = await factory.CreateVehicleAsync("customer");
        var admin = await factory.AsAdmin();
        await admin.PostForm("/repairs/create", new()
        {
            ["Id"] = "0", ["VehicleId"] = vehicle.Id.ToString(), ["MechanicId"] = await factory.UserIdAsync("mechanic"),
            ["RepairType"] = nameof(RepairType.Other), ["Description"] = payload, ["OpenedAt"] = "2026-03-01T09:00",
            ["MileageAtEntry"] = "1", ["LaborHours"] = "1", ["HourlyRate"] = "45"
        });
        var repair = await factory.WithDbAsync(ctx => ctx.Repairs.SingleAsync(r => r.VehicleId == vehicle.Id));

        var details = await (await admin.GetAsync($"/repairs/details/{repair.Id}")).Content.ReadAsStringAsync();
        var list = await (await admin.GetAsync("/repairs")).Content.ReadAsStringAsync();

        Assert.DoesNotContain(payload, details);
        Assert.DoesNotContain(payload, list);
        Assert.Contains("&lt;script&gt;", details);
    }

    [Fact]
    public async Task SparePartName_IsHtmlEncoded()
    {
        const string payload = "<img src=x onerror=alert(1)>";
        await (await factory.AsAdmin()).PostForm("/spareparts/save", new()
        {
            ["Id"] = "0", ["Reference"] = $"XSS-{Guid.NewGuid():N}"[..20], ["Name"] = payload, ["Brand"] = "x",
            ["UnitPrice"] = "1", ["Stock"] = "1", ["MinStock"] = "1"
        });

        var html = await (await (await factory.AsAdmin()).GetAsync("/spareparts")).Content.ReadAsStringAsync();

        Assert.DoesNotContain(payload, html);
    }

    // ---------- Rutas con ids raros ----------

    [Theory]
    [InlineData("/repairs/details/0")]
    [InlineData("/repairs/details/-1")]
    [InlineData("/vehicle/details/0")]
    [InlineData("/spareparts/details/-7")]
    [InlineData("/repairparts/details/0")]
    [InlineData("/repairs/details/2147483647")]
    public async Task NonexistentIds_Return404(string url)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await (await factory.AsAdmin()).GetAsync(url)).StatusCode);
    }

    [Theory]
    [InlineData("/repairs/details/abc")]
    [InlineData("/repairs/details/99999999999999")]
    public async Task NonNumericIds_DoNotCrashTheServer(string url)
    {
        var response = await (await factory.AsAdmin()).GetAsync(url);

        Assert.True((int)response.StatusCode < 500, $"{url} devolvió {(int)response.StatusCode}");
    }

    [Fact]
    public async Task UnknownRoute_Returns404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await (await factory.AsAdmin()).GetAsync("/no/existe/para/nada")).StatusCode);
    }

    [Fact]
    public async Task AuthCookie_IsHttpOnly()
    {
        var client = factory.CreateAnonymousClient();
        var token = await client.GetAntiforgeryTokenAsync("/account/login");

        var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "customer", ["Password"] = AppFactory.CustomerPassword, ["__RequestVerificationToken"] = token
        }));

        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("G3AspNet.Auth="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }
}
