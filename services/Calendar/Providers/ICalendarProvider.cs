namespace Calendar.Providers;

public interface ICalendarProvider
{
    string ProviderKey { get; }

    /// <summary>Synchronous SQLite row-presence check — fast, single-user.</summary>
    bool IsAuthenticated { get; }

    string GetAuthUrl(string state);

    Task HandleCallbackAsync(string code, CancellationToken ct = default);

    /// <summary>Sync; clears token row and events cache row.</summary>
    void Disconnect();

    /// <summary>Returns empty on any error rather than throwing — caller isolation.</summary>
    Task<IEnumerable<Calendar.Records.CalendarEvent>> GetEventsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
