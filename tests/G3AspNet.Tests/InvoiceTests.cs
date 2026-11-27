using System.Text;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
using G3AspNet.Services.Invoices;
using QuestPDF.Infrastructure;

namespace G3AspNet.Tests;

public sealed class InvoiceTests
{
    public InvoiceTests() => QuestPDF.Settings.License = LicenseType.Community;

    private static Repair ClosedRepair(decimal hours = 1.25m, decimal rate = 45.50m)
    {
        var repair = new Repair
        {
            Id = 7, Status = RepairStatus.Completed, RepairType = RepairType.OilChange, Description = "Aceite y filtro",
            CompletedAt = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc), LaborHours = hours, HourlyRate = rate,
            Vehicle = new Vehicle
            {
                LicensePlate = "1234ABC", Brand = "Renault", Model = "Clio",
                Owner = new ApplicationUser { DisplayName = "Cliente Demo", Email = "cliente@taller.test" }
            }
        };
        repair.RepairParts.Add(new RepairPart
        {
            Id = 1, Quantity = 2, UnitPrice = 36.90m,
            SparePart = new SparePart { Name = "Aceite 5W30", Reference = "CAS-5L", UnitPrice = 99m }
        });
        return repair;
    }

    [Fact]
    public void FromRepair_CalculatesLinesVatAndTotal_WithHistoricalPrices()
    {
        var invoice = Invoice.FromRepair(ClosedRepair(), new InvoiceOptions { VatRate = 21m });

        Assert.Equal("F-2026-000007", invoice.Number);
        Assert.Equal([56.88m, 73.80m], invoice.Lines.Select(line => line.Amount)); // 56,875 redondea hacia arriba.
        Assert.Equal(130.68m, invoice.TaxBase);
        Assert.Equal(27.44m, invoice.Vat);
        Assert.Equal(158.12m, invoice.Total);
    }

    [Fact]
    public void FromRepair_WithoutLabor_OmitsTheLaborLine()
    {
        var invoice = Invoice.FromRepair(ClosedRepair(hours: 0m), new InvoiceOptions());

        Assert.Single(invoice.Lines);
    }

    [Theory]
    [InlineData(RepairStatus.Pending)]
    [InlineData(RepairStatus.InProgress)]
    [InlineData(RepairStatus.WaitingParts)]
    public void FromRepair_OpenRepair_Throws(RepairStatus status)
    {
        var repair = ClosedRepair();
        repair.Status = status;

        Assert.Throws<InvalidOperationException>(() => Invoice.FromRepair(repair, new InvoiceOptions()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(150)] // Varias páginas.
    public void Generate_ReturnsAPdf(int extraLines)
    {
        var invoice = Invoice.FromRepair(ClosedRepair(), new InvoiceOptions());
        invoice = invoice with
        {
            Lines = [.. invoice.Lines, .. Enumerable.Range(1, extraLines)
                .Select(i => new InvoiceLine($"Pieza {i} " + new string('x', 150), 1, 1m))]
        };

        var pdf = InvoicePdf.Generate(invoice);

        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
    }
}
