# CLAUDE.md — platform-final

## FIRST INSTRUCTION (every prompt, before anything else)
On **every** prompt, before acting:
1. **Consult `CLAUDE.md` and [`AGENTS.md`](AGENTS.md) first.**
2. **Follow the mandatory development workflow** (see [§ Permanent development workflow](#permanent-development-workflow-mandatory-every-task)): understand → search the repo → consult `docs/` → trace end-to-end **in the actual code and cite `file:line`** (never answer from memory) → identify reuse and risks → explain the plan and **wait for approval before any architectural change** → make the **smallest possible change** → validate → update docs.
3. **Label every claim `verified` / `assumption` / `unknown` — never guess.**
4. **Production is READ-ONLY:** do not run queries, deploy, change schema, or invalidate caches unless **explicitly instructed**.

> A `UserPromptSubmit` hook in [`.claude/settings.json`](.claude/settings.json) re-injects these rules on every prompt so they are never skipped.

Guidance for Claude Code working in **platform-final**, the core .NET sportsbook backend of the
Song88 platform. Derived from the repository (see [`docs/`](docs/README.md), a 10-phase analysis).
This file is the quick operating manual; `docs/` is the authoritative deep reference.

## What this repo is
- **42-project .NET solution** ([`Platform/Platform.sln`](Platform/Platform.sln)) — a
  **microservice, message-driven sportsbook backend**. Headless: **no UI here** (the UI is the
  sibling `platform-web` Blazor app).
- **38 projects on `net10.0`; `Platform.Shared` + 3 Mongo services (`Sportsbook.API`, `QTech.API`, `CashOutStake.Servicev2`) on `net8.0`** (MongoDB.Driver 2.11.6 LINQ breaks on .NET 10; upgraded from `netcoreapp3.1`/`netstandard2.1`/`net7.0`/`net8.0`
  under GM-744); EF Core 8.0.30 (LTS - EF 10 needs net10, Shared is net8), Docker: sdk:10.0 build, aspnet:10.0 runtime (aspnet:8.0 for Sportsbook.API/CashOutStake.Servicev2).
- Deployed as Docker images → AWS ECR → EKS via the sibling `argocd` repo.

## Build / test

```
dotnet build Platform/Platform.sln
dotnet test Platform/Platform.sln
```

Four test projects: `Platform.Shared.Test`, `Platform.Sportsbook.API.Test`,
`Platform.Operator.API.Test`, `Platform.BetInfo.Service.Test`. CI runs them on pull requests only.
Restore needs `NUGET_PASSWORD` for the private feed.

## The 30-second architecture
- **One public gateway** `Platform.API` (REST) → fans out over HTTP to `Betting.API`,
  `Sportsbook.API`, `Wallet.API`, `Operator.API`, QTech; and over RabbitMQ via `RabbitMQ.API`.
- **Async spine = RabbitMQ topic exchange `e.central`**; routing keys namespaced `r.bet.*`/`r.wallet.*`
  (platform), `r.oe.*` (odds engines). ~18 background `Worker : BackgroundService` services.
- **Three data stores, three owners** (the most important fact — see [docs/DomainModel.md](docs/DomainModel.md)):
  - **MongoDB `Sportsbook`** = live catalogue (Fixture/Market/Selection/odds) — **owned & written by
    `odds-engine-2`**; platform-final **reads only** (never writes Mongo).
  - **SQL `SportsbookContext`** = curation/config (also written directly by `backoffice-final`).
  - **SQL `BettingContext`** + others = bets/cashout/audit (platform-final owns).
- **Real-time:** SignalR hub `Websocket.API2` on the `platform` route, **fed by `OE.Websocket.Service`**
  (odds-engine-2), consumed by platform-web. See [docs/WebSocket.md](docs/WebSocket.md).

## Critical rules (read before changing anything)
1. **`Platform.Shared` is the shared kernel with platform-wide blast radius.** A change there rebuilds
   **every** image (CI builds all 26 matrix images on every push to main and every PR — there is no affected-files
   detection). Treat its MessageModels, Data entities and enums as **public API**. (R-1)
2. **Cross-repo contracts are duplicated by convention, with no shared package and no contract test.**
   Renaming a message field, routing key, Mongo/SQL column, or SignalR method name is an **up-to-4-repo
   silent break** (platform-final ↔ odds-engine-2 ↔ platform-web, + oddsfeed/backoffice). (R-2,
   [docs/Contracts.md](docs/Contracts.md))
3. **Auth is custom, not framework-based.** No `AddAuthentication`/JWT/`[Authorize]`; `UseAuthorization()`
   is inert. Access control = API-key **ActionFilters** applied per-controller via `[ServiceFilter]`.
   **A new controller without a filter attribute is completely open** (no global fallback). (R-6,
   [docs/Authentication.md](docs/Authentication.md))
4. **No global exception handler.** Errors are shaped per-controller try/catch → `BaseResponseDTO`
   with a business `StatusCode`; unguarded actions leak a bare 500. Don't return `ex.StackTrace`. (TD-04)
5. **platform-final never writes MongoDB.** If you think you need to, you're probably in the wrong repo
   (odds-engine-2 owns the catalogue). ([docs/MongoDb.md](docs/MongoDb.md))
6. **Money is stored-proc only** (`Wallet.API`, `decimal(26,12)`); **parameterize all `FromSqlRaw`**
   (AgentReports interpolates — TD-01b/AP-2, do not copy it).
7. **Secrets:** never commit new secrets; never print secret values. Committed settings-file
   secrets are dev/staging fallbacks (prod = AWS SSM). (TD-01)
8. **Caches are per-pod** (`MemoryCache.Default`); invalidation is event-driven via `CacheSync.Service`
   — use the prefix convention so eviction works. ([docs/CacheInvalidation.md](docs/CacheInvalidation.md))

## How this team writes code (match it — don't introduce new patterns)
- **Repository pattern**: `IXxxRepository`+`XxxRepository` in each project's Repository folder, async, return
  `Task<BaseResponseDTO<T>>` (business `StatusCode`). Controllers stay thin (open `Activity`, call repo,
  `switch` on `StatusCode`). Business logic lives **in repositories**, not services.
- **DI**: built-in container; group registrations in `IServiceCollection` extension methods
  (`AddBettingProviders`/`AddProviders`); config via `IConfiguration["A:B"]` (no `IOptions<T>`).
- **Providers**: add via `IBetProvider` + `IBetProviderFactory.Resolve(ChannelType)` — no call-site `if`s.
- **Mapping**: manual via LINQ `.Select` (no AutoMapper). **Validation**: ActionFilters + inline +
  `StatusCode` (no FluentValidation). **Feature flags**: `Custom:*` config keys (no flag framework).
- **Messaging**: `On*` POCO contracts; consume via `IMessageBroker.MessageReceived` + `switch(MessageType)`;
  manual ack for loss-sensitive consumers; carry `CorrelationId`.
- **Logging/telemetry**: Serilog console-JSON + OpenTelemetry `Instrumentation`; propagate trace context
  through RabbitMQ headers.
- Full detail: [docs/ImplementationPatterns.md](docs/ImplementationPatterns.md),
  [docs/CodingStandards.md](docs/CodingStandards.md), [docs/BestPractices.md](docs/BestPractices.md),
  [docs/AntiPatterns.md](docs/AntiPatterns.md).

## Where things live (hotspots)
| Need | File |
|------|------|
| Public API surface | `Platform/Platform.API/Controllers/IntegrationController.cs` and its InternalController |
| Gateway logic | `Platform/Platform.API/Repository/PlatformRepository.cs` |
| Sportsbook reads (Mongo) | `Platform/Platform.Sportsbook.API/Repository/SportsbookRepository.cs` (#1 churn) |
| Bet placement/config | `Platform/Platform.Betting.API/Repository/BettingRepository.cs` |
| Settlement | `Platform/Platform.Settlement.Service` · Cashout: `Platform/Platform.CashOut.Service`, `Platform/Platform.CashOutStake.Servicev2` |
| Messaging | `Platform/Platform.Shared/Utility/MessageBroker.cs` · Cache: `Platform/Platform.Shared/Utility/MemCache.cs` + `Platform/Platform.CacheSync.Service` |
| Entities / contracts / enums | `Platform/Platform.Shared` — its Data, MessageModels and Enums folders |
| Providers | `Platform/Platform.Shared/Providers` (MTS, GTS, BetGenius) |
See [docs/knowledge/FrequentlyModifiedAreas.md](docs/knowledge/FrequentlyModifiedAreas.md).

## Permanent development workflow (MANDATORY — every task)
Follow these steps in order on every task (Phase 12 standing rule):
1. **Understand the requirement.**
2. **Search the repo** for existing implementations (the find-service, find-repository and find-pattern commands).
3. **Consult the docs** ([docs/README.md](docs/README.md) index).
4. **Trace the feature end-to-end** (the trace commands) — don't stop at boundaries.
5. **Identify reusable components** ([docs/knowledge/CommonServices.md](docs/knowledge/CommonServices.md)).
6. **Explain the proposed solution** before building.
7. **Identify risks and assumptions** (the impact-analysis command, [docs/ArchitectureRisks.md](docs/ArchitectureRisks.md)).
8. **Ask clarifying questions** when the requirement/evidence is unclear.
9. **Wait for approval before architectural changes** (anything touching `Platform.Shared`, a cross-repo
   contract, a new pattern, or a new project/service).
10. **Implement the smallest possible change.**
11. **Validate** compilation (.NET 10 / `net10.0`) and behavior (run the relevant checklist).
12. **Update documentation** if architectural knowledge changed (the update-docs command; keep CLAUDE.md/AGENTS.md/
    commands/skills/playbooks in sync). **Also update `README.md` in the same change whenever the change
    affects anything the README documents** (projects/frameworks, prerequisites, build/run/test
    instructions, configuration, endpoints) — the README must never describe a repo state that no
    longer exists.
13. **Summarize** changes, risks, and validation results.

## Engineering principles (MANDATORY)
- **The repository is the source of truth.** Verify in code.
- **Preserve existing architecture; never introduce a new pattern without approval** (no AutoMapper/
  FluentValidation/MediatR/`IOptions<T>`/etc. where an established pattern exists — see
  [docs/ImplementationPatterns.md](docs/ImplementationPatterns.md)).
- **Reuse the shared kernel — never duplicate a definition per-service.** Entities, `DbContext`s,
  message/DTO contracts, and enums belong in the shared library (`Platform.Shared` here; `BG.Shared` /
  `OE.Shared` in siblings — e.g. `../bet-genius/BG.Shared/Data/Trading/TradingContext.cs`). **Before creating any of
  these, search the shared project first and reuse it.** If it's genuinely missing, **add it to the
  shared library** (additive) — do not copy a local per-service version, even if an existing service
  already has its own copy (that copy is the bug to fix, not a pattern to follow).
- **Never guess.** Distinguish **verified facts** vs **assumptions** vs **unknowns**; tag them and
  record unknowns ([docs/Unknowns.md](docs/Unknowns.md)). Treat Likely and Unknown tags as verify-before-rely.
- **A new project/service ships with tests — don't forget them.** When creating a new project or
  service, add a matching test project (xUnit, mirroring `Platform.Shared.Test` / sibling conventions:
  `xunit` + `Microsoft.NET.Test.Sdk`, Arrange/Act/Assert, no mocking framework unless one is already
  used). Extract pure logic (key building, SQL/parameter shaping, parsing) out of EF/RabbitMQ glue so
  it is unit-testable, and cover the core/safety-critical paths. Run the tests and report results.
- **Production is READ-ONLY by default.** Never perform write operations, deployments, schema changes,
  cache invalidations, or administrative actions unless **explicitly instructed**
  ([.claude/playbooks/production-readonly.md](.claude/playbooks/production-readonly.md)).
- **Every SQL script must be idempotent** (data fixes, seeds, DDL, hand-run/flyway migrations): guard
  INSERTs with `NOT EXISTS`/`MERGE`, schema changes with `IF NOT EXISTS`/`OBJECT_ID` checks, so
  re-running is a no-op with the same end state.
- **External repos: identify dependencies, don't invent behavior.** Siblings (`../odds-engine-2`,
  `../Auth`, `../backoffice-final`, `../bet-genius`, `../platform-web`, `../argocd`,
  `../infrastructure`) are read-only references; **never modify files outside this repo**.
- **Keep docs + CLAUDE.md/AGENTS.md/commands/skills/playbooks synchronized** with architectural changes.

## Scope & build reminders (repo-specific)
- Subject is **platform-final** (backend); UI work belongs to **platform-web**.
- Code targets **.NET 10** (`net10.0`), except `Platform.Shared` + the 3 Mongo services (`Sportsbook.API`, `QTech.API`, `CashOutStake.Servicev2`) on **`net8.0`** (MongoDB.Driver 2.11.6 breaks on net10 — ADR-14). Build SDK is 10.0; runtime images 10.0 (8.0 for the two net8 services with Dockerfiles).
- **Confirm the live variant** before editing duplicated code (`*.Service` vs `*.Servicev2`,
  `WebSocket.API` vs `Websocket.API2`).

## Assets in this repo
- **`docs/`** — the 10-phase knowledge base (start at [docs/README.md](docs/README.md)).
- **`.claude/commands/`** — trace, impact-analysis, find and review-feature commands.
- **`.claude/skills/`**, **`.claude/playbooks/`**, **`.claude/checklists/`**, **`.claude/templates/`**,
  **`.claude/prompts/`** — task guidance, derived from this repo.

## See also
[AGENTS.md](AGENTS.md) · [docs/Architecture.md](docs/Architecture.md) ·
[docs/ArchitectureRisks.md](docs/ArchitectureRisks.md) · [docs/KnowledgeValidation.md](docs/KnowledgeValidation.md)
