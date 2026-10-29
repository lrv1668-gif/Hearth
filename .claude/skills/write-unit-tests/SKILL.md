---
name: write-unit-tests
description: Write xUnit tests for a Hearth .NET backend service (apps/services/<Service>). Use this skill when the user asks to add tests, write a test suite, create test cases, improve test coverage, or generate unit or endpoint tests for a backend service — even if they just say "add tests for the Weather service" without mentioning the skill by name. Not for frontend (vitest) or renderer (node --test) tests.
---

# Hearth Unit Test Writer

Creates (or extends) an xUnit test project for one Hearth backend service. (Frontend tests are vitest `*.test.ts` files next to the code they test, run with `cd apps/frontend && npm run test`; renderer tests are `apps/renderer/*.test.mjs`, run with `cd apps/renderer && npm test`.)

## The Files

| File | What changes |
|------|-------------|
| `tests/<Service>.Tests/<Service>.Tests.csproj` | Create test project (skip if it already exists) |
| `tests/<Service>.Tests/<Subject>Tests.cs` | Create test class(es) |
| `Hearth.slnx` | Add project entry (new projects only) |

Shared helpers already exist in `tests/Hearth.TestUtilities/` — reference that project, never copy the helpers:

- `FakeHttpMessageHandler(string responseJson, HttpStatusCode status = OK)` — canned HTTP responses. `ResponseJson`/`Status` can be changed mid-test (upstream changes or fails), `ByPath[path] = (status, json)` answers a service that calls several endpoints, and `RequestUris`/`RequestCount`/`LastRequestUri` let you assert what was called and what a cache saved
- `ThrowingHttpMessageHandler` — a network-level failure (throws `HttpRequestException`) rather than an error status
- `TempDatabase` — a real `Database` on a throwaway SQLite file (`tmp.Db`), deleted on `Dispose()`
- `TestDataProtection.Provider` — an `IDataProtectionProvider` for stores that encrypt tokens

For time-dependent code (cache expiry, OAuth state, schedules), inject `TimeProvider` and use `FakeTimeProvider` from the `Microsoft.Extensions.TimeProvider.Testing` package (`using Microsoft.Extensions.Time.Testing;`, `new FakeTimeProvider(start)`, `.Advance(...)`) — don't hand-roll a clock. Likewise assert on logging with `FakeLogger` from `Microsoft.Extensions.Diagnostics.Testing` (`using Microsoft.Extensions.Logging.Testing;`, `logger.Collector.Count`, `logger.LatestRecord`).

Service-specific one-off helpers go in that test project's `Helpers/` folder. If a helper becomes useful to a second project, move it into `Hearth.TestUtilities`.

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

`TargetFramework`, `Nullable`, `ImplicitUsings`, and `IsPackable` come from `tests/Directory.Build.props`; package versions come from `tests/Directory.Packages.props` (central package management). Do **not** redeclare them or add `Version=` attributes.

```xml
<!-- tests/<Service>.Tests/<Service>.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\apps\services\<Service>\<Service>.csproj" />
    <ProjectReference Include="..\Hearth.TestUtilities\Hearth.TestUtilities.csproj" />
  </ItemGroup>
</Project>
```

> Always use `Microsoft.NET.Sdk` — **not** `Microsoft.NET.Sdk.Web` (that starts an ASP.NET host and is wrong for test projects).

Endpoint tests also need `<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />`; clock-dependent tests need `<PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" />` (both already versioned centrally).

If you genuinely need a new package, add a version-less `<PackageReference Include="..." />` here **and** a `<PackageVersion Include="..." Version="x.y.z" />` entry in `tests/Directory.Packages.props`.

---

## Step 2: Write Tests

Naming convention: `Method_Scenario_ExpectedOutcome`

> `ImplicitUsings` does not include third-party namespaces. Add `using Xunit;` and `using Hearth.TestUtilities;` explicitly.

### Pattern A — HTTP-backed (FakeHttpMessageHandler)

```csharp
using System.Net;
using Hearth.TestUtilities;
using Xunit;

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

Stores take `IDatabase`; pass `tmp.Db` (a real SQLite-backed `Database`). An in-memory DB won't work — `Database` opens a fresh connection per query. Run the store's migration before exercising it.

```csharp
using Hearth.TestUtilities;
using Xunit;

private static MyStore Migrated(TempDatabase tmp)
{
    var store = new MyStore(tmp.Db);
    store.Migrate();
    return store;
}

[Fact]
public void Create_NewItem_AppearsInList()
{
    using var tmp = new TempDatabase();
    var store = Migrated(tmp);

    store.Create("Buy milk");

    var listed = Assert.Single(store.List());
    Assert.Equal("Buy milk", listed.Title);
}
```

### Pattern D — Endpoint (WebApplicationFactory)

Boots the real service in memory and swaps its database (and clock, if needed) for test ones. The service's `Program.cs` must end with `public partial class Program;` (Tasks, Spotify and Display do) — add it if missing.

```csharp
using System.Net;
using System.Net.Http.Json;
using Data.Abstractions;
using Hearth.TestUtilities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class MyEndpointTests : IDisposable
{
    private readonly TempDatabase _db = new();
    private readonly WebApplicationFactory<Program> _factory;

    public MyEndpointTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
            host.ConfigureTestServices(services => services.AddKeyedSingleton<IDatabase>("my-key", _db.Db)));
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
- [ ] Referenced both the target service and `Hearth.TestUtilities`
- [ ] Used shared helpers instead of copying them; `FakeTimeProvider` from the package for time; no mocking library added
- [ ] Tests named `Method_Scenario_ExpectedOutcome`
- [ ] Happy path, empty/null response, and error path covered
- [ ] Added test project to `Hearth.slnx`
- [ ] `dotnet test tests/<Service>.Tests/<Service>.Tests.csproj` passes
