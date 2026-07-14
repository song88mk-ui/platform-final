# /find-repository

Find the repository class + method that owns a data operation (the unit where logic lives).

## Steps
1. Repositories live in each project's `Repository/` as `IXxxRepository`+`XxxRepository`, returning
   `Task<BaseResponseDTO<T>>`. Patterns: [docs/RepositoryPatterns.md](../../docs/RepositoryPatterns.md).
2. By store: SQL (EF LINQ/proc) — `BettingRepository`/`PlatformRepository`; Mongo (read-only) —
   `SportsbookRepository`/`CashOutStake`; HTTP — provider/integration repos; wallet — stored procs.
3. Hotspots: `SportsbookRepository.cs` (724 changes), `BettingRepository.cs` (in 5+ projects — confirm
   which one). [docs/knowledge/FrequentlyModifiedAreas.md](../../docs/knowledge/FrequentlyModifiedAreas.md).

## Note
`BettingRepository` exists separately in Betting.API/Settlement/Placement/CashOut/Operator.Servicev2 —
same name, different bodies. Confirm the project before editing.
