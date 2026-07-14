# Knowledge Validation

> **Phase 10 — Repository Validation.** Validates the knowledge base built in Phases 1–9 **before**
> any Claude assets are generated. States what is **[Verified] / [Likely] / [Unknown]**, the
> **assumptions** made, evidence quality, and remaining gaps. **No refactoring is suggested** — only
> *investigation*. Companions: [TechnicalDebt.md](TechnicalDebt.md), [Unknowns.md](Unknowns.md),
> [ArchitectureRisks.md](ArchitectureRisks.md).

## Validation method
- **Source of truth:** platform-final source (read directly across phases) + git history.
- **Sibling claims:** marked **[Verified-sibling]** when read in the sibling repo; never inferred.
- **Cross-checks performed this phase:** project-reference graph (cycle check), dead-code markers,
  contract-sharing, secrets-injection, Mongo write-paths, stored-proc usage, route catalogue vs source.

## Confidence by topic (overall knowledge-base health)
| Topic | Confidence | Basis |
|-------|-----------|-------|
| Solution structure / 39 projects / frameworks | **High [Verified]** | every `.csproj` read |
| Compile-time dependency graph (acyclic) | **High [Verified]** | this phase: 37→Shared + 1 (Client→WebSocket.API) |
| Startup / DI / middleware / hosting | **High [Verified]** | `Program`/`Startup` + filters read |
| Auth model (API-key filters, no JWT scheme) | **High [Verified]** | grep + filters read |
| Persistence (SQL contexts, Mongo read-only, procs, caches) | **High [Verified]** | `OnModelCreating` + repos + grep |
| Domain model & enums | **High [Verified]** | enums + entities read |
| API gateway route catalogue | **High [Verified]** | `IntegrationController`/`InternalController` line-cited |
| WebSocket + odds-engine integration | **High** | **[Verified]** (hub) + **[Verified-sibling]** (OE2 feeder) |
| Cross-repo contracts (duplicated-by-convention) | **High [Verified]** | grep (no shared pkg) + sibling reads |
| UI tier (platform-web Blazor) | **Medium-High [PW]** | platform-web docs + spot source checks (not full re-read) |
| Exact entity field semantics (189 DTOs / 88 data files) | **Medium [Likely]** | categorised, not all field-by-field traced |
| Runtime message topology (key↔queue↔consumer, ordering) | **Medium [Likely]** | message types verified; full binding not |
| Deploy/secrets/infra | **Medium-High [Verified-sibling]** | argocd/infrastructure read |
| Stored-proc bodies, Mongo indexes, BetConfig JSON schemas | **Low [Unknown]** | live in DB / sibling, not in this repo |

## Assumptions made (explicit) `[validated where noted]`
| # | Assumption | Status |
|---|-----------|--------|
| A-1 | "Platform-Web" in the phase prompts is templated boilerplate; the analysis target is **platform-final** with docs in `platform-final/docs`. | **Confirmed by user** (Phase 2 AskUserQuestion). |
| A-2 | The platform's **UI tier is platform-web** (platform-final is headless). | **[Verified]** — no UI code in platform-final. |
| A-3 | platform-web's own `docs/` are an accurate, verified source for the UI/consumer half. | **[Likely]** — spot-checked against source; not exhaustively re-derived. |
| A-4 | The 8 SQL DbContexts map to 8 separate databases named like the context. | **[Verified]** for connection keys; DB-name identity **[Likely]**. |
| A-5 | The committed `appsettings.json` secrets are dev/staging and overridden by SSM in prod. | **[Verified-sibling]** (argocd External Secrets). |
| A-6 | Settlement/parlay/void math behaves per standard sportsbook semantics. | **[Likely]** — fields verified, math in `Settlement.Service` not line-traced (U-21). |
| A-7 | odds-engine-2 (not oddsfeed) is the primary catalogue/odds owner for this brand. | **[Likely]** — both reach MTS / share the bus (U-26). |
| A-8 | The `*.Service.Repository`/`*.Telemetry` are folders, not projects. | **[Verified]** this phase (not in ProjectReference set). |

> Any later Claude asset generation must respect A-1/A-2 (subject = platform-final; UI = platform-web).

## What is well-evidenced (safe to build on) `[Verified]`
Repository+`BaseResponseDTO` pattern; `StatusCode` enum; API-key ActionFilter auth (no JWT);
`Platform.Shared` as the shared kernel + blast radius; MongoDB read-only (odds-engine-2 writes);
`IMemCache` per-pod + `CacheSync` invalidation; RabbitMQ `e.central` + routing-key namespaces;
WebSocket hub = `Websocket.API2` fed by `OE.Websocket.Service`; duplicated-by-convention contracts;
SSM secret injection; the four data-access flavors; the gateway route catalogue.

## Validated negatives (things confirmed ABSENT — don't assume they exist) `[Verified]`
No EF migrations · no `IOptions<T>` app-config binding · no AutoMapper/Mapster · no FluentValidation
· no MediatR/CQRS · no GoF Builder · no ASP.NET authentication/authorization scheme · no distributed
cache · no global exception middleware · no shared cross-repo contract package · no localization ·
platform-final never writes MongoDB.

## Known gaps in the knowledge base (missing documentation) `[Unknown]`
1. Per-endpoint request/response **field-level** contracts beyond the most-used DTOs.
2. Full RabbitMQ **routing-key ↔ queue ↔ consumer** binding table (U-01).
3. **Stored-proc bodies**, **Mongo index definitions** (U-28), **BetConfig JSON schemas** (U-23).
4. Settlement **math** (void/dead-heat/parlay) — U-21.
5. Deploy path for **non-CI projects** (U-06) and the **`Maintenance` Mongo writer** (U-24).
6. Three untraced workers (`Cancellation`, `ESportOperator`, `Operator.Cashout`) — U-04.
7. UI tier verified mostly via platform-web docs, not a full independent source read (A-3).
All tracked in [Unknowns.md](Unknowns.md) with suggested investigation.

## Internal consistency check `[Verified]`
- Cross-references between the 67 docs resolve; enum values, route names, and routing keys are stated
  consistently across phases (e.g. `StatusCode`, `ChannelType`, `r.oe.sb.engine.*`).
- One earlier ambiguity (Channel vs ChannelType label) was caught and corrected in Phase 3.
- One earlier [Likely] (companion sub-projects) is now **[Verified]** as folders (A-8).

## Readiness statement
The knowledge base is **sufficient and well-evidenced for backend work on platform-final** (patterns,
contracts, persistence, integration, hotspots). Treat **[Likely]/[Unknown]** items as
**verify-before-rely**, especially settlement math, exact field contracts, and anything owned by a
sibling repo (catalogue schema, odds production, deploy). The validated negatives and assumptions
above should bound any generated Claude assets.
