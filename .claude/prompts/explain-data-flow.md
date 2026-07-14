# Prompt: Explain a Data / Message Flow

"Show how [data] flows from source to consumer." (platform-final is backend; UI flow = platform-web.)

## Pick the flow type
- **Read (catalogue):** odds-engine-2 → Mongo `Sportsbook` → `Sportsbook.API` (projection) → gateway →
  consumer. (`/trace-fixture`/`/trace-market`/`/trace-selection`)
- **Write (bet):** gateway → `Betting.API` (validate+provider+persist SQL) → `OnBetPlacement` →
  workers. (`/trace-bet`)
- **Settlement/cashout:** odds-engine-2 `OnBetSettlement` → `Settlement.Service`; cashout via
  `CashOut.Service`/`CashOutStake.Servicev2`. (`/trace-settlement`, `/trace-cashout`)
- **Real-time:** odds-engine-2 → RabbitMQ `r.oe.sb.engine.*` → `OE.Websocket.Service` →
  `Websocket.API2` hub → platform-web. (`/trace-websocket`)
- **Config/cache:** backoffice writes → `On*Change` → `CacheSync.Service` evicts per-pod `IMemCache`.

## Output
A source→…→consumer trace across stores/messages/repos, file:line cited, with ownership (platform-final
vs sibling) explicit and `[Likely]/[Unknown]` flags. Ref [docs/StateFlow.md](../../docs/StateFlow.md),
[docs/DependencyGraph.md](../../docs/DependencyGraph.md), [docs/RequestLifecycle.md](../../docs/RequestLifecycle.md).
