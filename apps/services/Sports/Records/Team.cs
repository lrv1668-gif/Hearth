namespace Sports.Records;

/// <summary>A team in one league — what Settings lists and what a followed team is saved as.</summary>
public record Team(
    string  League,
    string  Id,
    string  Abbreviation,
    string  Name,
    string  ShortName,
    string? Logo,
    string? Color);
