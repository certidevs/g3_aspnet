using System.Text;
using QRCoder;

namespace G3AspNet.Utilities;

/// <summary>Genera códigos QR en el servidor, sin enviar su contenido a servicios externos.</summary>
public static class QrCodeDataUri
{
    /// <summary>Devuelve el contenido como un QR en SVG, listo para el <c>src</c> de una imagen.</summary>
    public static string FromText(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new SvgQRCode(data);

        // Negro sobre blanco con margen: se puede escanear también con el tema oscuro.
        var svg = qrCode.GetGraphic(
            pixelsPerModule: 8,
            darkColorHex: "#000000",
            lightColorHex: "#ffffff",
            sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute);

        return $"data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(svg))}";
    }
}
