namespace Weather.Records;

/// <summary>
/// One thing worth a line on a screen that otherwise stays quiet: a storm, frost, heat.
/// <paramref name="Kind"/> is a stable id for icons ("storm", "snow", "frost", "heat",
/// "wind", "air", "uv"); <paramref name="Text"/> is the line itself.
/// </summary>
public record WeatherAlert(string Kind, string Text);
