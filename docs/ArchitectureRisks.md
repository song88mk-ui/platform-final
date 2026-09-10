# Architecture Risks

> **Phase 10.** Systemic, architecture-level risks (vs the itemized list in
> [TechnicalDebt.md](TechnicalDebt.md) and [AntiPatterns.md](AntiPatterns.md)). Each: **Evidence ·
> Files · Confidence · Suggested investigation**. **No refactoring suggested** — investigation only.
> Risk = Likelihood × Impact (H/M/L), a judgment aid.

## R-1 — `Platform.Shared` blast radius (single shared kernel) — **Risk: High**
- **Evidence:** 37 projects reference `Platform.Shared`; CI rebuilds all 26 matrix images on every push to main/PR (no affected-files detection), so any Shared change ships in
  `Platform.Shared/**` for every app → any change rebuilds **all** images; it's the #1 churn area
  (1857 touches).
- **Files:** `Platform.Shared/**`, `.github/workflows/ci.yml`, every `*.csproj`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** map which `Platform.Shared` types are truly shared vs project-specific;
  measure rebuild/deploy fan-out of a representative Shared change.

## R-2 — Cross-repo contracts coupled by convention only — **Risk: High**
- **Evidence:** message contracts, routing keys, Mongo/SQL schemas, and SignalR method names are
  **duplicated per repo** (no shared package, no contract test); a rename is an up-to-4-repo silent
  break. Abbreviated field names raise the error rate.
- **Files:** `Platform.Shared/MessageModels/*`, `Data/Sportsbook/Entities/*`, `Websocket.API2/Messages/*`;
  sibling OE2/platform-web equivalents.
- **Confidence:** **[Verified]** (no shared pkg) + **[Verified-sibling]**.
- **Suggested investigation:** diff platform-final's `On*`/Mongo field names against odds-engine-2 &
  platform-web; check whether any contract/integration tests exist anywhere in the org.

## R-3 — End-of-life runtime (.NET Core 3.1) — **Resolved** (2026-08-20, GM-744)
- **Evidence (historical):** ~36 projects on EOL `netcoreapp3.1`; old transitive deps (EF Core 5,
  RabbitMQ.Client 5.2.0, Swashbuckle 5.6/6.0); ad-hoc partial moves to net7/net8; build SDK 6.0
  ≠ runtime 3.1.
- **Files:** all `*.csproj`; Dockerfiles.
- **Confidence:** **[Verified]**.
- **Resolution:** 36 projects target `net10.0`, Platform.Shared + 3 Mongo services `net8.0` (driver 2.11.6 net10 LINQ break); Docker builds on sdk:10.0, runs on
  aspnet:10.0 (aspnet:8.0 for the 2 Mongo services with Dockerfiles); EF Core 8.0.30 and Swashbuckle 10.2.3 unified. RabbitMQ.Client 5.2.0 (and other
  wire-format-sensitive packages) deliberately kept for cross-repo safety.

## R-4 — Secrets exposure in git — **Risk: High**
- **Evidence:** committed base `appsettings.json` carry live dev/staging DB passwords + keys (prod
  uses SSM, but dev/staging secrets ship in images and persist in history).
- **Files:** `Platform.*/appsettings.json` (e.g. `Platform.API/appsettings.json`).
- **Confidence:** **[Verified]** + **[Verified-sibling]** (SSM).
- **Suggested investigation:** confirm whether the committed dev/staging creds are still active;
  scan git history for the breadth of exposure.

## R-5 — SQL injection via interpolated `FromSqlRaw` — **Risk: High (input-dependent)**
- **Evidence:** `AgentReports.API/ReportsRepository` interpolates params into the SQL string for 6
  report procs (vs parameterized calls elsewhere).
- **Files:** `Platform.AgentReports.API/Repository/ReportsRepository.cs` (~L67,167,999,1091,1217,1279).
- **Confidence:** **[Verified]** (pattern); exploitability **[Likely]** depends on input trust.
- **Suggested investigation:** trace whether any interpolated value is caller-controlled (account ids,
  filters) vs server-derived; review the report-param call chain from the controller.

## R-6 — No global authorization fallback — **Risk: High**
- **Evidence:** auth is opt-in per controller via `[ServiceFilter]`; `UseAuthorization()` is inert; a
  controller missing the attribute is fully open. Coarse shared internal key opens all BO endpoints.
- **Files:** `*/ActionFilters/*`, `*/Startup.cs`, `*/Controllers/*`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** audit every controller for a filter attribute (find any unprotected
  action); review internal-key distribution/rotation.

## R-7 — Per-pod caches with broadcast-only coherence — **Risk: Medium-High**
- **Evidence:** `MemoryCache.Default` is per-pod; cross-pod coherence relies on RabbitMQ eviction
  (`CacheSync.Service`); a missed message → stale until TTL (hours). Redis tier invalidation path
  unverified (U-29); platform-web adds a 30s tier.
- **Files:** `Platform.Shared/Utility/MemCache.cs`, `Platform.CacheSync.Service/*`.
- **Confidence:** **[Verified]** (mechanism); failure frequency **[Unknown]**.
- **Suggested investigation:** check for invalidation ack/retry; confirm whether Redis is evicted on
  config change; measure cross-pod cache divergence in practice.

## R-8 — Central RabbitMQ as a shared single point of failure — **Risk: Medium-High**
- **Evidence:** one exchange `e.central` on `rabbitmq-main-cluster.bet-genius-apps.svc` carries
  **all** platform + sibling traffic; many services depend on it.
- **Files:** `Platform.Shared/Utility/MessageBroker.cs`; argocd configs.
- **Confidence:** **[Verified]** + **[Verified-sibling]**.
- **Suggested investigation:** broker HA/cluster topology, queue durability, and back-pressure
  behavior under outage (services auto-recover at 5s, but consumer lag impact is unmeasured).

## R-9 — No global exception handling — **Risk: Medium**
- **Evidence:** only `UseDeveloperExceptionPage` (dev); unguarded actions leak bare 500s; some catches
  return `ex.StackTrace` to clients; unguarded `x-og-*` parsing can throw.
- **Files:** `*/Startup.cs`, controllers, `Platform.API/ActionFilters/APIKeyFilter.cs`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** survey production 5xx/unhandled exceptions; confirm whether stack
  traces actually reach external callers in prod config.

## R-10 — Hand-rolled 100-thread consumers on hot queues — **Risk: Medium**
- **Evidence:** `CashOut`, `Placement.Servicev2`, `Operator.Servicev2`, `Operator.Cashout`,
  `Client.TopMarket` use raw `new Thread` + `BlockingCollection` round-robin.
- **Files:** those services' `Worker.cs`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** observe thread/CPU/memory behavior and message-ordering guarantees
  under peak load; check for the singleton-repo + scoped-DbContext hazard (R-13).

## R-11 — Cross-store consistency with no FKs / no transactions — **Risk: Medium**
- **Evidence:** SQL (bets) and Mongo (catalogue) link by value only; no spanning transaction; bets
  snapshot catalogue values; orphan references possible.
- **Files:** `BetSelection` entity, `MarketMapping`, repositories.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** quantify orphan/stale-reference frequency; review reconciliation paths.

## R-12 — Boundary violation: back-office writes platform SQL directly — **Risk: Medium**
- **Evidence:** backoffice-final writes `ClientSportOrder`/`ClientSegmentOrder`/`ClientLeagueOrder` +
  market-template groups **directly** to the shared `Sportsbook` DB (bypassing platform-final's API),
  and reads platform DBs directly.
- **Files:** sibling `backoffice-final/.../SportsbookRepository.cs`; platform `SportsbookContext`.
- **Confidence:** **[Verified-sibling]**.
- **Suggested investigation:** enumerate all backoffice direct-write tables; assess schema-change
  coordination risk between the two repos.

## R-13 — Singleton repo holding scoped `DbContext` (worker lifetime hazard) — **Risk: Medium**
- **Evidence:** worker services register repos Singleton while the hosted `Worker` is singleton; a
  cached `DbContext` would be unsafe.
- **Files:** `*.Service/Program.cs`, `*/Repository/*`.
- **Confidence:** **[Likely]** (pattern; per-service behavior not all traced).
- **Suggested investigation:** per worker, confirm a `DbContext` is created per message (not cached).

## R-14 — Duplicate / parallel implementations (unclear source of truth) — **Risk: Medium**
- **Evidence:** `WebSocket.API` vs `Websocket.API2`; `*.Service` vs `*.Servicev2`; duplicated
  `MessageBroker` in some services; superseded v1 services still in git history; `oddsengine` vs
  `odds-engine-2`.
- **Files:** the paired projects; `BetStatus.Service/Utility/MessageBroker.cs`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** confirm via argocd which variant is deployed; mark the dead ones.

## R-15 — Inconsistent cross-cutting adoption — **Risk: Low-Medium**
- **Evidence:** `Client.API`/`ClientSportsbook.API` lack Serilog; `Client.TopMarket` lacks OTel;
  Swashbuckle 5.6 vs 6.0; EF/Mongo driver drift; inconsistent `Async` suffix.
- **Files:** those projects' `Program.cs`/`.csproj`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** observability-coverage audit; dependency-version alignment review.

## R-16 — Thin test safety net — **Risk: Medium**
- **Evidence:** 3 of 41 projects have tests (`Platform.Shared.Test`, `Platform.Sportsbook.API.Test`,
  `Platform.Operator.API.Test`, `Platform.BetInfo.Service.Test`). The shared kernel is gated by
  Stryker at `break 80`; the money-path services (settlement, cashout, placement) are still
  untested, because their repositories build a `DbContext` inline and offer no seam.
- **Files:** `Platform.Shared.Test/*`, `Platform.Sportsbook.API.Test/*`,
  `Platform.Operator.API.Test/*`, `Platform.BetInfo.Service.Test/*`.
- **Confidence:** **[Verified]**.
- **Suggested investigation:** measure actual coverage; identify the highest-risk untested paths
  (settlement, cashout, placement).

## Circular dependencies — **none at compile time** `[Verified]`
- **Evidence:** project-reference graph this phase = 37→`Platform.Shared` + 1 (`WebSocket.Client`→
  `WebSocket.API`); no deployable→deployable cycles; `Platform.Shared` references no app project.
- **Confidence:** **[Verified]** compile-time. **Runtime** message/HTTP cycles (e.g. publish→consume→
  republish loops across services) are **[Unknown]**.
- **Suggested investigation:** trace runtime message loops (e.g. `Provider.Service` republishing
  `OnBetPlacement`) for unintended cycles/feedback.

## Risk heat summary
| Risk | L×I | Confidence |
|------|-----|-----------|
| R-1 Shared blast radius · R-2 contract coupling · ~~R-3 EOL runtime~~ (resolved 2026-08-20, GM-744) · R-4 secrets · R-5 SQLi · R-6 no-auth-fallback | **High** | Verified |
| R-7 cache coherence · R-8 central broker SPOF | **Med-High** | Verified |
| R-9 no global exceptions · R-10 thread pools · R-11 cross-store · R-12 BO direct writes · R-13 DbContext lifetime · R-14 duplicates · R-16 tests | **Medium** | Verified/Likely |
| R-15 inconsistent cross-cutting | **Low-Med** | Verified |
