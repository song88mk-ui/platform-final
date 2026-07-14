# Repository Patterns

> Phase 6. The **repository abstraction and code organization** for data access. Storage-mechanism
> patterns (transactions, schema, sync) are in [PersistencePatterns.md](PersistencePatterns.md); DI
> lifetimes in [DependencyInjection.md](DependencyInjection.md). Confidence **High [Verified]**.

## The convention: `IXxxRepository` + `XxxRepository` `[Verified]`
- Every data/integration concern is an **interface + implementation pair** in the project's
  `Repository/` folder (e.g. `IBettingRepository`/`BettingRepository`, `ISportsbookRepository`,
  `IWalletRepository`, `IPlatformRepository`, `IReportsRepository`, `ICasinoRepository`,
  `IBetGeniusRepository`, MTS/GTS auth repos).
- **No generic/base repository, no Unit-of-Work** abstraction over EF — repositories use the
  `DbContext` / `MongoClient` / `HttpClient` / stored procs **directly**. **[Verified]**
- Methods are **async** and (for API repos) return the uniform envelope
  **`Task<BaseResponseDTO<T>>`** — repositories own the mapping of data + status into the response
  contract. **[Verified]**

## Registration styles (recap) `[Verified]`
| Backing | Registration | Examples |
|---------|--------------|----------|
| HTTP integration | `AddHttpClient<I,Impl>` | Betting/Operator/Sportsbook/Casino/RabbitMQ/MTS/GTS/BetGenius repos |
| SQL (EF) | `AddScoped<I,Impl>` (+ `AddDbContext`) | PlatformRepository, BettingRepository, WalletRepository |
| Mongo / cache infra | `AddSingleton` | `RedisCache`, `IMemCache`, `IMessageBroker` |
See [DependencyInjection.md](DependencyInjection.md).

## Repository "shapes" by store
### EF-backed repository `[Verified]`
Holds a scoped `DbContext`; LINQ + `AsNoTracking` for reads; `Add/Update/SaveChanges` (often inside a
`TransactionScope` ReadCommitted) for writes; **full-replace** for curation sets; **stored procs** via
`FromSqlRaw`/`ExecuteSqlRaw` for reporting/wallet/top-markets.

### Mongo-backed repository `[Verified]`
Constructs a `MongoClient` from `ConnectionStrings:MongoDB-Sportsbook`, caches collection handles
(secondary + primary read-pref variants), and reads via `.Find(filter).Project<TProjection>(...)`.
**Read-only** — no write methods. (`Sportsbook.API`, `CashOutStake.Servicev2`, `QTech.API`.)

### Stored-proc gateway `[Verified]`
`Wallet.API/WalletRepository` is a thin wrapper over `ExecuteSqlRawAsync` with `SqlParameter`
in/out arrays — **no EF entities** (`WalletContext` is empty). `AgentReports.API/ReportsRepository`
maps procs to **keyless DbSets** via `FromSqlRaw`.

### HttpClient repository (`BaseRepository` style) `[Verified]` / `[Verified-sibling]`
Typed-`HttpClient` repositories set `BaseAddress` + default headers in the **constructor** from
`API:*`/`Custom:*` config, then GET/POST JSON. (platform-web's `PlatformRepository`/`PlatformRepository2`
use a `BaseRepository.PrepareHeaders` + `GetStreamAsync`/`PostAsJsonAsync` 2-attempt-retry pattern —
the consumer-side mirror. **[Verified-sibling]**.)

## Projection & DTO mapping `[Verified]`
- Mongo reads project to **slim projection types** (`ActiveFixtureProjection`,
  `CashOutMarketFilter`, …) defined in each API's `Models/`.
- Repositories map entities/projections → **response DTOs** (`Platform.Shared/Models/*ResponseDTO`)
  wrapped in `BaseResponseDTO<T>`. The 189 `Models/` DTOs are largely these mapping targets.

## Error handling in repositories `[Verified]`
- Repositories catch exceptions and return `BaseResponseDTO` with `StatusCode.Error` (often including
  `ex.Message`) rather than throwing to the controller — which is why controllers can `switch` on
  `result.StatusCode`. (Stack-trace leakage is a debt item — [TechnicalDebt.md](TechnicalDebt.md).)
- Provider/external repos map upstream failures to specific `StatusCode` values (e.g. `Mts_Error`).

## Caching inside repositories `[Verified]`
Read-through caching is done **in the repository** via `IMemCache.GetCache(key, loader, ttl)` (e.g.
auth tokens, client-code, catalogue lookups) — not in a separate caching layer. See
[Caching.md](Caching.md).

## Grouping & factory registration `[Verified]`
Cohesive repository/provider sets are registered via `IServiceCollection` extension methods
(`AddBettingProviders`, `AddProviders`, RedCat `ServiceExtension`), and providers are resolved through
`IBetProviderFactory`/`ILiveStreamFactory` by `ChannelType`. See
[ImplementationPatterns.md](ImplementationPatterns.md) and [SharedLibraries.md](SharedLibraries.md).

## Consistency of the pattern `[Verified]`
The `IXxxRepository`+`XxxRepository` + `BaseResponseDTO<T>` + read-through-`IMemCache` triad is
applied **uniformly** across the platform — it is the single most repeated structural pattern and the
one to follow when adding data access. Divergences (no-cache client APIs, the empty `WalletContext`
proc gateway) are noted in [CodingStandards.md](CodingStandards.md) / [TechnicalDebt.md](TechnicalDebt.md).
