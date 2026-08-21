# WebSocket / Real-Time Contracts

> Added during continued Phase 1. The SignalR hub lives in platform-final
> (`Platform.WebSocket.API` / `Platform.Websocket.API2`); the **message contracts and
> subscribe/push names** below are **[Verified: consumer]** from the platform-web sibling
> (which connects as a client) and **[Verified]** where confirmed against platform-final's hub
> repository. See [CrossRepositoryDependencies.md](CrossRepositoryDependencies.md).

## Connection (consumer view) `[Verified: consumer]`
- **URL:** `API:PlatformWebsocketAPI` (default staging `https://websocket-api.stg.song88.com/platform`),
  resolved per-domain via `UrlMappings` (same scheme as the REST gateway).
- **Hub path:** `/platform`. **Transport:** WebSockets only (`SkipNegotiation = true`).
- **Protocol:** SignalR + **MessagePack** when `Custom:UseMessagePack = true` (default), else JSON;
  `ContractlessStandardResolver`, `MessagePackSecurity.UntrustedData`.
- **Reconnect:** `WithAutomaticReconnect()`. **No auth token** on the hub connection.
- platform-web caps connections at `Custom:MaximumConnections = 300`.

## Subscribe / Unsubscribe methods (client → hub) `[Verified]` (hub) / `[Verified: consumer]` (payloads)
These match the `SubscribeTo*`/`UnsubscribeTo*` methods on platform-final's
`WebSocketRepository : Hub<IPlatformEvent>`.

| Method | Payload | Purpose |
|--------|---------|---------|
| `GetConnectionId` | — | returns connectionId |
| `SubscribeToEndpoint` / `UnSubscribeToEndpoint` | `{ ClientId, ChannelKey }` | fixture/endpoint group |
| `SubscribeToTopMarkets` / `UnSubscribeToTopMarkets` | `{ ClientId, FixtureId }` | top-markets feed |
| `SubscribeToBetslip` / `UnSubscribeToBetslip` | `{ ClientId, FixtureId }` | betslip odds for a fixture |
| `SubscribeToBetStatus` / `UnsubscribeToBetStatus` | `{ ClientId, UserId }` | bet status |
| `SubscribeToCashOutChange` / `UnsubscribeToCashOutChange` | `{ ClientId, UserId }` | cashout updates |
| `SubscribeToWalletChange` / `UnsubscribeToWalletChange` | `{ ClientId, UserId }` | wallet balance |
| `SubscribeToFavorites` / `UnsubscribeToFavorites` | `{ ClientId, UserId }` | favourites feed |

Channel-key patterns: `fixturedetails:{fixtureId}`, `sports:{sportId}:livefixtures`. `[Verified: consumer]`

## Server → client push events `[Verified: consumer]`
Hub `Publish*` methods on platform-final send these named events:

| Event name | Message model (consumer) | Trigger (RabbitMQ routing key) |
|------------|--------------------------|--------------------------------|
| `OnOddsChange` | `OddsChangeFixtureMessageModel` | `r.oe.sb.engine.onoddschange` |
| `OnBetslipOddsChange` | `OddsChangeFixtureMessageModel` | (betslip-scoped odds) |
| `OnTopMarketsOddsChange` | `OddsChangeFixtureMessageModel` | top-markets odds |
| `OnBetStopFixture` / `OnBetslipBetStopFixture` / `OnTopMarketsBetStopFixture` | `BetStopFixtureMessageModel` | `r.oe.sb.engine.onbetstopfixture` |
| `OnBetStopGlobal` | `BetStopGlobalMessageModel` | `r.oe.sb.engine.onbetstopglobal` |
| `OnBetStatus` | `BetStatusMessageModel` | `r.bet.betstatus.s88` |
| `OnCashOutChange` | `CashOutChangeMessageModel` | `r.bet.cashoutchange.s88` |
| `OnWalletChange` | `WalletChangeModel` | `r.wallet.s88` |

> Note the **abbreviated MessagePack field names** in the wire models (`spId`, `fiId`, `deOd`,
> `maStId`, …) — they are deliberately short to minimise binary payload size. Full field maps are
> in platform-web `docs/WebSocket.md`.

## Hub ownership & origin of pushes `[Verified]` (hub) / `[Likely]` (feeder)
- **The production `websocket-api/platform` hub IS platform-final's `Platform.Websocket.API2`**
  (the `net7.0` SignalR hub). **[Verified — confirmed by maintainer]**. `Platform.WebSocket.API`
  (the 3.1 original) is superseded; CI ships `Websocket.API2` as image `platform-websocket-api`.
- **What feeds it:** odds/bet-stop pushes are driven by `r.oe.sb.engine.*` messages originating
  from **odds-engine-2**, which reach the hub's `PublishOddsChangeForMainMarkets` /
  `PublishOddsChangeForAllMarkets` / `PublishBetStop*` methods.
  **Finding [Verified]:** `Websocket.API2` itself contains **no in-process RabbitMQ consumer,
  `BackgroundService`, or push-trigger HTTP endpoint** — its `Repository/WebSocketRepository.cs`
  defines the `Publish*` methods and `SubscribeTo*` group management, but `SportsbookController`
  exposes only `v1/heartbeat` and `v1/connections`. Therefore the odds feeder is **external**:
  most likely an **odds-engine-2 service (`OE.Websocket.Service`)** pushing into the shared hub via
  the **Azure SignalR Service** backplane (consistent with platform-web's attribution). `[Likely]`
- bet-status / cashout / wallet pushes (`r.bet.betstatus.s88`, `r.bet.cashoutchange.s88`,
  `r.wallet.s88`) originate from platform-final services (`BetStatus.Service` et al.). `[Verified]`

## Cross-reference
- Subscribe/push method inventory aligns with [ProjectOverview.md](ProjectOverview.md) →
  `WebSocket.API` / `Websocket.API2`.
- Routing keys complement the messaging table in [DependencyGraph.md](DependencyGraph.md) §3.
