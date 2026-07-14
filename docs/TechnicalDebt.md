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

### TD-02 — End-of-life runtime (.NET Core 3.1 / netstandard2.1)
- **Evidence:** ~36 projects `netcoreapp3.1`; Shared `netstandard2.1`; old transitive deps; partial
  net7/net8 moves.
- **Files:** all `*.csproj`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** CVE scan of the locked dependency set; 3.1-only API inventory.

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

### TD-05 — Build-SDK / runtime version mismatch
- **Evidence:** Dockerfiles build on `dotnet/sdk:6.0`, run on `dotnet/aspnet:3.1`.
- **Files:** `Platform.*/Dockerfile`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** check for any 6.0-only API usage that could fail at runtime.

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
  Swashbuckle 5.6 vs 6.0; EF Core patch + MongoDB driver drift (2.11.6 vs 2.13.1); `Async` suffix uneven.
- **Files:** those projects' `Program.cs`/`.csproj`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** observability-coverage + dependency-version audit.

### TD-09 — Shipping integrations absent from CI
- **Evidence:** QTech, RedCat*, Oddin, AgentReports*, Cancellation, ESportOperator, Operator.Cashout,
  WebSocket.API (v1) not in the CI image matrix.
- **Files:** `.github/workflows/ci.yml` vs project list.
- **Confidence:** **[Verified]** (absence); deploy path **[Unknown]**.
- **Suggested investigation:** check argocd/other pipelines for how these are built/deployed (U-06).

### TD-10 — Thin automated-test net
- **Evidence:** only `Platform.Shared.Test`; broad coverage not evident.
- **Files:** `Platform.Shared.Test/*`.
- **Confidence:** **[Likely]**.
- **Suggested investigation:** measure coverage; identify untested high-risk paths (settlement/cashout/placement).

### TD-13 — Singleton repo holding scoped `DbContext` (worker hazard)
- **Evidence:** worker services register repos Singleton while the hosted `Worker` is singleton.
- **Files:** `*.Service/Program.cs`, `*/Repository/*`.
- **Confidence:** **[Likely]**.
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
