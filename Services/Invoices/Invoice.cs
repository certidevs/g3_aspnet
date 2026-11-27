using System.ComponentModel.DataAnnotations;
using System.Reflection;
using G3AspNet.Models;

namespace G3AspNet.Services.Invoices;

/// <summary>Un concepto facturado. El importe se redondea a céntimos línea a línea.</summary>
public sealed record InvoiceLine(string Concept, decimal Quantity, decimal UnitPrice)
{
    public decimal Amount => Invoice.Round(Quantity * UnitPrice);
}

/// <summary>Contenido de una factura y sus totales, sin nada relativo al diseño del PDF.</summary>
public sealed record Invoice(
    string Number,
    DateTime IssueDate,
    InvoiceOptions Issuer,
    string CustomerName,
    string CustomerEmail,
    string Vehicle,
    string RepairDescription,
    IReadOnlyList<InvoiceLine> Lines)
{
    /// <summary>Base imponible: suma de las líneas.</summary>
    public decimal TaxBase => Lines.Sum(line => line.Amount);

    public decimal Vat => Round(TaxBase * Issuer.VatRate / 100m);

    public decimal Total => TaxBase + Vat;

    public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Prepara la factura de una reparación cerrada. Deben estar cargados el vehículo, su
    /// propietario y las piezas con su recambio.
    /// </summary>
    public static Invoice FromRepair(Repair repair, InvoiceOptions issuer)
    {
        ArgumentNullException.ThrowIfNull(repair);
        ArgumentNullException.ThrowIfNull(issuer);

        if (!repair.IsClosed)
        {
            throw new InvalidOperationException("Solo se pueden facturar reparaciones completadas o entregadas.");
        }

        var issueDate = (repair.CompletedAt ?? repair.OpenedAt).ToLocalTime();
        var lines = new List<InvoiceLine>();

        if (repair.LaborHours > 0)
        {
            lines.Add(new InvoiceLine("Mano de obra (horas)", repair.LaborHours, repair.HourlyRate));
        }

        // El precio es el guardado en la línea (histórico), no el actual del catálogo.
        lines.AddRange(repair.RepairParts
            .OrderBy(part => part.Id)
            .Select(part => new InvoiceLine(
                $"{part.SparePart.Name} (ref. {part.SparePart.Reference})", part.Quantity, part.UnitPrice)));

        var vehicle = repair.Vehicle;
        var owner = vehicle.Owner;
        var type = typeof(Models.Enum.RepairType).GetField(repair.RepairType.ToString())?
            .GetCustomAttribute<DisplayAttribute>()?.Name ?? repair.RepairType.ToString();

        return new Invoice(
            Number: $"F-{issueDate:yyyy}-{repair.Id:D6}",
            IssueDate: issueDate,
            Issuer: issuer,
            CustomerName: owner.DisplayName ?? owner.UserName ?? string.Empty,
            CustomerEmail: owner.Email ?? string.Empty,
            Vehicle: $"{vehicle.LicensePlate} · {vehicle.Brand} {vehicle.Model}",
            RepairDescription: $"{type}: {repair.Description}",
            Lines: lines);
    }
}
