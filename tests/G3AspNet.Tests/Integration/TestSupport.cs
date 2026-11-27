using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using G3AspNet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace G3AspNet.Tests.Integration;

/// <summary>Utilidades compartidas: cuentas desechables, sesiones y códigos TOTP.</summary>
public static partial class TestSupport
{
    public const string Password = "Prueba123!";

    /// <summary>Crea una cuenta propia de la prueba para no alterar las cuentas demo compartidas.</summary>
    public static async Task<ApplicationUser> CreateAccountAsync(this AppFactory factory, string role)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var name = $"t{Guid.NewGuid():N}"[..20];
        var user = new ApplicationUser
        {
            UserName = name,
            Email = $"{name}@taller.test",
            DisplayName = name,
            EmailConfirmed = true
        };
        Assert.True((await users.CreateAsync(user, Password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        return user;
    }

    /// <summary>Inicia una sesión nueva (sin caché) y comprueba que se ha concedido.</summary>
    public static async Task<HttpClient> NewSessionAsync(this AppFactory factory, string username, string password = Password)
    {
        var client = factory.CreateAnonymousClient();
        var response = await client.PostFormAsync("/account/login", "/account/login", new()
        {
            ["Username"] = username,
            ["Password"] = password
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return client;
    }

    public static Task<ApplicationUser?> FindUserAsync(this AppFactory factory, string id) =>
        factory.WithDbAsync(db => db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id));

    public static async Task<bool> IsSignedInAsync(this HttpClient client) =>
        (await client.GetAsync("/profile")).StatusCode == HttpStatusCode.OK;

    [GeneratedRegex(@"<[^>]+alert-danger[^>]*>(.*?)</div>", RegexOptions.Singleline)]
    private static partial Regex ErrorAlertRegex();

    /// <summary>Texto del aviso de error que la siguiente página muestra tras una redirección.</summary>
    public static async Task<string> FollowErrorAsync(this HttpClient client, HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var html = await (await client.GetAsync(response.Location())).HtmlAsync();
        return ErrorAlertRegex().Match(html).Groups[1].Value;
    }

    /// <summary>Código de 6 dígitos (RFC 6238) como el que genera una app autenticadora.</summary>
    public static string Totp(string base32Key)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var character in base32Key.ToUpperInvariant().Where(c => c is not (' ' or '=')))
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)(buffer >> bits));
            }
        }

        var counter = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        Array.Reverse(counter);
        var hash = HMACSHA1.HashData(bytes.ToArray(), counter);
        var offset = hash[^1] & 0x0f;
        var value = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (value % 1_000_000).ToString("D6");
    }

    public static async Task<string> AuthenticatorKeyAsync(this AppFactory factory, string userId)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByIdAsync(userId);
        return (await users.GetAuthenticatorKeyAsync(user!))!;
    }
}

/// <summary>
/// Ejecuta una acción justo antes del siguiente guardado de la aplicación. Permite reproducir
/// de forma determinista que otra persona cambie los datos entre la lectura y la escritura.
/// </summary>
public sealed class BeforeSaveInterceptor : SaveChangesInterceptor
{
    private Func<Task>? pending;

    public void OnNextSave(Func<Task> action) => pending = action;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var action = Interlocked.Exchange(ref pending, null);
        if (action is not null)
        {
            await action();
        }

        return result;
    }
}
