using G3AspNet.Data;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace G3AspNet.Tests.Integration;

/// <summary>
/// Arranca la aplicación real en memoria con una base SQLite temporal propia.
/// DbInitializer recrea la base y siembra los usuarios demo (admin, mechanic, customer...).
/// </summary>
public sealed class AppFactory : WebApplicationFactory<Program>
{
    private readonly string dbPath = Path.Combine(Path.GetTempPath(), $"g3-tests-{Guid.NewGuid():N}.db");
    private readonly Dictionary<string, HttpClient> sessions = new();

    public const string AdminPassword = "Admin123!";
    public const string MechanicPassword = "Mechanic123!";
    public const string CustomerPassword = "Customer123!";
    public const string UserPassword = "User123!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // UseSetting llega a la configuración antes de que Program.cs lea la cadena de conexión.
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={dbPath}");
        builder.UseSetting("SeedData:Enabled", "true");

        // El limitador de login particiona por IP y TestServer no trae IP: cada cliente de prueba
        // se identifica con una IP propia (cabecera X-Test-Ip) para no compartir cupo entre tests.
        builder.ConfigureTestServices(services => services.AddTransient<IStartupFilter, FakeRemoteIpStartupFilter>());
    }

    private sealed class FakeRemoteIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue("X-Test-Ip", out var ip) &&
                    System.Net.IPAddress.TryParse(ip.ToString(), out var address))
                {
                    context.Connection.RemoteIpAddress = address;
                }

                return nextMiddleware(context);
            });
            next(app);
        };
    }

    private static int ipCounter;

    /// <summary>Crea un cliente con IP simulada propia. Pasa una IP concreta para compartir cupo.</summary>
    public HttpClient CreateAnonymousClient(string? fakeIp = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var n = Interlocked.Increment(ref ipCounter);
        client.DefaultRequestHeaders.Add("X-Test-Ip", fakeIp ?? $"10.{(n >> 16) & 255}.{(n >> 8) & 255}.{n & 255}");
        return client;
    }

    /// <summary>Cliente con sesión iniciada. Se cachea por usuario para no agotar el límite de login.</summary>
    public async Task<HttpClient> LoginAsync(string username, string password)
    {
        lock (sessions)
        {
            if (sessions.TryGetValue(username, out var cached))
            {
                return cached;
            }
        }

        var client = CreateAnonymousClient();
        var response = await client.PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = username,
            ["Password"] = password
        });
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);

        lock (sessions)
        {
            sessions[username] = client;
        }
        return client;
    }

    public Task<HttpClient> AsAdmin() => LoginAsync("admin", AdminPassword);
    public Task<HttpClient> AsMechanic() => LoginAsync("mechanic", MechanicPassword);
    public Task<HttpClient> AsMechanic2() => LoginAsync("mechanic2", MechanicPassword);
    public Task<HttpClient> AsCustomer() => LoginAsync("customer", CustomerPassword);
    public Task<HttpClient> AsCustomer2() => LoginAsync("customer2", CustomerPassword);
    public Task<HttpClient> AsUser() => LoginAsync("user", UserPassword);

    /// <summary>Ejecuta código contra la base de datos de la aplicación.</summary>
    public async Task<T> WithDbAsync<T>(Func<ApplicationDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await action(context);
    }

    public Task WithDbAsync(Func<ApplicationDbContext, Task> action) =>
        WithDbAsync<object?>(async ctx => { await action(ctx); return null; });

    public Task<string> UserIdAsync(string username) =>
        WithDbAsync(ctx => ctx.Users.Where(u => u.UserName == username).Select(u => u.Id).SingleAsync());

    public async Task<Vehicle> CreateVehicleAsync(string ownerUsername, string? plate = null)
    {
        var ownerId = await UserIdAsync(ownerUsername);
        var vehicle = new Vehicle
        {
            LicensePlate = plate ?? $"T{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            VIN = Guid.NewGuid().ToString("N")[..17].ToUpperInvariant(),
            Brand = "Seat",
            Model = "Ibiza",
            Year = 2020,
            FuelType = FuelType.Petrol,
            Mileage = 50000,
            OwnerId = ownerId
        };
        await WithDbAsync(async ctx => { ctx.Vehicles.Add(vehicle); await ctx.SaveChangesAsync(); });
        return vehicle;
    }

    public async Task<SparePart> CreateSparePartAsync(int stock = 10, decimal price = 10m, int minStock = 2)
    {
        var part = new SparePart
        {
            Reference = $"REF-{Guid.NewGuid():N}"[..20],
            Name = "Recambio de prueba",
            Brand = "Test",
            UnitPrice = price,
            Stock = stock,
            MinStock = minStock
        };
        await WithDbAsync(async ctx => { ctx.SpareParts.Add(part); await ctx.SaveChangesAsync(); });
        return part;
    }

    public async Task<Repair> CreateRepairAsync(
        string ownerUsername,
        string mechanicUsername,
        RepairStatus status = RepairStatus.Pending,
        decimal laborHours = 0m)
    {
        var vehicle = await CreateVehicleAsync(ownerUsername);
        var mechanicId = await UserIdAsync(mechanicUsername);
        var repair = new Repair
        {
            VehicleId = vehicle.Id,
            MechanicId = mechanicId,
            RepairType = RepairType.OilChange,
            Status = status,
            Description = "Reparación de prueba",
            CompletedAt = status is RepairStatus.Completed or RepairStatus.Delivered ? DateTime.UtcNow : null,
            MileageAtEntry = 50000,
            LaborHours = laborHours,
            HourlyRate = 45m
        };
        await WithDbAsync(async ctx => { ctx.Repairs.Add(repair); await ctx.SaveChangesAsync(); });
        return repair;
    }

    /// <summary>Añade una pieza a una reparación usando la regla de dominio (descuenta stock).</summary>
    public async Task<RepairPart> AddPartAsync(int repairId, int sparePartId, int quantity)
    {
        return await WithDbAsync(async ctx =>
        {
            var repair = await ctx.Repairs.Include(r => r.RepairParts).SingleAsync(r => r.Id == repairId);
            var part = await ctx.SpareParts.SingleAsync(s => s.Id == sparePartId);
            var line = repair.AddPart(part, quantity);
            await ctx.SaveChangesAsync();
            return line;
        });
    }

    public Task<RepairPart> GetRepairPartAsync(int id) =>
        WithDbAsync(ctx => ctx.RepairParts.AsNoTracking().SingleAsync(p => p.Id == id));

    public Task<Repair> GetRepairAsync(int id) =>
        WithDbAsync(ctx => ctx.Repairs.Include(r => r.RepairParts).AsNoTracking().SingleAsync(r => r.Id == id));

    public Task<SparePart> GetSparePartAsync(int id) =>
        WithDbAsync(ctx => ctx.SpareParts.AsNoTracking().SingleAsync(s => s.Id == id));

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        foreach (var client in sessions.Values)
        {
            client.Dispose();
        }

        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(dbPath) + "*"))
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }
}
