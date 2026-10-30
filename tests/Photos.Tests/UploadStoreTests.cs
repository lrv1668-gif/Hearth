using Hearth.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;

namespace Photos.Tests;

public sealed class UploadStoreTests : IDisposable
{
    private readonly TempDirectory _data = new();
    private readonly IConfiguration _config;

    public UploadStoreTests()
    {
        Directory.CreateDirectory(_data.Path);
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DATA_PATH"] = _data.Path })
            .Build();
    }

    public void Dispose() => _data.Dispose();

    private UploadStore MakeStore() => new(_config, NullLogger<UploadStore>.Instance);

    // Where the stored file's red corner should be, for a source whose top-left corner is red.
    // EXIF orientations: 1 upright, 2 mirrored, 3 upside down, 4 flipped, 5 transposed,
    // 6 rotated 90° clockwise to view (how a phone saves a portrait shot), 7 transverse,
    // 8 rotated 90° anticlockwise to view. 5–8 also turn the landscape pixels portrait.
    [Theory]
    [InlineData(1, false, false)]
    [InlineData(2, true, false)]
    [InlineData(3, true, true)]
    [InlineData(4, false, true)]
    [InlineData(5, false, false)]
    [InlineData(6, true, false)]
    [InlineData(7, true, true)]
    [InlineData(8, false, true)]
    public async Task SaveAsync_EachExifOrientation_MovesTheCornerWhereItIsViewed(byte orientation, bool right, bool bottom)
    {
        var store = MakeStore();

        var saved = await store.SaveAsync(FormFile(WithExifOrientation(Jpeg(80, 40), orientation)));

        using var stored = SKBitmap.Decode(StoredPath(saved!));
        Assert.Equal(orientation >= 5 ? (40, 80) : (80, 40), (stored.Width, stored.Height));
        var pixel = stored.GetPixel(right ? stored.Width - 4 : 3, bottom ? stored.Height - 4 : 3);
        Assert.True(pixel.Red > 200 && pixel.Blue < 100, $"expected the red corner there, got {pixel}");
    }

    [Fact]
    public async Task SaveAsync_LargeJpeg_StoresAtMostFullSize()
    {
        var store = MakeStore();

        var saved = await store.SaveAsync(FormFile(Jpeg(6000, 3000)));

        using var stored = SKBitmap.Decode(StoredPath(saved!));
        Assert.Equal((2560, 1280), (stored.Width, stored.Height));
    }

    [Fact]
    public void Constructor_TornCaptionsFile_StartsEmptyAndKeepsIt()
    {
        var path = Path.Combine(_data.Path, "captions.json");
        File.WriteAllText(path, """{"abc": "Half a capt""");

        var store = MakeStore();

        Assert.Empty(store.List());
        Assert.False(File.Exists(path));
        Assert.True(File.Exists(path + ".bad"));
    }

    private string StoredPath(UploadedPhoto photo) => Path.Combine(_data.Path, "uploads", $"{photo.Id}.webp");

    private static byte[] Jpeg(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.SteelBlue);
        // A red top-left corner, to see where the orientation moved it
        using (var canvas = new SKCanvas(bitmap))
            canvas.DrawRect(0, 0, 12, 12, new SKPaint { Color = SKColors.Red });
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
        return data.ToArray();
    }

    /// <summary><paramref name="jpeg"/> with an EXIF APP1 segment holding just the orientation tag.</summary>
    private static byte[] WithExifOrientation(byte[] jpeg, byte orientation)
    {
        byte[] app1 =
        [
            0xFF, 0xE1, 0x00, 0x22,                         // APP1, 34 bytes
            0x45, 0x78, 0x69, 0x66, 0x00, 0x00,             // "Exif\0\0"
            0x4D, 0x4D, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08, // big-endian TIFF, IFD at 8
            0x00, 0x01,                                     // one entry
            0x01, 0x12, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, // Orientation, SHORT, 1 value
            0x00, orientation, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,                         // no next IFD
        ];
        return [.. jpeg[..2], .. app1, .. jpeg[2..]];
    }

    private static FormFile FormFile(byte[] bytes) =>
        new(new MemoryStream(bytes), 0, bytes.Length, "file", "photo.jpg");
}
