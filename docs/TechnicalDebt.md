# Technical Debt, Inconsistencies & Risks

> **Phase 10 (authoritative).** Consolidated debt register. Every finding has **Evidence · Files ·
> Confidence · Suggested investigation**. Per the brief, **no refactoring is suggested** — the fourth
> field is *what to investigate/confirm*, not how to fix. Severity (High/Med/Low) is a prioritization
> aid. Systemic versions: [ArchitectureRisks.md](ArchitectureRisks.md); observed-pattern framing:
> [AntiPatterns.md](AntiPatterns.md); open questions: [Unknowns.md](Unknowns.md). TD-IDs are stable
> (cross-referenced across docs).

## High severity

### TD-01 — Committed plaintext secrets in `appsettings.json`
- **Evidence:** base `appsettings.json` contain DB passwords, API keys, MessageBroker password,
  partner creds; build strips `appsettings.*.json` but **not** the base file (ships in image). In
  cluster, SSM/External-Secrets override them → committed values are dev/staging fallbacks.
- **Files:** `Platform.*/appsettings.json` (e.g. `Platform.API/appsettings.json` staging RDS password).
- **Confidence:** **[Verified]** + **[Verified-sibling]** (argocd SSM).
- **Suggested investigation:** confirm whether committed dev/staging creds are still active; scope the
  git-history exposure.

### TD-01b — SQL injection via interpolated `FromSqlRaw`
- **Evidence:** `ReportsRepository` interpolates parameters into the SQL string for 6 report procs
  (vs parameterized `FromSqlRaw("proc {0}", arg)` used by Wallet/Betting).
- **Files:** `Platform.AgentReports.API/Repository/ReportsRepository.cs` (~L67,167,999,1091,1217,1279).
- **Confidence:** **[Verified]** (pattern); exploitability **[Likely]** (input-trust dependent).
- **Suggested investigation:** trace whether interpolated values are caller-controlled.

### TD-02 — End-of-life runtime (.NET Core 3.1 / netstandard2.1) — **Resolved** (2026-08-20, GM-744)
- **Evidence (historical):** ~36 projects `netcoreapp3.1`; Shared `netstandard2.1`; old transitive
  deps; partial net7/net8 moves.
- **Files:** all `*.csproj`.
- **Confidence:** **[Verified]**.
- **Resolution:** 36 projects target `net10.0` and 4 target `net8.0` (Shared + Mongo services); EF Core unified at 8.0.30,
  Microsoft.Extensions.* at 10.0.11, Swashbuckle at 10.2.3. Wire-format-sensitive packages
  (RabbitMQ.Client 5.2.0, MongoDB.Driver 2.11.6/2.13.1, Newtonsoft.Json 12.0.3,
  Sportradar.MTS.SDKCore 2.4.1.2) deliberately unchanged for cross-repo safety.

### TD-03 — Hardcoded credentials/endpoints (`BetPlacement.Console`)
- **Evidence:** staging gateway URL + `x-client-id`/`x-operator-token`/`x-api-key`/IP hardcoded.
- **Files:** `Platform.BetPlacement.Console/Program.cs`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** confirm the tool isn't shipped/deployed; check the creds' validity.

### TD-04 — Exception detail leaked to API callers
- **Evidence:** catches build `BaseResponseDTO` with `ex.Message`/`ex.StackTrace`.
- **Files:** e.g. `Platform.API/Controllers/IntegrationController.cs`; various repositories.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** confirm prod (non-Dev) responses actually include stack traces.

## Medium severity

### TD-05 — Build-SDK / runtime version mismatch — **Resolved** (2026-08-20, GM-744)
- **Evidence (historical):** Dockerfiles built on `dotnet/sdk:6.0`, ran on `dotnet/aspnet:3.1`.
- **Files:** `Platform.*/Dockerfile`.
- **Confidence:** **[Verified]**.
- **Resolution:** all 31 Dockerfiles now build on `dotnet/sdk:10.0` and run on
  `dotnet/aspnet:10.0`, with `ENV ASPNETCORE_URLS=http://+:80` pinned in the runtime stage
  (aspnet:8.0+ defaults to port 8080) and portable `CMD ["dotnet", "<App>.dll"]` (exceptions: Placement.Servicev2 runs `entrypoint.sh`; Websocket.API2 uses `ENTRYPOINT`).

### TD-06 — Hand-rolled 100-thread fan-out consumers
- **Evidence:** raw `new Thread` + `BlockingCollection` round-robin.
- **Files:** `CashOut.Service`, `Placement.Servicev2`, `Operator.Servicev2`, `Operator.Cashout.Service`,
  `Client.TopMarket` `Worker.cs`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** observe load behavior + ordering guarantees; check DbContext lifetime (TD-13).

### TD-07 — Duplicate / parallel implementations
- **Evidence:** `WebSocket.API` vs `Websocket.API2`; `*.Service` vs `*.Servicev2`; **duplicated
  `MessageBroker`** in some services; superseded v1 services in git history; `oddsengine` vs `odds-engine-2`.
- **Files:** the paired projects; `Platform.BetStatus.Service/Utility/MessageBroker.cs`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** confirm via argocd which variant deploys; identify the dead ones (see TD-15).

### TD-08 — Inconsistent cross-cutting adoption
- **Evidence:** `Client.API`/`ClientSportsbook.API` lack Serilog; `Client.TopMarket` lacks OTel;
  ~~Swashbuckle 5.6 vs 6.0; EF Core patch drift~~ (resolved 2026-08-20, GM-744: Swashbuckle
  unified 10.2.3, EF Core unified 10.0.11); MongoDB driver drift (2.11.6 vs 2.13.1) remains
  (deliberately untouched by GM-744); `Async` suffix uneven.
- **Files:** those projects' `Program.cs`/`.csproj`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** observability-coverage + dependency-version audit.

### TD-09 — Shipping integrations absent from CI
- **Evidence:** QTech, RedCat*, Oddin, AgentReports*, ESportOperator,
  WebSocket.API (v1) not in the CI image matrix (`Operator.Cashout.Service` **is** in the matrix;
  `Cancellation.Service` is a bin/obj-only orphan dir, not a project). Because CI never built them,
  6 of these projects carried an unnoticed compile break since commit `95add342` (stale
  `using Platform.Shared.Telemetry;`, missing `Infra.Extensions.Telemetry` ref) — fixed 2026-08-20,
  GM-744, but the CI gap itself remains.
- **Files:** `.github/workflows/ci.yml` vs project list.
- **Confidence:** **[Verified]** (absence); deploy path **[Unknown]**.
- **Suggested investigation:** check argocd/other pipelines for how these are built/deployed (U-06).

### TD-10 — Thin automated-test net
- **Evidence:** six test projects exist: `Platform.Shared.Test` (360 tests, 83.51% Stryker
  mutation score over `Extensions`/`Helpers`/`Utility`/`Providers`), `Platform.Sportsbook.API.Test`
  (13 tests over one pure static class), `Platform.Operator.API.Test` (209 tests, added GM-809, resettlement matrix added GM-815) and
  `Platform.BetInfo.Service.Test` (12 tests, added GM-814), `Platform.CacheSync.Service.Test`
  (13 tests, added GM-853) and `Platform.Operator.Servicev2.Test` (16 tests: the cashout wallet
  retry path and host registration, added GM-862, and OG bet placement key masking, added GM-880).
  All run by a PR-only CI test job. Settlement remains untested. Of placement, only the
  OG failure-path logging is covered (GM-880); of cashout, only the Operator.Servicev2 wallet
  retry path.
- **Files:** `Platform.Shared.Test/*`, `Platform.Sportsbook.API.Test/*`,
  `Platform.Operator.API.Test/*`, `Platform.BetInfo.Service.Test/*`,
  `Platform.CacheSync.Service.Test/*`, `Platform.Operator.Servicev2.Test/*`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** 28 inline `new DbContextOptionsBuilder<T>()` sites across 12 files
  still have no test seam. `Platform.BetInfo.Service` shows the fix: swap the inline construction
  for `IDbContextFactory<T>` and register it with `AddDbContextFactory`.

### TD-13 — Singleton repo holding scoped `DbContext` (worker hazard)
- **Evidence:** worker services register repos Singleton while the hosted `Worker` is singleton.
- **Files:** `*.Service/Program.cs`, `*/Repository/*`.
- **Confidence:** **[Likely]**.
- **Resolved for `Platform.BetInfo.Service`** (GM-814): the repository stays a singleton but now
  takes `IDbContextFactory<BettingContext>`, which `AddDbContextFactory` registers as a singleton
  handing out a fresh context per call. That pairing is safe where injecting a scoped `DbContext`
  into a singleton would not be, and it is the pattern the other workers should follow.
- **Suggested investigation:** per worker, confirm a `DbContext` is created per message, not cached.

### TD-14 — README stub; no in-repo docs before this set
- **Evidence:** root `README.md` = "# platform-final"; `docs/` did not exist prior.
- **Files:** `README.md`, `docs/`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** n/a (this `docs/` set addresses it).

### TD-15 — Dead / superseded code (new, Phase 10)
- **Evidence:** `JwtParser` defined but unreferenced; **5 commented-out `ExecuteSqlRaw` blocks** in the
  #1 hotspot; commented-out `BetMapping` in `RedCatBettingContext`; v1 services
  (`Placement.Service`, `Operator.Service`, `Job.BetAudit.Tableau`) live in git history; `WebSocket.API`
  (v1) superseded by `Websocket.API2`.
- **Files:** `Platform.Shared/Utility/JwtParser.cs`; `Platform.Sportsbook.API/Repository/SportsbookRepository.cs`
  (~L9801–10040); `Data/RedCatBetting/RedCatBettingContext.cs`; deleted projects in git log.
- **Confidence:** **[Verified]** (JwtParser unused, commented blocks) / **[Likely]** (which projects are truly dead).
- **Suggested investigation:** confirm against argocd which projects/files are still deployed/used before treating as dead.

### TD-16 — TODO/HACK/FIXME/Obsolete markers (new, Phase 10)
- **Evidence:** 11 files carry `TODO`/`HACK`/`FIXME`/`[Obsolete]`/"deprecated" markers.
- **Files:** 11 `*.cs` (grep `//\s*(TODO|HACK|FIXME)|\[Obsolete\]|deprecated`).
- **Confidence:** **[Verified]** (count).
- **Suggested investigation:** enumerate and triage each marker for still-relevant intent.

## Low severity / hygiene

### TD-11 — Misspelled identifiers baked into contracts
- **Evidence:** procs `InserOrUpdateClientMarketSortOrder`/`…TemplateGroup`; `Custom:Fibonnaci`;
  `ProcesssBetPlacement`; `OnBOBetCancellaton`; `OnMarketDeactivationMesssage`; `ParlayBonusService .cs`
  (trailing space).
- **Files:** as named (schema + `Platform.Shared`).
- **Confidence:** **[Verified]**.
- **Suggested investigation:** these are real schema/contract identifiers — confirm blast radius before any rename.

### TD-12 — Over-permissive edge defaults
- **Evidence:** `AllowAllOrigins` CORS; `ForwardedHeaders` trusting all upstreams; Swagger exposed in all envs.
- **Files:** every web `Startup.cs`/`Program.cs`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** confirm the ingress is the sole entry point (so allow-all is mitigated).

### TD-17 — `TranslationContext` namespace mismatch
- **Evidence:** lives under `Data/Translation` but namespace is `Platform.Shared.Data.Sportsbook`.
- **Files:** `Data/Translation/TranslationContext.cs`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** n/a (cosmetic).

### TD-18 — MongoDB.Driver 2.11.6 blocks .NET 10 for the Mongo services (added 2026-08-24, GM-744)
- **Evidence:** the driver's LINQ partial evaluator throws `NotSupportedException:
  Specified method is not supported` on .NET 10 when rendering any filter with `Contains`
  over an array (reproduced in isolation: same expression renders on net8.0, throws on
  net10.0; hit in staging via `SportsbookRepository.GetSports`, `/v2/sportsbook/sports`).
- **Files:** `Platform.Shared` + `Platform.Sportsbook.API`, `Platform.QTech.API`,
  `Platform.CashOutStake.Servicev2` (all pinned to `net8.0`, ADR-14); EF Core pinned to
  8.0.30 solution-wide as a consequence (EF 10 needs net10; **EF 8 LTS support ends
  2026-11-10**).
- **Confidence:** **[Verified]** (isolated repro, 2026-08-24).
- **Suggested investigation:** upgrade MongoDB.Driver to 2.19+/3.x (LINQ3 provider),
  validate query translation against the odds-engine-2-owned catalogue, then move the 4
  net8 projects to `net10.0` and EF Core to 10.x.

### TD-19 - Secret value logged in plaintext (`BettingRepository.cs`) - **Resolved** (2026-10-02, GM-880)
- **Evidence (historical):** three `_logger.LogError` calls in
  `Platform.Operator.Servicev2/Repository/BettingRepository.cs` (originally lines 1035, 1092, 1158
  at tag `v0.1.124-prd`) passed `_ogConfig.PrivateAPIKey`/`privateKey` as a template argument on
  every failed OG bet placement. PRD logs ship to Loki, so the key was readable by anyone with
  Grafana read access; confirmed present in 20 PRD log lines on 2026-09-30.
- **Files:** `Platform.Operator.Servicev2/Repository/BettingRepository.cs`.
- **Fix:** all three calls now log `privateKey.Mask()` / `_ogConfig.PrivateAPIKey.Mask()`. `Mask()`
  is a new `Platform.Shared.Extensions.StringExtensions` method (keeps the last 4 characters,
  replaces the rest with `*`), covered by `Platform.Shared.Test/Extensions/StringExtensionsTests.cs`.
  The three call sites are covered by
  `Platform.Operator.Servicev2.Test/Repository/BettingRepositoryOGKeyLoggingTests.cs`, which drives
  each failure path against `OperatorStubServer` with a capturing logger and fails if any log line
  holds the raw key. Stryker cannot guard the call sites, because `stryker-config.json` ignores
  `Log*` calls and no mutator removes `.Mask()` from an argument. Removing any one `.Mask()` fails
  at least one of these tests (checked by hand).
  Reuse it for any other secret that must appear in a log line rather than inventing ad-hoc masking.
  See the rule 7 entry in [CLAUDE.md](../CLAUDE.md) for the standing policy this established.
- **Not in scope of the code fix:** rotating the exposed OG private key, and restricting/deleting
  the already-ingested Loki lines (both ops actions, tracked on the ticket, not in this repo).
- **Confidence:** **[Verified]** (traced in code, tests passing).

## Consolidated cross-views
- **Duplicate implementations** → TD-07 (+ AntiPatterns AP-8): v1/v2 projects, duplicated MessageBroker.
- **Dead code** → TD-15: JwtParser, commented SQL blocks, superseded v1 services/WebSocket.API.
- **Inconsistent patterns** → TD-08, TD-11; naming detail in [NamingConventions.md](NamingConventions.md).
- **Architecture violations / high-risk areas** → [ArchitectureRisks.md](ArchitectureRisks.md) (R-1…R-16).
- **Circular dependencies** → **none compile-time** [Verified]; runtime message loops [Unknown] (ArchitectureRisks).
- **Missing documentation / unknowns** → [Unknowns.md](Unknowns.md), [KnowledgeValidation.md](KnowledgeValidation.md).

## Debt concentration (where it lives) `[Verified]`
Most High/Medium items concentrate in the **change hotspots** — `Platform.Shared`, `Platform.API`,
`Sportsbook.API`/`SportsbookRepository.cs`, `Betting.API`/`BettingRepository.cs`, and the worker
services — i.e. the same areas as [FrequentlyModifiedAreas.md](knowledge/FrequentlyModifiedAreas.md).
