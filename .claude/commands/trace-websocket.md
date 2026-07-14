# /trace-websocket

Trace a real-time message from producer → hub → consumer.

## Topology (resolved)
`OE.Websocket.Service` (odds-engine-2) consumes `r.oe.sb.engine.*` + `r.bet.*`/`r.wallet.*` →
**pushes into platform-final's `Websocket.API2` `/platform` hub** → platform-web `HubConnection`.

## Steps
1. **Hub:** `Platform.Websocket.API2/Repository/WebSocketRepository.cs` — `SubscribeTo*` group mgmt +
   `Publish*` methods. **No in-process RabbitMQ consumer** (fed externally by OE.Websocket.Service).
2. **Event → routing key:** `OnOddsChange`←`r.oe.sb.engine.onoddschange`; `OnBetStopFixture/Global`;
   `OnBetStatus`←`r.bet.betstatus.s88`; `OnCashOutChange`←`r.bet.cashoutchange.s88`; `OnWalletChange`←`r.wallet.s88`.
3. **Message shape:** MessagePack, abbreviated fields (`fiId/maId/seId/deOd…`). Map:
   [docs/knowledge/WebSocketMessages.md](../../docs/knowledge/WebSocketMessages.md).
4. **Consume (sibling):** platform-web `HubConnection.On<T>` → mutate `FixtureModel` → `StateHasChanged`.

## Caution
Method names + message field names are duplicated across 3 repos (no compiler check) — verify
alignment. ([docs/WebSocket.md](../../docs/WebSocket.md), [docs/Contracts.md](../../docs/Contracts.md))
