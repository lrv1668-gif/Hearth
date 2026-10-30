namespace Hearth.TestUtilities;

/// <summary>A throwaway folder under the temp path (not created until something writes to it), deleted on dispose.</summary>
public sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"hearth-test-{Guid.NewGuid():N}");

    public void Dispose()
    {
        try { Directory.Delete(Path, recursive: true); } catch { /* best effort */ }
    }
}
