using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace G3AspNet.Services.Invoices;

/// <summary>Maqueta una factura en PDF con QuestPDF.</summary>
public static class InvoicePdf
{
    // La factura se escribe siempre en español, sea cual sea la cultura del servidor.
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");
    private const string Accent = "#c4161c";

    /// <summary>Devuelve el PDF como bytes, listo para descargar, guardar o adjuntar.</summary>
    public static byte[] Generate(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontSize(10));

            page.Header().Element(header => ComposeHeader(header, invoice));
            page.Content().PaddingVertical(20).Element(content => ComposeContent(content, invoice));
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
        }))
        .WithMetadata(new DocumentMetadata { Title = $"Factura {invoice.Number}", Author = invoice.Issuer.IssuerName })
        .GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, Invoice invoice)
    {
        container.BorderBottom(2).BorderColor(Accent).PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(invoice.Issuer.IssuerName).FontSize(18).Bold().FontColor(Accent);

                if (!string.IsNullOrWhiteSpace(invoice.Issuer.IssuerTaxId))
                {
                    column.Item().Text($"NIF: {invoice.Issuer.IssuerTaxId}");
                }

                if (!string.IsNullOrWhiteSpace(invoice.Issuer.IssuerAddress))
                {
                    column.Item().Text(invoice.Issuer.IssuerAddress);
                }
            });

            row.ConstantItem(180).AlignRight().Column(column =>
            {
                column.Item().AlignRight().Text("FACTURA").FontSize(18).Bold();
                column.Item().AlignRight().Text(invoice.Number);
                column.Item().AlignRight().Text($"Fecha: {invoice.IssueDate.ToString("dd/MM/yyyy", Spanish)}");
            });
        });
    }

    private static void ComposeContent(IContainer container, Invoice invoice)
    {
        container.Column(column =>
        {
            column.Spacing(15);

            column.Item().Row(row =>
            {
                row.Spacing(20);
                row.RelativeItem().Column(customer =>
                {
                    customer.Item().Text("Cliente").SemiBold();
                    customer.Item().Text(invoice.CustomerName);
                    customer.Item().Text(invoice.CustomerEmail);
                });
                row.RelativeItem().Column(vehicle =>
                {
                    vehicle.Item().Text("Vehículo").SemiBold();
                    vehicle.Item().Text(invoice.Vehicle);
                    vehicle.Item().Text(invoice.RepairDescription);
                });
            });

            column.Item().Element(table => ComposeTable(table, invoice));

            // ShowEntire evita que los totales queden partidos entre dos páginas.
            column.Item().ShowEntire().AlignRight().Width(220).Column(totals =>
            {
                Total(totals, "Base imponible", invoice.TaxBase);
                Total(totals, $"IVA ({invoice.Issuer.VatRate.ToString("0.##", Spanish)} %)", invoice.Vat);
                totals.Item().BorderTop(1).PaddingTop(4).DefaultTextStyle(style => style.Bold().FontSize(13))
                    .Row(row =>
                    {
                        row.RelativeItem().Text("Total");
                        row.AutoItem().Text(Money(invoice.Total));
                    });
            });
        });
    }

    private static void ComposeTable(IContainer container, Invoice invoice)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.ConstantColumn(60);
                columns.ConstantColumn(80);
                columns.ConstantColumn(80);
            });

            // La cabecera se repite si la factura ocupa varias páginas.
            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Concepto");
                header.Cell().Element(HeaderCell).AlignRight().Text("Cantidad");
                header.Cell().Element(HeaderCell).AlignRight().Text("Precio");
                header.Cell().Element(HeaderCell).AlignRight().Text("Importe");
            });

            foreach (var line in invoice.Lines)
            {
                table.Cell().Element(BodyCell).Text(line.Concept);
                table.Cell().Element(BodyCell).AlignRight().Text(line.Quantity.ToString("0.##", Spanish));
                table.Cell().Element(BodyCell).AlignRight().Text(Money(line.UnitPrice));
                table.Cell().Element(BodyCell).AlignRight().Text(Money(line.Amount));
            }
        });

        static IContainer HeaderCell(IContainer cell) =>
            cell.BorderBottom(1).PaddingVertical(5).PaddingHorizontal(3).DefaultTextStyle(style => style.SemiBold());

        static IContainer BodyCell(IContainer cell) =>
            cell.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(3);
    }

    private static void Total(ColumnDescriptor column, string label, decimal amount) =>
        column.Item().PaddingBottom(4).Row(row =>
        {
            row.RelativeItem().Text(label);
            row.AutoItem().Text(Money(amount));
        });

    private static string Money(decimal amount) => amount.ToString("N2", Spanish) + " €";
}
