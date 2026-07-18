namespace Calendar.Tests.Helpers;

/// <summary>
/// A settable clock for tests that need to control time-dependent boundaries
/// (e.g. OAuth-state expiry) without waiting real time or using reflection.
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; }

    public FakeTimeProvider(DateTimeOffset utcNow) => UtcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;

    public void Advance(TimeSpan by) => UtcNow += by;
}
