# Test conventions (xUnit)

Loaded when working under `tests/`. Use the `write-unit-tests` skill to scaffold or extend a test project — it has the patterns (HTTP, database, pure logic, endpoint).

- Each service that gets tests has a matching `<Service>.Tests/` project here, added to `Hearth.slnx`.
- Test projects use `Microsoft.NET.Sdk` (not `Microsoft.NET.Sdk.Web`) and inherit `TargetFramework`/`Nullable`/`ImplicitUsings` from `Directory.Build.props` and package versions from `Directory.Packages.props` — don't redeclare them.
- Shared helpers live in `Hearth.TestUtilities/` — reference it rather than copying helpers: `FakeHttpMessageHandler` for HTTP-backed services, `TempDatabase` (real SQLite temp file) for database-backed stores. Don't add a mocking library for `IDatabase`.
- Time-dependent tests use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` — don't hand-roll a clock.
- Endpoint tests use `WebApplicationFactory<Program>` (`Microsoft.AspNetCore.Mvc.Testing`) with the keyed `IDatabase` swapped for a `TempDatabase`.
- Test method naming: `Method_Scenario_ExpectedOutcome`.
