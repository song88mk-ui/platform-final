# Knowledge Base

> Reusable knowledge, domain glossary, and "how things really work" notes for fast onboarding.
> Confidence per item; defaults to **[Verified]** unless marked.

## Domain glossary

| Term | Meaning in this platform |
|------|--------------------------|
| **Bet / Selection** | A `Bet` has one or more `BetSelection`s. `BetType`: Single, Parlay, PriceBoost, BetBuilder. |
| **Channel / ChannelType** | The bet provider routing key. `BG=0` (BetGenius/GTS), `EM=1` (Kambi/EM), `OD=2` (OddsFeed), `SR=3` (Sportradar/MTS). Drives `IBetProviderFactory.Resolve`. |
| **Provider** | External validation/risk system for a bet: MTS (Sportradar) and GTS (BetGenius). |
| **Operator / Client** | Operator = a brand/tenant; Client = a consuming application/partner identified by `x-client-id`/clientCode. |
| **Cashout** | Early settlement of a running bet. Full vs partial. `CashOutStake.Servicev2` maintains live cashout value as odds change. |
| **Settlement / Resettlement** | Final grading (Won/Lost/Void/Half…). Resettlement = retrying failed settlements (`Job.Resettlement`). |
| **Parlay bonus** | Bonus on multi-selection wins (3+) meeting min-odds (`ParlayBonusService`, `ParlayBonusAudit`). |
| **CCF / Confidence Factor** | Risk parameter recomputed by `Job.CCF` every 300s; stored in `PlatformContext.ConfidenceFactor`. |
| **Top market / Top fixtures/leagues** | Curated/featured sportsbook content; per-client ordering tables in `SportsbookContext`. |
| **UOF** | Unified Odds Feed (Betradar) — consumed by odds engines, surfaced as `OnOddsChange`/`OnMarketChange`. |
| **GGR / WinLoss** | Reporting metrics in `AgentReportsContext`. |

## Bet status codes (`Enums/BetStatus`)
`Waiting=1000, Running=2000, Won=3000, HalfWon=3001, Lost=4000, HalfLost=4001, Void=5000,
Rejected=6000, Cancelled=7000, CashedOut=8000`. **[Verified]**

## Market status codes (`Enums/MarketStatus`)
`Active=1, Inactive=0, Suspended=-1, HandedOver=-2, Settled=-3, Cancelled=-4, NotMapped=99`. **[Verified]**

## Reusable building blocks (prefer these over re-inventing)

| Need | Use | Location |
|------|-----|----------|
| Publish/consume RabbitMQ | `IMessageBroker` | `Platform.Shared/Utility/MessageBroker.cs` |
| In-proc cache w/ lazy load | `IMemCache.GetCache<T>(key, loader, ttl)` | `Platform.Shared/Utility/MemCache.cs` |
| Bet provider selection | `IBetProviderFactory.Resolve(channelTypeId)` | `Platform.Shared/Providers/` |
| Livestream provider | `ILiveStreamFactory.Resolve(channelType)` | `Platform.Shared/Providers/` |
| Uniform API response | `BaseResponseDTO<T>` + `StatusCode` enum | `Platform.Shared/Models/` |
| Parlay bonus eval | `IParlayBonusService.GetParlayBonus(...)` | `Platform.Shared/Services/` |
| MTS/GTS/BetGenius auth | the `*AuthRepository` classes | `Platform.Shared/Repositories/` |
| API-key validation | `APIKeyFilter` / `InternalAPIKeyFilter` | each project's `ActionFilters/` |
| Telemetry span | `Instrumentation.ActivitySource.StartActivity(...)` | each project's `Telemetry/` |
| Message contract | add/extend `On*` in `MessageModels` | `Platform.Shared/MessageModels/` |
| RabbitMQ from an API (no AMQP) | `IRabbitMQRepository` → `Platform.RabbitMQ.API` | per-API `Repository/` |

## Mental model of the message bus
- One topic exchange `e.central`; producers tag with `r.<domain>.<event>[.<client>]`.
- Services subscribe with a queue + routing-key binding; `MessageBroker` handles connect,
  prefetch (~1000), optional priority/exclusive, persistence, recovery, and trace headers.
- Publish from **APIs** typically goes through `Platform.RabbitMQ.API`; **services** publish
  directly. Hot queues (placement, cashout) are drained by 100-thread fan-out consumers.

## "Gotchas" worth knowing
- **Business errors return HTTP 400**, not 2xx — check `StatusCode` in the body, not the HTTP code. `[Verified]`
- Some endpoints **return exception stack traces** in the response body. `[Verified]` (debt)
- `appsettings.Development.json` is gitignored — local devs need their own. `[Verified]`
- Build SDK (6.0) ≠ runtime (3.1): code must stay **3.1-compatible** despite a newer build SDK. `[Verified]`
- `Platform.Shared` change ⇒ **all** images rebuild and behavior can shift platform-wide. `[Verified]`
- Two WebSocket APIs exist; **`Websocket.API2` (net7.0)** is the one CI ships as `platform-websocket-api`. `[Verified]`
- Prefer **`odds-engine-2`** over `oddsengine` for any odds-engine reasoning. (project rule)
- `WalletContext` lives in `Platform.Wallet.API`, not `Platform.Shared`. `[Verified]`

## Onboarding path (suggested reading order)
1. [Architecture.md](Architecture.md) → the big picture + bet lifecycle.
2. [SharedLibraries.md](SharedLibraries.md) → the shared kernel you'll touch most.
3. [RepositoryMap.md](RepositoryMap.md) + [ProjectOverview.md](ProjectOverview.md) → find your service.
4. [ImplementationPatterns.md](ImplementationPatterns.md) + [CodingStandards.md](CodingStandards.md) → match the house style.
5. [CrossRepositoryDependencies.md](CrossRepositoryDependencies.md) → where the edges go.
6. [TechnicalDebt.md](TechnicalDebt.md) + [Unknowns.md](Unknowns.md) → what to be careful about.
