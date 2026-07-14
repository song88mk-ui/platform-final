# Skill: Code Review

Review a diff for correctness + platform-final conformance.

## What to check (in priority order)
1. **Blast radius:** `Platform.Shared`/contract changes → cross-repo verification done? (R-1/R-2)
2. **Security:** no new committed secrets (TD-01); parameterized SQL (TD-01b); controller has a
   `[ServiceFilter]` (R-6); no leaked stack traces (TD-04); guarded header parsing (AP-7).
3. **Pattern conformance:** repository + `BaseResponseDTO`/`StatusCode`; thin controller; manual
   `.Select` mapping; `IConfiguration` indexer (no `IOptions<T>`); no new framework (AutoMapper/
   FluentValidation/MediatR) — [docs/AntiPatterns.md](../../docs/AntiPatterns.md).
4. **Persistence:** `AsNoTracking` reads; `TransactionScope` multi-write; audit-append; money via proc;
   no Mongo writes.
5. **Messaging/observability:** correct ack + `CorrelationId`; OTel `Activity` + Serilog; trace through MQ.
6. **Compatibility:** stays .NET Core 3.1-compatible.
7. **Live variant:** editing the deployed project, not a superseded duplicate (TD-07).

## Output
Findings with file:line + severity + a conformance verdict. Cross-ref
[docs/CodingStandards.md](../../docs/CodingStandards.md), [docs/BestPractices.md](../../docs/BestPractices.md).
