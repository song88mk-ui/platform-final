# Prompt: Onboard an Engineer to platform-final

Use this to ramp someone (or yourself) on the codebase fast.

## Suggested reading order
1. [CLAUDE.md](../../CLAUDE.md) — operating manual + critical rules.
2. [docs/Architecture.md](../../docs/Architecture.md) — the big picture + bet lifecycle.
3. [docs/DomainModel.md](../../docs/DomainModel.md) — the three-store/three-owner split (the key idea).
4. [docs/RepositoryMap.md](../../docs/RepositoryMap.md) + [docs/ProjectOverview.md](../../docs/ProjectOverview.md) — find your service.
5. [docs/ImplementationPatterns.md](../../docs/ImplementationPatterns.md) + [docs/BestPractices.md](../../docs/BestPractices.md) — the house style.
6. [docs/CrossRepositoryDependencies.md](../../docs/CrossRepositoryDependencies.md) — how it connects to siblings.
7. [docs/ArchitectureRisks.md](../../docs/ArchitectureRisks.md) + [docs/AntiPatterns.md](../../docs/AntiPatterns.md) — what to be careful about.
8. [docs/knowledge/](../../docs/knowledge/) — quick-reference cards (services/DTOs/models/config/endpoints/WS/utils/troubleshooting/hotspots).

## The five things to internalize
1. It's a **microservice backend** (no UI — that's platform-web); RabbitMQ `e.central` is the spine.
2. **`Platform.Shared` change = rebuild everything**; **contracts are convention-only across repos**.
3. **Catalogue (Mongo) is read-only here** — odds-engine-2 owns it.
4. **Repository + `BaseResponseDTO`/`StatusCode`** is the universal shape; auth is **per-controller ActionFilters**.
5. **Errors return HTTP 400 with a body** — read the `StatusCode`, not the HTTP code.

## First task
Pick a feature and run `/trace-feature`; then `/find-pattern` before writing anything.
