# CLAUDE.md — platform-final

<!-- song88:standing-rules:begin -->
## Standing rules (every prompt, before anything else)

Shared by every song88 repo. The canonical text is `../song88-claude/standing-rules/STANDING-RULES.md`:
edit it there and run `../song88-claude/standing-rules/sync_standing_rules.py`, never edit this block by hand. A `UserPromptSubmit`
hook in `.claude/settings.json` re-injects these rules on every prompt.

1. **Read this repo's CLAUDE.md and AGENTS.md first.**
2. **Fetch latest before investigating.** Run `git fetch origin` and compare with origin/main for this repo and every sibling repo you read, before citing file contents or repo or deployment state as fact. A local checkout can be hundreds of commits behind. When you cannot or should not pull, read files with `git show origin/main:<path>`, and say so explicitly if you did not check.
3. **Trace in the code, never from memory.** Search the repo and its docs, trace the feature end to end, and cite `file:line`.
4. **Label every claim verified, assumption or unknown.** Never guess.
5. **Explain the plan and wait for approval before any code change.** Then make the smallest change that does the job.
6. **Production is read-only.** Do not run queries, deploy, change schema or invalidate caches unless explicitly instructed.
7. **Update docs and README in the same change** whenever the change affects what they describe.
8. **Every SQL script must be idempotent.** Re-running it is a no-op with the same end state.
9. **Do not modify a sibling repo as a side effect of work here.** Siblings are read-only references unless the user asks for a change in that repo.
10. **Never print, log or commit a secret.** Mask it if it must appear in a log line.
11. **A new project or service ships with tests** in the repo's existing test framework.
12. **Commit, push or open a PR only when explicitly asked.** "Implement it" authorizes local edits only.
13. **Report validation with every change:** build, unit tests, integration tests, manual checks and remaining risks. If something could not be validated, say what and why, name the tests to run before deployment, and mark the work Implementation Complete – Validation Pending, never Completed.
14. **If a workflow step does not apply, say so** instead of skipping it silently.
<!-- song88:standing-rules:end -->

Guidance for Claude Code working in **platform-final**, the core .NET sportsbook backend of the
Song88 platform. Derived from the repository (see [`docs/`](docs/README.md), a 10-phase analysis).
This file is the quick operating manual; `docs/` is the authoritative deep reference.

## What this repo is
- **44-project .NET solution** ([`Platform/Platform.sln`](Platform/Platform.sln)) — a
  **microservice, message-driven sportsbook backend**. Headless: **no UI here** (the UI is the
  sibling `platform-web` Blazor app).
- **40 projects on `net10.0`; `Platform.Shared` + 3 Mongo services (`Sportsbook.API`, `QTech.API`, `CashOutStake.Servicev2`) on `net8.0`** (MongoDB.Driver 2.11.6 in `Sportsbook.API`/`QTech.API`, 2.13.1 in `CashOutStake.Servicev2`; its 2.x LINQ breaks on .NET 10; upgraded from `netcoreapp3.1`/`netstandard2.1`/`net7.0`/`net8.0`
  under GM-744); EF Core 8.0.30 (LTS - EF 10 needs net10, Shared is net8), Docker: sdk:10.0 build, aspnet:10.0 runtime (aspnet:8.0 for Sportsbook.API/CashOutStake.Servicev2).
- Deployed as Docker images → AWS ECR → EKS via the sibling `argocd` repo.

## Build / test

```
dotnet build Platform/Platform.sln
dotnet test Platform/Platform.sln
```

Thirteen test projects, one per `Platform/*.Test` folder. Twelve are in `Platform/Platform.sln`;
`Platform.CashOut.Service.Test` is not, so `dotnet test Platform/Platform.sln` skips it.
CI runs them on pull requests only. Restore needs `NUGET_PASSWORD` for the private feed.
Before the tests, the CI job builds `Platform.sln` with `-warnaserror`, so any new compiler warning
fails the PR; only the NuGet vulnerability audit warnings (NU1900-NU1905) stay warnings. Check
locally with `dotnet build Platform/Platform.sln -c Release -warnaserror
-warnnotaserror:NU1900,NU1901,NU1902,NU1903,NU1904,NU1905`.

`Platform.Betting.API.IntegrationTests` (DEVOPS-408) is also outside `Platform.sln`: it needs
Docker and the sibling `infrastructure` clone, which CI does not check out yet. It boots
Betting.API with `WebApplicationFactory<Startup>` against SQL Server in Testcontainers, built
from the dev Flyway migrations, with every `API:*` URL on a loopback stub. Seeded `Bet` rows
must set its ten NOT NULL columns, which EF InMemory never checks. See README "Running tests".

`Platform.API.Test` tests `PlatformRepository` directly: seed `InMemoryPlatformContext`, pass
hand-written fakes for the repositories the method reaches and `null!` for the rest (see
`Platform/Platform.API.Test/Repository/CashoutBetWebPropsGuardrailTests.cs`). Pure logic shared across services still belongs
in `Platform.Shared` and `Platform.Shared.Test`, as `Platform/Platform.Shared/Helpers/TranslationChangeMapper.cs`
does (GM-874). A static class outside `MessageModels` changes neither the `PublicTypeContractTests`
sweep nor `message-contracts.approved.txt`.

Mutation testing uses `dotnet-stryker`. `Platform/Platform.Shared.Test/stryker-config.json` ignores the `Log*`
methods, so it never mutates a log statement. To test a log call, run from the test project folder
with `--mutate "**/Repository/<File>.cs"` and filter the JSON report by line. A `{start..end}` span
on `--mutate` removed every mutant in our runs. No mutator removes an extension call such as
`.Mask()` from a log argument, so guard that with a capturing-logger test, as
`Platform/Platform.Operator.Servicev2.Test/Repository/BettingRepositoryOGKeyLoggingTests.cs` does (GM-880).

## The 30-second architecture
- **One public gateway** `Platform.API` (REST) → fans out over HTTP to `Betting.API`,
  `Sportsbook.API`, the operator endpoint (`IOperatorRepository`), Casino (QTech) and WebProps. It has no
  `Wallet.API` or `RabbitMQ.API` client; it publishes directly with its own publish-only `MessageBroker` (`CPublish`).
- **Async spine = RabbitMQ topic exchange `e.central`**; routing keys namespaced `r.bet.*`/`r.wallet.*`
  (platform), `r.oe.*` (odds engines). ~18 background `Worker : BackgroundService` services.
- **Three data stores, three owners** (the most important fact — see [docs/DomainModel.md](docs/DomainModel.md)):
  - **MongoDB `Sportsbook`** = live catalogue (Fixture/Market/Selection/odds) — **owned & written by
    `odds-engine-2`**; platform-final **reads only** (never writes Mongo).
  - **SQL `SportsbookContext`** = curation/config (also written directly by `backoffice-final`).
  - **SQL `BettingContext`** + others = bets/cashout/audit (platform-final owns).
- **Real-time:** SignalR hub `Websocket.API2` on the `platform` route, **fed by `OE.Websocket.Service`**
  (odds-engine-2), consumed by platform-web. See [docs/WebSocket.md](docs/WebSocket.md).

## Critical rules
1. **`Platform.Shared` is the shared kernel with platform-wide blast radius.** A change there rebuilds
   **every** image (CI builds all 26 matrix images on every push to main and every PR — there is no affected-files
   detection). Treat its MessageModels, Data entities and enums as **public API**. (R-1)
2. **Cross-repo contracts are duplicated by convention, with no shared package and no contract test.**
   Renaming a message field, routing key, Mongo/SQL column, or SignalR method name is an **up-to-4-repo
   silent break** (platform-final ↔ odds-engine-2 ↔ platform-web, + oddsfeed/backoffice). (R-2,
   [docs/Contracts.md](docs/Contracts.md)) The one exception is CashOutStake's odds-change mirror,
   which has a consumer-side test (rule 12). A message type is a string literal passed to `Publish`,
   may differ from the class name, and must match the consumer's `switch` case exactly: GM-893 found
   8 operator publishes typed `OnBetBackofficeCancellation` that Provider.Service silently dropped.
3. **Auth is custom, not framework-based.** No `AddAuthentication`/JWT/`[Authorize]`; `UseAuthorization()`
   is inert. Access control = API-key **ActionFilters** applied per-controller via `[ServiceFilter]`.
   **A new controller without a filter attribute is completely open** (no global fallback). (R-6,
   [docs/Authentication.md](docs/Authentication.md))
   `Platform/Platform.API/ActionFilters/APIKeyFilter.cs` returns 400 with StatusCode 102 when a
   valid `x-api-key` (not the WebSocket key) arrives without `x-client-id`. Before GM-873 that was
   a NullReferenceException and a 500. `Platform/Platform.API.Test/ActionFilters/APIKeyFilterTests.cs`
   shows how to unit-test a filter. `Platform.ClientPlatform.API` has its own copy.
4. **No global exception handler.** Errors are shaped per-controller try/catch → `BaseResponseDTO`
   with a business `StatusCode`; unguarded actions leak a bare 500. Don't return `ex.StackTrace`. (TD-04)
5. **platform-final never writes MongoDB.** If you think you need to, you're probably in the wrong repo
   (odds-engine-2 owns the catalogue). ([docs/MongoDb.md](docs/MongoDb.md))
6. **Money is stored-proc only** (`Wallet.API`, `decimal(26,12)`); **parameterize all `FromSqlRaw`**
   (AgentReports interpolates — TD-01b/AP-2, do not copy it).
7. **Secrets in log lines: mask with `StringExtensions.Mask()`.** GM-880 (2026-09-30): `Platform.Operator.Servicev2/Repository/BettingRepository.cs` logged
   `_ogConfig.PrivateAPIKey` in plaintext across three `_logger.LogError` calls on every failed OG
   bet placement, visible to anyone with Grafana/Loki read access on PRD. Fixed by masking with
   `Platform.Shared.Extensions.StringExtensions.Mask()` (keeps the last 4 characters, masks the
   rest), reuse it for any other secret that needs to appear in a log line, rather than passing
   the raw value as a template argument. Committed settings-file secrets are dev/staging fallbacks
   (prod = AWS SSM). (TD-01)
8. **Caches are per-pod** (`MemoryCache.Default`); invalidation is event-driven via `CacheSync.Service`
   — use the prefix convention so eviction works. ([docs/CacheInvalidation.md](docs/CacheInvalidation.md))
   `DeleteCache(key)` removes one exact key, so `DeleteCache("leo")` evicts nothing when the entry
   is stored as `leo-{clientId}`: use `DeleteCacheByPrefix` or the full key (TD-22, found in GM-851;
   the Sport Tree purge still has this bug). GM-851: the Early/Today/Live lists order leagues from
   `ClientOddstableLeagueOrder` (`oleo-{clientId}`), not `ClientLeagueOrder`, and
   `OnOddstableLeagueSortOrderChange` purges only the changed client
   (`Platform.Sportsbook.API/Utility/OddstableLeagueSortOrder.cs`). The same order is returned by
   `GetClientFixtureSortOrder`, which odds-engine-2's websocket service stamps on pushed fixtures as
   `SoOr` and caches per client. CacheSync relays `OnOddstableLeagueSortOrderChange` to it after the
   purge so it reloads that client; it still does not reload on `OnSportSortOrderChange`
   (docs/CacheInvalidation.md).
9. **Config key sections are not interchangeable.** `appsettings.json` defaults point at localhost,
   and deployed values come from env vars in `../argocd/apps/platform/<service>/{dev,stg,prd}.yaml`,
   where `A__B` maps to `A:B`. A wrong section fails silently back to the default. Example:
   `Sportsbook.API/Repository/RabbitMQRepository.cs:23` reads `API:RabbitMQAPI`, but argocd
   `prd.yaml` sets `Custom__RabbitMQAPI`, so PRD calls `localhost:55866` (DEVOPS-389). When you
   add or rename a key, check all three env files.
10. **An optional parameter that only matters on one branch is easy to forget at the call site.**
    GM-879 (2026-09-30): both `CancelBet(..., isOGClient: true, ...)` call sites in
    `Platform.Operator.Servicev2/Repository/BettingRepository.cs`'s `ProcesssBetPlacementOG` set
    `isOGClient: true` but left the optional `OGParameters` argument at its `null` default, so
    `ProcesssBetCancellationOG` dereferenced a null `OGRequestDTO` and threw outside its own try
    block, crashing before OG was notified and before the retry path ever ran. Fixed by passing the
    already-parsed `ogParam` through both call sites and adding a null guard in
    `ProcesssBetCancellationOG` (TD-20). When adding a new `isOGClient: true` call site, pass
    `OGParameters` explicitly.
11. **A `catch` block that makes another network call must guard it.** GM-892: the `catch` block
    of `ProcesssBetCancellationOG` asked OG for the transaction status (and then the wallet
    balance) with no guard. When OG was unreachable that second call threw too, the exception
    escaped before `AddOrUpdateOperatorSettlementRetry` ran, and no `OperatorSettlementRetry` row
    was written, so Job.Resettlement had nothing to replay. Both calls now sit in their own
    try/catch: a failed status lookup means "status unknown" and falls through to the retry row,
    and a failed wallet refresh no longer stops a pending retry row from being closed (TD-20). Do
    the same for any recovery path that talks to the system that just failed.
12. **A consumer-side mirror of an OE2 message needs an explicit wire name on every property.**
    GM-902 (2026-10-15): `CashOutStake.Servicev2` deserialized `OnOddsChange` into a class that used
    the OE v1 names (`FixtureId`, `Markets`), but odds-engine-2 publishes short names (`FiId`, `Ma`,
    ...). `System.Text.Json` is case-sensitive and ignores unknown properties, so `FixtureId` was 0
    and `Markets` empty, `ProcessOddsChange` matched no bet, and no cashout value was ever pushed
    from an odds change, with no log line or error (TD-24). Fixed with `[JsonPropertyName]` on
    `OnExternalOddsChangeMessage`, `ExternalMarketMessage` and `ExternalSelectionMessage`, pinned by
    `Platform.CashOutStake.Servicev2.Test/Repository/OddsChangeContractTests.cs` (which also fails
    any public property added without a mapping). When you add or change a consumer of an `r.oe.*`
    message, map the OE2 names and add a test that deserializes an OE2-shaped payload.

## How this team writes code
- **Repository pattern**: `IXxxRepository`+`XxxRepository` in each project's Repository folder, async, return
  `Task<BaseResponseDTO<T>>` (business `StatusCode`). Controllers stay thin (open `Activity`, call repo,
  `switch` on `StatusCode`). Business logic lives **in repositories**, not services.
- **DI**: built-in container; group registrations in `IServiceCollection` extension methods
  (`AddBettingProviders`/`AddProviders`); config via `IConfiguration["A:B"]` (no `IOptions<T>`).
- **Providers**: add via `IBetProvider` + `IBetProviderFactory.Resolve(int channelType)` — no call-site `if`s.
- **Mapping**: manual via LINQ `.Select` (no AutoMapper). **Validation**: ActionFilters + inline +
  `StatusCode` (no FluentValidation). **Feature flags**: `Custom:*` config keys (no flag framework).
- **Messaging**: `On*` POCO contracts; consume via `IMessageBroker.MessageReceived` + `switch(MessageType)`;
  manual ack for loss-sensitive consumers; carry `CorrelationId`.
- **Logging/telemetry**: Serilog console-JSON + OpenTelemetry `Instrumentation`; propagate trace context
  through RabbitMQ headers.
- **Logging rules** (shared with odds-engine-2):
  - Pass the exception as the first argument: `_logger.LogError(ex, "... {Payload}", payload)`.
    Never put `ex.Message` or `ex.StackTrace` in the message text, and never pass the exception
    as a template argument.
  - Use PascalCase placeholders (`{Message}`, `{Payload}`).
  - Keep operational log lines (start, connecting, connected, progress). Do not remove or merge
    them to satisfy SonarQube S6664.
  - Older code still interpolates exceptions into the message text, e.g.
    `Platform/Platform.API/Repository/BettingRepository.cs:572`. Do not copy it.
- Full detail: [docs/ImplementationPatterns.md](docs/ImplementationPatterns.md),
  [docs/CodingStandards.md](docs/CodingStandards.md), [docs/BestPractices.md](docs/BestPractices.md),
  [docs/AntiPatterns.md](docs/AntiPatterns.md).

## Where things live
| Need | File |
|------|------|
| Public API surface | `Platform/Platform.API/Controllers/IntegrationController.cs` and its InternalController |
| Gateway logic | `Platform/Platform.API/Repository/PlatformRepository.cs` |
| Sportsbook reads (Mongo) | `Platform/Platform.Sportsbook.API/Repository/SportsbookRepository.cs` (#1 churn) |
| Bet placement/config | `Platform/Platform.Betting.API/Repository/BettingRepository.cs` |
| Settlement | `Platform/Platform.Settlement.Service` · Cashout: `Platform/Platform.CashOut.Service`, `Platform/Platform.CashOutStake.Servicev2` |
| Messaging | `Platform/Platform.Shared/Utility/MessageBroker.cs` · Cache: `Platform/Platform.Shared/Utility/MemCache.cs` + `Platform/Platform.CacheSync.Service` |
| Entities / contracts / enums | `Platform/Platform.Shared` — its Data, MessageModels and Enums folders |
| Providers | `Platform/Platform.Shared/Providers` (bet providers MTS, GTS; BetGenius only as the `BGLiveStream` `ILiveStreamProvider`) |
See [docs/knowledge/FrequentlyModifiedAreas.md](docs/knowledge/FrequentlyModifiedAreas.md).

## Development workflow
Follow these steps in order on every task:
1. **Understand the requirement.**
2. **Search the repo** for existing implementations (the find-service, find-repository and find-pattern commands).
3. **Consult the docs** ([docs/README.md](docs/README.md) index).
4. **Trace the feature end-to-end** (the trace commands) — don't stop at boundaries.
5. **Identify reusable components** ([docs/knowledge/CommonServices.md](docs/knowledge/CommonServices.md)).
6. **Explain the proposed solution** before building.
7. **Identify risks and assumptions** (the impact-analysis command, [docs/ArchitectureRisks.md](docs/ArchitectureRisks.md)).
8. **Ask clarifying questions** when the requirement/evidence is unclear.
9. **Wait for approval before any code change.** Say so explicitly when the change touches
   `Platform.Shared`, a cross-repo contract, a new pattern, or a new project/service.
10. **Implement the smallest possible change.**
11. **Validate** compilation (.NET 10 / `net10.0`) and behavior (run the relevant checklist).
12. **Update `docs/` for every change.** A bug fix, security fix, or resolved debt item belongs in
    `docs/` too (TechnicalDebt.md or whichever file already covers it; the update-docs command).
    Keep CLAUDE.md/AGENTS.md/commands/skills/playbooks in sync, and `README.md` whenever it
    documents what changed (projects/frameworks, prerequisites, build/run/test, configuration, endpoints).
13. **Summarize** changes, risks, and validation results.

## Engineering principles
- **Preserve existing architecture; never introduce a new pattern without approval** (no AutoMapper/
  FluentValidation/MediatR/`IOptions<T>`/etc. where an established pattern exists — see
  [docs/ImplementationPatterns.md](docs/ImplementationPatterns.md)).
- **Reuse the shared kernel — never duplicate a definition per-service.** Entities, `DbContext`s,
  message/DTO contracts, and enums belong in the shared library (`Platform.Shared` here; `BG.Shared` /
  `OE.Shared` in siblings — e.g. `../bet-genius/BG.Shared/Data/Trading/TradingContext.cs`). **Before creating any of
  these, search the shared project first and reuse it.** If it's genuinely missing, **add it to the
  shared library** (additive) — do not copy a local per-service version, even if an existing service
  already has its own copy (that copy is the bug to fix, not a pattern to follow).
- **Record unknowns** in [docs/Unknowns.md](docs/Unknowns.md). Treat the docs' Likely and Unknown tags
  as verify-before-rely.
- **Test project shape.** A new project's test project mirrors `Platform.Shared.Test` / sibling
  conventions: `xunit` + `Microsoft.NET.Test.Sdk`, Arrange/Act/Assert, no mocking framework unless one
  is already used. Extract pure logic (key building, SQL/parameter shaping, parsing) out of EF/RabbitMQ
  glue so it is unit-testable, and cover the core/safety-critical paths. Run the tests and report results.
- **Production investigations** follow [.claude/playbooks/production-readonly.md](.claude/playbooks/production-readonly.md).
- **Idempotent SQL here means** (data fixes, seeds, DDL, hand-run/Flyway migrations): guard INSERTs with
  `NOT EXISTS`/`MERGE` and schema changes with `IF NOT EXISTS`/`OBJECT_ID` checks.
- **Siblings are integration references: identify dependencies, don't invent behavior** (`../odds-engine-2`,
  `../Auth`, `../backoffice-final`, `../bet-genius`, `../platform-web`, `../argocd`, `../infrastructure`).

## Scope & build reminders
- Subject is **platform-final** (backend); UI work belongs to **platform-web**.
- Code targets **.NET 10** (`net10.0`), except `Platform.Shared` + the 3 Mongo services (`Sportsbook.API`, `QTech.API`, `CashOutStake.Servicev2`) on **`net8.0`** (MongoDB.Driver 2.11.6, 2.13.1 in `CashOutStake.Servicev2`, breaks on net10 — ADR-14). Build SDK is 10.0; runtime images 10.0 (8.0 for the two net8 services with Dockerfiles).
- **Confirm the live variant** before editing duplicated code (`*.Service` vs `*.Servicev2`,
  `WebSocket.API` vs `Websocket.API2`).

## Assets in this repo
- **`docs/`** — the 10-phase knowledge base (start at [docs/README.md](docs/README.md)).
- **`.claude/`** — index of every command, skill and guide: [.claude/README.md](.claude/README.md).
- **`.claude/commands/`** — trace, impact-analysis, find and review-feature commands.
- **`.claude/skills/jira-updates`** — rules for Jira writes, git push/PR approval, and build + test before commit.
- **`.claude/skills/`**, **`.claude/playbooks/`**, **`.claude/checklists/`**, **`.claude/templates/`**,
  **`.claude/prompts/`** — task guidance, derived from this repo.

## See also
[AGENTS.md](AGENTS.md) · [docs/Architecture.md](docs/Architecture.md) ·
[docs/ArchitectureRisks.md](docs/ArchitectureRisks.md) · [docs/KnowledgeValidation.md](docs/KnowledgeValidation.md)
