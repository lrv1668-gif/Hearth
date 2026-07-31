using Plants;
using Plants.Records;

namespace Plants.Extensions;

public static class WebApplicationExtensions
{
    public static void InitializeWebAppForPlants(this WebApplication app)
    {
        app.MigratePlantStore();
        app.AddPlantsEndpoints();
    }

    private static void MigratePlantStore(this WebApplication app)
    {
        app.Services.GetRequiredService<PlantStore>().Migrate();
    }

    private static void AddPlantsEndpoints(this WebApplication app)
    {
        // Nested under /plants/items (not bare /plants) so Caddy can proxy API
        // sub-paths to this service while leaving the bare /plants page route
        // to the frontend — same split Calendar uses for /calendar/items.
        app.MapGet("/plants/items", (PlantStore store) => store.List());

        app.MapPost("/plants/items", (CreatePlantRequest req, PlantStore store) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest("name required");
            if (req.WateringIntervalDays <= 0)
                return Results.BadRequest("watering_interval_days must be positive");
            var plant = store.Create(req.Name, req.Species, req.WateringIntervalDays);
            return Results.Created($"/plants/{plant.Id}", plant);
        });

        app.MapPut("/plants/{id:long}", (long id, UpdatePlantRequest req, PlantStore store) =>
        {
            var plant = store.Update(id, req.Name, req.Species, req.WateringIntervalDays);
            return plant is null ? Results.NotFound() : Results.Ok(plant);
        });

        app.MapPost("/plants/{id:long}/water", (long id, PlantStore store) =>
        {
            var plant = store.Water(id);
            return plant is null ? Results.NotFound() : Results.Ok(plant);
        });

        app.MapDelete("/plants/{id:long}", (long id, PlantStore store) =>
        {
            store.Delete(id);
            return Results.NoContent();
        });
    }
}
