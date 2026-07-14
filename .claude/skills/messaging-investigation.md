# Skill: Messaging / RabbitMQ Investigation

Investigate async message flows.

## Knowledge
- One topic exchange **`e.central`** (`rabbitmq-main-cluster.bet-genius-apps.svc`, TLS 5671,
  prefetch ~1000). Wrapper: `Platform.Shared/Utility/MessageBroker.cs` (`event MessageReceived`,
  `Publish`/`CPublish`/`ExternalPublish`, manual/auto ack, W3C trace headers).
- Contracts: `On*` POCOs in `Platform.Shared/MessageModels` (duplicated per-repo by convention — R-2).
- Routing-key namespaces: `r.bet.*`/`r.wallet.*` (platform), `r.oe.*` (odds engines), `r.bg.*`,
  `r.of.*`; `.s88` = brand suffix. Registry: [docs/Contracts.md](../../docs/Contracts.md).

## Method
1. Map message type → producer/consumer ([docs/DependencyGraph.md](../../docs/DependencyGraph.md) §3).
2. In a worker: read `Worker.ExecuteAsync` → `MessageReceived` → `switch(MessageType)`; note ack mode.
3. Publishers: services publish directly; **APIs publish over HTTP via `RabbitMQ.API`**.
4. Hot queues use 100-thread fan-out (CashOut/Placement/…) — see [docs/Hosting.md](../../docs/Hosting.md).
5. Watch for runtime loops (e.g. `Provider.Service` republishing `OnBetPlacement`).

## Output
Producer → routing key → queue/consumer → handler, with ack semantics and file:line.
