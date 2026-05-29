---
name: write-unit-tests
description: Write unit tests for a Hearth backend service. Use this skill when the user asks to add tests, write a test suite, create test cases, improve test coverage, or generate unit tests for a service — even if they just say "add tests for X" without mentioning the skill by name.
---

# Hearth Unit Test Writer

Creates an xUnit test project for one Hearth backend service. Every test project touches exactly four locations.

## The Four Files

| File | What changes |
|------|-------------|
| `services/<Service>.Tests/<Service>.Tests.csproj` | Create test project |
| `services/<Service>.Tests/Helpers/FakeHttpMessageHandler.cs` | Create only if service uses HttpClient |
| `services/<Service>.Tests/<Subject>Tests.cs` | Create test class(es) |
| `Hearth.slnx` | Add project entry |

---

## Step 0: Explore the Target Service

Before writing any code, read the service source to understand its dependencies:

1. Read `services/<Service>/Program.cs` — what's registered? What are the constructor deps?
2. Read the fetcher or store file — how many external dependencies? (`HttpClient`? `IDatabase`?)
3. Identify testable units: pure functions, mapping logic, or methods with mockable deps

**Pick your test type** based on what you find:

| Service type | Example | Test approach |
|---|---|---|
| HTTP-backed | `QuoteFetcher`, `WeatherFetcher` | `FakeHttpMessageHandler` |
| Database-backed | `TaskStore`, `RssStore` | Mock `IDatabase` interface |
| Pure logic | WMO mapping, recurrence math | Direct instantiation, no mocks |

---

## Step 1: Create the Test Project

```xml
<!-- services/<Service>.Tests/<Service>.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\<Service>\<Service>.csproj" />
  </ItemGroup>
</Project>
```

> Always use `Microsoft.NET.Sdk` — **not** `Microsoft.NET.Sdk.Web` (that starts an ASP.NET host and is wrong for test projects).

---

## Step 2: Add FakeHttpMessageHandler (HTTP-backed services only)

Copy this helper verbatim. It's reusable across any HTTP-backed service test project.

```csharp
// services/<Service>.Tests/Helpers/FakeHttpMessageHandler.cs
using System.Net;

namespace <Service>.Tests.Helpers;

public sealed class FakeHttpMessageHandler(string responseJson, HttpStatusCode status = HttpStatusCode.OK)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
```

---

## Step 3: Write Tests

Naming convention: `Method_Scenario_ExpectedOutcome`

> **Important:** `ImplicitUsings` does not include third-party namespaces. Always add `using Xunit;` explicitly (or a `GlobalUsings.cs` with `global using Xunit;`).

### Pattern A — HTTP-backed (FakeHttpMessageHandler)

```csharp
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
    var result = await fetcher.FetchAsync();
    Assert.Null(result);
}

[Fact]
public async Task FetchAsync_MalformedJson_ReturnsNull()
{
    var fetcher = MakeFetcher("not-json");
    var result = await fetcher.FetchAsync();
    Assert.Null(result);
}
```

### Pattern B — Pure logic (no mocks)

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

### Pattern C — Database-backed (IDatabase mock)

Use `NSubstitute` or `Moq` to mock `IDatabase`. Add the package to the csproj:

```xml
<PackageReference Include="NSubstitute" Version="*" />
```

```csharp
var db = Substitute.For<IDatabase>();
db.ExecuteAsync(Arg.Any<string>(), Arg.Any<object?>()).Returns(1);
var store = new MyStore(db);
```

---

## Step 4: Add to Hearth.slnx

Open `Hearth.slnx` and add the new project entry inside the `<Folder Name="/services/">` block, adjacent to the service it tests:

```xml
<Project Path="services/<Service>.Tests/<Service>.Tests.csproj" />
```

---

## Checklist

- [ ] Read the target service before starting
- [ ] Identified test type (HTTP / database / pure logic)
- [ ] Created `<Service>.Tests.csproj` with `Microsoft.NET.Sdk` (not Web)
- [ ] Added `ProjectReference` to the target service
- [ ] Created `FakeHttpMessageHandler` if service uses `HttpClient`
- [ ] Tests named `Method_Scenario_ExpectedOutcome`
- [ ] Happy path covered
- [ ] Empty/null response covered
- [ ] Error/exception path covered
- [ ] Added test project to `Hearth.slnx`
- [ ] `dotnet test` passes with all tests green

---

## Running Tests

```bash
cd services/<Service>.Tests
dotnet test
# or from repo root:
dotnet test services/<Service>.Tests
```
