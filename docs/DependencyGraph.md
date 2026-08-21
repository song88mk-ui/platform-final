# Dependency Graph

> Confidence: **High [Verified]** for compile-time references and package usage (read from
> `.csproj` files); **Medium [Likely]** for runtime HTTP/AMQP edges (inferred from typed
> HttpClient registrations, config base-URL keys, and message producers/consumers).

## 1. Compile-time dependency direction

The compile-time graph is a **star**: every project depends only on `Platform.Shared`
(plus, for some services, a companion `*.Repository` / `*.Telemetry` sub-project). There are
**no project references between deployable APIs/services**. **[Verified]**

```
                       ┌──────────────────┐
   all 37 deployables ─┤  Platform.Shared │ (netstandard2.1)
                       └──────────────────┘
                                ▲
   service hosts also ── *.Service.Repository / *.Service.Telemetry (per service)
```

Representative `.csproj` references — **[Verified]**:
- `Platform.Sportsbook.API.csproj` → `Platform.Shared`
- `Platform.Betting.API.csproj` → `Platform.Shared`
- `Platform.Settlement.Service` → `Platform.Shared`, `…Service.Repository`, `…Service.Telemetry`
- `Platform.CashOutStake.Servicev2` → `Platform.Shared`, `Platform.Shared.Data.Betting`, `…Servicev2.Repository`

Consequence: a change to `Platform.Shared` recompiles and (per CI's affected-files check)
**rebuilds every image** — CI's changed-files filter includes `Platform/Platform.Shared/**`
for every app. **[Verified]** — `.github/workflows/ci.yml`.

## 2. Runtime HTTP dependency edges (synchronous)

Derived from typed `AddHttpClient<IXxxRepository, XxxRepository>` registrations and the
`API:*` / `*Uri` base-address config keys. **[Likely]** (endpoints not load-tested, only read).

```
Platform.API ──▶ Platform.Betting.API        (IBettingRepository)
Platform.API ──▶ Platform.Operator.API        (IOperatorRepository)
Platform.API ──▶ Platform.Sportsbook.API      (ISportsbookRepository)
Platform.API ──▶ Casino (QTech) API           (ICasinoRepository)

Platform.Betting.API ──▶ Platform.Sportsbook.API   (ISportsbookRepository)
Platform.Betting.API ──▶ Platform.RabbitMQ.API     (IRabbitMQRepository, publish-over-HTTP)
Platform.Betting.API ──▶ GTS / MTS auth+maxstake   (providers)

Platform.Sportsbook.API ──▶ Platform.RabbitMQ.API  (IRabbitMQRepository)
Platform.Sportsbook.API ──▶ BetGenius              (IBetGeniusRepository, livestream)
Platform.Sportsbook.API ──▶ BetBuilder             (IBetBuilderRepository)

Platform.ClientSportsbook.API ──▶ Sportsbook.API, RabbitMQ.API
Platform.ClientPlatform.API   ──▶ Platform (read) + ReadOnly repo
Platform.Operator.API ──▶ Auth (sibling), Wallet.API, Reports
Platform.AgentReports.API ──▶ Agent service, Wallet.API

Platform.RedCat.API ──▶ Platform.RedCatBetting.API ──▶ Platform.RedCatOperator.API ──▶ RabbitMQ
Platform.Oddin.API / QTech.API ──▶ Operator + RabbitMQ.API (publish events)
```

> Many publishers reach RabbitMQ **through** `Platform.RabbitMQ.API` over HTTP, rather than
> opening AMQP themselves. Background **services**, by contrast, hold direct AMQP connections
> via `MessageBroker`. **[Verified]**

## 3. Runtime messaging edges (asynchronous, RabbitMQ `e.central`)

Producers/consumers of the key `On*` contracts (from `Platform.Shared/MessageModels`).
**[Verified]** that each service handles the listed types; **[Likely]** for exact routing keys.

| Message | Produced by | Consumed by |
|---------|-------------|-------------|
| `OnBetPlacement` | Betting.API, Provider.Service (delayed) | Placement.Servicev2, BetInfo.Service, AgentReports.Service, BetForwarder.Service, CashOutStake.Servicev2, Client.TopMarket |
| `OnBetSettlement` | Settlement.Service, Odds Engine 2 (`r.oe.bet.settlement`) | Settlement.Service, CashOutStake, AgentReports, BetForwarder, BetStatus, Client.TopMarket |
| `OnBetCancellation` | Betting/Operator flows | Settlement.Service, Provider.Service, CashOutStake, AgentReports, Client.TopMarket |
| `OnBetFullCashOut` / `OnBetPartialCashOut` | Betting.API cashout | CashOut.Service, CashOutStake, AgentReports |
| `OnOddsChange` / `OnMarketChange` | **odds-engine-2** (sibling) | CashOutStake.Servicev2, WebSocket.API |
| `OnWalletChange` | Wallet flows, Oddin/QTech | BetStatus.Service, WebSocket.API |
| `OnCasinoBetSettlement` | QTech.API | AgentReports.Service |
| `OnESportBetSettlement` | Oddin.API | AgentReports.Service |
| `On*CacheChange` (7 types) | back-office / config | CacheSync.Service |
| `OnBetStatus`/`OnCashOutChange`/`OnPayableChange` | various | BetStatus.Service → WebSocket.API (push) |

## 4. External & datastore dependencies

| Dependency | Used by | Mechanism |
|------------|---------|-----------|
| **SQL Server** (8 DBs) | most APIs/services | EF Core 5 (`Microsoft.EntityFrameworkCore.SqlServer`) |
| **MongoDB** | Sportsbook.API, QTech.API, CashOutStake.Servicev2 | `MongoDB.Driver` |
| **Redis** | ClientSportsbook.API | `StackExchange.Redis` |
| **RabbitMQ** | all services + RabbitMQ.API | `RabbitMQ.Client` 5.2.0 |
| **Sportradar MTS** | Shared providers, CashOut.Service | `Sportradar.MTS.SDKCore` 2.4.1.2 |
| **BetGenius / GTS** | Shared providers/repos | HTTP |
| **Ably realtime** | Provider.Service | `ably.io` 1.2.18 |
| **Azure SignalR** | WebSocket.API / API2 | `Microsoft.Azure.SignalR` |
| **OpenTelemetry collector** | most | `Infra.Extensions.Telemetry` (private feed) |

## 5. Key shared NuGet versions `[Verified]`

| Package | Version | Where |
|---------|---------|-------|
| `Microsoft.EntityFrameworkCore(.SqlServer)` | 5.0.x (varies 5.0.2–5.0.17 per project) | shared + per-project |
| `RabbitMQ.Client` | 5.2.0 | Shared + services |
| `MongoDB.Driver` / `MongoDB.Bson` | 2.11.6 (some 2.13.1) | Sportsbook, Casino, CashOutStake |
| `Sportradar.MTS.SDKCore` | 2.4.1.2 | Shared |
| `Serilog` / `Serilog.AspNetCore` | 4.3.0 / 10.0.0 | nearly all |
| `Swashbuckle.AspNetCore.*` | 5.6.3 / 6.0.x | APIs |
| `Infra.Extensions.Telemetry` | 0.0.1-1 | private GitHub feed |
| `OpenTelemetry.Api` | 1.14.0 | Shared |

> **Version drift** is present: EF Core patch versions differ across projects, MongoDB driver
> differs (2.11.6 vs 2.13.1). Tracked in [TechnicalDebt.md](TechnicalDebt.md).

## 6. The "affected files" build coupling

Because every app's CI build triggers on changes to `Platform.Shared/**`, the shared kernel is
both the **greatest reuse asset** and the **largest blast-radius** in the repo. Any modification
there is, by construction, a platform-wide change. **[Verified]**
