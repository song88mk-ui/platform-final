# Odds Engine Integration

> Phase 5. How platform-final integrates with the **odds engines / odds providers**:
> **odds-engine-2** (primary), **bet-genius** (GTS validation + livestream), **oddsfeed** (MTS/
> Betradar). Classification: **[Verified]** platform-final · **[Verified-sibling]** the engine repo.
> `oddsengine` (v1) is **obsolete** — not used here.

## 1. odds-engine-2 — the catalogue & odds source of truth

### Purpose
odds-engine-2 ingests the **UOF / Betradar** feed, computes odds/market state, **writes the MongoDB
read model** platform-final serves, and **pushes real-time changes** to the WebSocket hub. It is the
**owner** of fixtures/markets/selections/odds — platform-final neither creates nor grades them.
**[Verified-sibling]** + **[Verified]** (platform reads, never writes, the catalogue).

### Integration channel 1 — MongoDB (shared datastore) `[Verified-sibling]`
odds-engine-2 (`OE.Sportsbook.Service`, `OE.Job.*`) **writes** the `sportsbook` Mongo DB collections
that platform-final's `Sportsbook.API` **reads**:

| Collection | Written by OE2 | Read by platform-final |
|------------|----------------|------------------------|
| `Fixture` | ✅ | ✅ (`Sportsbook.API/Repository/SportsbookRepository.cs`) |
| `Market` | ✅ | ✅ |
| `Selection` | ✅ | ✅ |
| `MarketMapping` | ✅ | ✅ |
| `BettingStatus` | ✅ | ✅ |
| `Maintenance` | **not found** in OE2 | ✅ read | → **[Unknown]** writer |

Same abbreviated document shape on both sides (`FiId/SpId/MaId/SeId/DeOd…`), but the **C# classes
are separate per repo** (OE2 `OE.Shared.Data.Sportsbook.MongoDB.Collections.*` vs platform-final
`Platform.Shared/Data/Sportsbook/Entities/*`). Compatibility is by **BSON field-name convention**,
not a shared type. See [Contracts.md](Contracts.md).

### Integration channel 2 — RabbitMQ (`e.central`) `[Verified-sibling]`
OE2 publishes (consumed by platform-final services / the WS feeder):

| Routing key | Message (OE2 class) | platform-final consumer |
|-------------|---------------------|--------------------------|
| `r.oe.sb.engine.onoddschange` | `OnInternalOddsChangeMessage` | `CashOutStake.Servicev2`; WS feeder → hub `OnOddsChange` |
| `r.oe.sb.engine.onbetstopfixture` | bet-stop fixture | WS feeder → `OnBetStopFixture` |
| `r.oe.sb.engine.onbetstopglobal` | bet-stop global | WS feeder → `OnBetStopGlobal` |
| `r.oe.bet.settlement` | `OnBetSettlement` / cancellation | `Settlement.Service`, `CashOutStake.Servicev2` (`r.oe.bet.settlement` routing) |
| `r.oe.adapter.on*.*` | `OnFixtureChange`/`OnMarketChange`/`OnMarketSettlement`/`OnMarketCancellation` | consumed within OE2 (adapter→sportsbook); platform sees the derived `r.oe.sb.engine.*` |
| `r.oe.uof.heartbeat` | `OnHeartbeatMessage` | health/keepalive |

> **Wire names (GM-902):** `CashOutStake.Servicev2` reads `r.oe.sb.engine.onoddschange` into its
> own mirror, `OnExternalOddsChangeMessage` in `Repository/BettingRepository.cs`, which maps OE2's
> short names with `[JsonPropertyName]` (`FiId`, `FiStId`, `IsSu`, `Ma`/`MaId`/`MaStId`, `Se`/`SeId`/
> `IsAc`/`Pr`/`WoPr`/`LoPr`/`VoPr`/`HaWoPr`/`HaLoPr`). Before GM-902 it used the OE v1 names, every
> field deserialized to its default, and no cashout value was pushed from an odds change. It is pinned
> by `CashOutStake.Servicev2.Test/Repository/OddsChangeContractTests.cs`. `r.oe.bet.settlement` is
> not affected: OE2 publishes it with the long names. **[Verified]** + **[Verified-sibling]**.

> Note the **two layers**: `OE.Adapter.Service` (`r.oe.adapter.*`, raw UOF-derived) feeds
> `OE.Sportsbook.Service`, which emits the **`r.oe.sb.engine.*`** events platform-final's edge cares
> about. **[Verified-sibling]**

### Integration channel 3 — WebSocket feeder `[Verified-sibling]` (resolves U-19)
`OE.Websocket.Service` (net8, SignalR client + MessagePack) consumes `r.oe.sb.engine.*` **and**
platform's own `r.bet.betstatus.s88` / `r.bet.cashoutchange.s88` / `r.wallet.s88`, then
`SendAsync(method, clientCode, payload)` into platform-final's **`/platform` hub** (`Websocket.API2`).
argocd's `oev2-websocket-service` config points `Custom__WebSocketAPI` →
`http://websocket-api.platform.svc:3000/platform`. So **OE2 owns odds production; platform-final owns
the hub** clients connect to. See [WebSocket.md](WebSocket.md).

### No REST `[Verified-sibling]`
`OE.API` is an empty stub — odds-engine-2 exposes **no REST** that platform-final calls. Integration
is Mongo + RabbitMQ + the WS feeder only. (Back office, however, calls a separate `TradingAPI` /
`api.oddsengine.svc` for trading data — that is backoffice↔odds-engine, not platform-final.)

## 2. bet-genius — GTS validation + livestream `[Verified]` / `[Verified-sibling]`
- **Outbound HTTP from platform-final:** `GTSBetProvider` (ChannelType **BG**) calls BetGenius/GTS
  for `Auth-v1/PROD/login`, bet **assessment**, `SendBet` (with delayed-acceptance support);
  `BetGeniusLiveStream` fetches livestream tokens. Config `Custom:BetGenius:Uri|User|Password`,
  `API:GTSAPI`, `Custom:GTSBookmakerId`. **[Verified]**
- **Async assessment:** `Provider.Service` also subscribes to **Ably** for GTS assessment responses
  and republishes `OnBetPlacement` with the acceptance result. **[Verified]**
- bet-genius itself ingests the Genius feed (`r.bg.*`, `BG.Engine.Sportsbook` consumes
  `r.bg.receiver.#` + `r.oe.mapping.bg.#`) and references the `OE.Shared` package. **[Verified-sibling]**

## 3. oddsfeed — MTS/Betradar (parallel, bidirectional) `[Verified]` / `[Verified-sibling]`
- **MTS** is reached by **both** stacks: platform-final's `MtsBetProvider` (ChannelType **SR**) +
  `CashOut.Service` MTS SDK use the same Betradar MTS as oddsfeed (bookmaker 32290). **[Verified]**
- **Bidirectional:** oddsfeed (`OF.PlatformClient.API`) calls platform-final `Platform.API` for
  client/bet-config/exchange-rate; platform-final's `Operator.API/OddsFeedController` receives
  oddsfeed bet-status callbacks; `OnOddsFeedBetCancellationMessage` is a platform-final contract.
  **[Verified]** + **[Verified-sibling]**.
- oddsfeed publishes `r.of.bet.*`, and shares settlement keys `r.bet.settlement`/`r.oe.bet.settlement`.

## Channel-by-engine summary
| Engine | Mongo | RabbitMQ | WebSocket | HTTP | platform-final touchpoint |
|--------|-------|----------|-----------|------|---------------------------|
| odds-engine-2 | writes catalogue | `r.oe.*` | feeds `/platform` hub | none | Sportsbook.API reads; Settlement/CashOutStake consume; Websocket.API2 |
| bet-genius | (own Trading DB) | `r.bg.*` | — | assess/livestream | GTSBetProvider, BetGeniusLiveStream, Provider.Service (Ably) |
| oddsfeed | (own Sportsbook DB) | `r.of.*`, `r.bet/oe.settlement` | — | bidirectional REST | OddsFeedController; MtsBetProvider shares MTS |

## Risks `[Verified]`
- **Convention-coupled catalogue:** platform-final's Mongo entity classes must stay field-aligned
  with odds-engine-2's writers; no compiler enforces this. A field rename in OE2 silently breaks
  platform reads. Tracked in [Contracts.md](Contracts.md) / [TechnicalDebt.md](TechnicalDebt.md).
- **`Maintenance` collection writer unknown** (U-24, [Unknowns.md](Unknowns.md)).
- **Two odds providers (OE2 + oddsfeed) + MTS shared** — operationally which is authoritative per
  fixture is config-driven and not traced here (**[Unknown]**).
