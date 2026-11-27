using System.Globalization;
using System.Reflection;
using System.Text;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace G3AspNet.Services.Invoices;

/// <summary>
/// Maqueta una factura en PDF con QuestPDF, con el mismo lenguaje visual que la portada
/// (sección «Página de inicio» de wwwroot/css/site.css): si cambian allí los colores, cámbialos aquí.
/// </summary>
public static class InvoicePdf
{
    // La factura se escribe siempre en español, sea cual sea la cultura del servidor.
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

    // Colores de la portada.
    private const string Red = "#ea271c";
    private const string LightRed = "#ff5a4f";
    private const string Dark = "#151515";
    private const string Ink = "#212529";
    private const string Muted = "#6c757d";
    private const string Soft = "#f8f9fa";
    private const string Line = "#dee2e6";
    private const string OnDark = "#cccccc";

    private const string TitleFont = "Oswald";
    private const string FallbackFont = "Lato"; // Incluida en QuestPDF.

    private static readonly string CarIllustration;

    static InvoicePdf()
    {
        // Un emoji o un alfabeto sin fuente se imprime como un recuadro; no debe impedir
        // descargar la factura.
        QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;

        var assembly = typeof(InvoicePdf).Assembly;

        // Las fuentes de la web van incrustadas: el PDF sale igual en cualquier servidor.
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith("Invoice.Fonts.")))
        {
            using var font = assembly.GetManifestResourceStream(name)!;
            FontManager.RegisterFontFromStream(font);
        }

        CarIllustration = ReadText(assembly, "Invoice.hero-taller.svg");
    }

    /// <summary>Devuelve el PDF como bytes, listo para descargar, guardar o adjuntar.</summary>
    public static byte[] Generate(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            // Lato (incluida en QuestPDF) cubre los caracteres que no tenga Roboto.
            page.DefaultTextStyle(style => style.FontFamily("Roboto", FallbackFont).FontSize(10).FontColor(Ink));

            page.Header().Column(column =>
            {
                column.Item().ShowOnce().Element(hero => ComposeHero(hero, invoice));
                column.Item().SkipOnce().Element(header => ComposeCompactHeader(header, invoice));
            });
            page.Content().PaddingVertical(18).Element(content => ComposeContent(content, invoice));
            page.Footer().Element(footer => ComposeFooter(footer, invoice));
        }))
        .WithMetadata(new DocumentMetadata { Title = $"Factura {invoice.Number}", Author = invoice.Issuer.IssuerName })
        .GeneratePdf();
    }

    // ---------- Cabecera: banda oscura con brillo rojo, ilustración y franja de seguridad

    private static void ComposeHero(IContainer container, Invoice invoice)
    {
        container.Layers(layers =>
        {
            layers.Layer().Svg(size => HeroBackground(size.Width, size.Height));

            layers.PrimaryLayer().PaddingBottom(StripeHeight).Row(row =>
            {
                row.RelativeItem().Padding(18).Column(brand =>
                {
                    brand.Item().AlignLeft().BorderLeft(3).BorderColor(Red).Background("#2b2b2b")
                        .PaddingVertical(3).PaddingHorizontal(8)
                        .Text("TALLER MECÁNICO · REVISIÓN Y REPARACIÓN")
                        .FontSize(7).Bold().FontColor(Colors.White).LetterSpacing(0.04f);

                    brand.Item().PaddingTop(8).Text(text => BrandName(text, invoice.Issuer.IssuerName, 28));

                    if (!string.IsNullOrWhiteSpace(invoice.Issuer.IssuerTaxId))
                    {
                        brand.Item().Text($"NIF: {invoice.Issuer.IssuerTaxId}").FontColor(OnDark);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.Issuer.IssuerAddress))
                    {
                        brand.Item().Text(invoice.Issuer.IssuerAddress).FontColor(OnDark);
                    }
                });

                row.ConstantItem(140).AlignBottom().Svg(CarIllustration);

                row.ConstantItem(125).PaddingVertical(18).PaddingRight(18).Column(meta =>
                {
                    meta.Item().AlignRight().Text("FACTURA")
                        .FontFamily(TitleFont).Bold().FontSize(24).FontColor(Colors.White);
                    meta.Item().AlignRight().Text(invoice.Number)
                        .FontFamily(TitleFont).SemiBold().FontSize(13).FontColor(LightRed);
                    meta.Item().PaddingTop(4).AlignRight()
                        .Text($"Fecha: {invoice.IssueDate.ToString("dd/MM/yyyy", Spanish)}").FontColor(OnDark);
                });
            });
        });
    }

    private static void ComposeCompactHeader(IContainer container, Invoice invoice)
    {
        container.Background(Dark).CornerRadius(6).PaddingVertical(8).PaddingHorizontal(14).Row(row =>
        {
            row.RelativeItem().Text(text => BrandName(text, invoice.Issuer.IssuerName, 14));
            row.AutoItem().AlignMiddle().Text($"Factura {invoice.Number}").FontColor(OnDark);
        });
    }

    /// <summary>Nombre del taller en mayúsculas, con la última palabra en rojo.</summary>
    private static void BrandName(TextDescriptor text, string name, float size)
    {
        var upper = name.ToUpper(Spanish);
        var split = upper.LastIndexOf(' ');
        text.DefaultTextStyle(style => style.FontFamily(TitleFont, FallbackFont).Bold().FontSize(size));

        if (split < 0)
        {
            text.Span(upper).FontColor(Colors.White);
            return;
        }

        text.Span(upper[..(split + 1)]).FontColor(Colors.White);
        text.Span(upper[(split + 1)..]).FontColor(LightRed);
    }

    // ---------- Contenido

    private static void ComposeContent(IContainer container, Invoice invoice)
    {
        container.Column(column =>
        {
            column.Spacing(14);

            // Tarjetas con borde rojo, como los contadores de la portada.
            column.Item().Row(row =>
            {
                row.Spacing(10);
                row.RelativeItem(4).Element(card => Card(card, "Cliente", invoice.CustomerName, invoice.CustomerEmail));
                row.RelativeItem(4).Element(card => Card(card, "Vehículo", invoice.Vehicle, null));
                row.RelativeItem(3).Element(card => SideBorderCard(card).Column(total =>
                {
                    total.Item().Text("TOTAL A PAGAR").Style(Label());
                    var amount = Money(invoice.Total);
                    // Los importes muy largos se reducen para que no se parta la cifra.
                    total.Item().Text(amount).FontFamily(TitleFont).Bold().FontSize(amount.Length > 11 ? 16 : 20);
                }));
            });

            column.Item().BorderTop(3).BorderColor(Red).Background(Soft).Padding(10).Column(repair =>
            {
                repair.Item().Text("REPARACIÓN").Style(Label());
                repair.Item().Text(invoice.RepairDescription);
            });

            column.Item().Element(heading => SectionHeading(heading, "Conceptos"));
            column.Item().Element(table => ComposeTable(table, invoice));

            // ShowEntire evita que los totales queden partidos entre dos páginas.
            column.Item().ShowEntire().AlignRight().Width(240).Element(totals => ComposeTotals(totals, invoice));
        });
    }

    private static TextStyle Label() =>
        TextStyle.Default.FontFamily(TitleFont).SemiBold().FontSize(9).FontColor(Muted);

    private static IContainer SideBorderCard(IContainer container) =>
        container.BorderLeft(4).BorderColor(Red).Background(Soft).Padding(10);

    private static void Card(IContainer container, string title, string main, string? detail) =>
        SideBorderCard(container).Column(card =>
        {
            card.Item().Text(title.ToUpper(Spanish)).Style(Label());
            card.Item().Text(main).Bold();

            if (!string.IsNullOrWhiteSpace(detail))
            {
                card.Item().Text(detail).FontColor(Muted).FontSize(9);
            }
        });

    private static void SectionHeading(IContainer container, string title) =>
        container.Row(row =>
        {
            row.Spacing(10);
            row.AutoItem().Text(title.ToUpper(Spanish)).FontFamily(TitleFont).Bold().FontSize(15);
            row.RelativeItem().AlignMiddle().Height(3).Svg(size => FadingRule(size.Width, size.Height));
        });

    private static void ComposeTable(IContainer container, Invoice invoice)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.ConstantColumn(65);
                columns.ConstantColumn(80);
                columns.ConstantColumn(85);
            });

            // La cabecera se repite si la factura ocupa varias páginas.
            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("CONCEPTO");
                header.Cell().Element(HeaderCell).AlignRight().Text("CANTIDAD");
                header.Cell().Element(HeaderCell).AlignRight().Text("PRECIO");
                header.Cell().Element(HeaderCell).AlignRight().Text("IMPORTE");
            });

            foreach (var line in invoice.Lines)
            {
                table.Cell().Element(BodyCell).Text(line.Concept);
                table.Cell().Element(BodyCell).AlignRight().Text(line.Quantity.ToString("0.##", Spanish));
                table.Cell().Element(BodyCell).AlignRight().Text(Money(line.UnitPrice));
                table.Cell().Element(BodyCell).AlignRight().Text(Money(line.Amount)).Bold();
            }
        });

        static IContainer HeaderCell(IContainer cell) =>
            cell.Background(Dark).PaddingVertical(6).PaddingHorizontal(8)
                .DefaultTextStyle(style => style.FontFamily(TitleFont).SemiBold().FontColor(Colors.White));

        static IContainer BodyCell(IContainer cell) =>
            cell.BorderBottom(1).BorderColor(Line).PaddingVertical(6).PaddingHorizontal(8);
    }

    /// <summary>Bloque oscuro de la portada con el importe final destacado.</summary>
    private static void ComposeTotals(IContainer container, Invoice invoice)
    {
        container.Background(Dark).CornerRadius(8).Padding(14).Column(totals =>
        {
            TotalRow(totals, "Base imponible", invoice.TaxBase);
            TotalRow(totals, $"IVA ({invoice.Issuer.VatRate.ToString("0.##", Spanish)} %)", invoice.Vat);
            totals.Item().PaddingVertical(5).LineHorizontal(2).LineColor(Red);
            totals.Item().Row(row =>
            {
                row.RelativeItem().AlignMiddle().Text("TOTAL")
                    .FontFamily(TitleFont).Bold().FontSize(15).FontColor(Colors.White);
                row.AutoItem().Text(Money(invoice.Total))
                    .FontFamily(TitleFont).Bold().FontSize(22).FontColor(LightRed);
            });
        });
    }

    private static void TotalRow(ColumnDescriptor column, string label, decimal amount) =>
        column.Item().PaddingBottom(4).Row(row =>
        {
            row.RelativeItem().Text(label).FontColor(OnDark);
            row.AutoItem().Text(Money(amount)).FontColor(Colors.White);
        });

    private static void ComposeFooter(IContainer container, Invoice invoice)
    {
        container.Column(column =>
        {
            column.Item().Height(StripeHeight).Svg(size => Svg(size.Width, size.Height, Stripes(size.Width, 0, size.Height)));
            column.Item().PaddingTop(5).DefaultTextStyle(style => style.FontSize(8).FontColor(Muted)).Row(row =>
            {
                row.RelativeItem().Text(invoice.Issuer.IssuerName);
                row.AutoItem().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                    text.Span(" de ");
                    text.TotalPages();
                });
            });
        });
    }

    // Espacio de no separación: el símbolo del euro no salta de línea solo.
    private static string Money(decimal amount) => amount.ToString("N2", Spanish) + "\u00a0€";

    // ---------- Dibujos en SVG. Los números van siempre con cultura invariable (punto decimal).

    private const float StripeHeight = 7;

    private static string Svg(float width, float height, string content) => FormattableString.Invariant(
        $"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">{content}</svg>""");

    /// <summary>Franja diagonal roja y negra (.taller-stripe).</summary>
    private static string Stripes(float width, float top, float height)
    {
        const float step = 11;
        var shapes = new StringBuilder(FormattableString.Invariant(
            $"""<rect x="0" y="{top}" width="{width}" height="{height}" fill="#111111"/>"""));

        for (var x = -height; x < width; x += step * 2)
        {
            // Se recorta a mano para no salirse del ancho disponible.
            float Clamp(float value) => Math.Clamp(value, 0, width);
            shapes.Append(FormattableString.Invariant(
                $"""<polygon points="{Clamp(x)},{top + height} {Clamp(x + height)},{top} {Clamp(x + height + step)},{top} {Clamp(x + step)},{top + height}" fill="{Red}"/>"""));
        }

        return shapes.ToString();
    }

    /// <summary>Fondo de la cabecera: degradado oscuro, brillo rojo, franja y esquinas redondeadas.</summary>
    private static string HeroBackground(float width, float height) => Svg(width, height, FormattableString.Invariant(
        $"""
         <defs>
           <linearGradient id="base" x1="0" y1="0" x2="1" y2="1">
             <stop offset="0" stop-color="#0e0e0e"/><stop offset="1" stop-color="#232323"/>
           </linearGradient>
           <radialGradient id="glow" cx="{width * 0.8f}" cy="{height * 0.3f}" r="{width * 0.5f}" gradientUnits="userSpaceOnUse">
             <stop offset="0" stop-color="{Red}" stop-opacity="0.35"/><stop offset="1" stop-color="{Red}" stop-opacity="0"/>
           </radialGradient>
           <clipPath id="round"><rect width="{width}" height="{height}" rx="10"/></clipPath>
         </defs>
         <g clip-path="url(#round)">
           <rect width="{width}" height="{height}" fill="url(#base)"/>
           <rect width="{width}" height="{height}" fill="url(#glow)"/>
           {Stripes(width, height - StripeHeight, StripeHeight)}
         </g>
         """));

    /// <summary>Línea roja que se desvanece junto a los títulos (.taller-rule).</summary>
    private static string FadingRule(float width, float height) => Svg(width, height, FormattableString.Invariant(
        $"""
         <defs><linearGradient id="fade" x1="0" y1="0" x2="1" y2="0">
           <stop offset="0" stop-color="{Red}"/><stop offset="1" stop-color="{Red}" stop-opacity="0"/>
         </linearGradient></defs>
         <rect width="{width}" height="{height}" fill="url(#fade)"/>
         """));

    private static string ReadText(Assembly assembly, string resource)
    {
        using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Falta el recurso incrustado {resource}."));
        return reader.ReadToEnd();
    }
}
