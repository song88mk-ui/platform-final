# AGENTS.md — platform-final

Cross-tool agent guidance for **platform-final** (the .NET sportsbook backend). The full operating
manual is [CLAUDE.md](CLAUDE.md); the authoritative reference is [`docs/`](docs/README.md). This file
is the agent-oriented summary — read [CLAUDE.md](CLAUDE.md) first.

## Mission boundaries
- **Subject = platform-final** (backend). **UI work belongs to the sibling `platform-web`** (Blazor) —
  platform-final has no UI. (User-confirmed assumption A-1/A-2; [docs/KnowledgeValidation.md](docs/KnowledgeValidation.md).)
- **Only modify files inside this repo.** Siblings (`odds-engine-2`, `Auth`, `backoffice-final`,
  `bet-genius`, `oddsfeed`, `argocd`, `infrastructure`) are **read-only** references for integration.
- `/Reference` = pre-AI baseline snapshot — never modify; use only to compare/detect regressions.
- Prefer `odds-engine-2` over the obsolete `oddsengine`.

## Hard constraints (do not violate)
1. **`Platform.Shared` change = platform-wide rebuild + cross-service behavior change.** Treat its
   message models, entities, and enums as public API. (R-1)
2. **Cross-repo contracts are convention-only** (no shared package, no contract test). A field/key/
   SignalR-method rename silently breaks up to 4 repos — verify alignment with odds-engine-2 &
   platform-web before changing. (R-2, [docs/Contracts.md](docs/Contracts.md))
3. **Code targets .NET 10 (`net10.0`)**, except Platform.Shared + the 3 Mongo services on **`net8.0`** (MongoDB.Driver 2.11.6 net10 incompatibility, ADR-14). Build SDK 10.0 (GM-744).
4. **Never write MongoDB** from platform-final (odds-engine-2 owns the catalogue).
5. **Never commit or print secrets, in a log line or anywhere else.** Don't reproduce values from
   `appsettings.json`. GM-880 found a private API key printed in three `_logger.LogError` calls in
   `Platform.Operator.Servicev2/Repository/BettingRepository.cs`; mask with
   `Platform.Shared.Extensions.StringExtensions.Mask()` if a secret must appear in a log line. See
   [CLAUDE.md](CLAUDE.md) rule 7 and [docs/TechnicalDebt.md](docs/TechnicalDebt.md) TD-19.
6. **Every new controller needs a `[ServiceFilter(typeof(<Filter>))]`** — there is no global auth fallback.
7. **Parameterize SQL** (`FromSqlRaw("proc {0}", arg)` / `SqlParameter`); never string-interpolate (AP-2).
8. **An optional argument defaulting to `null` still has to be supplied at every call site that needs
   it.** GM-879: a `CancelBet(..., isOGClient: true, ...)` call left the optional `OGParameters`
   argument at its default, so the OG cancellation path dereferenced a null `OGRequestDTO` and
   crashed before OG was notified. See [CLAUDE.md](CLAUDE.md) rule 10 and
   [docs/TechnicalDebt.md](docs/TechnicalDebt.md) TD-20.
9. **A `catch` block that makes another network call must guard it.** GM-892: the OG cancellation
   `catch` block asked OG for the transaction status and wallet balance unguarded, so when OG was
   unreachable the exception escaped before the `OperatorSettlementRetry` row was written. Guard
   the second call and treat its failure as "status unknown". See [CLAUDE.md](CLAUDE.md) rule 11
   and [docs/TechnicalDebt.md](docs/TechnicalDebt.md) TD-20.
10. **`DeleteCache(key)` is an exact-key removal.** `DeleteCache("leo")` evicts nothing when the entry
    is cached as `leo-{clientId}`; use `DeleteCacheByPrefix` or the full key (TD-22, found in
    GM-851). The Early/Today/Live lists order leagues from `ClientOddstableLeagueOrder`
    (`oleo-{clientId}`), purged per client by `OnOddstableLeagueSortOrderChange`. The same order
    is returned by `GetClientFixtureSortOrder`, which the odds-engine-2 websocket service stamps on
    pushed fixtures; CacheSync relays the change to it after the purge so it reloads that client
    (not on `OnSportSortOrderChange`). See [CLAUDE.md](CLAUDE.md) rule 8 and
    [docs/CacheInvalidation.md](docs/CacheInvalidation.md).

## Mandatory workflow (Phase 12 — every task)
Understand → **search repo** (`/find-*`) → **consult docs** → **trace end-to-end** (`/trace-*`) →
identify reusable components → **explain the proposed solution** → identify risks & assumptions
(`/impact-analysis`) → **ask clarifying questions when unclear** → **wait for approval before
architectural changes** → implement the **smallest possible change** → **validate** (build on .NET 10
+ behavior + checklist) → **update docs/CLAUDE.md/AGENTS.md/commands/skills** if architecture changed →
**summarize changes, risks, validation**. Full text: [CLAUDE.md](CLAUDE.md) §Permanent development workflow.

## Mandatory principles
- **Repo = source of truth**; preserve existing architecture; **no new pattern without approval**.
- **Never guess** — distinguish verified / assumption / unknown; record unknowns.
- **Production is READ-ONLY by default** — no writes/deploys/schema/cache-invalidation/admin actions
  unless explicitly instructed.
- **SQL scripts must be idempotent** — guard INSERTs with `NOT EXISTS`/`MERGE` and schema changes with
  `IF NOT EXISTS`/`OBJECT_ID` checks so re-running is a no-op.
- **External repos:** identify dependencies, don't invent behavior; never modify outside this repo.
- **Keep docs + Claude assets synchronized** with architectural changes.
See [CLAUDE.md](CLAUDE.md) §Engineering principles.

## Investigation defaults
- **Fetch latest first.** `git fetch origin` and compare against `origin/main` for this repo and any
  sibling repo, before citing file contents or repo/deployment state as fact — a local checkout can
  be stale by hundreds of commits.
- **Read-only first** for production investigations ([.claude/playbooks/production-readonly.md](.claude/playbooks/production-readonly.md)).
- **Errors:** check the body `StatusCode` (not HTTP code); see [docs/knowledge/Troubleshooting.md](docs/knowledge/Troubleshooting.md).
- **Use sub-agents** for breadth (sweeping many files); keep conclusions, not file dumps.

## Tooling notes
- Build needs the private GitHub NuGet feed (`NUGET_PASSWORD`). Tests: `Platform.Shared.Test`, `Platform.Sportsbook.API.Test`,
  `Platform.Operator.API.Test`, `Platform.BetInfo.Service.Test`, `Platform.CacheSync.Service.Test`
  and `Platform.Operator.Servicev2.Test`.
- Commit/PR only when asked; don't bypass hooks.

## Asset index
`.claude/commands` (trace/find/impact/review/security/performance) · `.claude/skills` ·
`.claude/playbooks` · `.claude/checklists` · `.claude/templates` · `.claude/prompts`. See [CLAUDE.md](CLAUDE.md).
