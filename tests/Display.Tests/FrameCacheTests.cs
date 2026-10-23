using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Display.Tests;

public sealed class FrameCacheTests : IDisposable
{
    private static readonly DateTimeOffset Noon = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"hearth-frame-{Guid.NewGuid():N}");

    private FrameCache Cache() => new(_dir, NullLogger<FrameCache>.Instance);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Current_NothingStored_ReturnsNull()
    {
        Assert.Null(Cache().Current);
    }

    [Fact]
    public void Store_SamePixels_KeepsETagAndRenderedAt()
    {
        var cache = Cache();
        var first = cache.Store([1, 2, 3], "daily", null, Noon);

        var second = cache.Store([1, 2, 3], "daily", null, Noon.AddMinutes(15));

        Assert.Equal(first.ETag, second.ETag);
        Assert.Equal(Noon, second.RenderedAt);
        Assert.Equal(Noon.AddMinutes(15), second.CheckedAt);
    }

    [Fact]
    public void Store_NewPixels_ChangesETag()
    {
        var cache = Cache();
        var first = cache.Store([1, 2, 3], "daily", null, Noon);

        var second = cache.Store([4, 5, 6], "night", null, Noon.AddMinutes(15));

        Assert.NotEqual(first.ETag, second.ETag);
        Assert.Equal(Noon.AddMinutes(15), second.RenderedAt);
        Assert.Equal([4, 5, 6], cache.Current!.Value.Png);
    }

    [Fact]
    public void Store_ETag_IsAQuotedHash()
    {
        Assert.Matches("^\"[0-9a-f]{16}\"$", Cache().Store([1, 2, 3], "daily", null, Noon).ETag);
    }

    [Fact]
    public void Current_AfterARestart_ReadsTheSavedFrame()
    {
        var stored = Cache().Store([1, 2, 3], "photo", "a1", Noon);

        var reloaded = Cache().Current!.Value;

        Assert.Equal(stored, reloaded.Info);
        Assert.Equal([1, 2, 3], reloaded.Png);
    }

    [Fact]
    public void Current_PngNoLongerMatchingItsETag_IsIgnored()
    {
        Cache().Store([1, 2, 3], "daily", null, Noon);
        // As if a crash came between writing the new PNG and its frame.json
        File.WriteAllBytes(Path.Combine(_dir, "frame.png"), [4, 5, 6]);

        Assert.Null(Cache().Current);
    }

    [Fact]
    public void DirectoryFor_DbPath_IsItsDirectory()
    {
        Assert.Equal(Path.GetFullPath("/data"), FrameCache.DirectoryFor("/data/display.db"));
    }
}
