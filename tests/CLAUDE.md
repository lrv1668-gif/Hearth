# Test conventions (xUnit)

Loaded when working under `tests/`. Use the `write-unit-tests` skill to scaffold or extend a test project — it has the patterns (HTTP, database, pure logic, endpoint).

- Each service that gets tests has a matching `<Service>.Tests/` project here, added to `Hearth.slnx`.
- Test projects use `Microsoft.NET.Sdk` (not `Microsoft.NET.Sdk.Web`). `Directory.Build.props` gives every `*.Tests` project `TargetFramework`/`Nullable`/`ImplicitUsings`, the xUnit packages, a global `using Xunit;` and a reference to `Hearth.TestUtilities`, and `Directory.Packages.props` holds the versions — a test `.csproj` only references its service plus any extra test packages (`Mvc.Testing`, `TimeProvider.Testing`, …). Don't redeclare any of it.
- Shared helpers live in `Hearth.TestUtilities/` — use them rather than copying helpers: `FakeHttpMessageHandler` (with `ByPath`, keyed by path or path-and-query, for services that call several endpoints) and `ThrowingHttpMessageHandler` for HTTP-backed services, `TestDataProtection` for stores that encrypt tokens, `TempDatabase` (real SQLite temp file) for database-backed stores, `TempDirectory` for a throwaway data folder, and `HearthApp.Create<Program>(dbKey, db, configure)` for endpoint tests. Don't add a mocking library for `IDatabase`.
- Store tests hold one `TempDatabase` and the migrated store as fields (set up in the constructor, disposed in `Dispose`) rather than creating them in every test. Helpers used by several classes in one project (e.g. `Display.Tests/TestData.cs`, `Rss.Tests/FeedServer.cs`) live in that project.
- Time-dependent tests use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` — don't hand-roll a clock. Log assertions use `FakeLogger` from `Microsoft.Extensions.Diagnostics.Testing`.
- Endpoint tests use `HearthApp.Create<Program>` (a `WebApplicationFactory<Program>` with the keyed `IDatabase` swapped for a `TempDatabase`).
- Test method naming: `Method_Scenario_ExpectedOutcome`.
