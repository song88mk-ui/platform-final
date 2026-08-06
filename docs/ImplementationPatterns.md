# Implementation Patterns

> **Phase 8 — Implementation Pattern Discovery** (authoritative; supersedes the Phase 1 draft).
> Catalogue of how this team **actually** builds software in platform-final. **Descriptive, not
> prescriptive** — follow these; do not introduce new patterns. Confidence **High [Verified]** unless
> noted. Companions: [CodingStandards.md](CodingStandards.md), [NamingConventions.md](NamingConventions.md),
> [BestPractices.md](BestPractices.md), [AntiPatterns.md](AntiPatterns.md). Deep dives:
> [RepositoryPatterns.md](RepositoryPatterns.md), [DependencyInjection.md](DependencyInjection.md).

## Patterns the brief asks about — present / absent (quick map)
| Pattern | Status | Where |
|---------|--------|-------|
| Repository | **Used (core)** | `IXxxRepository`+`XxxRepository` everywhere |
| Service | **Used (narrow)** | `ParlayBonusService`; `*.Service` = hosted workers, not domain services |
| Factory | **Used** | `IBetProviderFactory`, `ILiveStreamFactory` (by `ChannelType`) |
| Builder | **Absent (as a pattern)** | only framework builders (`HostBuilder`); `BetBuilder` is a domain feature, not GoF Builder |
| Extension methods | **Used (core)** | `IServiceCollection` extensions + `Platform.Shared/Extensions` |
| Validators | **No framework** | inline + ActionFilters + business `StatusCode` (no FluentValidation/DataAnnotations) |
| Mappers | **Manual** | LINQ `.Select(...)` entity/Mongo→DTO (no AutoMapper/Mapster) |
| Helpers | **Used** | `OddsTypeHelper`, `Formatting`, `DecimalJsonConverter`, `Helper`/`IHelper` |
| Background services | **Used (core)** | `Worker : BackgroundService` + `AddHostedService` (18) |
| Event handling | **Used (messaging)** | `event EventHandler<BasicDeliverEventArgs> MessageReceived` on `MessageBroker` |
| Logging | **Serilog** | console JSON, `ReadFrom.Configuration` |
| Exception handling | **Per-controller try/catch** | → `BaseResponseDTO`/`StatusCode`; no global handler |
| Feature flags | **Plain config keys** | `Custom:*` booleans (no FeatureManagement) |
| CQRS / MediatR | **Absent** | not used |

## 1. Dependency Injection `[Verified]`
Built-in `Microsoft.Extensions.DependencyInjection` only. Web → `Startup.ConfigureServices`; workers →
`Program.ConfigureServices`; net7 → inline `Program`. Cohesive groups via `IServiceCollection`
extension methods (`AddBettingProviders`, `AddProviders`, RedCat `ServiceExtension`, `AddTelemetry`).
Lifetimes: Singleton (MemCache/Instrumentation/MessageBroker/Redis), Scoped (DbContext/DB repos/
filters), typed `HttpClient` for HTTP integrations. **No `IOptions<T>` for app config** — read via
`IConfiguration["A:B"]`. See [DependencyInjection.md](DependencyInjection.md).

## 2. Repository pattern `[Verified]`
`IXxxRepository`+`XxxRepository` pair per concern in `Repository/`; **no UoW / generic repo**; async,
returns `Task<BaseResponseDTO<T>>`. Four backing flavors: EF LINQ, stored procs, Mongo driver,
HttpClient. Read-through caching inside the repo. See [RepositoryPatterns.md](RepositoryPatterns.md).

## 3. Service pattern `[Verified]`
- **Domain service**: rare — `ParlayBonusService`/`IParlayBonusService` is the clearest (encapsulates
  parlay-bonus eligibility, opens its own `BettingContext`).
- **`*.Service` projects are hosted workers**, not domain services — naming overlap. **[Verified]**
- Most "business logic" lives **in repositories**, not separate service classes. **[Verified]**

## 4. Factory + Strategy `[Verified]`
`IBetProviderFactory.Resolve(int channelType)` enumerates injected `IEnumerable<IBetProvider>` and
matches by `ChannelType` (BG/EM/OD/SR); `ILiveStreamFactory` mirrors it. Add a provider = implement
the interface + register. See [SharedLibraries.md](SharedLibraries.md) §3.

### How to resolve a provider from a worker service (the canonical recipe)
Reference implementations: `Platform.Placement.Servicev2` (placement), `Platform.Provider.Service`
(cancellation/settlement/cashout bet update), `Platform.CashOut.Service` (cashout gate).

1. **The message must carry the channel.** Use the `ChannelTypeId` property on the message model
   (`OnBetPlacement`, `OnBetSettlement`, `OnCashoutBetSettlement` all default it to
   `(int)ChannelType.BG`). If a flow re-builds a payload (e.g. the live-cashout re-quote), copy
   `ChannelTypeId` across or the default silently wins.
2. **Register with the per-service `AddBettingProviders()` extension**
   (`Extensions/ServiceCollectionBettingExtensions.cs`, copy the one in `Platform.Placement.Servicev2`):
   Scoped `IBetProviderFactory`, `AddHttpClient` for the GTS **and** MTS repositories, Scoped
   `MtsBetProvider` + `GTSBetProvider`, `IParlayBonusService`, Singleton `IMemCache`.
3. **Both providers are constructed on every `Resolve`** (the factory takes
   `IEnumerable<IBetProvider>`), so appsettings must contain the API endpoints for **both**
   providers even if only one is used: `API:GTSAuthAPI|GTSAPI|MTSAuthAPI|MTSMaxStakeAPI`
   (+ `Custom:GTSClientID|GTSClientSecret`) — a missing key throws `new Uri(null)` at resolve time.
4. **From a Singleton repository/worker**: inject `IServiceScopeFactory`, then per message
   `using var scope = _scopeFactory.CreateScope();` →
   `scope.ServiceProvider.GetRequiredService<IBetProviderFactory>()` → `Resolve(message.ChannelTypeId)`.
5. **Never add call-site provider flags.** Providers self-gate on `Custom:IsProviderEnabled`
   (disabled ⇒ auto-accept); the legacy `Custom:IsMTSEnabled` pattern is retired.
6. **The MTS SDK is provider-owned** (`MtsBetProvider.EnsureInitialized`, reads `App.config` via
   `MtsSdk.GetConfiguration()` — the service needs an `App.config`). Do not open an `MtsSdk` in a
   `Worker`.

## 5. Extension methods `[Verified]`
Two kinds: (a) **`IServiceCollection` registration extensions** (the DI grouping idiom); (b)
**type extensions** in `Platform.Shared/Extensions` (DateTime/Decimal/String/Hash/List/Int/Object/
Enum + bet-audit). Plus the private `Infra.Extensions.Telemetry` `AddTelemetry`.

## 6. Mapping — manual via LINQ `[Verified]`
**No AutoMapper/Mapster.** Repositories map entity/Mongo-projection → response DTO **by hand**, almost
always via `.Select(x => new XxxDTO { ... })` (674 `.Select` uses). DTO targets live in
`Platform.Shared/Models` (189). Projection-first for Mongo (`.Project<T>()`). Consequence: mapping is
explicit and verbose but transparent. See [PersistencePatterns.md](PersistencePatterns.md).

## 7. Validation — filters + inline + StatusCode `[Verified]`
**No FluentValidation/DataAnnotations framework** (one `[Required]`-style attribute on
`SportsbookDynamic` aside). Validation happens at three levels:
1. **ActionFilters** (`APIKeyFilter`/`AuthorizationFilter`/`PassKeyFilter`) — auth/headers/client.
2. **Inline guards** in repositories (`string.IsNullOrEmpty`, range checks) → set a business
   `StatusCode`.
3. **Business `StatusCode` enum** (100–157) is the validation *result vocabulary* returned in
   `BaseResponseDTO`. See [BusinessRules.md](BusinessRules.md), [PublicApiSurface.md](PublicApiSurface.md).

## 8. Messaging & event handling `[Verified]`
- **Contracts:** POCO `On*` classes in `Platform.Shared/MessageModels`.
- **Event mechanism:** `IMessageBroker` exposes `event EventHandler<BasicDeliverEventArgs>
  MessageReceived`; workers subscribe (`+= MessageReceivedEvent`) and `switch` on `MessageType`.
- **Publish** two ways: direct `IMessageBroker.Publish<T>` (services) or over HTTP via `RabbitMQ.API`
  (`IRabbitMQRepository`) for APIs.
- **Ack:** auto vs **manual** (`ConnectWithManualAck`) for loss-sensitive consumers.
- **Hot-path fan-out:** 5 workers use `new Thread` + `BlockingCollection` round-robin (CashOut,
  Placement, Operator.Servicev2, Operator.Cashout, Client.TopMarket). See [Hosting.md](Hosting.md).
- **Dual-channel:** `Provider.Service` adds **Ably** for GTS assessment. **[Verified]**
- **Note:** some services carry a **duplicated** `IMessageBroker`/`MessageBroker` copy in their own
  `Utility/` (e.g. `BetStatus.Service`) instead of using `Platform.Shared`'s — see
  [AntiPatterns.md](AntiPatterns.md).

## 9. Background services / jobs `[Verified]`
`Worker : BackgroundService.ExecuteAsync` is the unit of work; real init (broker connect, preload,
thread pools) happens there. Jobs are `while(!stoppingToken){ work; Task.Delay }` loops (CCF 300s;
Resettlement Fibonacci backoff). 4 services `UseWindowsService`. See [StartupFlow.md](StartupFlow.md),
[Hosting.md](Hosting.md).

## 10. Caching `[Verified]`
Read-through via `IMemCache.GetCache(key, loader, hours)` (per-pod `MemoryCache.Default`); Redis only
in `ClientSportsbook.API`; event-driven invalidation via `CacheSync.Service`. See
[Caching.md](Caching.md), [CacheInvalidation.md](CacheInvalidation.md).

## 11. Telemetry & logging `[Verified]`
Per-project `Telemetry/Instrumentation.cs` (`ActivitySource`+`Meter` named `Platform.<Project>`) wired
via `AddTelemetry`; OTLP/gRPC export. Serilog console-JSON, `ReadFrom.Configuration`; trace context
propagated through RabbitMQ headers. See [Logging.md](Logging.md).

## 12. Edge auth via ActionFilters `[Verified]`
`[ServiceFilter(typeof(<Filter>))]` at controller level; `OnActionExecuting` short-circuits with
`BadRequestObjectResult`+`StatusCode`. No ASP.NET auth scheme. See [Authentication.md](Authentication.md),
[Authorization.md](Authorization.md).

## 13. Response & exception shape `[Verified]`
Uniform `BaseResponseDTO<T>` { business `StatusCode`, Message, Result }; success/maintenance → 200,
else 400-with-body. Controllers `try/catch` → `StatusCode.Error` (sometimes with `ex.Message`/stack);
**no global exception middleware** (dev page only). See [MiddlewarePipeline.md](MiddlewarePipeline.md).

## 14. Feature flags `[Verified]`
Plain `Custom:*` boolean/string config keys (`IsProviderEnabled`, `IsMTSEnabled`, `IsProduction`,
`UseMessagePack`, `TestBetClientCodes`, `Telemetry:Enabled`, `Fibonnaci`). No flag framework; toggling
needs a config change + restart. See [Configuration.md](Configuration.md).

## 15. Concurrency `[Verified]`
`async/await` throughout the request path; **dedicated `new Thread` pools** on hot queues;
`TransactionScope` (ReadCommitted) for multi-write SQL operations; `CancellationToken` on UI/cashout
timers (platform-web). See [PersistencePatterns.md](PersistencePatterns.md).

## Patterns explicitly NOT used `[Verified]`
CQRS/MediatR, AutoMapper/Mapster, FluentValidation, GoF Builder, `IOptions<T>` app config,
third-party DI container, EF migrations, distributed cache, ASP.NET authentication/authorization.
