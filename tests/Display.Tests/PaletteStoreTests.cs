using Display.Records;
using Hearth.TestUtilities;

namespace Display.Tests;

public sealed class PaletteStoreTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly PaletteStore _store;

    public PaletteStoreTests()
    {
        _store = new PaletteStore(_db.Db);
        _store.Migrate();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Save_ThenGet_RoundTrips()
    {
        _store.Save(new FramePalette(FramePalette.BlackAndWhite));

        Assert.True(_store.Get().IsBlackAndWhite);
    }
}
