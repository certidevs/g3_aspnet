using System.Net;
using G3AspNet.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Tests.Integration;

/// <summary>Valores límite de validación en servidor (justo dentro y justo fuera del rango).</summary>
public sealed class BoundaryValueTests : IClassFixture<AppFactory>
{
    private readonly AppFactory factory;

    public BoundaryValueTests(AppFactory factory) => this.factory = factory;

    // ---------- Vehículos ----------

    private async Task<bool> SaveVehicleAsync(Action<Dictionary<string, string>> tweak)
    {
        var form = new Dictionary<string, string>
        {
            ["Id"] = "0", ["LicensePlate"] = $"B{Guid.NewGuid().ToString("N")[..8]}", ["VIN"] = Guid.NewGuid().ToString("N")[..17],
            ["Brand"] = "Seat", ["Model"] = "Leon", ["Year"] = "2020", ["FuelType"] = nameof(FuelType.Petrol),
            ["Mileage"] = "100", ["OwnerId"] = await factory.UserIdAsync("customer")
        };
        tweak(form);
        var response = await (await factory.AsAdmin()).PostForm("/vehicle/save", form);
        return response.StatusCode == HttpStatusCode.Redirect;
    }

    [Theory]
    [InlineData("Year", "1950", true)]
    [InlineData("Year", "1949", false)]
    [InlineData("Year", "2100", true)]
    [InlineData("Year", "2101", false)]
    [InlineData("Mileage", "0", true)]
    [InlineData("Mileage", "-1", false)]
    [InlineData("Mileage", "2000000", true)]
    [InlineData("Mileage", "2000001", false)]
    [InlineData("LicensePlate", "ABCDEFGHIJ", true)]       // 10
    [InlineData("LicensePlate", "ABCDEFGHIJK", false)]     // 11
    [InlineData("VIN", "12345678901234567", true)]         // 17
    [InlineData("VIN", "123456789012345678", false)]       // 18
    [InlineData("Brand", "", false)]
    [InlineData("Model", "", false)]
    [InlineData("OwnerId", "", false)]
    public async Task Vehicle_FieldBoundaries(string field, string value, bool accepted)
    {
        var ok = await SaveVehicleAsync(f =>
        {
            f[field] = value;
            // Matrícula y VIN fijos pueden chocar entre ejecuciones: los hacemos únicos manteniendo la longitud.
        });

        Assert.Equal(accepted, ok);
    }

    [Fact]
    public async Task Vehicle_BrandLengthBoundary()
    {
        Assert.True(await SaveVehicleAsync(f => f["Brand"] = new string('a', 50)));
        Assert.False(await SaveVehicleAsync(f => f["Brand"] = new string('a', 51)));
    }

    [Fact]
    public async Task Vehicle_WhitespaceIsTrimmedOnSave()
    {
        var plate = $"W{Guid.NewGuid().ToString("N")[..3]}";
        await SaveVehicleAsync(f => f["LicensePlate"] = $"  {plate}  ");

        Assert.True(await factory.WithDbAsync(ctx => ctx.Vehicles.AnyAsync(v => v.LicensePlate == plate)));
    }

    // ---------- Recambios ----------

    private async Task<bool> SaveSparePartAsync(Action<Dictionary<string, string>> tweak)
    {
        var form = new Dictionary<string, string>
        {
            ["Id"] = "0", ["Reference"] = $"B-{Guid.NewGuid():N}"[..20], ["Name"] = "Pieza", ["Brand"] = "X",
            ["UnitPrice"] = "10", ["Stock"] = "5", ["MinStock"] = "1"
        };
        tweak(form);
        return (await (await factory.AsAdmin()).PostForm("/spareparts/save", form)).StatusCode == HttpStatusCode.Redirect;
    }

    [Theory]
    [InlineData("UnitPrice", "0", true)]
    [InlineData("UnitPrice", "-1", false)]
    [InlineData("UnitPrice", "100000", true)]
    [InlineData("UnitPrice", "100001", false)]
    [InlineData("Stock", "0", true)]
    [InlineData("Stock", "-1", false)]
    [InlineData("Stock", "100000", true)]
    [InlineData("Stock", "100001", false)]
    [InlineData("MinStock", "-1", false)]
    [InlineData("MinStock", "100001", false)]
    [InlineData("Name", "", false)]
    [InlineData("Reference", "", false)]
    public async Task SparePart_FieldBoundaries(string field, string value, bool accepted)
    {
        Assert.Equal(accepted, await SaveSparePartAsync(f => f[field] = value));
    }

    [Fact]
    public async Task SparePart_LengthBoundaries()
    {
        Assert.True(await SaveSparePartAsync(f => f["Name"] = new string('n', 100)));
        Assert.False(await SaveSparePartAsync(f => f["Name"] = new string('n', 101)));
        Assert.True(await SaveSparePartAsync(f => f["Reference"] = new string('r', 50)));
        Assert.False(await SaveSparePartAsync(f => f["Reference"] = new string('r', 51)));
    }

    [Fact]
    public async Task SparePart_ReferenceUniquenessIgnoresSurroundingSpaces()
    {
        var existing = await factory.CreateSparePartAsync();

        Assert.False(await SaveSparePartAsync(f => f["Reference"] = $"  {existing.Reference}  "));
    }

    [Fact]
    public async Task SparePart_EditKeepingOwnReferenceIsAllowed()
    {
        var existing = await factory.CreateSparePartAsync();

        var response = await (await factory.AsAdmin()).PostForm("/spareparts/save", new()
        {
            ["Id"] = existing.Id.ToString(), ["Reference"] = existing.Reference, ["Name"] = "Renombrada", ["Brand"] = "X",
            ["UnitPrice"] = "1", ["Stock"] = "1", ["MinStock"] = "1"
        });

        response.AssertRedirectTo("/SpareParts/Details/");
        Assert.Equal("Renombrada", (await factory.GetSparePartAsync(existing.Id)).Name);
    }

    [Fact]
    public async Task SparePart_EditUnknownId_Returns404()
    {
        var response = await (await factory.AsAdmin()).PostForm("/spareparts/save", new()
        {
            ["Id"] = "999999", ["Reference"] = "GHOST-1", ["Name"] = "x", ["Brand"] = "x",
            ["UnitPrice"] = "1", ["Stock"] = "1", ["MinStock"] = "1"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Reparaciones ----------

    private async Task<bool> CreateRepairAsync(Action<Dictionary<string, string>> tweak)
    {
        var vehicle = await factory.CreateVehicleAsync("customer");
        var form = new Dictionary<string, string>
        {
            ["Id"] = "0", ["VehicleId"] = vehicle.Id.ToString(), ["MechanicId"] = await factory.UserIdAsync("mechanic"),
            ["RepairType"] = nameof(RepairType.OilChange), ["Description"] = "Cambio de aceite",
            ["OpenedAt"] = "2026-03-01T09:00", ["MileageAtEntry"] = "1000", ["LaborHours"] = "1", ["HourlyRate"] = "45"
        };
        tweak(form);
        return (await (await factory.AsAdmin()).PostForm("/repairs/create", form)).StatusCode == HttpStatusCode.Redirect;
    }

    [Theory]
    [InlineData("LaborHours", "0", true)]
    [InlineData("LaborHours", "-1", false)]
    [InlineData("LaborHours", "1000", true)]
    [InlineData("LaborHours", "1001", false)]
    [InlineData("HourlyRate", "0", true)]
    [InlineData("HourlyRate", "-1", false)]
    [InlineData("HourlyRate", "1000", true)]
    [InlineData("HourlyRate", "1001", false)]
    [InlineData("MileageAtEntry", "0", true)]
    [InlineData("MileageAtEntry", "-1", false)]
    [InlineData("MileageAtEntry", "2000000", true)]
    [InlineData("MileageAtEntry", "2000001", false)]
    [InlineData("VehicleId", "0", false)]
    [InlineData("VehicleId", "-5", false)]
    public async Task Repair_FieldBoundaries(string field, string value, bool accepted)
    {
        Assert.Equal(accepted, await CreateRepairAsync(f => f[field] = value));
    }

    [Fact]
    public async Task Repair_DescriptionLengthBoundary()
    {
        Assert.True(await CreateRepairAsync(f => f["Description"] = new string('d', 500)));
        Assert.False(await CreateRepairAsync(f => f["Description"] = new string('d', 501)));
        Assert.False(await CreateRepairAsync(f => f["Description"] = "   "));
    }

    [Fact]
    public async Task Repair_AllRepairTypesAreAccepted()
    {
        foreach (var type in Enum.GetNames<RepairType>())
        {
            Assert.True(await CreateRepairAsync(f => f["RepairType"] = type), type);
        }
    }

    [Fact]
    public async Task Repair_InvalidEnumValueIsRejected()
    {
        Assert.False(await CreateRepairAsync(f => f["RepairType"] = "NoExiste"));
    }
}
