# Project Overview

> Per-project purpose, responsibilities, consumers, and integrations. Confidence is tagged
> per row group. Endpoint/line citations come from cluster analysis of `Controllers/`,
> `Repository/`, `Startup.cs`/`Program.cs`, and `.csproj`. Where a detail was inferred from a
> single read it is **[Likely]**; where directly confirmed, **[Verified]**.

Legend: **DI styles** — `HttpClient` = typed `AddHttpClient<I,Impl>`; `Scoped`/`Singleton` =
`AddScoped`/`AddSingleton`; `Worker` = `AddHostedService`.

---

## Shared kernel

### Platform.Shared `[Verified]`
- **Purpose:** the single shared library (`netstandard2.1`); see [SharedLibraries.md](SharedLibraries.md).
- **Responsibilities:** EF Core entities + 8 `DbContext`s, 30 RabbitMQ message contracts,
  189 DTO models, 33 enums, bet/livestream provider integrations (MTS, GTS, BetGenius),
  `MessageBroker`, `MemCache`, `ParlayBonusService`, extension/helper utilities.
- **Consumers:** **every** other project.

---

## Edge / public APIs

### Platform.API `[Verified]`
- **Purpose:** main public integration gateway. `IntegrationController` (public, behind
  `APIKeyFilter`) + `InternalController` (behind `InternalAPIKeyFilter`).
- **Key endpoints:** `POST /v1/authentication/login|logout`, `GET /v1/wallet/balance`,
  `POST /v2/bet/placement`, `GET/POST /v1/bet/history*`, `POST /v1/bet/cashout`,
  sportsbook reads; internal: client upsert, cache invalidation, manual settlement, cancel.
- **DI:** `HttpClient` → Betting, Operator, Sportsbook, Casino repos; `Scoped` PlatformRepo;
  `Singleton` MemCache. **DbContext:** `PlatformContext`.
- **Consumers:** external clients / front-end gateway; back-office (internal endpoints).

### Platform.Betting.API `[Verified]`
- **Purpose:** betting operations engine. `BetController` + `ConfigController`.
- **Key endpoints:** `POST /v1/bet/placement|maxstake|history|history/settled|history/running|`
  `config|cancel|manualsettlement|cashout|recommendations`; `POST /v1/config/client|parlaybonus`.
- **DI:** `AddBettingProviders()` extension (MTS + GTS providers, factory, GTS/MTS HTTP repos);
  `HttpClient` → SportsbookRepo, RabbitMQRepo, MTSProbabilityRepo; `Scoped` Betting/BetConfig repos.
  **DbContext:** `BettingContext`. **Publishes** to RabbitMQ (via HTTP `IRabbitMQRepository`).
- **Consumers:** `Platform.API`, operator/back-office.

### Platform.Sportsbook.API `[Verified]`
- **Purpose:** sportsbook catalog/reads + livestream + translations.
- **Key endpoints:** `DELETE /v1/cache/maintenance`; `POST /v1/sportsbook/livestream|topleagues|`
  `allleagues|topfixtures|markettemplategroups`.
- **DI:** `AddProviders()` (BetGenius livestream provider + factory); `HttpClient` → Sportsbook,
  RabbitMQ, BetBuilder repos; `Scoped` TranslationRepo. **DbContexts:** `SportsbookContext`,
  `TranslationContext`. **Data:** MongoDB (primary/secondary read prefs) + SQL.
- **Consumers:** `Platform.API`, client-facing sportsbook APIs, back-office.
- **Note:** This project has the only uncommitted change at analysis time —
  `Repository/SportsbookRepository.cs` (working tree modified; see git status).

### Platform.ClientSportsbook.API `[Verified]`
- **Purpose:** client-facing sportsbook (active fixtures, details, market subscribe).
- **Key endpoints:** `GET /v1/sportsbook/activefixtures`, `.../details/{fixtureId}`,
  `POST /v1/sportsbook/market/subscribe`.
- **DI:** `Scoped` `AuthorizationFilter` (per-client `x-client-id`/`x-api-key`/`x-language-code`
  DB lookup); `Singleton` Redis cache + MemCache; `HttpClient` → Sportsbook, RabbitMQ.
  **DbContext:** `PlatformContext`. **Cache:** Redis (`StackExchange.Redis`).
- **No Serilog** in `Program.cs`. `[Verified]`

### Platform.ClientPlatform.API `[Verified]`
- **Purpose:** client-facing platform reads (top/active fixtures, fixture details, bet history).
- **DI:** `Scoped` `APIKeyFilter`; `HttpClient` → PlatformRepo, ReadOnlyRepo. **DbContext:** none registered. `[Verified]`

### Platform.Client.API `[Verified]`
- **Purpose:** legacy client sportsbook fixtures (active fixtures/details/count).
- **DI:** `Scoped` `APIKeyFilter`; `Singleton` MemCache; `HttpClient` → MTSAuth, MTSCCF, Sportsbook.
  **DbContext:** `PlatformContext`. **No Serilog.** Likely superseded by `ClientSportsbook`/`ClientPlatform`. `[Likely]`

### Platform.Operator.API `[Verified]`
- **Purpose:** back-office/operator API. `OperatorController`, `OddsFeedController`, `IntegrationController`.
- **Key endpoints:** `POST /operator/v1/authentication/login|logout`, `POST /operator/v1/bet/placement`.
- **DI:** `Scoped` Platform/Operator repos + `InternalAPIKeyFilter`; `HttpClient` → Auth, Wallet, Reports.
  **DbContext:** `PlatformContext`.

### Platform.Wallet.API `[Verified]`
- **Purpose:** wallet ledger. `IntegrationController`: `POST /v1/credit|debit|userwallettransfer`, `GET /v1/userwallet`.
- **DI:** `Scoped` WalletRepo; `APIKeyFilter`. **DbContext:** `WalletContext` (`ConnectionStrings:Wallet`).
- **Implementation:** SQL **stored procedures** (not LINQ). Excludes `System.Data.SqlClient`. `[Verified]`
- **Consumers:** `Platform.Operator.API`, `Platform.AgentReports.API`, settlement flow (credit on win).

---

## Integration / third-party APIs `[Verified] structure, [Likely] partner semantics`

### Platform.Oddin.API
- **Partner:** Oddin / Betradar (eSports odds). `IntegrationController`:
  `POST /v1/userdetails|debituser|credituser`. **DbContexts:** `PlatformContext`, `ESportBettingContext`.
  **Publishes:** `OnESportsBetSettlement` (`r.bet.reports.agent`), `OnWalletChange` (`r.wallet.{clientCode}`).

### Platform.QTech.API
- **Partner:** QTech casino aggregator. `IntegrationController` (behind `PassKeyFilter`):
  session, balance, `POST /v1/transactions(/rollback)`, bonus, lobby, bet history.
  **DbContexts:** `PlatformContext`, `CasinoBettingContext`. **MongoDB:** `MongoDB-Casino` (`BettingStatus` collection).
  **Publishes:** `OnWalletChange`, `OnCasinoBetSettlement`.

### Platform.RedCat.API / RedCatBetting.API / RedCatOperator.API
- **Partner:** RedCat operator/POS integration (a virtual-sports/retail channel). `[Likely]`
  - **RedCat.API:** inbound operator callbacks (`api/redcat/validatetoken|getbalance|getapproval|stake|settle|voidstake|voidsettle`); calls RedCatBetting via `API:RedCatBettingURI`; **DbContext** `PlatformContext`.
  - **RedCatBetting.API:** `v1/vs/bet/placement|settle|void/*`; **DbContext** `RedCatBettingContext` (pooled); calls RedCatOperator via `API:RedCatOperatorURI`.
  - **RedCatOperator.API:** `v1/vs/bet/*`; publishes to RabbitMQ; **DbContext** `PlatformContext` (pooled).
- All three enable **health checks**. `[Verified]`

### Platform.AgentReports.API
- **Purpose:** agent/partner reporting. `PaymentController` (transfer list/transfer/GGR settlement),
  `ReportsController` (winloss, allbets, parlaybets, outstanding, P&L, GGR, transfer history…).
  Behind `APIKeyFilter`. **DbContext:** `AgentReportsContext`. **HttpClient** → Agent service, Wallet API.
- **Not in CI matrix.** `[Verified]`

---

## Messaging / real-time

### Platform.RabbitMQ.API `[Verified]`
- **Purpose:** HTTP→AMQP bridge. `POST /v1/broker/publish` { Payload, MessageType, RoutingKey, CorrelationId }.
- **DI:** `Singleton IMessageBroker`. Reads `MessageBroker:*` config. Lets other APIs publish without an AMQP client.

### Platform.WebSocket.API `[Verified]` / Platform.Websocket.API2 `[Verified]`
- **Purpose:** SignalR hub (`Hub<IPlatformEvent>`), `/platform` route, MessagePack, Azure SignalR in prod.
- **Client methods:** `SubscribeTo{Endpoint,Betslip,BetStatus,CashOutChange,WalletChange,PayableChange,Favorites}` (+ unsubscribe).
- **Server pushes:** `Publish{OddsChange*,BetStop*,BetStatus,CashOutChange,WalletChange,PayableChange}`.
- **DbContext:** `PlatformContext`. **API2** targets `net7.0`, adds `Custom:UseMessagePack` toggle + `UserHandler`.
- **Relationship:** `API2` is the modern rewrite of `API`. CI builds **API2** as image `platform-websocket-api`.

### Platform.WebSocket.Client `[Verified]`
- Sample SignalR client console (connects to `ws.song88.com/platform`). Not deployed.

---

## Background services (RabbitMQ workers)

> All use Generic Host + `BackgroundService` consuming RabbitMQ via `MessageBroker`. `[Verified]`
> for the ones traced below; `[Likely]` for `Cancellation.Service`, `ESportOperator.Service`,
> `Operator.Cashout.Service` (Workers not fully read — see [Unknowns.md](Unknowns.md)).

| Service | Consumes (message types) | Produces / does | Notable |
|---------|--------------------------|-----------------|---------|
| `Placement.Servicev2` | `OnBetPlacement` (manual ack) | provider dispatch via `IBetProviderFactory` | 100-thread pool, OpenTelemetry metrics |
| `BetInfo.Service` | `OnBetPlacement` (manual ack) | `BettingRepository.ProcessBetPlacement` | persists bet info |
| `BetForwarder.Service` | `OnBetPlacement/Rejection/Cancellation/FullCashOut/Settlement` | publish to **external** broker | bridge to partner broker; Windows service |
| `BetStatus.Service` | `OnBetStatus/CashOutChange/WalletChange` | external publish `r.bet.onbetstatus.{clientId}` | per-client routing; Serilog file sink |
| `Settlement.Service` | `OnBetSettlement/Cancellation/BOCancellation/BOManualSettlement/OddsFeedBetCancellation` | settle → wallet/reports | OpenTelemetry, trace propagation from MQ headers |
| `CashOut.Service` | `OnBetFullCashOut/PartialCashOut` | cashout via repo + MTS SDK; error → `r.bet.cashout.error` | 100-thread pool; `Custom:IsMTSEnabled` |
| `CashOutStake.Servicev2` | `OnOddsChange/BetPlacement/Cancellation/Settlement/PartialCashOut/FullCashOut` | maintain active-bet stake cache; republish | EF `BettingContext` + Mongo; preloads active bets |
| `Cancellation.Service` | `[Likely]` cancellation events | cancel processing | not in CI matrix |
| `Provider.Service` | `OnBetTimeout/BOCancellation/Cancellation/Settlement` + **Ably** GTS assessment | route to provider; publish `OnBetPlacement` (delayed) | dual channel RabbitMQ + Ably (`ably.io`) |
| `Operator.Servicev2` | `OnBetTimeout/BOCancellation/Cancellation/Settlement` | provider routing (operator channel) | OpenTelemetry |
| `Operator.Cashout.Service` | `[Likely]` cashout events | operator cashout | not in CI matrix |
| `ESportOperator.Service` | `[Unknown]` | eSports operator messaging; `PlatformRepository` | Windows service; not in CI matrix |
| `CacheSync.Service` | `OnMarketSortOrderChange/SportSortOrderChange/UpdateUserFavorites/Maintenance/ParlayBonusChange/ClientChange/DynamicTranslationChange` | invalidate caches | 7 message types; Windows service |
| `AgentReports.Service` | `OnBetPlacement/FullCashOut/Settlement/Cancellation/CasinoBetSettlement/ESportBetSettlement` | write reporting DB | Windows service; not in CI matrix |
| `Client.TopMarket` | `OnBetPlacement/Settlement/Cancellation` (top-market) | update `ClientTopMarket` (`BettingContext`) | excludes virtual sports & outrights |

---

## Jobs (timed / loop)

| Job | Trigger | Does | Data | Confidence |
|-----|---------|------|------|-----------|
| `Job.CCF` | every 300s loop | `PlatformRepository.ProcessCCF()` (Confidence Factor) | `PlatformContext`, MTSAuth HTTP | `[Verified]` |
| `Job.ForexEUR` | loop (interval [Unknown]) | EUR forex rate update | `PlatformContext` | `[Likely]` |
| `Job.Operator.Score` | loop | fetch scores from Betradar + CricketScore HTTP APIs | `BettingContext`, MemCache | `[Likely]` |
| `Job.Resettlement` | loop, Fibonacci backoff (`Custom:Fibonnaci`) | query `OperatorSettlementRetry` (IsRetry, RetryCount<8) → republish to MQ | `PlatformContext` (30-min cmd timeout) | `[Verified]` |

---

## Console

### Platform.BetPlacement.Console `[Verified]`
- **Purpose:** load/stress-test tool. Loops placing bets against a **hardcoded staging
  gateway** (`sportsbook-gateway-nippon10-staging.playingops.com`) with hardcoded
  client-id/operator-token/api-key headers, measuring latency.
- **Risk:** hardcoded credentials/IP in source. See [TechnicalDebt.md](TechnicalDebt.md).
