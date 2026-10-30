using System.Net;
using Display.Records;
using Hearth.TestUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Display.Tests;

/// <summary><see cref="FramePhotos"/> — the one photo the panel, Settings' preview and /frame share.</summary>
public sealed class FramePhotosTests : IDisposable
{
    private const string Uploads = """
        [{"id":"u1","url":"/photos/files/u1.jpg","thumb_url":"/photos/files/u1_t.jpg","caption":"Lake"},
         {"id":"u2","url":"/photos/files/u2.jpg","thumb_url":"/photos/files/u2_t.jpg","caption":null},
         {"id":"u3","url":"/photos/files/u3.jpg","thumb_url":"/photos/files/u3_t.jpg","caption":null}]
        """;

    private static readonly FrameSettings Saved = new("almanac", "inter", "landscape", [], [], 50);

    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"hearth-photos-{Guid.NewGuid():N}");
    private readonly FakeHttpMessageHandler _photos = new(Unsplash("s1"));
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));

    public FramePhotosTests() => _photos.ByPath["/photos/uploads"] = (HttpStatusCode.OK, Uploads);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
    }

    private static string Unsplash(string id) =>
        $$"""{"id":"{{id}}","url":"https://images.unsplash.com/{{id}}","thumb_url":null,"description":null,"photographer_name":"Ana","unsplash_link":"https://unsplash.com/{{id}}","source":"unsplash"}""";

    private FramePhotos Holder(string? baseUrl = "http://photos/") => new(
        new PhotosClient(new HttpClient(_photos) { BaseAddress = baseUrl is null ? null : new Uri(baseUrl) }),
        _dir, _clock, NullLogger<FramePhotos>.Instance);

    private int RandomCalls => _photos.RequestUris.Count(u => u.AbsolutePath == "/photos/random");

    [Fact]
    public async Task Ambient_SameChoice_KeepsThePhoto()
    {
        var photos = Holder();
        var first = await photos.GetAsync(FrameModes.Ambient, Saved);
        _photos.ResponseJson = Unsplash("s2");

        Assert.Equal("s1", first?.Id);
        Assert.Equal("s1", (await photos.GetAsync(FrameModes.Ambient, Saved))?.Id);
        Assert.Equal(1, RandomCalls);
    }

    [Fact]
    public async Task Ambient_LookChange_KeepsThePhoto()
    {
        var photos = Holder();
        await photos.GetAsync(FrameModes.Ambient, Saved);
        _photos.ResponseJson = Unsplash("s2");

        var restyled = Saved with { Theme = "garden", Font = "lora", AmbientPosition = "top-right" };
        Assert.Equal("s1", (await photos.GetAsync(FrameModes.Ambient, restyled))?.Id);
    }

    [Fact]
    public async Task Ambient_ChoiceChange_PicksAgain()
    {
        var photos = Holder();
        await photos.GetAsync(FrameModes.Ambient, Saved);
        _photos.ResponseJson = Unsplash("s2");

        var picked = await photos.GetAsync(FrameModes.Ambient, Saved with { PhotoCategories = ["ocean"] });

        Assert.Equal("s2", picked?.Id);
        Assert.Contains("query=ocean", _photos.LastRequestUri!.Query);
    }

    [Fact]
    public async Task Ambient_Next_PicksAgain()
    {
        var photos = Holder();
        await photos.GetAsync(FrameModes.Ambient, Saved);
        _photos.ResponseJson = Unsplash("s2");

        Assert.Equal("s2", (await photos.GetAsync(FrameModes.Ambient, Saved, next: true))?.Id);
        Assert.Equal("s2", (await photos.GetAsync(FrameModes.Ambient, Saved))?.Id);
    }

    [Fact]
    public async Task Ambient_OlderThanMaxAge_PicksAgain()
    {
        var photos = Holder();
        await photos.GetAsync(FrameModes.Ambient, Saved);
        _photos.ResponseJson = Unsplash("s2");
        _clock.Advance(TimeSpan.FromMinutes(60));

        Assert.Equal("s2", (await photos.GetAsync(FrameModes.Ambient, Saved, maxAge: TimeSpan.FromMinutes(60)))?.Id);
    }

    [Fact]
    public async Task Ambient_YoungerThanMaxAge_KeepsThePhoto()
    {
        var photos = Holder();
        await photos.GetAsync(FrameModes.Ambient, Saved);
        _photos.ResponseJson = Unsplash("s2");
        _clock.Advance(TimeSpan.FromMinutes(5));

        Assert.Equal("s1", (await photos.GetAsync(FrameModes.Ambient, Saved, maxAge: TimeSpan.FromMinutes(60)))?.Id);
    }

    [Fact]
    public async Task Ambient_HeldUploadDeleted_PicksAnother()
    {
        var photos = Holder();
        var local = Saved with { PhotoSource = "local" };
        var first = (await photos.GetAsync(FrameModes.Ambient, local))!;
        _photos.ByPath["/photos/uploads"] = (HttpStatusCode.OK, $"[{Upload(first.Id == "u1" ? "u2" : "u1")}]");

        var next = await photos.GetAsync(FrameModes.Ambient, local);

        Assert.NotNull(next);
        Assert.NotEqual(first.Id, next.Id);
    }

    [Fact]
    public async Task Photo_Album_NextMovesToAnotherPick()
    {
        var photos = Holder();
        var album = Saved with { PhotoIds = ["u1", "u2"] };
        var first = (await photos.GetAsync(FrameModes.Photo, album))!;

        Assert.Equal(first.Id, (await photos.GetAsync(FrameModes.Photo, album))?.Id);
        var next = await photos.GetAsync(FrameModes.Photo, album, next: true);
        Assert.Contains(next!.Id, new[] { "u1", "u2" });
        Assert.NotEqual(first.Id, next.Id);
    }

    [Fact]
    public async Task Photo_Pinned_BeatsTheAlbum()
    {
        var photo = await Holder().GetAsync(FrameModes.Photo, Saved with { PhotoIds = ["u1", "u2"] }, pinnedId: "u3");

        Assert.Equal("u3", photo?.Id);
        Assert.Equal("local", photo?.Source);
    }

    [Fact]
    public async Task Photo_NothingPicked_ShowsAmbientsPhoto()
    {
        var photos = Holder();
        var ambient = await photos.GetAsync(FrameModes.Ambient, Saved);

        Assert.Equal(ambient, await photos.GetAsync(FrameModes.Photo, Saved));
    }

    [Fact]
    public async Task Photo_AllPicksDeleted_ShowsAmbientsPhoto()
    {
        var photo = await Holder().GetAsync(FrameModes.Photo, Saved with { PhotoIds = ["gone"] });

        Assert.Equal("s1", photo?.Id);
    }

    [Fact]
    public async Task Held_SurvivesARestart()
    {
        await Holder().GetAsync(FrameModes.Ambient, Saved);
        _photos.ResponseJson = Unsplash("s2");

        Assert.Equal("s1", (await Holder().GetAsync(FrameModes.Ambient, Saved))?.Id);
    }

    [Fact]
    public async Task PhotosDown_KeepsTheHeldPhoto()
    {
        var photos = Holder();
        await photos.GetAsync(FrameModes.Ambient, Saved);
        _photos.Status = HttpStatusCode.InternalServerError;

        Assert.Equal("s1", (await photos.GetAsync(FrameModes.Ambient, Saved, next: true))?.Id);
    }

    [Fact]
    public async Task NoPhotosUrl_IsNull()
    {
        Assert.Null(await Holder(baseUrl: null).GetAsync(FrameModes.Ambient, Saved));
        Assert.Empty(_photos.RequestUris);
    }

    [Fact]
    public async Task Photo_Album_ShowsEveryPhotoBeforeRepeating()
    {
        var photos = Holder();
        var album = Saved with { PhotoIds = ["u1", "u2", "u3"] };
        var first = (await photos.GetAsync(FrameModes.Photo, album))!.Id;
        var second = (await photos.GetAsync(FrameModes.Photo, album, next: true))!.Id;
        var third = (await photos.GetAsync(FrameModes.Photo, album, next: true))!.Id;

        Assert.Equal(3, new[] { first, second, third }.Distinct().Count());
        Assert.NotEqual(third, (await photos.GetAsync(FrameModes.Photo, album, next: true))?.Id);
    }

    [Fact]
    public async Task Photo_Pin_KeepsTheAlbumsPass()
    {
        var photos = Holder();
        var album = Saved with { PhotoIds = ["u1", "u2", "u3"] };
        var first = (await photos.GetAsync(FrameModes.Photo, album))!.Id;
        var second = (await photos.GetAsync(FrameModes.Photo, album, next: true))!.Id;
        var third = new[] { "u1", "u2", "u3" }.Except([first, second]).Single();

        await photos.GetAsync(FrameModes.Photo, album, pinnedId: first);

        Assert.Equal(third, (await photos.GetAsync(FrameModes.Photo, album, next: true))?.Id);
    }

    [Fact]
    public async Task Ambient_Uploads_ShowEveryPhotoBeforeRepeating()
    {
        var photos = Holder();
        var local = Saved with { PhotoSource = "local" };
        var ids = new List<string>();
        for (var i = 0; i < 3; i++) ids.Add((await photos.GetAsync(FrameModes.Ambient, local, next: i > 0))!.Id);

        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public async Task Ambient_Unsplash_SkipsThePhotosShownThisPass()
    {
        var photos = Holder();
        await photos.GetAsync(FrameModes.Ambient, Saved);
        _photos.ResponseJson = Unsplash("s2");
        await photos.GetAsync(FrameModes.Ambient, Saved, next: true);
        _photos.ResponseJson = Unsplash("s3");
        await photos.GetAsync(FrameModes.Ambient, Saved, next: true);

        Assert.Contains("exclude=s1%2Cs2", _photos.LastRequestUri!.Query);
    }

    [Fact]
    public async Task Ambient_Unsplash_AShownPhotoBackStartsANewPass()
    {
        var photos = Holder();
        await photos.GetAsync(FrameModes.Ambient, Saved);
        _photos.ResponseJson = Unsplash("s2");
        await photos.GetAsync(FrameModes.Ambient, Saved, next: true);
        // Photos had none left to skip to
        _photos.ResponseJson = Unsplash("s1");
        await photos.GetAsync(FrameModes.Ambient, Saved, next: true);
        _photos.ResponseJson = Unsplash("s3");
        await photos.GetAsync(FrameModes.Ambient, Saved, next: true);

        Assert.Contains("exclude=s1&", _photos.LastRequestUri!.Query + "&");
    }

    [Fact]
    public async Task Pass_SurvivesARestart()
    {
        var album = Saved with { PhotoIds = ["u1", "u2", "u3"] };
        var first = (await Holder().GetAsync(FrameModes.Photo, album))!.Id;
        var second = (await Holder().GetAsync(FrameModes.Photo, album, next: true))!.Id;
        var third = (await Holder().GetAsync(FrameModes.Photo, album, next: true))!.Id;

        Assert.Equal(3, new[] { first, second, third }.Distinct().Count());
    }

    [Fact]
    public void Deal_GoesThroughEveryPhotoBeforeRepeating()
    {
        var from = new[] { "a", "b", "c", "d" }.Select(Uploaded).ToList();
        var shown = new List<string>();
        var random = new Random(7);
        string? current = null;
        var pass = new List<string>();
        for (var i = 0; i < 4; i++) pass.Add(current = FramePhotos.Deal(from, shown, current, random).Id);

        Assert.Equal(4, pass.Distinct().Count());
        Assert.NotEqual(current, FramePhotos.Deal(from, shown, current, random).Id);
        Assert.Single(shown);
    }

    [Fact]
    public void Deal_ForgetsPhotosNoLongerThere()
    {
        var shown = new List<string> { "a", "gone" };

        var picked = FramePhotos.Deal([Uploaded("a"), Uploaded("b")], shown, "a", new Random(1));

        Assert.Equal("b", picked.Id);
        Assert.Equal(["a", "b"], shown);
    }

    [Fact]
    public void Deal_NoPassYet_CountsThePhotoOnShow()
    {
        var picked = FramePhotos.Deal([Uploaded("a"), Uploaded("b")], [], "a", new Random(1));

        Assert.Equal("b", picked.Id);
    }

    [Theory]
    [InlineData("unsplash", 5, 0.01, "unsplash")]
    [InlineData("local", 0, 0.9, "local")]
    [InlineData("both", 5, 0.19, "local")]    // 5 of 25 photos: uploads a fifth of the time
    [InlineData("both", 5, 0.21, "unsplash")]
    [InlineData("both", 60, 0.74, "local")]   // 60 of 80: three quarters
    [InlineData("both", 60, 0.76, "unsplash")]
    [InlineData("both", 0, 0.0, "unsplash")]
    public void ResolveSource_WeighsUploadsAgainstUnsplash(string source, int uploads, double random, string expected)
    {
        Assert.Equal(expected, FramePhotos.ResolveSource(source, uploads, random));
    }

    [Fact]
    public void Query_NoCategories_IsNature()
    {
        Assert.Equal("nature", FramePhotos.Query([]));
        Assert.Equal("ocean,forest", FramePhotos.Query(["ocean", "forest"]));
    }

    private static UploadedPhoto Uploaded(string id) => new(id, $"/photos/files/{id}.jpg", null, null);

    private static string Upload(string id) =>
        $$"""{"id":"{{id}}","url":"/photos/files/{{id}}.jpg","thumb_url":null,"caption":null}""";
}
