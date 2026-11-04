namespace History.Records;

/// <summary>Something that happened on today's date, from Wikipedia's "On this day".</summary>
/// <param name="Year">The year it happened; negative for BC.</param>
/// <param name="PageTitle">The Wikipedia article it links to first, for the credit line.</param>
public record HistoryEvent(int Year, string Text, string? PageTitle);

/// <summary>Today's events, for the household's date.</summary>
/// <param name="Date">The local date they're for, yyyy-MM-dd.</param>
public record OnThisDay(string Date, IReadOnlyList<HistoryEvent> Events);
