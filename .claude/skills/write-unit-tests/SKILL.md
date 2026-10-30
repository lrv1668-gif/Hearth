---
name: write-unit-tests
description: Write xUnit tests for a Hearth .NET backend service (apps/services/<Service>). Use this skill when the user asks to add tests, write a test suite, create test cases, improve test coverage, or generate unit or endpoint tests for a backend service — even if they just say "add tests for the Weather service" without mentioning the skill by name. Not for frontend (vitest) or renderer (node --test) tests.
---

# Hearth Unit Test Writer

Creates (or extends) an xUnit test project for one Hearth backend service. (Frontend tests are vitest `*.test.ts` files next to the code they test, run with `cd apps/frontend && npm run test` — `mockFetch()` in `src/lib/test/fetch.ts` stubs `fetch` and `task()` in `src/lib/test/fixtures.ts` builds a `Task`; renderer tests are `apps/renderer/*.test.mjs`, run with `cd apps/renderer && npm test`.)

## The Files

| File | What changes |
|------|-------------|
| `tests/<Service>.Tests/<Service>.Tests.csproj` | Create test project (skip if it already exists) |
| `tests/<Service>.Tests/<Subject>Tests.cs` | Create test class(es) |
| `Hearth.slnx` | Add project entry (new projects only) |

Shared helpers already exist in `tests/Hearth.TestUtilities/` — reference that project, never copy the helpers:

- `FakeHttpMessageHandler(string responseJson, HttpStatusCode status = OK)` — canned HTTP responses. `ResponseJson`/`Status` can be changed mid-test (upstream changes or fails), `ByPath[path] = (status, json)` answers a service that calls several endpoints (a path-and-query key such as `"/schedule?seasontype=2"` wins over the bare path), and `RequestUris`/`RequestCount`/`LastRequestUri` let you assert what was called and what a cache saved
- `ThrowingHttpMessageHandler` — a network-level failure (throws `HttpRequestException`) rather than an error status; `RequestCount` says how often it was tried
- `TempDatabase` — a real `Database` on a throwaway SQLite file (`Db`, and `Path` to open a second `Database` on it), deleted on `Dispose()`
- `TempDirectory` — a throwaway folder (`Path`) for services that keep files under `DATA_PATH`, deleted on `Dispose()`
- `HearthApp.Create<Program>(dbKey, db, configure)` — the service in memory with its keyed `IDatabase` swapped for a `TempDatabase`; `configure` adds settings and test services
- `TestDataProtection.Provider` — an `IDataProtectionProvider` for stores that encrypt tokens

For time-dependent code (cache expiry, OAuth state, schedules), inject `TimeProvider` and use `FakeTimeProvider` from the `Microsoft.Extensions.TimeProvider.Testing` package (`using Microsoft.Extensions.Time.Testing;`, `new FakeTimeProvider(start)`, `.Advance(...)`) — don't hand-roll a clock. Likewise assert on logging with `FakeLogger` from `Microsoft.Extensions.Diagnostics.Testing` (`using Microsoft.Extensions.Logging.Testing;`, `logger.Collector.Count`, `logger.LatestRecord`).

Helpers shared by several classes in one test project sit in that project (e.g. `Display.Tests/TestData.cs`, `Spotify.Tests/SpotifyEndpointTests.cs`). If a helper becomes useful to a second project, move it into `Hearth.TestUtilities`.

---

## Step 0: Explore the Target Service

Before writing any code, read the service source to understand its dependencies:

1. Read `apps/services/<Service>/Program.cs` and `ServiceCollectionExtensions.cs` — what's registered?
2. Read the fetcher or store file — how many external dependencies? (`HttpClient`? `IDatabase`?)
3. Identify testable units: pure functions, mapping logic, or methods with fakeable deps
4. Read an existing test project for a similar service (e.g. `tests/Quote.Tests/`, `tests/Tasks.Tests/`) and match its style

**Pick your test type** based on what you find:

| Service type | Example | Test approach |
|---|---|---|
| HTTP-backed | `QuoteFetcher`, `WeatherFetcher` | `FakeHttpMessageHandler` |
| Database-backed | `TaskStore`, `ModeStore` | `TempDatabase` (real SQLite) |
| Pure logic | WMO mapping, recurrence math | Direct instantiation, no fakes |
| Endpoint (status codes, JSON shape, health) | `TaskEndpointTests`, `ModeEndpointTests` | `WebApplicationFactory<Program>` + `TempDatabase` |

---

## Step 1: Create the Test Project

`tests/Directory.Build.props` gives every `*.Tests` project `TargetFramework`, `Nullable`, `ImplicitUsings` and `IsPackable`, the xUnit packages (`xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`), a global `using Xunit;` and a reference to `Hearth.TestUtilities`; package versions come from `tests/Directory.Packages.props` (central package management). Do **not** redeclare any of them or add `Version=` attributes — the project only references its service:

```xml
<!-- tests/<Service>.Tests/<Service>.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\..\apps\services\<Service>\<Service>.csproj" />
  </ItemGroup>
</Project>
```

> Always use `Microsoft.NET.Sdk` — **not** `Microsoft.NET.Sdk.Web` (that starts an ASP.NET host and is wrong for test projects).

Endpoint tests also need `<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />`; clock-dependent tests need `<PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />` (both already versioned centrally).

If you genuinely need a new package, add a version-less `<PackageReference Include="..." />` here **and** a `<PackageVersion Include="..." Version="x.y.z" />` entry in `tests/Directory.Packages.props`.

---

## Step 2: Write Tests

Naming convention: `Method_Scenario_ExpectedOutcome`

> `using Xunit;` is global (from `Directory.Build.props`); add `using Hearth.TestUtilities;` explicitly where you use a helper.

### Pattern A — HTTP-backed (FakeHttpMessageHandler)

```csharp
using System.Net;
using Hearth.TestUtilities;

private static MyFetcher MakeFetcher(string json, HttpStatusCode status = HttpStatusCode.OK)
{
    var handler = new FakeHttpMessageHandler(json, status);
    var http = new HttpClient(handler);
    return new MyFetcher(http);
}

[Fact]
public async Task FetchAsync_ValidResponse_ReturnsMappedItem()
{
    var fetcher = MakeFetcher("""[{"field":"value"}]""");
    var result = await fetcher.FetchAsync();
    Assert.NotNull(result);
    Assert.Equal("value", result.Field);
}

[Fact]
public async Task FetchAsync_HttpError_ReturnsNull()
{
    var fetcher = MakeFetcher("", HttpStatusCode.ServiceUnavailable);
    Assert.Null(await fetcher.FetchAsync());
}

[Fact]
public async Task FetchAsync_MalformedJson_ReturnsNull()
{
    var fetcher = MakeFetcher("not-json");
    Assert.Null(await fetcher.FetchAsync());
}
```

### Pattern B — Pure logic (no fakes)

```csharp
[Theory]
[InlineData(0, "Clear sky")]
[InlineData(95, "Thunderstorm")]
[InlineData(999, "Unknown")]
public void Describe_KnownAndUnknownCodes_ReturnsExpected(int code, string expected)
{
    Assert.Equal(expected, MyService.Describe(code));
}
```

### Pattern C — Database-backed (TempDatabase)

Stores take `IDatabase`; pass `_db.Db` (a real SQLite-backed `Database`). An in-memory DB won't work — `Database` opens a fresh connection per query. xUnit makes a new instance of the class per test, so hold the database and the migrated store as fields:

```csharp
using Hearth.TestUtilities;

public sealed class MyStoreTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly MyStore _store;

    public MyStoreTests()
    {
        _store = new MyStore(_db.Db);
        _store.Migrate();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Create_NewItem_AppearsInList()
    {
        _store.Create("Buy milk");

        var listed = Assert.Single(_store.List());
        Assert.Equal("Buy milk", listed.Title);
    }
}
```

(A test that needs the database before migration — e.g. a legacy-row upgrade — keeps `_db` as a field and builds the store itself; see `FrameSettingsStoreTests`.)

### Pattern D — Endpoint (WebApplicationFactory)

Boots the real service in memory and swaps its database (and clock, if needed) for test ones. The service's `Program.cs` must end with `public partial class Program;` (Tasks, Spotify and Display do) — add it if missing.

```csharp
using System.Net;
using System.Net.Http.Json;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class MyEndpointTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly WebApplicationFactory<Program> _factory;

    public MyEndpointTests()
    {
        // Settings and extra test services go in the optional third argument:
        // host => { host.UseSetting("KEY", "value"); host.ConfigureTestServices(services => ...); }
        _factory = HearthApp.Create<Program>("my-key", _db);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _db.Dispose();
    }

    [Fact]
    public async Task Health_WithTestDatabase_ReturnsOk()
    {
        var response = await _factory.CreateClient().GetAsync("/my-prefix/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

Read JSON back with `ServiceDefaults.HearthJson.SnakeCaseLower` so field names match the service's snake_case output.

---

## Step 3: Add to Hearth.slnx

Open `Hearth.slnx` and add the new project entry inside the `<Folder Name="/tests/">` block:

```xml
<Project Path="tests/<Service>.Tests/<Service>.Tests.csproj" />
```

---

## Checklist

- [ ] Read the target service and a similar existing test project before starting
- [ ] Identified test type (HTTP / database / pure logic / endpoint)
- [ ] csproj uses `Microsoft.NET.Sdk`, has no `TargetFramework` or `Version=` attributes
- [ ] Referenced only the target service (xUnit and `Hearth.TestUtilities` come from `Directory.Build.props`)
- [ ] Used shared helpers instead of copying them; `FakeTimeProvider` from the package for time; no mocking library added
- [ ] Tests named `Method_Scenario_ExpectedOutcome`
- [ ] Happy path, empty/null response, and error path covered
- [ ] Added test project to `Hearth.slnx`
- [ ] `dotnet test tests/<Service>.Tests/<Service>.Tests.csproj` passes
