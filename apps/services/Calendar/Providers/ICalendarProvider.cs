namespace Calendar.Providers;

public interface ICalendarProvider
{
    string ProviderKey { get; }

    /// <summary>Synchronous SQLite row-presence check — fast, single-user.</summary>
    bool IsAuthenticated { get; }

    string GetAuthUrl(string state);

    Task HandleCallbackAsync(string code, CancellationToken ct = default);

    /// <summary>Sync; clears token row and items cache row.</summary>
    void Disconnect();

    /// <summary>Returns empty on any error rather than throwing — caller isolation.</summary>
    Task<IEnumerable<Calendar.Records.CalendarItem>> GetItemsAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);

    /// <summary>Toggles a Google Task's completion state and invalidates the items cache.</summary>
    Task SetTaskCompletedAsync(
        string taskListId, string taskId, bool completed, CancellationToken ct = default);
}
