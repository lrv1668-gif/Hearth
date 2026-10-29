---
name: hearth-service
description: Add a new .NET backend service to Hearth (apps/services/<Service>) and wire it through Docker Compose, Caddy, the Vite dev proxy, CI image publishing, health checks and the docs. Use this skill whenever the user asks for a new backend service, API, or data source that no existing service covers — including when a new widget needs data no service provides yet — even if they don't mention the skill by name.
---

# Hearth Service Creator

Adds one ASP.NET Core 10 Minimal API service. Every service is small and single-domain, owns its own SQLite file (if it stores anything), and is reached same-origin through Caddy at `/<prefix>`.

Throughout: `<Service>` is the PascalCase project name (`Birds`), `<prefix>` the lowercase route/compose name (`birds`), `<port>` the next free port.

## Step 0: Explore first

1. Pick the closest existing service and read it end to end:
   - `apps/services/Quote/` — upstream API + FusionCache, no database, no secrets
   - `apps/services/Birds/` — upstream API with an API key, SQLite-backed L2 cache, home location
   - `apps/services/Tasks/` — database-backed CRUD store with a migration
2. Pick `<port>`: the next unused one after the services' `ASPNETCORE_HTTP_PORTS` in `infra/docker-compose.yml` (8081–8091 today). The localhost mappings in `infra/docker-compose.override.yml` also use 8092/8093 (renderer, Dozzle) — skip those.
3. Confirm the domain logic belongs in the backend (third-party calls, caching, math) — it does by default; the frontend stays a thin renderer.

## Step 1: The project

```
apps/services/<Service>/
  <Service>.csproj
  Program.cs
  Extensions/ServiceCollectionExtensions.cs
  Extensions/WebApplicationExtensions.cs
  Records/…            # response records (PascalCase properties; JSON is snake_case)
  .env.example          # only if it needs env vars
```

**`<Service>.csproj`** — `TargetFramework`/`Nullable`/`ImplicitUsings` and every package version come from `apps/services/Directory.Build.props` / `apps/services/Directory.Packages.props`; don't redeclare them or add `Version=`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <AssemblyName><Service></AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <!-- database-backed only: -->
    <ProjectReference Include="..\Data.Abstractions\Data.Abstractions.csproj" />
    <ProjectReference Include="..\Data\Data.csproj" />
    <!-- always: -->
    <ProjectReference Include="..\ServiceDefaults\ServiceDefaults.csproj" />
  </ItemGroup>
  <ItemGroup>
    <!-- only if it reads a local .env when run outside Docker: -->
    <PackageReference Include="DotNetEnv" />
  </ItemGroup>
  <ItemGroup>
    <InternalsVisibleTo Include="<Service>.Tests" />
  </ItemGroup>
</Project>
```
Never add `Microsoft.Data.Sqlite` directly — SQLite stays inside `apps/services/Data`. A new package is a version-less `<PackageReference>` here plus a `<PackageVersion>` in `apps/services/Directory.Packages.props`.

**`Program.cs`** — the health probe must be the first line:
```csharp
using ServiceDefaults;
using <Service>.Extensions;
using DotNetEnv; // only with DotNetEnv

// Docker's health check runs this binary with --health (see HealthProbe)
HealthProbe.ExitIfProbe(args);

Env.Load(); // only with DotNetEnv

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddServicesFor<Service>();
var app = builder.Build();
app.InitializeWebAppFor<Service>();
app.Run();

public partial class Program; // lets endpoint tests use WebApplicationFactory<Program>
```

**`ServiceCollectionExtensions.cs`** — every service calls `AddHearthWebDefaults()` (snake_case JSON; no CORS — everything is same-origin via Caddy) and registers health checks:
```csharp
public static void AddServicesFor<Service>(this IServiceCollection services)
{
    services.AddSqliteDatabase("<prefix>", "<prefix>.db"); // reads DB_PATH; never AddKeyedSingleton<IDatabase> yourself
    services.AddSqliteDistributedCache("<prefix>");        // restart-surviving L2 for upstream caches
    services.AddHearthCache();                             // FusionCache
    services.AddHttpClient<<Service>Fetcher>();
    services.AddHearthHomeLocation();                      // if it needs the household's location/time zone

    services.AddHearthWebDefaults();
    services.AddHealthChecks()
        .AddDatabase("<prefix>")
        .AddRequiredConfig("MY_API_KEY")
        .AddHomeLocation();
}
```
Drop the lines it doesn't need. A service with no database or config still gets `AddHearthWebDefaults()`; `MapHearthHealth` works without extra checks.

**`WebApplicationExtensions.cs`** — endpoints plus the health route:
```csharp
public static void InitializeWebAppFor<Service>(this WebApplication app)
{
    app.Add<Service>Endpoints();
    app.MapHearthHealth("<prefix>"); // GET /<prefix>/health — Settings → Diagnostics and Docker read it
}
```

### Backend rules

- **Stores** take `IDatabase` via `[FromKeyedServices("<prefix>")]` — never the concrete `Database` or SQLite types. Run the store's `Migrate()` at startup (see `apps/services/Tasks/Extensions/WebApplicationExtensions.cs`).
- **Upstream caching:** `IFusionCache.GetOrSetAsync` with `IsFailSafeEnabled = true` — no hand-written cache tables or staleness checks. Key caches by the coordinates when location-dependent. Return 502/503 only when the call throws (nothing cached at all). See `apps/services/Birds/Extensions/WebApplicationExtensions.cs`.
- **Required env vars:** `config.RequireOrFail(app.Logger, …, "MY_API_KEY")` in the endpoint — it logs a `LogError` and returns the 503 when one is missing.
- **Location/time zone:** read through `HomeLocationClient`, never `LATITUDE`/`LONGITUDE`/`TZ` directly (it falls back to them).
- **Numbers and URLs:** `config.GetValue<double>(…)` (invariant culture), never `double.Parse(config[…])`; build URLs with `string.Create(CultureInfo.InvariantCulture, …)`.
- **Records:** PascalCase properties; the snake_case JSON comes from `AddHearthWebDefaults()`.

## Step 2: Environment variables (if any)

1. `apps/services/<Service>/.env.example` — every var, commented, with where to get keys. The real `apps/services/<Service>/.env` is gitignored; create an empty one so Compose starts.
2. README → Configuration: a `**<Service>** (\`apps/services/<Service>/.env\`)` block.

## Step 3: Wire it up

| File | Add |
|------|-----|
| `Hearth.slnx` | `<Project Path="apps/services/<Service>/<Service>.csproj" />` in the `/services/` folder |
| `infra/docker-compose.yml` | A service block (below), plus `<prefix>-data:` under `volumes:` if it has a database |
| `infra/docker-compose.override.yml` | `<prefix>: ports: ["127.0.0.1:<port>:<port>"]`, and `<PREFIX>_URL=http://<prefix>:<port>` under `frontend.environment` |
| `infra/Caddyfile` | `handle /<prefix>* { reverse_proxy <prefix>:<port> }` alongside the others |
| `apps/frontend/vite.config.ts` | `'/<prefix>': process.env.<PREFIX>_URL ?? 'http://localhost:<port>'` in the dev proxy |
| `apps/frontend/src/lib/constants/services.ts` | `{ id: '<prefix>', label: '…' }` so Settings → Diagnostics lists it |
| `.github/workflows/ci.yml` | `- { service: <prefix>, project: <Service> }` in the `publish-images` matrix |

There are no per-service Dockerfiles — `apps/services/Dockerfile` builds every service from the `SERVICE` build arg.

```yaml
    <prefix>:
        <<: *defaults
        image: ${HEARTH_IMAGE_PREFIX:-hearth}-<prefix>:${HEARTH_TAG:-latest}
        healthcheck:
            <<: *probe
            test: ["CMD", "/app/service", "--health", "/<prefix>/health"]
        build:
            context: ./src
            args:
                SERVICE: <Service>
        env_file:                       # only with env vars
            - ./src/<Service>/.env
        environment:
            - DB_PATH=/data/<prefix>.db # only with a database
            - ASPNETCORE_HTTP_PORTS=<port>
            - DISPLAY_URL=http://display:8090 # only with AddHearthHomeLocation
        volumes:
            - <prefix>-data:/data       # only with a database
```

## Step 4: Frontend access

Add the response interfaces and an `api.<prefix>` namespace on `ApiClient` in `apps/frontend/src/lib/api.ts` (relative paths, the private `get<T>`/`post<T>` helpers). The widget, store and polling are the `hearth-widget` skill's job.

## Step 5: Tests

Use the `write-unit-tests` skill: `tests/<Service>.Tests/`, added to `Hearth.slnx`. Cover the fetcher's mapping and failure paths, the store against `TempDatabase`, and at least `GET /<prefix>/health` with `WebApplicationFactory<Program>`.

## Step 6: Docs

- `docs/SOFTWARE-DESIGN.md` — a row in the Backend Services table (service, port, status, responsibility) and a `## <Service> Service` section (endpoints, env vars, caching, frontend integration), matching the others
- `CLAUDE.md` — the `apps/services/` entry in Directory Structure
- `README.md` — the service count and list in the "How it works" diagram, and the Configuration block
- `site/` — the host's service list (the diagram in `site/index.html`) and the status table in `site/build.html`
- `docs/PRODUCT.md` — if it changes product scope

## Checklist

- [ ] Read a similar service end to end; picked a free port
- [ ] csproj inherits shared props; no `Microsoft.Data.Sqlite`; `ServiceDefaults` referenced
- [ ] `HealthProbe.ExitIfProbe(args)` first in `Program.cs`; `public partial class Program;` at the end
- [ ] `AddHearthWebDefaults()`, health checks, `MapHearthHealth("<prefix>")`
- [ ] Stores use `[FromKeyedServices]` `IDatabase`; upstream calls cached with fail-safe FusionCache
- [ ] Missing env vars → `RequireOrFail` (logs `LogError`, returns 503); `.env.example` + README Configuration
- [ ] `Hearth.slnx`, `infra/docker-compose.yml` (+ volume), override (port + frontend env), `infra/Caddyfile`, `vite.config.ts`, `services.ts`, CI `publish-images` matrix
- [ ] `api.<prefix>` namespace in `api.ts`
- [ ] Test project added; `dotnet build Hearth.slnx && dotnet test Hearth.slnx` pass
- [ ] `docs/SOFTWARE-DESIGN.md`, `CLAUDE.md`, `README.md`, `site/` updated
- [ ] `cd infra && docker compose up --build` → `curl localhost:<port>/<prefix>/health` is healthy and Settings → Diagnostics lists it
