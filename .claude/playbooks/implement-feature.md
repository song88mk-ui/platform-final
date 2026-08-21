# Playbook: Implement a Feature

End-to-end procedure for a backend feature in platform-final.

1. **Scope & locate.** Confirm it's backend (UI → platform-web). Find the owning project/area
   ([docs/RepositoryMap.md](../../docs/RepositoryMap.md)).
2. **Trace the nearest analog** (`/trace-feature`/`/trace-bet`) — copy its structure.
3. **Design to the patterns** (Feature Development skill): repository + `BaseResponseDTO`/`StatusCode`,
   thin controller + `[ServiceFilter]`, DI extension method, manual mapping, `IConfiguration` indexer.
4. **Data path:** EF (`AsNoTracking`/parameterized proc), Mongo read-only projection, or HttpClient.
   Money → Wallet proc. No Mongo writes.
5. **Async (if needed):** add an `On*` contract in `Platform.Shared/MessageModels`, publish via
   `IMessageBroker`/`RabbitMQ.API`, consume in a `Worker` (right ack + `CorrelationId`).
6. **Cross-cutting:** OTel `Activity` + Serilog; read-through `IMemCache` + invalidation if cached.
7. **Impact:** `/impact-analysis` — verify `Platform.Shared`/contract changes against siblings (R-2).
8. **Verify:** build (3.1-compatible); `/review-feature`; [.claude/checklists/before-pull-request.md](../checklists/before-pull-request.md).
9. **Docs:** `/update-docs`.
10. **Commit/PR only if asked.**
