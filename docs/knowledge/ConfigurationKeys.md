# Configuration Keys (Quick Reference)

> Phase 9. Fast lookup of config sections/keys. **No secret values** — names only. Authoritative:
> [Configuration.md](../Configuration.md), [ConfigurationOverview.md](../ConfigurationOverview.md).
> In-cluster, keys arrive as `Section__Key` env vars from **AWS SSM Parameter Store** (External
> Secrets), overriding committed `appsettings.json`. All **[Verified]**.

## `ConnectionStrings`
`Platform`, `Betting`, `Sportsbook`, `Translation`, `Wallet`, `AgentReports`, `ESportBetting`,
`RedCatBetting`, casino, `MongoDB-Sportsbook`, `MongoDB-Casino`, `RedisInstances` (section of
client-code→connection). One per DbContext/datastore.

## `MessageBroker` (+ parallel `ExternalMessageBroker`)
`Host`, `Port`, `Username`, `Password`, `VirtualHost`, `Exchange` (=`e.central`), `Queue`, `Consumer`,
`RoutingKey` (pipe-delimited multi), `PrefetchCount` (~1000), `IsExclusive`, `IsPriority`.
→ `Platform.Shared/Utility/MessageBroker.cs`.

## `API` (downstream base URLs)
`PlatformBettingAPI`, `PlatformSportsbookAPI`, `CasinoAPI`/`QTechAPI`, `MTSAuthAPI`, `GTSAPI`,
`RabbitMQAPI`, `RedCatBettingURI`, `RedCatOperatorURI`. Each → a typed `HttpClient` base address.

## `Custom` (flags, partner creds, keys) — many hold secrets
**Flags:** `IsProviderEnabled`, `IsMTSEnabled`, `IsProduction`, `UseMessagePack`, `IsOddsString`,
`TestBetClientCodes`, `MaximumConnections`, `Fibonnaci` (sic), `EnableAzureSignalRService`.
**Keys/creds:** `APIKey`, `InternalAPIKey`, `WebSocketAPIKey`, `PassKey`, `SharedKey`,
`MTSClientSecret`/`MTSUsername`/`MTSPassword`, `GTSBookmakerId`/GTS client creds,
`BetGenius:Uri|User|Password`, `Ably:ApiKey|Channel|ChannelCount`, `Oddin:BaseUrl|BrandToken`,
`OrientalGaming:VendorName|PublicAPIKey|PrivateAPIKey|ClientIds`, QTech API user/pass.

## `Serilog`
`Using`, `MinimumLevel.Default`+`Override` (Microsoft→Warning, OTLP HttpClient→Warning,
Microsoft.Hosting.Lifetime→Information), `WriteTo` (Console; some File), `Enrich` (FromLogContext).

## `Telemetry`
`Enabled` (committed default `false`; enabled per-env), `Environment`, `Collector`.

## Misc
`Logging:LogLevel`, `AllowedHosts` (`*`), `PlatformWallet:Uri|ApiKey`, `AgentUrl`,
`LogSettings:DirectoryPath|RetentionInDays` (BetStatus.Service file logging).

## Feature-flag cheat sheet `[Verified]`
| Flag | Effect |
|------|--------|
| `Custom:IsProviderEnabled` | enable bet providers (MTS/GTS) |
| `Custom:IsMTSEnabled` | enable Sportradar MTS SDK (CashOut/providers) |
| `Custom:IsProduction` | WebSocket.API: local vs Azure SignalR |
| `Custom:UseMessagePack` | Websocket.API2: MessagePack vs JSON |
| `Custom:TestBetClientCodes` | test-mode client allow-list |
| `Telemetry:Enabled` | OTel on/off |
| `Custom:Fibonnaci` | Job.Resettlement backoff schedule |

> Toggling any flag requires a **config change + restart** (no hot reload subscribed). Secrets are
> dev/staging fallbacks in git; prod values come from SSM. See [TechnicalDebt.md](../TechnicalDebt.md) TD-01.
