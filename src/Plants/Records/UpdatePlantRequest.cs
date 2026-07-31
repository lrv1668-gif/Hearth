namespace Plants.Records;

record UpdatePlantRequest(string Name, string? Species, int WateringIntervalDays);
