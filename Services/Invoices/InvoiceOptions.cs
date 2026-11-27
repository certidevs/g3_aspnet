namespace G3AspNet.Services.Invoices;

/// <summary>Datos del taller que emite las facturas. Se leen de la sección <c>Invoice</c>.</summary>
public sealed class InvoiceOptions
{
    /// <summary>Porcentaje de IVA que se suma a los precios de la aplicación.</summary>
    public decimal VatRate { get; set; } = 21m;

    public string IssuerName { get; set; } = "TALLER .NET";

    public string IssuerTaxId { get; set; } = string.Empty;

    public string IssuerAddress { get; set; } = string.Empty;
}
