# Prompt: Explain the Architecture

"Explain how platform-final is structured and how a [feature] works end-to-end, citing files."

## Frame the answer with
- **Style:** microservice, message-driven (.NET Core 3.1); one gateway `Platform.API`; ~18 RabbitMQ
  workers; SignalR edge (`Websocket.API2`).
- **Spine:** RabbitMQ topic exchange `e.central`; routing keys `r.bet.*`/`r.wallet.*` (platform),
  `r.oe.*` (odds engines).
- **Three stores/owners:** Mongo `Sportsbook` (catalogue, **odds-engine-2 owns/writes**, platform reads);
  SQL `SportsbookContext` (curation, also written by backoffice); SQL `BettingContext`+ (bets, platform owns).
- **Shared kernel:** `Platform.Shared` (entities, `On*` contracts, enums, MessageBroker/MemCache,
  providers) — high blast radius.
- **Patterns:** repository + `BaseResponseDTO`/`StatusCode`; ActionFilter auth (no JWT scheme).

## Then trace the specific feature
Use `/trace-feature` and the [docs/SequenceDiagrams.md](../../docs/SequenceDiagrams.md). Always cite
file:line; mark unverified hops `[Likely]/[Unknown]`. Keep platform-final vs sibling ownership explicit.
