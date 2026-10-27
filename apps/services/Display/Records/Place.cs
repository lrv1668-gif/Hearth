namespace Display.Records;

/// <summary>A city search result for Settings → Home.</summary>
/// <param name="Label">The name with its region and country, e.g. "Denver, Colorado, United States".</param>
/// <param name="TimeZone">The place's IANA zone, when the geocoder knows it.</param>
public sealed record Place(string Name, string Label, double Latitude, double Longitude, string? TimeZone);
