using System.Data.Common;
using Data.Abstractions;
using Plants.Records;

namespace Plants
{
    public sealed class PlantStore([FromKeyedServices("plants")] IDatabase db)
    {
        public void Migrate()
        {
            db.NonQuery("""
                CREATE TABLE IF NOT EXISTS lu_plants (
                    id                     INTEGER  PRIMARY KEY AUTOINCREMENT,
                    name                   TEXT     NOT NULL,
                    species                TEXT     NULL,
                    watering_interval_days INTEGER  NOT NULL,
                    last_watered_at        DATETIME NULL,
                    created_at             DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                )
                """);
        }

        public IEnumerable<PlantItem> List()
        {
            var plants = db.Query("""
                SELECT id, name, species, watering_interval_days, last_watered_at, created_at
                FROM lu_plants
                """, Map);

            return plants.OrderBy(p => p.NextWateringDue);
        }

        public PlantItem Create(string name, string? species, int wateringIntervalDays)
        {
            return db.QueryOne("""
                INSERT INTO lu_plants (name, species, watering_interval_days)
                VALUES ($name, $species, $watering_interval_days)
                RETURNING id, name, species, watering_interval_days, last_watered_at, created_at
                """, Map, cmd =>
            {
                cmd.AddParam("$name", name);
                cmd.AddParam("$species", species);
                cmd.AddParam("$watering_interval_days", wateringIntervalDays);
            })!;
        }

        public PlantItem? Update(long id, string name, string? species, int wateringIntervalDays)
        {
            return db.QueryOne("""
                UPDATE lu_plants
                SET name = $name, species = $species, watering_interval_days = $watering_interval_days
                WHERE id = $id
                RETURNING id, name, species, watering_interval_days, last_watered_at, created_at
                """, Map, cmd =>
            {
                cmd.AddParam("$name", name);
                cmd.AddParam("$species", species);
                cmd.AddParam("$watering_interval_days", wateringIntervalDays);
                cmd.AddParam("$id", id);
            });
        }

        public PlantItem? Water(long id)
        {
            return db.QueryOne("""
                UPDATE lu_plants
                SET last_watered_at = $now
                WHERE id = $id
                RETURNING id, name, species, watering_interval_days, last_watered_at, created_at
                """, Map, cmd =>
            {
                cmd.AddParam("$now", DateTime.UtcNow.ToString("o"));
                cmd.AddParam("$id", id);
            });
        }

        public void Delete(long id)
        {
            db.NonQuery("DELETE FROM lu_plants WHERE id = $id", cmd => cmd.AddParam("$id", id));
        }

        private static PlantItem Map(DbDataReader r)
        {
            var lastWateredAt = r.Field<DateTime?>("last_watered_at");
            var createdAt = r.Field<DateTime>("created_at");
            var wateringIntervalDays = r.Field<int>("watering_interval_days");
            var nextWateringDue = (lastWateredAt ?? createdAt).Date.AddDays(wateringIntervalDays);

            return new(
                r.Field<long>("id"),
                r.Field<string>("name")!,
                r.Field<string?>("species"),
                wateringIntervalDays,
                lastWateredAt,
                createdAt,
                nextWateringDue,
                nextWateringDue < DateTime.UtcNow.Date);
        }
    }
}
