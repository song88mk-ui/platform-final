# Contracts (Shared & Cross-Repo)

> Phase 5. The contracts that cross repository boundaries — message contracts, DTOs, shared
> libraries, and the **routing-key registry** — plus the single most important finding:
> **contracts are duplicated per repo and aligned by convention, not physically shared.**
> Classification: **[Verified]** platform-final · **[Verified-sibling]** the other repo.

## Headline: there is no shared contract package across the platform `[Verified]`
- platform-final's RabbitMQ message contracts (`Platform.Shared/MessageModels/On*.cs`, 30 files) are
  **local to `Platform.Shared`**. The solution references **no shared contracts NuGet** and **no
  sibling namespace** (`grep` for `OE.`/`BG.`/`Auth.Shared`/`using *Contracts` = none). **[Verified]**
- odds-engine-2 defines its **own** classes (`OE.Shared/Messages/...`), platform-web defines its
  **own** (`Models/EngineMessages/*`), oddsfeed/bet-genius theirs. bet-genius *does* reference the
  `OE.Shared` package, but **platform-final does not**. **[Verified-sibling]**
- **Consequence:** wire compatibility is maintained by **convention** (identical JSON/BSON/MessagePack
  field names + agreed routing keys), enforced by **no compiler**. A rename in one repo silently
  breaks the others. This is the central cross-repo risk — see [TechnicalDebt.md](TechnicalDebt.md).

## 1. Message contracts (RabbitMQ `e.central`)
Each repo owns its copy; they interoperate by field-name + routing-key convention.

### platform-final's own contracts (`Platform.Shared/MessageModels`) `[Verified]`
`OnBetPlacement`, `OnBetSettlement` (+`Partial`/`Retry`), `OnBetCancellation`, `OnBOBetCancellaton`
(sic), `OnBOBetManualSettlement`, `OnBetBackofficeCancellation`, `OnBetTimeout`, `OnBetStatus`,
`OnCashOut`/`OnCashOutChange`/`OnCashOutResult`/`OnCashoutBetSettlement`, `OnWalletChange`,
`OnPayableChange`, `OnCasinoBetSettlement`, `OnESportBetSettlement`, ESport/OG credit-debit
request/response, `OnClientTopMarketBet*`, `OnUOFBetCancellation`/`OnUOFBetSettlement`,
`OnOddsFeedBetCancellationMessage`, `OnMarketDeactivationMesssage` (sic), `OnDeleteCacheMessage`.

> **Back-office cancellation (GM-893):** the class `OnBetBackofficeCancellation` travels as message
> type `"OnBetBackOfficeCancellation"` (capital `O`) on `r.bet.cancellation`. Operator.Servicev2
> publishes it with only `BetId`, and Provider.Service consumes it (`Worker.cs:189`). The type is a
> string literal at each `Publish` call, not the class name, and must match the consumer `switch`
> exactly; a mismatch is dropped with no log (TechnicalDebt TD-27).

### Contracts platform-final consumes from siblings (by convention) `[Verified-sibling]`
| Inbound contract | Producer (sibling class) | Routing key |
|------------------|--------------------------|-------------|
| odds change | odds-engine-2 `OnInternalOddsChangeMessage` | `r.oe.sb.engine.onoddschange` |
| bet-stop fixture/global | odds-engine-2 | `r.oe.sb.engine.onbetstop*` |
| settlement | odds-engine-2 `OnBetSettlementMessage` | `r.oe.bet.settlement` |
| oddsfeed bet status | oddsfeed | HTTP callback (`OddsFeedController`) + `OnOddsFeedBetCancellationMessage` |
| cache invalidation | backoffice-final (`OnClientChange`/`OnMarketSortOrderChange`/…) | consumed by `CacheSync.Service` |

> Note: platform-final and odds-engine-2 each define `OnDeleteCacheMessage`, `OnBetSettlement`, etc.
> **separately** — same intent, different types. **[Verified]** + **[Verified-sibling]**.

> **Odds change (GM-902):** OE2 serializes `OnInternalOddsChangeMessage` with short names (`FiId`,
> `Ma`, `Se`, `Pr`, ...). `CashOutStake.Servicev2` mirrors it in `OnExternalOddsChangeMessage`
> (`Repository/BettingRepository.cs`) and maps every property with `[JsonPropertyName]`, because
> `System.Text.Json` is case-sensitive and ignores unknown properties: an unmapped mirror silently
> reads `FixtureId` as 0 and `Markets` as empty. A consumer-side mirror of an OE2 message needs an
> explicit wire name on every property and a test that deserializes an OE2-shaped payload
> (`CashOutStake.Servicev2.Test/Repository/OddsChangeContractTests.cs`). **[Verified]**.

## 2. Routing-key registry (the real cross-repo contract) `[Verified]`/`[Verified-sibling]`
| Namespace | Owner | Examples | Direction vs platform-final |
|-----------|-------|----------|------------------------------|
| `r.oe.sb.engine.*` | odds-engine-2 | onoddschange, onbetstopfixture/global, onvisit*, onmarketsortorderchange.s88 | **inbound** (consume / WS feed) |
| `r.oe.adapter.*`, `r.oe.uof.heartbeat` | odds-engine-2 | adapter on*, heartbeat | internal to OE2 |
| `r.oe.bet.settlement` | odds-engine-2 / shared | settlement | **inbound** (Settlement/CashOutStake) |
| `r.uof.*` | UOF feed → OE2 | — | not platform-final |
| `r.bg.*`, `r.bg.receiver.*`, `r.oe.mapping.bg.*` | bet-genius | feed/assessment | bet-genius internal |
| `r.of.bet.*`, `r.of.sportsbook.*` | oddsfeed | placement/cancellation/recovery | oddsfeed internal |
| `r.bet.*` | **platform-final** | `r.bet.betstatus.s88`, `r.bet.cashoutchange.s88`, `r.bet.reports.agent`, `r.bet.cashout.error`, `r.bet.placement` | **outbound** (produce) |
| `r.wallet.*` | **platform-final** | `r.wallet.s88`, `r.wallet.{clientCode}` | **outbound** |

`*.s88` is the client-code suffix for the Song88 brand (per-client routing). **[Verified-sibling]**

## 3. HTTP / REST contracts
- **platform-final as provider:** `Platform.API` (public + internal), `ClientSportsbook.API`,
  `Operator.API/OddsFeedController` — see [PlatformApi.md](PlatformApi.md) and
  [PublicApiSurface.md](PublicApiSurface.md). Envelope `BaseResponseDTO<T>` + `StatusCode` enum is the
  shared shape. **No OpenAPI contract package** is shared; consumers hand-write DTOs. **[Verified]**
- **platform-final as consumer:** Auth (`v1/authentication/*`), BetGenius/GTS, MTS, QTech, RabbitMQ.API.
- **DTO sharing:** **none cross-repo** — platform-web, oddsfeed, backoffice each define their own
  request/response models matching platform-final's by field name. **[Verified]** + **[Verified-sibling]**.

## 4. WebSocket contracts
SignalR method-name constants (`SubscribeTo*`, `OnOddsChange`, …) and the MessagePack message shapes
(`OddsChangeFixtureMessageModel` etc.) are a **three-party contract** (odds-engine-2 producer ↔
platform-final hub ↔ platform-web consumer), duplicated in each repo. See [WebSocket.md](WebSocket.md)
and [WebSocketContracts.md](WebSocketContracts.md). **[Verified]** + **[Verified-sibling]**.

## 5. Shared datastore "contracts" (implicit) `[Verified-sibling]`
The **MongoDB `sportsbook` document schema** is a contract between odds-engine-2 (writer) and
platform-final (reader) — again **field-name-aligned, separate C# classes**. The **SQL `Sportsbook`
schema** is shared between platform-final and backoffice-final (which writes ordering tables
directly). These are the highest-risk implicit contracts: schema drift breaks reads silently.

## 6. Shared libraries
| Library | Shared with platform-final? | Notes |
|---------|------------------------------|-------|
| `Infra.Extensions.Telemetry` (private NuGet) | **Yes** | OTel `AddTelemetry`; used across most repos | 
| `Sportradar.MTS.SDKCore` | yes (3rd-party) | MTS SDK |
| `OE.Shared` (odds-engine-2 package) | **No** (used by bet-genius, **not** platform-final) | OE2 contracts/models |
| `Auth.Shared` | **No** (Auth + its consumers; models only) | platform-final doesn't reference it |
| `Platform.Shared` | internal to platform-final only | the in-repo shared kernel |

## 7. Compatibility & versioning posture `[Verified]`/`[Unknown]`
- **REST:** path-versioned (`v1`/`v2`); no deprecation policy found (**[Unknown]**).
- **Messages/WebSocket/Mongo:** **no version field, no schema registry, no shared package** — purely
  convention. There is no automated contract test across repos in this repo; the one in-repo
  consumer-side test is `CashOutStake.Servicev2.Test/Repository/OddsChangeContractTests.cs` (GM-902).
  Cross-repo checkers under DEVOPS-409 (qa-robot-automation) are reported in Jira and were not read
  for this doc (**[Unknown]**).
- **Recommendation register (not an action):** the absence of shared contract types + the
  abbreviated field names make cross-repo changes fragile; a shared contracts package or
  contract tests would de-risk it. Logged in [TechnicalDebt.md](TechnicalDebt.md).

## The contract blast-radius (one-line takeaway)
> A field rename or routing-key change ripples across **up to four repos** (odds-engine-2 ↔
> platform-final ↔ platform-web, and oddsfeed/bet-genius/backoffice for their flows) with **no
> compiler and no contract test** to catch it. Treat `Platform.Shared/MessageModels`, the routing
> keys, the Mongo/SQL schemas, and the SignalR method names as **public API**.
