# Checklist: Architecture Review

- [ ] **Ownership respected:** no Mongo writes; money via Wallet procs; curation via the right path;
      reads of the right store. ([docs/DomainModel.md](../../docs/DomainModel.md))
- [ ] **Coupling minimized:** avoids unnecessary `Platform.Shared` change (R-1) and cross-repo
      contract change (R-2). If unavoidable, sibling alignment verified.
- [ ] **Communication style consistent:** sync (gateway HTTP) vs async (RabbitMQ `On*`) matches
      existing flows; no new runtime message cycle.
- [ ] **Auth/observability/caching** cross-cutting concerns wired (filter, OTel/Serilog, invalidation).
- [ ] **No reliance on absent infra:** no global exception handler, no distributed cache, no DI options
      binding to lean on.
- [ ] **Risk mapped** to [docs/ArchitectureRisks.md](../../docs/ArchitectureRisks.md) R-1…R-16 with L×I.
- [ ] **No new pattern** where an established one exists; no new framework dependency.
- [ ] **Single points of failure** considered (central RabbitMQ R-8, `Platform.Shared` R-1, gateway).
