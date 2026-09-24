---
name: websocket-investigation
description: Investigate real-time SignalR push behavior (Websocket.API2 hub, the OE.Websocket.Service feeder, event and routing key mapping). Use when a real-time update is missing, late or wrong in platform-web.
---

# Skill: WebSocket Investigation

Investigate real-time push behavior.

## Knowledge (resolved topology)
Hub = platform-final `Websocket.API2` `/platform` (self-hosted SignalR, MessagePack when
`Custom:UseMessagePack`; Azure SignalR only in the legacy `Platform.WebSocket.API`). **Fed by
`OE.Websocket.Service` (odds-engine-2)** which consumes `r.oe.sb.engine.*` + `r.bet.*`/`r.wallet.*`.
Consumer = platform-web `HubConnection`. The hub has **no in-process RabbitMQ consumer**.

## Method
1. `/trace-websocket` for a specific event.
2. Map event ↔ routing key ↔ message model:
   [docs/knowledge/WebSocketMessages.md](../../../docs/knowledge/WebSocketMessages.md) /
   [docs/WebSocketContracts.md](../../../docs/WebSocketContracts.md).
3. Producer side: odds → odds-engine-2; bet-status/cashout/wallet → platform-final services
   (`BetStatus.Service`, `CashOutStake.Servicev2`, wallet flow).
4. Check `Websocket.API2/Repository/WebSocketRepository.cs` `Publish*`/`SubscribeTo*` methods.

## Caution
Method + field names are duplicated across 3 repos (no contract test) — verify alignment (R-2).
