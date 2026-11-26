using G3AspNet.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace G3AspNet.Tests;

/// <summary>Validación y almacenamiento seguro de avatares.</summary>
public sealed class ImageStorageTests : IDisposable
{
    private static readonly byte[] Png = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0];
    private static readonly byte[] Jpg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0];
    private static readonly byte[] Gif = "GIF89a......"u8.ToArray();
    private static readonly byte[] Webp = "RIFF....WEBP"u8.ToArray();

    private readonly string root = Path.Combine(Path.GetTempPath(), $"g3-img-{Guid.NewGuid():N}");

    private ImageStorage CreateStorage(long maxBytes = 5 * 1024 * 1024) => new(
        new FakeEnvironment(root),
        Options.Create(new ImageStorageOptions { MaxFileSizeBytes = maxBytes }),
        NullLogger<ImageStorage>.Instance);

    private static IFormFile File(byte[] content, string name) =>
        new FormFile(new MemoryStream(content), 0, content.Length, "avatar", name);

    private string AvatarDir => Path.Combine(root, "uploads", "avatars");

    [Theory]
    [InlineData("foto.png")]
    [InlineData("FOTO.PNG")]
    public void Save_ValidPng_StoresFileWithRandomName(string name)
    {
        var result = CreateStorage().Save(File(Png, name));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.FileName);
        Assert.DoesNotContain("foto", result.FileName!, StringComparison.OrdinalIgnoreCase);
        Assert.True(System.IO.File.Exists(Path.Combine(AvatarDir, result.FileName!)));
    }

    [Theory]
    [MemberData(nameof(ValidImages))]
    public void Save_AcceptsEverySupportedFormat(string name, byte[] content)
    {
        Assert.True(CreateStorage().Save(File(content, name)).Succeeded);
    }

    public static TheoryData<string, byte[]> ValidImages => new()
    {
        { "a.png", Png }, { "a.jpg", Jpg }, { "a.jpeg", Jpg }, { "a.gif", Gif }, { "a.webp", Webp }
    };

    [Fact]
    public void Save_EmptyFile_IsRejected()
    {
        Assert.False(CreateStorage().Save(File([], "a.png")).Succeeded);
    }

    [Fact]
    public void Save_ExactlyMaxSizeIsAccepted_OneByteMoreIsRejected()
    {
        var atLimit = new byte[Png.Length + 8];
        Png.CopyTo(atLimit, 0);
        var storage = CreateStorage(maxBytes: atLimit.Length);

        Assert.True(storage.Save(File(atLimit, "a.png")).Succeeded);
        Assert.False(storage.Save(File([.. atLimit, 0], "a.png")).Succeeded);
    }

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("script.php")]
    [InlineData("imagen.svg")]
    [InlineData("sin-extension")]
    [InlineData("doble.png.exe")]
    public void Save_DisallowedExtension_IsRejected(string name)
    {
        Assert.False(CreateStorage().Save(File(Png, name)).Succeeded);
    }

    [Fact]
    public void Save_ContentNotMatchingExtension_IsRejected()
    {
        var storage = CreateStorage();

        Assert.False(storage.Save(File("<?php echo 1; ?>"u8.ToArray(), "a.png")).Succeeded);
        Assert.False(storage.Save(File(Jpg, "a.png")).Succeeded);   // JPG con extensión PNG
        Assert.False(storage.Save(File(Png, "a.jpg")).Succeeded);
        Assert.False(storage.Save(File(Png, "a.gif")).Succeeded);
        Assert.False(storage.Save(File(Png, "a.webp")).Succeeded);
    }

    [Fact]
    public void Save_TooShortFile_IsRejectedWithoutThrowing()
    {
        Assert.False(CreateStorage().Save(File([0xFF], "a.jpg")).Succeeded);
    }

    [Fact]
    public void Save_TwiceTheSameFile_GeneratesDifferentNames()
    {
        var storage = CreateStorage();

        var first = storage.Save(File(Png, "a.png"));
        var second = storage.Save(File(Png, "a.png"));

        Assert.NotEqual(first.FileName, second.FileName);
    }

    [Fact]
    public void Save_PreservesFullContent()
    {
        var content = new byte[Png.Length + 100];
        Png.CopyTo(content, 0);
        new Random(1).NextBytes(content.AsSpan(Png.Length));

        var result = CreateStorage().Save(File(content, "a.png"));

        Assert.Equal(content, System.IO.File.ReadAllBytes(Path.Combine(AvatarDir, result.FileName!)));
    }

    [Fact]
    public void Save_NameWithPathTraversal_DoesNotEscapeFolder()
    {
        var result = CreateStorage().Save(File(Png, "../../../evil.png"));

        Assert.True(result.Succeeded);
        Assert.Equal(AvatarDir, Path.GetDirectoryName(Path.Combine(AvatarDir, result.FileName!)));
        Assert.False(System.IO.File.Exists(Path.Combine(root, "evil.png")));
    }

    [Theory]
    [InlineData("../secreto.txt")]
    [InlineData("..\\secreto.txt")]
    [InlineData("sub/dir.png")]
    [InlineData("")]
    [InlineData(null)]
    public void Delete_UnsafeName_DoesNothing(string? name)
    {
        Directory.CreateDirectory(root);
        var outside = Path.Combine(root, "secreto.txt");
        System.IO.File.WriteAllText(outside, "x");

        CreateStorage().Delete(name);

        Assert.True(System.IO.File.Exists(outside));
    }

    [Fact]
    public void Delete_RemovesStoredFile_AndIgnoresMissingOnes()
    {
        var storage = CreateStorage();
        var name = storage.Save(File(Png, "a.png")).FileName!;

        storage.Delete(name);
        storage.Delete(name); // segunda vez: no debe lanzar

        Assert.False(System.IO.File.Exists(Path.Combine(AvatarDir, name)));
    }

    [Theory]
    [InlineData("../x.png")]
    [InlineData("a/b.png")]
    [InlineData("")]
    [InlineData(null)]
    public void GetUrl_UnsafeName_ReturnsNull(string? name)
    {
        Assert.Null(CreateStorage().GetUrl(name));
    }

    [Fact]
    public void GetUrl_SafeName_ReturnsPublicPath()
    {
        Assert.Equal("/uploads/avatars/abc.png", CreateStorage().GetUrl("abc.png"));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FakeEnvironment(string webRoot) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = webRoot;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = webRoot;
        public string EnvironmentName { get; set; } = "Development";
    }
}
