namespace Plants.Records;

record CreatePlantRequest(string Name, string? Species, int WateringIntervalDays);
