using Display.Records;
using Hearth.TestUtilities;

namespace Display.Tests;

public sealed class ModeStoreTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly ModeStore _store;

    public ModeStoreTests()
    {
        _store = new ModeStore(_db.Db);
        _store.Migrate();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void SetOverride_ThenClear_RoundTripsWithItsExpiry()
    {
        var pin = new ModeOverride(FrameModes.Photo, TestData.Noon.AddHours(3), "abc");

        _store.SetOverride(pin);

        Assert.Equal(pin, _store.GetOverride());
        _store.ClearOverride();
        Assert.Null(_store.GetOverride());
    }
}
