using G3AspNet.Models;

namespace G3AspNet.Tests;

/// <summary>Casos límite de las reglas de dominio (Repair.AddPart, TotalCost, SparePart.IsLowStock).</summary>
public sealed class DomainEdgeCaseTests
{
    private static SparePart Part(int stock, decimal price = 10m, int minStock = 0) =>
        new() { Id = 7, Name = "Pieza", Stock = stock, UnitPrice = price, MinStock = minStock };

    [Fact]
    public void AddPart_ExactlyTheStock_LeavesZero()
    {
        var part = Part(stock: 3);

        new Repair().AddPart(part, 3);

        Assert.Equal(0, part.Stock);
    }

    [Fact]
    public void AddPart_OneMoreThanStock_Throws_AndKeepsStockAndParts()
    {
        var part = Part(stock: 3);
        var repair = new Repair();

        Assert.Throws<InvalidOperationException>(() => repair.AddPart(part, 4));

        Assert.Equal(3, part.Stock);
        Assert.Empty(repair.RepairParts);
    }

    [Fact]
    public void AddPart_FromEmptyStock_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new Repair().AddPart(Part(stock: 0), 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void AddPart_NonPositiveQuantity_Throws_AndKeepsStock(int quantity)
    {
        var part = Part(stock: 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Repair().AddPart(part, quantity));

        Assert.Equal(5, part.Stock);
    }

    [Fact]
    public void AddPart_NullSparePart_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new Repair().AddPart(null!, 1));
    }

    [Fact]
    public void AddPart_UsesCatalogPrice_ButExplicitPriceWins_EvenZero()
    {
        var repair = new Repair();

        var byCatalog = repair.AddPart(Part(10, price: 20m), 1);
        var explicitPrice = repair.AddPart(Part(10, price: 20m), 1, unitPrice: 5m);
        var free = repair.AddPart(Part(10, price: 20m), 1, unitPrice: 0m);

        Assert.Equal(20m, byCatalog.UnitPrice);
        Assert.Equal(5m, explicitPrice.UnitPrice);
        Assert.Equal(0m, free.UnitPrice);
    }

    [Fact]
    public void AddPart_HistoricalPriceDoesNotChangeWhenCatalogPriceChanges()
    {
        var part = Part(10, price: 20m);
        var line = new Repair().AddPart(part, 1);

        part.UnitPrice = 99m;

        Assert.Equal(20m, line.UnitPrice);
    }

    [Fact]
    public void AddPart_SamePartTwice_AccumulatesStockDeduction()
    {
        var part = Part(10);
        var repair = new Repair();

        repair.AddPart(part, 4);
        repair.AddPart(part, 6);

        Assert.Equal(0, part.Stock);
        Assert.Equal(2, repair.RepairParts.Count);
    }

    [Fact]
    public void AddPart_LinksBothSides()
    {
        var part = Part(10);
        var repair = new Repair { Id = 3 };

        var line = repair.AddPart(part, 2);

        Assert.Same(repair, line.Repair);
        Assert.Same(part, line.SparePart);
        Assert.Equal(3, line.RepairId);
        Assert.Equal(7, line.SparePartId);
        Assert.Contains(line, repair.RepairParts);
    }

    [Fact]
    public void TotalCost_EmptyRepair_IsZero()
    {
        Assert.Equal(0m, new Repair().TotalCost);
    }

    [Fact]
    public void TotalCost_OnlyParts_OnlyLabor_AndFreeLabor()
    {
        var onlyLabor = new Repair { LaborHours = 3m, HourlyRate = 50m };
        var onlyParts = new Repair();
        onlyParts.AddPart(Part(10, 12.5m), 4);
        var freeLabor = new Repair { LaborHours = 5m, HourlyRate = 0m };

        Assert.Equal(150m, onlyLabor.TotalCost);
        Assert.Equal(50m, onlyParts.TotalCost);
        Assert.Equal(0m, freeLabor.TotalCost);
    }

    [Fact]
    public void TotalCost_KeepsCentsPrecision()
    {
        var repair = new Repair { LaborHours = 0.25m, HourlyRate = 45m }; // 11.25
        repair.AddPart(Part(10, 14.50m), 3);                               // 43.50

        Assert.Equal(54.75m, repair.TotalCost);
    }

    [Fact]
    public void TotalCost_MaximumValidInputs_DoesNotOverflow()
    {
        var repair = new Repair { LaborHours = 1000m, HourlyRate = 1000m };
        repair.AddPart(Part(1000, 100000m), 1000);

        Assert.Equal(1_000_000m + 100_000_000m, repair.TotalCost);
    }

    [Theory]
    [InlineData(5, 5, true)]   // igual al mínimo: hay que pedir
    [InlineData(4, 5, true)]
    [InlineData(6, 5, false)]  // justo por encima
    [InlineData(0, 0, true)]
    [InlineData(1, 0, false)]
    public void IsLowStock_Boundaries(int stock, int minStock, bool expected)
    {
        Assert.Equal(expected, Part(stock, minStock: minStock).IsLowStock);
    }

    [Fact]
    public void IsLowStock_BecomesTrueAfterConsumingStock()
    {
        var part = Part(stock: 6, minStock: 5);
        Assert.False(part.IsLowStock);

        new Repair().AddPart(part, 1);

        Assert.True(part.IsLowStock);
    }
}
