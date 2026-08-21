# Coding Standards (Observed)

> **Phase 8** (authoritative; supersedes the Phase 1 draft). Conventions **observed in the existing
> code** — follow them for consistency; this is descriptive, not aspirational. Naming detail is split
> into [NamingConventions.md](NamingConventions.md); patterns into
> [ImplementationPatterns.md](ImplementationPatterns.md); good/bad practices into
> [BestPractices.md](BestPractices.md) / [AntiPatterns.md](AntiPatterns.md). Confidence **High [Verified]**.

## Language & framework
- C# on **`net10.0`** for all 40 projects (since 2026-08-20, GM-744; previously a
  netcoreapp3.1/netstandard2.1/net7.0/net8.0 mix). **[Verified]**
- **No `<Nullable>enable</Nullable>`**, **no `<LangVersion>`** — nullable off; null-safety is manual
  (`string.IsNullOrEmpty/WhiteSpace`). **[Verified]**

## Files & namespaces
- **Block-scoped namespaces** mirroring folders; one primary type per file; some files carry a BOM. **[Verified]**

## Project & folder layout (per project) `[Verified]`
```
Controllers/ (or Worker.cs) · ActionFilters/ · Repository/ · Models/ · Extensions/
Telemetry/ · Properties/ · Dockerfile · appsettings.json · Program.cs [· Startup.cs]
```

## Async
- Public repository/controller methods are `async Task<...>`; `await` throughout. Workers use
  `BackgroundService.ExecuteAsync`. Hot paths intentionally use **dedicated `new Thread`** pools.
  `Async` suffix is applied **inconsistently**. **[Verified]**

## Dependency injection
- Web → `Startup.ConfigureServices`; worker → `Program.ConfigureServices`; net7 → inline. Cohesive
  groups → `IServiceCollection` extension methods. HTTP → `AddHttpClient<I,Impl>`; DB → `AddScoped`;
  cross-cutting → `AddSingleton`. **[Verified]** — [DependencyInjection.md](DependencyInjection.md).

## Configuration access
- `IConfiguration["Section:Key"]` indexer at point-of-use; **no `IOptions<T>` for app settings**
  (only framework options like `ForwardedHeadersOptions`). Typos fail silently (null). **[Verified]**

## HTTP pipeline (web hosts) `[Verified]`
`[dev exception page] → UseResponseCompression(Platform.API+WS only) → UseForwardedHeaders → Swagger
→ UseRouting → UseCors("AllowAllOrigins") → UseAuthorization(inert) → UseEndpoints(MapControllers)`.
CORS is allow-all; `ForwardedHeaders` trusts all upstreams. See [MiddlewarePipeline.md](MiddlewarePipeline.md).

## Controllers `[Verified]`
`[ApiController]`, `[EnableCors("AllowAllOrigins")]`, `[ServiceFilter(typeof(<Filter>))]`; inherit
`BaseController`/`IntegrationControllerBase`; explicit path-versioned routes; open a telemetry
`Activity`, call the repository, `switch` on `StatusCode`, return `Ok`/`BadRequest`.

## Data access `[Verified]`
EF Core 10 LINQ with `AsNoTracking` for reads; **manual mapping via `.Select`** (no AutoMapper);
stored procs for money/reporting; Mongo read-only projection-first; `TransactionScope` (ReadCommitted)
for multi-write ops; **DB-first** (no migrations). See [PersistencePatterns.md](PersistencePatterns.md).

## Error handling `[Verified]`
Per-controller / per-repository `try/catch` → `BaseResponseDTO` with business `StatusCode`. **No
global exception middleware** (dev page only). Some catches include `ex.Message`/`StackTrace` in the
body (debt — [AntiPatterns.md](AntiPatterns.md)).

## Logging & telemetry `[Verified]`
Serilog console-JSON, `ReadFrom.Configuration`; OpenTelemetry `Instrumentation` + `AddTelemetry`;
correlation via OTel trace context (propagated through RabbitMQ headers) + domain `CorrelationId`.
Two client APIs lack Serilog; some lack OTel. See [Logging.md](Logging.md).

## Source hygiene `[Verified]`
- `.pre-commit-config.yaml`: trailing-whitespace, end-of-file-fixer, check-yaml, check-added-large-files.
- `.gitignore`: bin/obj/.vs + `**/appsettings.Development.json`.
- **Secrets committed in base `appsettings.json`** (dev/staging fallbacks, overridden by SSM in
  cluster) — TD-01.

## Documentation & tests `[Verified]`
- Sparse XML doc comments; the boilerplate `// This method gets called by the runtime` left in
  `Startup`. No README beyond a stub (this `docs/` set is the first real documentation).
- Only `Platform.Shared.Test` and `Platform.Sportsbook.API.Test` exist (run by a PR-only CI
  test job); broad automated test coverage is not evident.

## Known inconsistencies (not standards) `[Verified]`
Serilog/OTel adoption uneven; Mongo driver version drift (2.11.6 vs 2.13.1; Swashbuckle and
EF Core were unified by GM-744, 2026-08-20); `Async` suffix
inconsistent; duplicated `MessageBroker` in some services; misspelled identifiers
(`InserOrUpdate…`, `Fibonnaci`, `ProcesssBetPlacement`, `OnBOBetCancellaton`). See
[NamingConventions.md](NamingConventions.md), [AntiPatterns.md](AntiPatterns.md).
