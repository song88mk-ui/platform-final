# /review-feature

Review a feature/change for correctness + conformance to platform-final conventions.

## Checklist
1. **Pattern conformance:** repository + `BaseResponseDTO`/`StatusCode`; thin controller; manual
   mapping; ActionFilter present on any new controller (no global auth fallback — R-6).
2. **Blast radius:** ran `/impact-analysis`? `Platform.Shared`/contract changes verified against
   siblings? (R-1/R-2)
3. **Persistence:** `AsNoTracking` reads; parameterized SQL (no interpolated `FromSqlRaw` — AP-2);
   `TransactionScope` for multi-write; audit-append preserved; money via proc.
4. **Caching:** read-through + prefix; invalidation event wired into `CacheSync.Service` if config/curation.
5. **Messaging:** correct ack mode; `CorrelationId`; routing key namespaced correctly.
6. **Observability:** Serilog + OTel `Activity`; trace context propagated through MQ.
7. **Safety:** no leaked stack traces (TD-04); no new committed secrets (TD-01); 3.1-compatible.
8. Run [.claude/checklists/before-pull-request.md](../checklists/before-pull-request.md).

Output findings with file:line + a conformance verdict.
