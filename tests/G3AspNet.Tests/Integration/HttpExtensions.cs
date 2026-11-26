using System.Net;
using System.Text.RegularExpressions;

namespace G3AspNet.Tests.Integration;

public static partial class HttpExtensions
{
    [GeneratedRegex(@"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""|<input[^>]*value=""([^""]+)""[^>]*name=""__RequestVerificationToken""")]
    private static partial Regex TokenRegex();

    /// <summary>Descarga una página y extrae el token antiforgery (y deja la cookie en el cliente).</summary>
    public static async Task<string> GetAntiforgeryTokenAsync(this HttpClient client, string pageUrl)
    {
        var html = await client.GetStringAsync(pageUrl);
        var match = TokenRegex().Match(html);
        Assert.True(match.Success, $"No hay token antiforgery en {pageUrl}.");
        return WebUtility.HtmlDecode(match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value);
    }

    /// <summary>Envía un formulario POST obteniendo antes el token de <paramref name="tokenPage"/>.</summary>
    public static async Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client,
        string url,
        string tokenPage,
        Dictionary<string, string>? fields = null)
    {
        var token = await client.GetAntiforgeryTokenAsync(tokenPage);
        var data = new Dictionary<string, string>(fields ?? new()) { ["__RequestVerificationToken"] = token };
        return await client.PostAsync(url, new FormUrlEncodedContent(data));
    }

    /// <summary>Destino de la redirección como ruta relativa (el servidor de pruebas devuelve URL absolutas).</summary>
    public static string? Location(this HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        if (location is null)
        {
            return null;
        }

        return location.IsAbsoluteUri && location.Host == "localhost"
            ? location.PathAndQuery
            : location.OriginalString;
    }

    /// <summary>Cuerpo HTML con las entidades decodificadas (los acentos salen como &amp;#xE1;).</summary>
    public static async Task<string> HtmlAsync(this HttpResponseMessage response) =>
        WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    /// <summary>Comprueba que la respuesta es un 302 hacia una ruta que empieza por <paramref name="prefix"/>.</summary>
    public static void AssertRedirectTo(this HttpResponseMessage response, string prefix)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(prefix, response.Location() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Cookie auth: sin permiso el servidor redirige a /account/accessdenied.</summary>
    public static void AssertForbidden(this HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return;
        }

        response.AssertRedirectTo("/account/accessdenied");
    }
}

public static class SessionExtensions
{
    /// <summary>POST autenticado: el token se toma del formulario de logout del layout.</summary>
    public static Task<HttpResponseMessage> PostForm(
        this HttpClient client,
        string url,
        Dictionary<string, string>? fields = null) =>
        client.PostFormAsync(url, "/profile", fields);
}
