# Repository Map

> One-line orientation for every project. Confidence **[Verified]** for names/types,
> **[Likely]** for purpose where based on naming + partial code. Full detail in
> [ProjectOverview.md](ProjectOverview.md).

## Where things live

```
Platform/
├── Platform.Shared/              ← shared kernel: entities, 8 DbContexts, message contracts,
│   ├── Data/ (8 contexts)            providers, MessageBroker, MemCache, enums, DTOs
│   ├── MessageModels/ (30)          (net8.0; referenced by everything)
│   ├── Providers/ (MTS, GTS, BGLiveStream + factories)
│   ├── Repositories/ (BetGenius, GTS, MTS auth/maxstake)
│   ├── Enums/ (33)  Models/ (189)  Extensions/  Helpers/  Services/  Utility/
│   └── DatabaseScripts/
├── Platform.Shared.Test/         ← test project
│
│  ── EDGE / PUBLIC APIs ──
├── Platform.API/                 ← main public integration gateway (auth, bet, wallet, sportsbook)
├── Platform.Betting.API/         ← bet placement, maxstake, history, config, cashout, settlement
├── Platform.Sportsbook.API/      ← sportsbook reads: fixtures, leagues, markets, livestream, translation
├── Platform.ClientSportsbook.API/← client-facing sportsbook (Redis-cached, per-client auth)
├── Platform.ClientPlatform.API/  ← client-facing platform reads (top/active fixtures, history)
├── Platform.Client.API/          ← legacy client sportsbook fixture API
├── Platform.Operator.API/        ← back-office/operator API (login, placement, reports)
├── Platform.Wallet.API/          ← wallet ledger (credit/debit/transfer/balance; stored procs)
│
│  ── INTEGRATION / 3rd-PARTY APIs ──
├── Platform.Oddin.API/           ← Oddin/Betradar eSports odds + wallet credit/debit
├── Platform.QTech.API/           ← QTech casino provider (sessions, balance, transactions, lobby)
├── Platform.RedCat.API/          ← RedCat operator callbacks (validatetoken, stake, settle, void)
├── Platform.RedCatBetting.API/   ← RedCat betting endpoint (placement/settle/void)
├── Platform.RedCatOperator.API/  ← RedCat operator-side (placement/settle/void → RabbitMQ)
├── Platform.AgentReports.API/    ← agent/partner reporting (winloss, GGR, P&L, transfers)
│
│  ── MESSAGING / REAL-TIME ──
├── Platform.RabbitMQ.API/        ← HTTP→AMQP publish bridge (/v1/broker/publish)
├── Platform.WebSocket.API/       ← SignalR hub (MessagePack/Azure SignalR), client push (superseded)
├── Platform.Websocket.API2/      ← newer SignalR hub rewrite (the one CI ships)
├── Platform.WebSocket.Client/    ← sample/test SignalR console client
│
│  ── BACKGROUND SERVICES (RabbitMQ workers) ──
├── Platform.Placement.Servicev2/ ← consumes OnBetPlacement, 100-thread provider dispatch
├── Platform.BetInfo.Service/     ← persists bet info on OnBetPlacement (manual ack)
├── Platform.BetForwarder.Service/← forwards bet events to EXTERNAL RabbitMQ broker
├── Platform.BetStatus.Service/   ← relays status/cashout/wallet events to external (per-client routing)
├── Platform.Settlement.Service/  ← settlement/cancellation/manual settlement (OpenTelemetry)
├── Platform.CashOut.Service/     ← full/partial cashout, 100-thread pool, MTS SDK
├── Platform.CashOutStake.Servicev2/← maintains active-bet cashout stakes; reacts to OnOddsChange
├── Platform.Provider.Service/    ← routes bet events to bet providers; Ably realtime GTS assessment
├── Platform.Operator.Servicev2/  ← operator-channel bet event → provider routing (OpenTelemetry)
├── Platform.Operator.Cashout.Service/ ← operator-channel cashout processing
├── Platform.ESportOperator.Service/   ← eSports operator messaging
├── Platform.CacheSync.Service/   ← consumes 7 cache-invalidation events; clears caches
├── Platform.AgentReports.Service/← consumes bet/cashout/settlement events → reporting DB
├── Platform.Client.TopMarket/    ← "top market" bet stats worker (ClientTopMarket.Service)
│
│  ── JOBS (timed/loop) ──
├── Platform.Job.CCF/             ← Confidence Factor job, every 300s
├── Platform.Job.ForexEUR/        ← EUR forex rate updates
├── Platform.Job.Operator.Score/  ← scores from Betradar + CricketScore APIs
├── Platform.Job.Resettlement/    ← retries failed settlements (Fibonacci backoff, RetryCount<8)
│
│  ── CONSOLE ──
└── Platform.BetPlacement.Console/← load/stress test client (hardcoded staging endpoint)
```

## Quick "where do I look for…" index

| I need to change / understand… | Go to |
|--------------------------------|-------|
| A bet's data model / DB schema | `Platform.Shared/Data/Betting/`, `.../Models/` |
| A RabbitMQ message contract | `Platform.Shared/MessageModels/On*.cs` |
| How RabbitMQ connect/publish works | `Platform.Shared/Utility/MessageBroker.cs` |
| How a bet is validated by a provider | `Platform.Shared/Providers/` (MTS, GTS) + factory |
| The public API surface | `Platform.API/Controllers/IntegrationController.cs` |
| Bet placement business logic | `Platform.Betting.API/Repository/`, `Platform.Placement.Servicev2/` |
| Settlement logic | `Platform.Settlement.Service/` |
| Cashout logic | `Platform.CashOut.Service/`, `Platform.CashOutStake.Servicev2/` |
| Wallet credit/debit | `Platform.Wallet.API/Repository/` |
| Real-time client push | `Platform.WebSocket.API/Repository/WebSocketRepository.cs` |
| API-key/auth filters | any `*/ActionFilters/` |
| Telemetry setup | any `*/Telemetry/Instrumentation.cs` + `Startup.cs`/`Program.cs` |
| Deploy/CI | `.github/workflows/ci.yml`; sibling `argocd` repo |
