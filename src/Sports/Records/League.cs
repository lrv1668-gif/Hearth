namespace Sports.Records;

/// <summary>A league Hearth can follow teams in. <c>Path</c> is ESPN's "{sport}/{league}" segment.</summary>
public record League(string Key, string Name, string Sport, string Path);
