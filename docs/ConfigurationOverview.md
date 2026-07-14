# Configuration Overview

> Confidence **High [Verified]** for section/key names (read across multiple `appsettings.json`).
> **No secret values are reproduced here** — only key names. Several files contain committed
> plaintext secrets; that risk is tracked in [TechnicalDebt.md](TechnicalDebt.md).

## Config sources & precedence
- Standard ASP.NET Core `Host.CreateDefaultBuilder` chain: `appsettings.json` →
  `appsettings.{Environment}.json` → environment variables → command line. `[Verified]` (default behavior)
- Values are read with the **`IConfiguration["Section:Key"]` string indexer** throughout
  (not `IOptions<T>`). `[Verified]`
- `**/appsettings.Development.json` is **gitignored** (local dev overrides). `[Verified]`
- In the Docker build, `appsettings.*.json` are **removed** from the published output, so
  runtime relies on the committed base `appsettings.json` plus env/mounted config. `[Verified]`

## Recurring sections & key names `[Verified]`

### `ConnectionStrings`
`Platform`, `Betting`, `Sportsbook`, `Translation`, `Wallet`, `AgentReports`,
`ESportBetting`, `RedCatBetting`, casino, and `MongoDB-*` (e.g. `MongoDB-Sportsbook`,
`MongoDB-Casino`). One key per `DbContext` / datastore.

### `MessageBroker` (and parallel `ExternalMessageBroker`)
`Host`, `Port`, `Username`, `Password`, `VirtualHost`, `Exchange` (e.g. `e.central`),
`Queue`, `Consumer`, `RoutingKey` (pipe-delimited multi), `PrefetchCount` (≈1000),
`IsExclusive`, `IsPriority`. Consumed by `Platform.Shared/Utility/MessageBroker.cs`.

### `API` (downstream HTTP base URLs)
`PlatformBettingAPI`, `PlatformSportsbookAPI`, `CasinoAPI`, `MTSAuthAPI`, `GTSAPI`,
`RabbitMQAPI`, `RedCatBettingURI`, `RedCatOperatorURI`, `QTechAPI`, etc. Each maps to a typed
`HttpClient` `BaseAddress`.

### `Custom` (feature flags, partner creds, keys)
`APIKey`, `InternalAPIKey`, `WebSocketAPIKey`, `IsProviderEnabled`, `IsMTSEnabled`,
`IsProduction`, `UseMessagePack`, `TestBetClientCodes`, `Fibonnaci` (sic),
`BetGenius:Uri|User|Password`, `MTSClientSecret|MTSUsername|MTSPassword`, `GTSBookmakerId`,
`Ably:ApiKey|Channel|ChannelCount`, `PassKey`, `SharedKey`, QTech API user/pass, etc.
**Many of these hold plaintext secrets.**

### `Serilog`
`Using` (sinks), `MinimumLevel.Default` + `Override` (`Microsoft`→Warning,
`Microsoft.Hosting.Lifetime`→Information, OTLP HttpClient categories→Warning),
`WriteTo` (Console; some add File), `Enrich` (`FromLogContext`).

### `Telemetry`
`Enabled`, `Environment`, `Collector` (consumed by `Infra.Extensions.Telemetry`).

### Misc
`Logging:LogLevel`, `AllowedHosts` (`"*"`), `PlatformWallet:Uri|ApiKey`, `AgentUrl`.

## Per-service config notes `[Verified]`
- `Job.Resettlement` — `Custom:Fibonnaci` (comma-separated backoff delays); 30-min DB command timeout.
- `CashOut.Service` / providers — `Custom:IsMTSEnabled` gates Sportradar MTS SDK.
- `Provider.Service` — `Custom:Ably:*` for realtime GTS assessment channels.
- `WebSocket.API` — `Custom:IsProduction` switches local vs Azure SignalR; `Websocket.API2` —
  `Custom:UseMessagePack` toggles protocol.
- `BetStatus.Service` — `LogSettings:DirectoryPath|RetentionInDays` for file logging.

## How configuration is injected at runtime `[Likely]`
- In Kubernetes (sibling `argocd`), config arrives as environment variables using the
  ASP.NET Core double-underscore convention (e.g. `ConnectionStrings__Platform`,
  `API__RabbitMqApi__...__Uri`, `MessageBroker__Host`). This pattern is **observed in sibling
  repo manifests** (`backoffice-final`, `oddsfeed`), not in this repo's committed files —
  hence **[Likely]** for `platform-final` specifically. See
  [CrossRepositoryDependencies.md](CrossRepositoryDependencies.md).

## Secrets handling — current state
- **Committed plaintext secrets exist** in multiple base `appsettings.json` files (DB passwords,
  API keys, MessageBroker password, partner credentials). **[Verified]**
- No use of a secrets manager (AWS Secrets Manager / K8s Secrets) is visible **inside this repo's
  committed config**; secret injection appears to happen at deploy time via `argocd`/`infrastructure`.
  **[Likely]**. This is the top remediation item in [TechnicalDebt.md](TechnicalDebt.md).
