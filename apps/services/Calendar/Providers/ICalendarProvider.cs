namespace Calendar.Providers;

/// <summary>
/// A source of calendar items for <c>GET /calendar/items</c>. How a provider is connected
/// (an OAuth sign-in, a pasted link) is its own business, behind its own endpoints.
/// </summary>
public interface ICalendarProvider
{
    string ProviderKey { get; }

    /// <summary>Whether there is anything to fetch. Synchronous SQLite row-presence check — fast, single-user.</summary>
    bool IsConnected { get; }

    /// <summary>Returns empty on any error rather than throwing — caller isolation.</summary>
    Task<IEnumerable<Calendar.Records.CalendarItem>> GetItemsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
