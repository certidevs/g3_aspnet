using G3AspNet.Data;
using G3AspNet.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Tests;

public sealed class RepairRelationshipTests
{
    [Fact]
    public void Schema_EnforcesVehicleAndMechanicRelationships()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);
        context.Database.EnsureCreated();

        var mechanic = new ApplicationUser { Id = "mechanic-1", UserName = "mechanic" };
        var vehicle = new Vehicle { LicensePlate = "1234 ABC", VIN = "123", Brand = "Seat", Model = "Ibiza", OwnerId = mechanic.Id };
        context.Add(mechanic);
        context.Add(vehicle);
        context.SaveChanges();

        var repair = new Repair { VehicleId = vehicle.Id, MechanicId = mechanic.Id };
        context.Add(repair);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var loaded = context.Repairs.Include(r => r.Vehicle).Include(r => r.Mechanic).Single();
        Assert.Equal("1234 ABC", loaded.Vehicle.LicensePlate);
        Assert.Equal("mechanic", loaded.Mechanic.UserName);

        context.Add(new Repair { VehicleId = vehicle.Id, MechanicId = "missing" });
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        context.ChangeTracker.Clear();

        context.Add(new Repair { VehicleId = -1, MechanicId = mechanic.Id });
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
        context.ChangeTracker.Clear();

        context.Remove(context.Vehicles.Single());
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Schema_RejectsSqlRepairsWithInvalidReferences(bool validVehicle, bool validMechanic)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var context = CreateContext(connection);
        context.Database.EnsureCreated();

        var mechanic = new ApplicationUser { Id = "mechanic-1", UserName = "mechanic" };
        var vehicle = new Vehicle { LicensePlate = "1234 ABC", VIN = "123", Brand = "Seat", Model = "Ibiza", OwnerId = mechanic.Id };
        context.Add(mechanic);
        context.Add(vehicle);
        context.SaveChanges();

        var vehicleId = validVehicle ? vehicle.Id : 999;
        var mechanicId = validMechanic ? mechanic.Id : "missing";

        // SQL directo: la base debe rechazar la referencia aunque no se pase por las entidades.
        var error = Assert.Throws<SqliteException>(() => context.Database.ExecuteSql($"""
            INSERT INTO Repairs (VehicleId, MechanicId, RepairType, Status, Description, OpenedAt, MileageAtEntry, LaborHours, HourlyRate)
            VALUES ({vehicleId}, {mechanicId}, 0, 0, 'Prueba', '2026-01-01', 0, '0', '0')
            """));

        const int foreignKeyViolation = 787; // SQLITE_CONSTRAINT_FOREIGNKEY
        Assert.Equal(foreignKeyViolation, error.SqliteExtendedErrorCode);
        Assert.Empty(context.Repairs);
    }

    private static ApplicationDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        return new ApplicationDbContext(options);
    }
}
