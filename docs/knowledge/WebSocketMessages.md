# WebSocket Messages (Quick Reference)

> Phase 9. Fast lookup of the SignalR hub contract. Authoritative: [WebSocketContracts.md](../WebSocketContracts.md),
> [WebSocket.md](../WebSocket.md). Hub = platform-final `Websocket.API2` `/platform`; fed by
> `OE.Websocket.Service` (odds-engine-2). All **[Verified]/[Verified-sibling]**.

## Connection
`websocket-api/platform` · MessagePack (`Custom:UseMessagePack`) or JSON · WebSockets-only
(`SkipNegotiation`) · auto-reconnect · `Custom:MaximumConnections=300` (consumer).

## Subscribe / unsubscribe (client → hub)
| Method | Payload |
|--------|---------|
| `GetConnectionId` | — |
| `SubscribeToEndpoint` / `UnSubscribeToEndpoint` | `{ ClientId, ChannelKey }` |
| `SubscribeToTopMarkets` / `UnSubscribeToTopMarkets` | `{ ClientId, FixtureId }` |
| `SubscribeToBetslip` / `UnSubscribeToBetslip` | `{ ClientId, FixtureId }` |
| `SubscribeToBetStatus` / `UnsubscribeToBetStatus` | `{ ClientId, UserId }` |
| `SubscribeToCashOutChange` / `UnsubscribeToCashOutChange` | `{ ClientId, UserId }` |
| `SubscribeToWalletChange` / `UnsubscribeToWalletChange` | `{ ClientId, UserId }` |
| `SubscribeToFavorites` / `UnsubscribeToFavorites` | `{ ClientId, UserId }` |

Channel keys: `fixturedetails:{fixtureId}`, `sports:{sportId}:livefixtures`.

## Server → client events
| Event | Message model | Producer / routing key |
|-------|---------------|------------------------|
| `OnOddsChange` | `OddsChangeFixtureMessageModel` | odds-engine-2 `r.oe.sb.engine.onoddschange` |
| `OnBetslipOddsChange` | `OddsChangeFixtureMessageModel` | (betslip-scoped) |
| `OnTopMarketsOddsChange` | `OddsChangeFixtureMessageModel` | top-markets |
| `OnBetStopFixture` / `OnBetslipBetStopFixture` / `OnTopMarketsBetStopFixture` | `BetStopFixtureMessageModel` | `r.oe.sb.engine.onbetstopfixture` |
| `OnBetStopGlobal` | `BetStopGlobalMessageModel` | `r.oe.sb.engine.onbetstopglobal` |
| `OnBetStatus` | `BetStatusMessageModel` | platform-final `r.bet.betstatus.s88` |
| `OnCashOutChange` | `CashOutChangeMessageModel` | platform-final `r.bet.cashoutchange.s88` |
| `OnWalletChange` | `WalletChangeModel` | platform-final `r.wallet.s88` |

## Message field maps (abbreviated MessagePack names) `[Verified-sibling]`
- **`OddsChangeFixtureMessageModel`**: `spId/spNa/segId/segNa/leId/leNa/fiId/prFiId/fiNa/hoNa/awNa/
  fiStDa/isOfLi/fiStId/sc/cuPe/cuMi/maCo/isBeBu` + translations `spNaTr/segNaTr/leNaTr/fiNaTr` +
  `ma:[OutgoingMarket]`. **OutgoingMarket**: `maTeId/maId/li/maStId/maStNa/maNa/soOr/isBeBu/maNaTr/
  se:[OutgoingSelection]`. **OutgoingSelection**: `seTeId/seId/seNa/deOd/maOd/hoOd/inOd/isAc/soOr/seNaTr`.
- **`BetStopFixtureMessageModel`**: `fiId, isBeSt, seAcMaToIn`.
- **`BetStopGlobalMessageModel`**: `isBeSt`.
- **`BetStatusMessageModel`**: `usId, beId, beStId, beStNa, me`.
- **`CashOutChangeMessageModel`**: `usId, beId, caOuStId, caOuStNa, caOuSt`.
- **`WalletChangeModel`**: `usId, ba`.

## Gotcha `[Verified]`
The message classes + method-name constants are **duplicated** across odds-engine-2 (producer),
platform-final `Websocket.API2` (hub), and platform-web (consumer) — aligned only by field-name
convention, no shared type or contract test. A rename is a 3-repo silent break. See
[Contracts.md](../Contracts.md), [AntiPatterns.md](../AntiPatterns.md) AP-9.
