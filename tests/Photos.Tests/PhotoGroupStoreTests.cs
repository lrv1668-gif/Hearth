using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Photos.Tests;

public sealed class PhotoGroupStoreTests : IDisposable
{
    private readonly string _dataPath = Path.Combine(Path.GetTempPath(), $"photos-{Guid.NewGuid():N}");
    private readonly IConfiguration _config;

    public PhotoGroupStoreTests()
    {
        // UploadStore indexes whatever files sit in uploads/ when it starts
        var uploadsDir = Path.Combine(_dataPath, "uploads");
        Directory.CreateDirectory(uploadsDir);
        foreach (var id in new[] { "a", "b", "c" }) File.WriteAllBytes(Path.Combine(uploadsDir, $"{id}.webp"), []);
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DATA_PATH"] = _dataPath })
            .Build();
    }

    public void Dispose() => Directory.Delete(_dataPath, recursive: true);

    private PhotoGroupStore MakeStore(UploadStore? uploads = null) => new(_config, uploads ?? new UploadStore(_config, NullLogger<UploadStore>.Instance), NullLogger<PhotoGroupStore>.Instance);

    [Fact]
    public void Create_KnownAndUnknownIds_KeepsOnlyUploadsOnce()
    {
        var store = MakeStore();

        var (group, error) = store.Create(new PhotoGroupInput("  Winter ", ["a", "zzz", "b", "a"]));

        Assert.Null(error);
        Assert.Equal("Winter", group!.Name);
        Assert.Equal(["a", "b"], group.PhotoIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankName_ReturnsError(string name)
    {
        var (group, error) = MakeStore().Create(new PhotoGroupInput(name, ["a"]));

        Assert.Null(group);
        Assert.NotNull(error);
    }

    [Fact]
    public void Create_NameTooLong_ReturnsError()
    {
        var (_, error) = MakeStore().Create(new PhotoGroupInput(new string('x', PhotoGroupStore.MaxNameLength + 1), []));

        Assert.NotNull(error);
    }

    [Fact]
    public void Update_ExistingGroup_ReplacesNameAndPhotos()
    {
        var store = MakeStore();
        var (group, _) = store.Create(new PhotoGroupInput("Summer", ["a"]));

        var (found, updated, error) = store.Update(group!.Id, new PhotoGroupInput("Beach", ["b", "c"]));

        Assert.True(found);
        Assert.Null(error);
        Assert.Equal(["b", "c"], updated!.PhotoIds);
        var saved = Assert.Single(store.List());
        Assert.Equal("Beach", saved.Name);
        Assert.Equal(["b", "c"], saved.PhotoIds);
    }

    [Fact]
    public void Update_MissingGroup_ReturnsNotFound()
    {
        var (found, _, _) = MakeStore().Update("nope", new PhotoGroupInput("X", []));

        Assert.False(found);
    }

    [Fact]
    public void Delete_ExistingGroup_RemovesIt()
    {
        var store = MakeStore();
        var (group, _) = store.Create(new PhotoGroupInput("Summer", ["a"]));

        Assert.True(store.Delete(group!.Id));
        Assert.Empty(store.List());
        Assert.False(store.Delete(group.Id));
    }

    [Fact]
    public void RemovePhoto_InGroups_DropsItFromEach()
    {
        var store = MakeStore();
        store.Create(new PhotoGroupInput("One", ["a", "b"]));
        store.Create(new PhotoGroupInput("Two", ["a", "c"]));

        store.RemovePhoto("a");

        Assert.All(store.List(), g => Assert.DoesNotContain("a", g.PhotoIds));
    }

    [Fact]
    public void List_UploadDeletedElsewhere_HidesIt()
    {
        var uploads = new UploadStore(_config, NullLogger<UploadStore>.Instance);
        var store = MakeStore(uploads);
        store.Create(new PhotoGroupInput("One", ["a", "b"]));

        uploads.Delete("a");

        Assert.Equal(["b"], Assert.Single(store.List()).PhotoIds);
    }

    [Fact]
    public void Constructor_ExistingFile_LoadsSavedGroups()
    {
        MakeStore().Create(new PhotoGroupInput("Kept", ["c"]));

        var reloaded = MakeStore();

        var group = Assert.Single(reloaded.List());
        Assert.Equal("Kept", group.Name);
        Assert.Equal(["c"], group.PhotoIds);
    }
}
