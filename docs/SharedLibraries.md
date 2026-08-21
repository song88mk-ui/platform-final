# Shared Libraries — `Platform.Shared`

> Confidence: **High [Verified]** for structure, DbContexts, MessageBroker API, provider
> factory, and enums (read directly); **[Likely]** for individual entity field semantics.

`Platform.Shared` is the **only** shared library in the repository and the de-facto **shared
kernel**. Target: `net10.0` (since 2026-08-20, GM-744; previously `netstandard2.1` so the
mixed 3.1/7.0/8.0 consumers could all reference it). Referenced by all other projects. **[Verified]**

`Platform.Shared.csproj` packages: EF Core 10.0.11 (+ SqlServer), `Microsoft.Extensions.Configuration.*`
10.0.11, `Microsoft.Extensions.Http`, `MongoDB.Bson`, `RabbitMQ.Client` 5.2.0, `Sportradar.MTS.SDKCore`
2.4.1.2, `System.Runtime.Caching` 10.0.11, `OpenTelemetry.Api` (`System.Text.Json` is framework-inbox on net10.0, no longer a PackageReference). **[Verified]**

## Folder inventory `[Verified]` (file counts)

| Folder | Files | Contents |
|--------|------:|----------|
| `Data` | 88 | 8 EF Core `DbContext`s + entity classes (one subfolder per context) |
| `Models` | 189 | Request/Response DTOs, internal contracts (`BaseResponseDTO<T>` etc.) |
| `Enums` | 33 | Domain enums |
| `MessageModels` | 30 | RabbitMQ message contracts (`On*`) |
| `Extensions` | 10 | Type extension methods |
| `Providers` | 9 | Bet & livestream provider integrations + factories |
| `Repositories` | 10 | External-system HTTP repositories (BetGenius, GTS, MTS) |
| `Utility` | 8 | `MessageBroker`, `MemCache`, helpers, JWT parser |
| `Enums`/`Helpers`/`Services` | 33/2/2 | enums; JSON/odds helpers; `ParlayBonusService` |
| `DatabaseScripts` | 0 (folder) | SQL scripts (no `.cs`) |

## 1. Data — 8 EF Core DbContexts `[Verified]`

Each context lives in its own subfolder/namespace and maps to a distinct SQL database.

| DbContext | Namespace / Folder | Connection key | Representative entities |
|-----------|--------------------|----------------|-------------------------|
| `BettingContext` | `Data/Betting` | `ConnectionStrings:Betting` | Bet, BetSelection, BetConfig, BetAudit, ParlayBonusAudit, StakeLimitFixture, League, Tier, ClientTopMarket, PartialCashoutAudit |
| `PlatformContext` | `Data/Platform` | `ConnectionStrings:Platform` | Client, Operator, ForexEUR, ConfidenceFactor, OperatorSettlementRetry, OperatorLog/Audit |
| `SportsbookContext` | `Data/Sportsbook` | `ConnectionStrings:Sportsbook` | ClientTopFixture/League, MarketTemplateGroup, UserFavorite, SportGroup, Announcements, Country |
| `TranslationContext` | `Data/Translation` (declared in Sportsbook ns) | `ConnectionStrings:Translation` | SportsbookDynamic, SportsbookStatic |
| `AgentReportsContext` | `Data/Reports` | `ConnectionStrings:AgentReports` | Bet/BetTransaction, WinLoss, CasinoWinLoss, ESportWinLoss, RunningBalance, PartnerRevenueShare + 6 keyless stored-proc result sets |
| `CasinoBettingContext` | `Data/CasinoBetting` | `ConnectionStrings:Casino*` | CasinoBet, CasinoBetAudit |
| `ESportBettingContext` | `Data/ESportBetting` | `ConnectionStrings:ESportBetting` | ESportBet, ESportBetAudit |
| `RedCatBettingContext` | `Data/RedCatBetting` | `ConnectionStrings:RedCatBetting` | Bet, BetAudit, BetSelection, BetSelectionAudit |

> `WalletContext` is **not** here — it lives in `Platform.Wallet.API`. **[Verified]**
> Exact entity field semantics are **[Likely]** (not every entity class read).

## 2. Utility — `MessageBroker` (the messaging backbone) `[Verified]`

`Utility/MessageBroker.cs` / `IMessageBroker.cs` wrap `RabbitMQ.Client`.

**Config keys read** (names only): `MessageBroker:Host|Port|Username|Password|VirtualHost|`
`Exchange|Queue|Consumer|RoutingKey|PrefetchCount|IsExclusive|IsPriority`, plus a parallel
`ExternalMessageBroker:*` set for the external/partner broker. `[Verified]`

**Connection model:**
- Exchange type **topic**; messages persistent (`DeliveryMode=2`); heartbeat 60s; network
  recovery 5s; **TLS when port == 5671**.
- `RoutingKey` supports multiple keys split on `|`.
- `PrefetchCount` default 1000 (QoS).
- `IsExclusive` → MD5-suffixed exclusive queue; `IsPriority` → queue with max-priority 10.

**Connect methods:** `Connect()` (auto-ack via internal handler), `ConnectWithManualAck()`,
`ConnectWithAutoAck()`, `ExternalConnect()`.

**Publish methods:** `Publish<T>(payload, messageType, routingKey, correlationId[, priority])`,
string overloads, `CPublish<T>(...)` (new channel per publish), `ExternalPublish<T>(...)`.

**Ack:** `Acknowledge(e)` (BasicAck), `NAcknowledge(e)` (BasicNack, requeue=true).

**Telemetry:** injects W3C trace context into RabbitMQ headers; producer-kind `Activity`
under source `nameof(MessageBroker)`. `[Verified]`

### `MemCache` / `IMemCache` `[Verified]`
In-process cache over `System.Runtime.Caching.MemoryCache`:
`AddOrUpdateCache<T>(key, obj, timeoutHours | DateTimeOffset)`, `GetCache<T>(key)`,
`GetCache<T>(key, Func<T> loader, expiration)` (lazy-load), `DeleteCache(key)`,
`DeleteCacheByPrefix(prefix)`.

Other utilities: `Helper`/`IHelper`, `JwtParser`, `Formatting`.

## 3. Providers — factory + strategy `[Verified]`

```
IBetProviderFactory.Resolve(int channelType)
   → matches (int)provider.ChannelType against channelType, else NotSupportedException
```
`ChannelType` enum (`Enums/ChannelType.cs`): **`BG=0, EM=1, OD=2, SR=3`**. `[Verified]`

| Implementation | `ChannelType` | Backing system |
|----------------|---------------|----------------|
| `MtsBetProvider` (`Providers/MTS`) | `SR` (Sportradar) | Sportradar MTS SDK (`IMtsSdk`, lazy init); deps `IMTSAuthRepository`, `IMTSMaxStakeRepository`, `IParlayBonusService` |
| `GTSBetProvider` (`Providers/GTS`) | `BG` (BetGenius/GTS) | HTTP GTS API (assessment, send bet, maxstake); supports **delayed acceptance** (pending bets) |
| `BetGeniusLiveStream` (`Providers/BGLiveStream`) | `BG` | BetGenius livestream HTTP (`ILiveStreamProvider`, resolved by `ILiveStreamFactory`) |

`IBetProvider` contract: `ValidateAsync(OnBetPlacement, ct)`, `IsAvailableAsync`,
`GetMaxStakeAsync`, `ProcessSettlement`, `ProcessCancellation`. Returns `BetProviderResult`
(`Acceptance` enum + reason) / `ProviderStakeResult`. `[Verified]`

Feature flag: `Custom:IsProviderEnabled`. Test-mode clients: `Custom:TestBetClientCodes`.

## 4. Repositories — external HTTP integrations `[Verified]`

| Repository | Talks to | Config keys (names) |
|------------|----------|---------------------|
| `BetGeniusRepository` | BetGenius API (auth/login, livestream) | `Custom:BetGenius:Uri|User|Password`; caches token `bg:auth` |
| `GTSRepository` | GTS API (assessment/sendbet/maxstake) | `API:GTSAPI`, `Custom:GTSBookmakerId`, `Custom:IsProviderEnabled` |
| `GTSAuthRepository` | GTS auth/token | — |
| `MTSAuthRepository` | MTS OpenID token (`auth/realms/mts/...`) | `API:MTSAuthAPI`, `Custom:MTSClientSecret|MTSUsername|MTSPassword`; caches `mts:auth` 55 min |
| `MTSMaxStakeRepository` | MTS max-stake API | uses MTS auth token |

## 5. MessageModels — 30 RabbitMQ contracts `[Verified]`

Bet lifecycle: `OnBetPlacement`, `OnBetSettlement` (+ `Partial`, `Retry`), `OnBetCancellation`,
`OnBOBetCancellation`, `OnBOBetManualSettlement`, `OnBetTimeout`, `OnBetStatus`.
Cashout: `OnCashOut`, `OnCashOutChange`, `OnCashOutResult`, `OnCashoutBetSettlement`,
`OnBetFullCashOut`/`OnBetPartialCashOut`.
Wallet/finance: `OnWalletChange`, `OnPayableChange`.
Product-specific: `OnCasinoBetSettlement`, `OnESportBetSettlement`, `OnRedCatBetSettlement`,
`OnClientTopMarketBet*`, ESport/OG credit-debit request/response pairs.
Odds/fixtures: `OnUOFBetCancellation`/`OnUOFBetSettlement`, `OnOddsFeedBetCancellationMessage`,
`OnMarketDeactivationMessage`. Cache: `OnDeleteCacheMessage`.

`OnBetPlacement` carries `Bet`, channel routing (`ChannelTypeId`), boost/builder flags,
`DelayedBetAcceptance`, `OddsChangeType`, and `BetSelections`. `[Verified]`

## 6. Enums — 33 `[Verified]` (selected)

- `ChannelType` (BG/EM/OD/SR), `Channel`, `ClientType`, `Product`, `UserType`.
- `BetStatus` (Waiting 1000, Running 2000, Won 3000/HalfWon 3001, Lost 4000/HalfLost 4001,
  Void 5000, Rejected 6000, Cancelled 7000, CashedOut 8000), `BetType` (Single/Parlay/PriceBoost/BetBuilder).
- `MarketStatus` (Active 1, Inactive 0, Suspended -1, HandedOver -2, Settled -3, Cancelled -4, NotMapped 99).
- `StatusCode` — large business/error code enum used in `BaseResponseDTO`.
- Odds (`OddsType`, `OddsChangeType`), cashout (`CashOutStatus`, `CashoutType`), fixture
  (`FixtureStatus*`, `FixtureType`, `FixtureScope`), reporting/filter enums.

## 7. Services / Helpers / Extensions `[Verified]`

- `Services/ParlayBonusService` — opens a `BettingContext` (`ConnectionStrings:Betting`),
  reads `ParlayBonusAudit`, returns `(bool HasBonus, ParlayBonusAudit Config)` for 3+ winning
  selections meeting min-odds; uses `TransactionScope` ReadCommitted. (Filename has a stray
  trailing space: `ParlayBonusService .cs` — minor cleanup item.)
- `Helpers` — `DecimalJsonConverter`, `OddsTypeHelper`.
- `Extensions` — DateTime/Decimal/String/Hash/List/Int/Object/Enum + bet-audit extensions.

## Why this matters

`Platform.Shared` is the contract surface of the whole platform: change a `DbContext`,
a message model, an enum value, or `MessageBroker` semantics here and you affect **every**
service. It is the highest-leverage and highest-risk area in the repo. See
[DependencyGraph.md](DependencyGraph.md) §6 and [TechnicalDebt.md](TechnicalDebt.md).
