# Common Services (Quick Reference)

> **Phase 9 — Repository Intelligence.** Fast lookup of the reusable services/abstractions an engineer
> reaches for in platform-final. Authoritative detail: [SharedLibraries.md](../SharedLibraries.md),
> [ImplementationPatterns.md](../ImplementationPatterns.md), [RepositoryPatterns.md](../RepositoryPatterns.md).
> All **[Verified]**.

## The reach-for-these table
| Need | Use | Lifetime | Where |
|------|-----|----------|-------|
| Publish/consume RabbitMQ | `IMessageBroker` / `MessageBroker` | Singleton | `Platform.Shared/Utility/MessageBroker.cs` |
| In-proc cache (read-through) | `IMemCache.GetCache(key, loader, hours)` | Singleton | `Platform.Shared/Utility/MemCache.cs` |
| Pick a bet provider by channel | `IBetProviderFactory.Resolve(channelTypeId)` | Scoped | `Platform.Shared/Providers/` |
| Pick a livestream provider | `ILiveStreamFactory.Resolve(channelType)` | Scoped | `Platform.Shared/Providers/` |
| Parlay-bonus eligibility | `IParlayBonusService.GetParlayBonus(...)` | — (news up `BettingContext`) | `Platform.Shared/Services/ParlayBonusService .cs` |
| MTS/GTS/BetGenius auth tokens | `IMTSAuthRepository`, `IGTSAuthRepository`, `IBetGeniusRepository` | HttpClient | `Platform.Shared/Repositories/` |
| MTS max stake | `IMTSMaxStakeRepository` | HttpClient | `Platform.Shared/Repositories/` |
| OpenTelemetry span/metric | `Instrumentation.ActivitySource` / `Meter` | Singleton | each project `Telemetry/Instrumentation.cs` |
| Wire OTel | `services.AddTelemetry(new TelemetrySettings(...))` | — | `Infra.Extensions.Telemetry` (private pkg) |
| Publish to MQ from an API | `IRabbitMQRepository` → `RabbitMQ.API` | HttpClient | per-API `Repository/` |

## Bet providers (strategy implementations) `[Verified]`
- `MtsBetProvider` — ChannelType **SR** (Sportradar MTS SDK).
- `GTSBetProvider` — ChannelType **BG** (BetGenius/GTS HTTP; supports delayed acceptance).
- `BetGeniusLiveStream` — `ILiveStreamProvider`, ChannelType BG.
Register a new one: implement `IBetProvider` with a distinct `ChannelType`, add to the provider
extension method; the factory dispatches. See [SharedLibraries.md](../SharedLibraries.md) §3.

## DI-grouping extension methods (cohesive registration) `[Verified]`
- `services.AddBettingProviders()` — `Betting.API` (MTS+GTS providers, factory, GTS/MTS repos).
- `services.AddProviders()` — `Sportsbook.API` (BetGenius livestream + factory).
- RedCat `ServiceExtension.{AddContexts,AddHttpClients,AddRepositories}()`.
- `services.AddTelemetry(...)` — OTel.

## Hosted-service "services" (workers, not domain services) `[Verified]`
`*.Service`/`*.Servicev2` projects are `BackgroundService` workers (Settlement, CashOut, Placement,
Provider, Operator*, BetForwarder, BetInfo, BetStatus, CacheSync, AgentReports, Client.TopMarket,
ESportOperator, Cancellation). See [ProjectOverview.md](../ProjectOverview.md), [Hosting.md](../Hosting.md).

## Note
Business logic lives mostly **in repositories**, not in service classes — only `ParlayBonusService`
is a true domain service. See [AntiPatterns.md](../AntiPatterns.md) AP-10.
