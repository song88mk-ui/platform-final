# WebSocket Integration (Cross-Repo)

> Phase 5. The real-time SignalR integration **across repos**. The per-message field maps and
> subscribe/push names are in [WebSocketContracts.md](WebSocketContracts.md); this doc is the
> **producer → hub → consumer** integration view, now fully resolved with sibling evidence.
> Classification: **[Verified]** platform-final · **[Verified-sibling]** OE2/platform-web · **[Likely]**.

## The three roles (resolved)
```
  PRODUCER                         HUB (owned by platform-final)            CONSUMER
  OE.Websocket.Service  ──push──▶  Websocket.API2  "/platform"   ──push──▶  platform-web HubConnection ──▶ Browser
  (odds-engine-2, net8)            (platform-final, net7, Azure SignalR)    (Blazor circuit)
        ▲ consumes
   RabbitMQ e.central: r.oe.sb.engine.* , r.bet.betstatus.s88 , r.bet.cashoutchange.s88 , r.wallet.s88
```
- **Hub:** platform-final's **`Websocket.API2`** (`/platform`, MessagePack, Azure SignalR in prod;
  CI image `platform-websocket-api`, `websocket-api.platform.svc:3000`). **[Verified]** + **[Verified-sibling: argocd]**.
- **Producer/feeder:** **`OE.Websocket.Service`** (odds-engine-2) consumes the RabbitMQ keys above and
  `SendAsync(method, clientCode, payload)` into the hub. argocd config:
  `Custom__WebSocketAPI = http://websocket-api.platform.svc:3000/platform`. **[Verified-sibling]** —
  **resolves U-19**.
- **Consumer:** platform-web's scoped `HubConnection` (WebSockets-only, `SkipNegotiation`, MessagePack,
  auto-reconnect). **[Verified-sibling]**.

> Important: `Websocket.API2` **defines** the hub (group management via `SubscribeTo*`, the
> `Publish*` methods) but contains **no in-process RabbitMQ consumer** (Phase 4 finding). The
> odds/bet-stop **content** is produced by OE.Websocket.Service; bet-status/cashout/wallet pushes
> originate from platform-final services (`BetStatus.Service` et al.) publishing `r.bet.*`/`r.wallet.*`
> which the OE feeder also relays. **[Verified]** + **[Verified-sibling]**.

## Message → routing-key → event map `[Verified-sibling]`
| Client event (platform-web) | Hub push method | RabbitMQ key (producer) | Origin |
|------------------------------|-----------------|-------------------------|--------|
| `OnOddsChange` | `PublishOddsChangeForMainMarkets`/`...AllMarkets` | `r.oe.sb.engine.onoddschange` | odds-engine-2 |
| `OnBetStopFixture` | `PublishBetStopFixture` | `r.oe.sb.engine.onbetstopfixture` | odds-engine-2 |
| `OnBetStopGlobal` | `PublishBetStopGlobal` | `r.oe.sb.engine.onbetstopglobal` | odds-engine-2 |
| `OnBetStatus` | `PublishBetStatus` | `r.bet.betstatus.s88` | platform-final (BetStatus.Service) |
| `OnCashOutChange` | `PublishCashOutChange` | `r.bet.cashoutchange.s88` | platform-final (CashOutStake/CashOut) |
| `OnWalletChange` | `PublishWalletChange` | `r.wallet.s88` | platform-final (wallet flow) |
| `OnTopMarketsOddsChange` / favorites / boosted | (top-market/visit pushes) | `r.oe.sb.engine.onvisit*` / `onmarketsortorderchange.s88` | odds-engine-2 |

Subscribe methods (client→hub): `SubscribeToEndpoint/Betslip/BetStatus/CashOutChange/WalletChange/
Favorites/TopMarkets` with `{ClientId, ChannelKey|FixtureId|UserId}`; channel keys
`fixturedetails:{fixtureId}`, `sports:{sportId}:livefixtures`. **[Verified-sibling]** — see
[WebSocketContracts.md](WebSocketContracts.md) for fields.

## Contract shape `[Verified-sibling]`
Messages are MessagePack with **abbreviated field names** (`spId/fiId/maId/seId/deOd/maOd/hoOd/inOd`,
inline translations `spNaTr/leNaTr/…`). The C# message classes are **defined separately** in each
repo (OE2 `OutgoingSportsbookMessages`, platform-final `Websocket.API2/Messages/Outgoing*`,
platform-web `Models/EngineMessages/*`); alignment is **by field-name convention**, not a shared
type. `Custom:UseMessagePack` toggles MessagePack vs JSON on both ends. See [Contracts.md](Contracts.md).

## Transport & resilience `[Verified-sibling]`
- WebSockets-only (`SkipNegotiation = true`), `WithAutomaticReconnect()`; platform-web caps at
  `Custom:MaximumConnections = 300` per circuit-host. Azure SignalR Service backplane lets an
  **external** producer (OE.Websocket.Service) push to hub groups created by platform-final's
  `Websocket.API2` — which is why the producer and hub can live in different repos/pods.

## Why this matters
A change to the odds-message field names or the `Subscribe*`/`Publish*` method-name constants is a
**three-repo breaking change** (OE2 producer ↔ platform-final hub ↔ platform-web consumer) with no
compiler to catch it. This is the real-time analogue of the `Platform.Shared` blast radius. See
[ComponentDataFlow.md](ComponentDataFlow.md) and [TechnicalDebt.md](TechnicalDebt.md).
