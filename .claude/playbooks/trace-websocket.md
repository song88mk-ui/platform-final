# Playbook: Trace a WebSocket Message

1. **Pick the event** (`OnOddsChange`/`OnBetStopFixture`/`OnBetStopGlobal`/`OnBetStatus`/
   `OnCashOutChange`/`OnWalletChange`).
2. **Producer + routing key:** odds/bet-stop → odds-engine-2 (`r.oe.sb.engine.*`); bet-status/cashout/
   wallet → platform-final services (`r.bet.*`/`r.wallet.*`).
3. **Feeder:** `OE.Websocket.Service` (odds-engine-2) consumes those keys and pushes into the hub.
4. **Hub:** platform-final `Websocket.API2/Repository/WebSocketRepository.cs` (`Publish*`/`SubscribeTo*`;
   no in-process RabbitMQ consumer).
5. **Message shape:** MessagePack abbreviated fields — map in
   [docs/knowledge/WebSocketMessages.md](../../docs/knowledge/WebSocketMessages.md).
6. **Consumer (sibling):** platform-web `HubConnection.On<T>` → mutate `FixtureModel` → `StateHasChanged`.
7. **Verify contract alignment** across the 3 repos (no compiler check — R-2).

Ref: [docs/WebSocket.md](../../docs/WebSocket.md), [docs/WebSocketContracts.md](../../docs/WebSocketContracts.md).
