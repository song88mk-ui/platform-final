# Template: New RabbitMQ Message Contract

⚠️ **Cross-repo blast radius (R-2):** message contracts are duplicated per repo and aligned only by
field-name convention — no shared package, no contract test. Adding/changing one may require matching
changes in odds-engine-2 / platform-web / oddsfeed / backoffice. Run `/impact-analysis` first.

## 1. Contract POCO (`Platform.Shared/MessageModels/On<Event>.cs`)
- Name `On<Event>`. Keep fields stable; match the field names other repos expect (abbreviated style
  for catalogue/odds messages). Document it in [docs/Contracts.md](../../docs/Contracts.md).

## 2. Publish
- From a **service:** `_messageBroker.Publish(payload, "On<Event>", "r.<domain>.<event>[.<client>]", correlationId)`.
- From an **API:** publish over HTTP via `IRabbitMQRepository` → `RabbitMQ.API`.
- Routing-key namespace: `r.bet.*`/`r.wallet.*` for platform-produced events; `.s88` brand suffix.

## 3. Consume (in a `Worker : BackgroundService`)
- `_messageBroker.MessageReceived += handler;` then `switch (e.BasicProperties.Type /* MessageType */)`.
- Choose ack mode: **manual** (`ConnectWithManualAck`) for loss-sensitive; carry `CorrelationId`.
- Wrap handler work in an OTel `Activity` (extract trace context from headers).

## 4. Register the routing key/queue in config (`MessageBroker:RoutingKey`/`Queue`) + argocd env.
## 5. `/update-docs` — `Contracts.md` routing-key registry + `DependencyGraph.md` §3.
