# Backend conventions (.NET 10 / ASP.NET Core Minimal APIs)

Loaded when working under `apps/services/`. Project-wide guidance is in the root [`CLAUDE.md`](../../CLAUDE.md); new services go through the `hearth-service` skill, which has the full wiring checklist (Compose, Caddy, Vite proxy, CI image, health checks, docs).

## Projects and packages

- `TargetFramework`/`Nullable`/`ImplicitUsings` and all NuGet package versions are centralized in `Directory.Build.props` + `Directory.Packages.props` — new `.csproj` files don't redeclare them; add packages as a version-less `<PackageReference Include="..." />` plus a matching `<PackageVersion>` in `Directory.Packages.props`.
- Every service references `ServiceDefaults` and calls `AddHearthWebDefaults()` in `ServiceCollectionExtensions.cs`, including services with no database. `Program.cs` starts with `HealthProbe.ExitIfProbe(args);`, and the service maps `app.MapHearthHealth("<prefix>")`.
- There are no per-service Dockerfiles — `Dockerfile` here builds every service from the `SERVICE` build arg.

## Data

- SQLite stays inside `Data/`: database-backed services reference `Data.Abstractions` (for `IDatabase`) and `Data` (for registration), and never add `Microsoft.Data.Sqlite` directly.
- Register with `services.AddSqliteDatabase("key", "service.db")` in `ServiceCollectionExtensions.cs` — it reads `DB_PATH` and does the keyed registration; never call `AddKeyedSingleton<IDatabase>` yourself.
- Stores (e.g. `TaskStore`) inject `IDatabase` via `[FromKeyedServices("key")]` — never the concrete `Database` class or SQLite types.
- Each service owns its SQLite file at `DB_PATH` (env var, defaults to `<service>.db`).

## Behaviour

- JSON is snake_case (`JsonNamingPolicy.SnakeCaseLower`, set by `AddHearthWebDefaults()`) — keep record property names PascalCase in C#.
- Cache third-party API responses with `IFusionCache.GetOrSetAsync` (registered by `AddHearthCache()`, plus `AddSqliteDistributedCache(key)` for a restart-surviving L2) with fail-safe on — don't hand-write cache tables or staleness checks. Return 502 only when the call throws (nothing cached at all).
- Required environment variables: check them in the endpoint with `config.RequireOrFail(...)`, which logs a `LogError` before returning the 503, and register `.AddRequiredConfig(...)` health checks. Env vars live in `apps/services/<Service>/.env` (gitignored) with a `.env.example` beside it, loaded by `env_file` in `infra/docker-compose.yml`.
- Read the household's location/time zone through `HomeLocationClient` (`AddHearthHomeLocation()`), never `LATITUDE`/`LONGITUDE`/`TZ` directly — it falls back to them; key upstream caches by the coordinates.
- Read numeric config with `config.GetValue<double>(...)` (invariant culture), never `double.Parse(config[...])`; format URLs with `string.Create(CultureInfo.InvariantCulture, ...)`.
