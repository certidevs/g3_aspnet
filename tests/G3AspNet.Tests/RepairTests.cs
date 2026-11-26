using System.ComponentModel.DataAnnotations;
using G3AspNet.Models;
using G3AspNet.Models.Enum;

namespace G3AspNet.Tests;

public sealed class RepairTests
{
    [Fact]
    public void NewRepair_DefaultsToPendingWithUtcOpenedAt()
    {
        var repair = new Repair();

        Assert.Equal(RepairStatus.Pending, repair.Status);
        Assert.Equal(DateTimeKind.Utc, repair.OpenedAt.Kind);
    }

    [Fact]
    public void Repair_ValidatesDescriptionAndNumericRanges()
    {
        var repair = new Repair
        {
            Description = new string('a', 501),
            MileageAtEntry = 2000001,
            LaborHours = 1001,
            HourlyRate = 1001
        };
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            repair,
            new ValidationContext(repair),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(Repair.Description)));
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(Repair.MileageAtEntry)));
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(Repair.LaborHours)));
        Assert.Contains(validationResults, result => result.MemberNames.Contains(nameof(Repair.HourlyRate)));
    }

    [Fact]
    public void TotalCost_AddsLaborAndAllPartCosts()
    {
        var repair = new Repair
        {
            LaborHours = 2.5m,
            HourlyRate = 40m,
            RepairParts =
            [
                new RepairPart { Quantity = 2, UnitPrice = 12.50m },
                new RepairPart { Quantity = 1, UnitPrice = 15m }
            ]
        };

        Assert.Equal(140m, repair.TotalCost);
    }

    [Fact]
    public void TotalCost_WithNoParts_ReturnsLaborCost()
    {
        var repair = new Repair { LaborHours = 1.5m, HourlyRate = 40m };

        Assert.Equal(60m, repair.TotalCost);
    }

    [Fact]
    public void TotalCost_ReflectsChangesToLaborAndParts()
    {
        var part = new RepairPart { Quantity = 1, UnitPrice = 10m };
        var repair = new Repair { LaborHours = 1m, HourlyRate = 20m, RepairParts = [part] };

        Assert.Equal(30m, repair.TotalCost);

        repair.LaborHours = 2m;
        part.Quantity = 3;

        Assert.Equal(70m, repair.TotalCost);
    }

    [Fact]
    public void AddPart_DeductsQuantityFromSparePartStock_AndAddsRepairPart()
    {
        var sparePart = new SparePart
        {
            Id = 5,
            Name = "Filtro de aceite",
            Stock = 10,
            UnitPrice = 15.50m
        };
        var repair = new Repair { Id = 1, LaborHours = 1m, HourlyRate = 40m };

        var addedPart = repair.AddPart(sparePart, quantity: 3);

        Assert.Equal(7, sparePart.Stock);
        Assert.Single(repair.RepairParts);
        Assert.Same(addedPart, repair.RepairParts.Single());
        Assert.Equal(3, addedPart.Quantity);
        Assert.Equal(15.50m, addedPart.UnitPrice);
        Assert.Equal(5, addedPart.SparePartId);
        Assert.Equal(86.50m, repair.TotalCost);
    }

    [Fact]
    public void AddPart_WhenInsufficientStock_ThrowsInvalidOperationException_AndDoesNotModifyStock()
    {
        var sparePart = new SparePart { Id = 1, Stock = 2, UnitPrice = 10m };
        var repair = new Repair();

        Assert.Throws<InvalidOperationException>(() => repair.AddPart(sparePart, quantity: 3));
        Assert.Equal(2, sparePart.Stock);
        Assert.Empty(repair.RepairParts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void AddPart_WithNonPositiveQuantity_ThrowsArgumentOutOfRangeException(int invalidQuantity)
    {
        var sparePart = new SparePart { Id = 1, Stock = 10, UnitPrice = 10m };
        var repair = new Repair();

        Assert.Throws<ArgumentOutOfRangeException>(() => repair.AddPart(sparePart, invalidQuantity));
        Assert.Equal(10, sparePart.Stock);
        Assert.Empty(repair.RepairParts);
    }
}
