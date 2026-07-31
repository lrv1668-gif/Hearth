namespace Plants.Records;

public record PlantItem(
    long Id,
    string Name,
    string? Species,
    int WateringIntervalDays,
    DateTime? LastWateredAt,
    DateTime CreatedAt,
    DateTime NextWateringDue,
    bool IsOverdue
);
