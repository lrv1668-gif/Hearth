namespace Birds.Records;

/// <summary>A raw eBird observation as returned by the API, before merging.</summary>
public record BirdObservation(
    string SpeciesCode,
    string CommonName,
    string ScientificName,
    string Location,
    string ObservedAt,   // "yyyy-MM-dd HH:mm" in the observation's local time
    int? Count,
    double Latitude,
    double Longitude);

/// <summary>A merged, deduplicated sighting served to the frontend.</summary>
public record BirdSighting(
    string SpeciesCode,
    string CommonName,
    string ScientificName,
    string Location,
    string ObservedAt,
    int? Count,
    double DistanceMi,
    bool IsNotable);
