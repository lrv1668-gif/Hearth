---
name: write-unit-tests
description: Write unit tests for a Hearth backend service. Use this skill when the user asks to add tests, write a test suite, create test cases, improve test coverage, or generate unit tests for a service — even if they just say "add tests for X" without mentioning the skill by name.
---

# Hearth Unit Test Writer

Creates (or extends) an xUnit test project for one Hearth backend service.

## The Files

| File | What changes |
|------|-------------|
| `tests/<Service>.Tests/<Service>.Tests.csproj` | Create test project (skip if it already exists) |
| `tests/<Service>.Tests/<Subject>Tests.cs` | Create test class(es) |
| `Hearth.slnx` | Add project entry (new projects only) |

Shared helpers already exist in `tests/Hearth.TestUtilities/` — reference that project, never copy the helpers:

- `FakeHttpMessageHandler(string responseJson, HttpStatusCode status = OK)` — canned HTTP responses
- `TempDatabase` — a real `Database` on a throwaway SQLite file (`tmp.Db`), deleted on `Dispose()`

Service-specific one-off handlers (e.g. `Quote.Tests/Helpers/ThrowingHttpMessageHandler.cs`) go in that test project's `Helpers/` folder. If a helper becomes useful to a second project, move it into `Hearth.TestUtilities`.

---

## Step 0: Explore the Target Service

Before writing any code, read the service source to understand its dependencies:

1. Read `src/<Service>/Program.cs` and `ServiceCollectionExtensions.cs` — what's registered?
2. Read the fetcher or store file — how many external dependencies? (`HttpClient`? `IDatabase`?)
3. Identify testable units: pure functions, mapping logic, or methods with fakeable deps
4. Read an existing test project for a similar service (e.g. `tests/Quote.Tests/`, `tests/Tasks.Tests/`) and match its style

**Pick your test type** based on what you find:

| Service type | Example | Test approach |
|---|---|---|
| HTTP-backed | `QuoteFetcher`, `WeatherFetcher` | `FakeHttpMessageHandler` |
| Database-backed | `TaskStore`, `ModeStore` | `TempDatabase` (real SQLite) |
| Pure logic | WMO mapping, recurrence math | Direct instantiation, no fakes |

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
    <ProjectReference Include="..\..\src\<Service>\<Service>.csproj" />
    <ProjectReference Include="..\Hearth.TestUtilities\Hearth.TestUtilities.csproj" />
  </ItemGroup>
</Project>
```

> Always use `Microsoft.NET.Sdk` — **not** `Microsoft.NET.Sdk.Web` (that starts an ASP.NET host and is wrong for test projects).

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

---

## Step 3: Add to Hearth.slnx

Open `Hearth.slnx` and add the new project entry inside the `<Folder Name="/tests/">` block:

```xml
<Project Path="tests/<Service>.Tests/<Service>.Tests.csproj" />
```

---

## Checklist

- [ ] Read the target service and a similar existing test project before starting
- [ ] Identified test type (HTTP / database / pure logic)
- [ ] csproj uses `Microsoft.NET.Sdk`, has no `TargetFramework` or `Version=` attributes
- [ ] Referenced both the target service and `Hearth.TestUtilities`
- [ ] Used shared helpers instead of copying them; no mocking library added
- [ ] Tests named `Method_Scenario_ExpectedOutcome`
- [ ] Happy path, empty/null response, and error path covered
- [ ] Added test project to `Hearth.slnx`
- [ ] `dotnet test tests/<Service>.Tests/<Service>.Tests.csproj` passes
