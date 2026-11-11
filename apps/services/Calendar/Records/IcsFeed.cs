namespace Calendar.Records;

/// <summary>A subscribed calendar. <see cref="Url"/> is a secret: whoever has it can read the calendar.</summary>
public record IcsFeed(string Id, string Name, string Url);

/// <summary>What Settings sees of a subscribed calendar — its host, never the link itself.</summary>
public record IcsFeedResponse(string Id, string Name, string Host);

public record AddIcsFeedRequest(string? Name, string? Url);
