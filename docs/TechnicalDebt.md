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
  Later, for NuGet vulnerability advisories: Newtonsoft.Json moved to 13.0.4 (pinned in
  `Platform.Shared`, GHSA-5crp-9r3c-p9vr; its only behavior change is a default `MaxDepth` of 64),
  System.Net.Http 4.3.4 and System.Text.RegularExpressions 4.3.1 are pinned in `Platform.Shared`,
  SignalR client/MessagePack packages moved to 10.0.13, MessagePack to 2.5.307 and
  Microsoft.Azure.SignalR to 1.23.0 in WebSocket.API. MongoDB.Driver (GHSA-7j9m-j397-g4wx, fixed in
  2.19.0) and its SharpCompress 0.23.0 are still open: the upgrade switches the LINQ provider to LINQ3.

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
  retry path. None of these touches a real database. `Platform.Betting.API.IntegrationTests`
  (8 tests, added DEVOPS-408) is the first to run against SQL Server, covering the GM-861
  cashout ownership checks and the `GetTopMarketBets` recommendations path over HTTP. It is not
  in `Platform.sln` or CI yet, because CI does not check out the `infrastructure` repo that
  holds the schema.
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

### TD-20 - OG bet cancellation crashed with a null `OGRequestDTO` (`BettingRepository.cs`) - **Resolved** (2026-10-02, GM-879)
- **Evidence (historical):** both `CancelBet(..., isOGClient: true, ...)` call sites in
  `ProcesssBetPlacementOG` (originally lines 1037 and 1161 at tag `v0.1.124-prd`) omitted the
  `OGParameters` argument, so it defaulted to `null`. `CancelBet` forwarded that `null` into
  `ProcesssBetCancellationOG`, which dereferenced `ogParam.OGCurrencyCode` outside its own try
  block and threw `NullReferenceException` before the cancellation reached OG. The catch block hit
  the same bug calling `CancelBet` a second time, so `RetryBetCancellation` never ran. Confirmed in
  PRD Loki: 10 occurrences on 2026-09-30 between 02:00-03:59 UTC, all client OGE, all following an
  OG `E-103` response on `/api/v2/platform/transaction/bulk/debit`.
- **Files:** `Platform.Operator.Servicev2/Repository/BettingRepository.cs`.
- **Fix:** `ogParam` is now hoisted above the `try` block in `ProcesssBetPlacementOG` so both
  `CancelBet` call sites can pass it through. `ProcesssBetCancellationOG` also got its own null
  guard that logs and returns instead of throwing, since `StringExtensions.ToOGParam()` can
  independently return `null` for a malformed `OperatorToken`, not only when a caller forgets to
  pass it. Covered by
  `Platform.Operator.Servicev2.Test/Repository/BettingRepositoryOGCancellationTests.cs`, which
  drives `ProcesssBetCancellationOG` directly against `OperatorStubServer`, and by
  `BettingRepositoryOGPlacementCancelTests.cs`, which drives both `CancelBet` call sites through
  `ProcesssBetPlacementOG` with a bet seeded in the in-memory betting database. Those call-site
  tests became possible when GM-894 gave `BettingRepository` an
  `IDbContextFactory<BettingContext>`. Before that, `CancelBet` built a SQL Server
  `BettingContext` inline and could not run in a unit test.
- **Mutation testing (2026-10-02):** a Stryker run on `BettingRepository.cs` (run from
  `Platform.Operator.Servicev2.Test`, `Log*` methods ignored) found that the first two tests
  killed only 11 of the 130 mutants in `ProcesssBetCancellationOG`. The suite now covers the
  rollback payload (OG currency and game code, redenominated amount, single or multiple bet, team
  names), the `OnBetStatus`, reports, back-office and `OnWalletChange` publishes, the operator
  logs, and the retry row on each failure branch. It kills 83 mutants. Of the 8 that still
  survive, 3 are the message-type literal described below, 3 sit on the `S-101`
  transaction-status path (the stub returns one body for every request), and 2 are equivalent.
  After GM-894, all 8 mutants on the two `CancelBet` call sites are killed, and removing
  `ogParam` from either call site fails both call-site tests.
- **Found by the mutation tests, not fixed (outside GM-879):**
  - **GM-892 (fixed 2026-10-07):** when the rollback request itself threw, for example because OG
    was unreachable, the `catch` block called `GetOGTransactionStatus` with no guard. That call
    threw against the same endpoint, the exception escaped before `AddOrUpdateOperatorSettlementRetry`
    ran, and no `OperatorSettlementRetry` row was written, so Job.Resettlement had nothing to
    replay and the player's stake stayed debited at OG. When reached from bet placement, the
    exception also landed in the placement `catch`, which called `CancelBet` a second time.
    The status lookup in the `catch` block is now in its own try/catch (a failure is logged and
    treated as "status unknown", so the existing fallback publishes the back-office cancellation
    and stores the retry row). The wallet balance refresh that follows a confirmed rollback is
    guarded the same way. Its failure used to escape before the pending retry row was closed
    (`IsRetry = false`), so Job.Resettlement would replay a cancellation OG had already rolled
    back. The `S-101` branch needed no edit: an exception there lands in the same `catch` block.
  - **Replay verified:** `AddOrUpdateOperatorSettlementRetry` never reads `RemainingRetries`, so
    the fallback row is stored with `RetryCount = 0`, `IsRetry = true`, `IsSuccess = false`.
    `Platform.Job.Resettlement/Worker.cs:87-88` selects `IsRetry && RetryCount < MaxRetryCount`
    and republishes the stored `Info`, so the row is replayed.
  - **Tests (`Platform.Operator.Servicev2.Test`, 47 passing):** `OperatorStubServer` gained
    `PathResponses` so one test can answer the debit, transaction status and wallet balance paths
    differently. New cases cover an unreadable and a 500 status answer, an unrelated status, a
    confirmed rollback (balance published, pending row closed), a failing wallet refresh (three
    shapes), a wallet error code, both `S-101` paths, the stored row being replayable, and a
    rejected debit not asking for the status. `OgUnreachable_ThrowsBeforeAnyRetryIsStored` became
    `OgUnreachable_StoresARetryForTheCancellationAndDoesNotThrow`, and
    `DebitResponseCannotBeRead_CancelsLocallyAndSendsTheRollback` now expects no exception and a
    stored retry. With the production change reverted, 9 of these fail (the rest guard behaviour
    that was already correct), so they do exercise the fix.
  - **Unknown:** PRD frequency. A search of PRD operator-service logs (2026-09-09 to 2026-10-07)
    found no occurrence, but the `OperatorLog` table is not in Loki, so absence there is not
    conclusive.
  - **GM-893:** the three failure branches and the `catch` block publish
    `"OnBetBackofficeCancellation"` with a lowercase `o`, in both `ProcesssBetCancellationOG` and
    the non-OG `ProcesssBetCancellation` (8 sites). `Platform.Settlement.Service/Worker.cs:115` and
    `Platform.Provider.Service/Worker.cs:189` switch on `"OnBetBackOfficeCancellation"`, so those
    messages match no case. The success paths spell it correctly.
- **Not in scope of the code fix:** confirming the settlement state of the two example PRD bet IDs
  from the ticket (`3703da0a-dd8b-4a8d-f33d-08df196c1dc0`, `4fa9363a-2745-4db1-f33e-08df196c1dc0`)
  on our side and OG's side, requested on the ticket but not checked here (production is read-only
  by default; flagged back to the ticket reporter).
- **Confidence:** **[Verified]** (traced in code, tests passing).

### TD-21 - CashOutStake Mongo fallback blocked the consumer and dropped cashout offers (`BettingRepository.cs`) - **Resolved** (2026-10-07, GM-901)
- **Evidence (historical):** `Platform.CashOutStake.Servicev2/appsettings.json` shipped a real
  `preprod` Atlas connection string as the default for `ConnectionStrings:MongoDB-Sportsbook`, plus
  a real MSSQL `sa` connection string for `Betting` (same class of issue as TD-01, specific instance
  here). `argocd/apps/platform/cashoutstake-service/{dev,stg,prd}.yaml` didn't set the Mongo key
  (DEVOPS-404), so all three environments silently ran on the unreachable preprod default. Each
  failed lookup blocked the single-threaded RabbitMQ handler in `Worker.MessageReceivedEvent` for
  the driver's 30s default server-selection timeout; `ProcessPartialCashOut` mutated the in-memory
  stake before the lookup, so a failure meant `OnCashOutChange` was never published and the player
  kept the stale offer; and nothing was cached on failure, so every later message for the same
  bet/market/selection repeated the same 30s block. Confirmed in PRD Loki: 13 `TimeoutException`s
  between 2026-10-06 09:59-12:57 UTC at the same minutes as 14 partial cashouts, with no
  `Process Partial CashOut` log line following any of them.
- **Files:** `Platform.CashOutStake.Servicev2/appsettings.json`,
  `Platform.CashOutStake.Servicev2/Repository/BettingRepository.cs`.
- **Fix:** `appsettings.json` no longer ships either real credential - both `MongoDB-Sportsbook` and
  `Betting` default to an empty string, so a deployment that forgets to set them fails immediately
  and visibly instead of silently falling back to a value that happens to parse. The
  `BettingRepository` constructor now throws `InvalidOperationException` at startup if
  `ConnectionStrings:MongoDB-Sportsbook` is missing or blank (the service fails loudly instead of
  silently falling back), and sets
  `MongoClientSettings.ServerSelectionTimeout` to 5s when the connection string doesn't already
  specify one. `PreloadMarket`/`PreloadSelection` now wrap the Mongo `Find` call in a try/catch,
  record the failure per market/selection id in a `ConcurrentDictionary`, and skip retrying the
  same id for 30s - so a down Mongo degrades to "cashout unavailable" (via `GetCashOut`'s existing
  null-cache branch) instead of throwing, and `ProcessPartialCashOut`/`ProcessCashOut` always reach
  their `messageBroker.Publish` call regardless of Mongo health.
  A market/selection that Mongo doesn't have is not cached (`MemCache` cannot store null:
  `MemoryCache.Set` throws `ArgumentNullException`), is logged at Warning as "not found" rather
  than as a Mongo failure, and is backed off for 30s like a failure. The two Mongo queries sit
  behind `protected virtual FindMarket`/`FindSelection` so tests can drive these paths.
  Covered by `Platform.CashOutStake.Servicev2.Test/Repository/MongoLookupResilienceTests.cs`:
  constructor fail-fast; offer still published after a failed lookup, on both the
  partial-cashout and the odds-change path (one test runs against an address nothing listens on
  with a short `serverSelectionTimeoutMS`); not-found logs a Warning, no Error, and publishes
  Unavailable; a second call inside the backoff window does not query Mongo again, after a
  failure and after a not-found (asserted by counting lookups, not by timing); a found
  market/selection is cached and not queried again.
- **Not in scope of the code fix:** `DEVOPS-404` (adding the Mongo key to the three argocd env
  files) was a separate, already-merged change - confirmed deployed by the absence of
  `TimeoutException`s in PRD Loki since 2026-10-06T13:24:41Z and by `Process Partial CashOut` log
  lines succeeding afterward. Rotating the two credentials that were committed in plaintext here is
  a separate ops action, same as TD-01, tracked in DEVOPS-407. The same leaked credentials are
  still committed elsewhere: the preprod Atlas credential in `Platform.QTech.API/appsettings.json`,
  and the `sa` credential for `23.100.93.193` in 18 `appsettings.json` files under `Platform/`.
  Removing them is tracked in GM-926, under the broader TD-01.
- **Confidence:** **[Verified]** (traced in code and prod Loki; CI Test job passed on PR #168; the
  follow-up tests pass locally).

### TD-22 - `OnSportSortOrderChange` never evicts the per-client `leo-`/`seo-` rows caches (`SportsbookRepository.cs`) - **Open** (found during GM-851)
- **Evidence:** `GetLeagueOrder` caches under `leo-{clientId}` and `GetSegmentOrder` under
  `seo-{clientId}`, 24 h each. The `CacheType.SportSortOrder` branch of `DeleteCachePreload` calls
  `_memCache.DeleteCache("leo")` and `DeleteCache("seo")`, and `MemCache.DeleteCache` is an exact-key
  `MemoryCache.Default.Remove` (`Platform.Shared/Utility/MemCache.cs`), so neither matches a real key.
  The same branch does purge the list responses by prefix, so after a Sport Tree league or segment
  reorder the next request rebuilds the list from the old, still-cached rows until the 24 h TTL
  runs out or the pod restarts.
- **Files:** `Platform.Sportsbook.API/Repository/SportsbookRepository.cs` (`GetLeagueOrder`,
  `GetSegmentOrder`, `DeleteCachePreload`).
- **Why it matters for GM-851:** leagues missing from an Oddstable configuration fall back to the
  Sport Tree order, so they inherit this staleness. The Oddstable's own rows are not affected: they
  are cached as `oleo-{clientId}` and purged by exact key.
- **Not fixed in GM-851:** it changes Sport Tree behaviour and is outside that ticket. The likely fix
  is `DeleteCacheByPrefix("leo-")` and `("seo-")` in that branch.
- **Confidence:** **[Verified]** in code (`DeleteCache("leo")` against `$"leo-{...}"` keys);
  production impact **[Unknown]**, not observed.

### TD-23 - `Job.ForexEUR` worker ignored the stopping token and retried with no delay (`Worker.cs`) - **Resolved** (2026-10-14, GM-890)
- **Evidence:** `ExecuteAsync` looped on `while (true)` and its `catch (Exception e)` looped straight
  back into `ProcessForex()`. The only `Task.Delay` sat inside the `try`, so on shutdown its
  `TaskCanceledException` was swallowed and the next call failed on the disposed `HttpClient`, in a
  tight loop until Kubernetes killed the pod (Loki: 198 errors in one PRD rollout burst, 44 on
  staging, 40-130 per DEV pod). Any runtime failure of `ProcessForex()` (for example a network
  error from openexchangerates.org) would also have retried with no backoff.
- **Fix:** the loop runs while `!stoppingToken.IsCancellationRequested`, waits 1 h after a success
  and 1 min after a failure, exits on `OperationCanceledException` once stopping, and logs the
  exception object instead of `e.Message`. `ProcessForex()` itself and the 1 h schedule are unchanged.
- **Files:** `Platform.Job.ForexEUR/Worker.cs`; tests in
  `Platform.Job.ForexEUR.Test/WorkerTests.cs` (new project, in `Platform.sln`).
- **Not fixed:** `ProcessForex()` takes no `CancellationToken`, so a shutdown during an in-flight
  HTTP call or DB write still waits for that call to finish or fail. `Job.Resettlement` and `Job.CCF`
  had the same pattern and were fixed under GM-955; `Job.Operator.Score` does not.
- **Confidence:** **[Verified]** in code and by unit tests; rollout behaviour after the fix not yet
  observed in Loki.

### TD-24 - CashOutStake read OE2 odds changes with the OE v1 field names, so no cashout value was pushed from an odds change (`BettingRepository.cs`) - **Resolved** (2026-10-15, GM-902)
- **Evidence:** `Worker.MessageReceivedEvent` deserialized `OnOddsChange` into
  `OnExternalOddsChangeMessage` (`FixtureId`, `Markets`, `MarketId`, `Selections`, `Probability`, ...),
  but odds-engine-2 publishes `OnInternalOddsChangeMessage` on `r.oe.sb.engine.onoddschange` with short
  names (`FiId`, `Ma`, `MaId`, `Se`, `Pr`, ...), serialized with default `System.Text.Json` options
  (`OE.Shared/Utilities/MessageBroker.cs`). `System.Text.Json` is case-sensitive and ignores unknown
  properties, so `FixtureId` was 0 and `Markets` was empty: `ProcessOddsChange` matched no bet and
  returned without logging, no `OnCashOutChange` was published from an odds change, and the cashout
  value in My Bets changed only when a new quote was fetched. Same root cause as BO-97 in the
  backoffice WebSocket service, which that ticket fixed only there. The ticket's PRD Loki counts (not
  re-queried here): 1,131 `Add Bet` and 1,101 `Remove Bet` actions but 0 `OddsChange` actions in the 7
  days to 2026-10-06.
- **Files:** `Platform.CashOutStake.Servicev2/Repository/BettingRepository.cs`
  (`OnExternalOddsChangeMessage`, `ExternalMarketMessage`, `ExternalSelectionMessage`).
- **Fix:** each property carries `[JsonPropertyName]` with the OE2 wire name (`SpId`, `FiId`, `FiStDa`,
  `FiStId`, `FiTyId`, `IsSu`, `Ma`, `MaId`, `MaStId`, `Se`, `SeId`, `IsAc`, `Pr`, `WoPr`, `LoPr`, `VoPr`,
  `HaWoPr`, `HaLoPr`); the C# names are unchanged, so the cashout code is untouched. `FiId` is the same
  id space as the `FixtureId` stored on bet selections: the platform-web push path sets
  `FixtureId = message.FiId` (`Pages/Base/BasePage.cs:643`) and Sportsbook.API maps `FixtureId = s.FiId`.
  Covered by `Platform.CashOutStake.Servicev2.Test/Repository/OddsChangeContractTests.cs`: a payload
  shaped like OE2's is read field by field (distinct probability values, so two swapped fields
  fail); unknown properties and OE2's `FixtureType.Unknown = -1` do not break it; every public
  property of the three classes must carry an explicit wire name; and an odds change for a fixture
  with an open bet publishes the recalculated value (76.5 for a stake of 100 at odds 2.0), or
  Unavailable when the fixture is suspended. 8 of the 10 new tests fail on the previous code.
- **Effect once deployed:** every OE2 odds change for a fixture with an open bet now runs
  `ProcessCashOut` per bet, which logs one `OddsChange` line with the full payload and publishes one
  `OnCashOutChange`. Expect more log volume and publish traffic in PRD; the size is **[Unknown]**.
  The GM-901 Mongo backoff (TD-21) is what keeps the cache-miss lookups from blocking the consumer.
- **Not changed:** `Platform.Job.Operator.Score/Repository/BettingRepository.cs` has its own copy of
  `OnExternalOddsChangeMessage` with the v1 names, but nothing in that project deserializes it. A
  single OE2-shaped class in `Platform.Shared` was not used: it rebuilds every image and changes
  `message-contracts.approved.txt` for a one-service bug. `r.oe.bet.settlement` is not affected: OE2
  publishes it with the long names.
- **Confidence:** **[Verified]** in code and by unit tests; PRD behaviour after the fix not yet
  observed in Loki. The DEVOPS-409 cross-repo checker `CashOutStake_OnOddsChange_ReadsEveryField`
  (qa-robot-automation, not read here) should report `NOW PASSES` after deploy **[Unknown]**.

### TD-25 - A boosted bet was refused with "approve any odds change": the boost was only applied behind `IsOEEnable` (`SportsbookRepository.cs`) - **Resolved** (2026-10-27, BO-10)
- **Evidence:** the site sends a boosted bet with `Odds = BoostedOdds` and `IsBoosted = true`;
  `PlatformRepository.PlaceBet` forces `OddsChangeType.None` for it and asks Sportsbook.API `GetBetFixture`.
  `GetBetFixtures2` priced each selection at its original odds, because the only code that swaps in the boosted
  price (`GetClientBoostedOdds`) sat inside `if (IsOEEnable(clientCode))` and `IsOEEnable` is hardcoded
  `return false` ("disable Margin since we are now using bet genius"). `GetBetSelectionStatus` then compared the
  boosted request price with the original odds, answered `Odds_Or_Lines_Have_Changed` with the original odds,
  and the slip fell back to them and asked to "approve any odds change". Seen on DEV on 2026-10-27 (1.64 sent,
  1.5 returned) once odds-engine-2 started stamping `ClBoOd`. The bet would also have been stored at the
  original odds: `PlatformRepository.PlaceBet` takes `DecimalOdds` from the same `GetBetFixture` result.
- **Files:** `Platform.Sportsbook.API/Repository/SportsbookRepository.cs` (`GetBetFixtures2`,
  `GetClientBoostedOdds`), new `Platform.Sportsbook.API/Utility/BoostedOddsPricing.cs`.
- **Fix:** when `IsOEEnable` is false and the request is boosted, `GetBetFixtures2` prices the selections at the
  client's boosted odds (`BoostedOddsPricing.Apply`; `OddsRef` keeps the original). `GetClientBoostedOdds` now
  delegates to the same class: the client is matched on its whole code (it was `Contains`, so a client whose code
  is part of another's read that client's boost), a malformed entry is no boost instead of an exception, and
  the odds are parsed with the invariant culture. The three in-memory `ClBoOd.Contains(clientCode)` filters of
  `GetBoostedMarkets` use the same match. Covered by `Platform.Sportsbook.API.Test/Utility/BoostedOddsPricingTests.cs`.
- **Flag for later, original odds vs client odds:** a boost replaces the ORIGINAL odds. If clients are ever priced
  with CLIENT odds (`IsOEEnable` can return true, margin from `ClCoMa`), the branch that already boosts over
  client odds runs instead and what a boost means against a client price has to be decided again: the boosted
  odds set in Backoffice, the `OE.Job.BoostedOddsCap` comparison (odds-engine-2 recomputes client odds from
  `ClCoMa`) and the stale-odds check of a boosted bet. `BoostedOddsPricingTests.ClientOddsTripwire_...` fails
  in CI the day `IsOEEnable` can return true, or is renamed or removed.
- **Not changed (still open):** the `Fixture.ClBo` / `Market.ClBo` filters of `GetBoostedFixtures` are
  Mongo-side `Contains(clientCode)` and still match by a part of a code; `GetMaxStake` still takes the odds from
  the request (`PlatformRepository.cs`, `DecimalOdds = request.IsBoosted == true ? s.Odds : ...`), so a client
  can still choose the price the max stake is computed on (the bet itself is now priced by the server);
  live `OnBetslipOddsChange` pushes to a slip holding a boosted selection were not traced.
- **Confidence:** **[Verified]** in code and by unit tests (the Mongo-reading `GetBetFixtures2` itself has no
  unit test: it needs a live Mongo); behaviour on DEV after deploy **[Unknown]**.

### TD-26 - Genius Sports rejected every boosted bet: its calls were sent the boosted price (`GTSRepository.cs`) - **Resolved** (2026-10-27, BO-10)
- **Evidence:** once TD-25 let a boosted bet through the odds check, DEV rejected it with "Current price 1.50
  differs from original 1.64" (the bet's rejection reason; the betting-api log showed the call to the GTS UAT
  `/assessment` endpoint answering 422 at 13:22:10 UTC). `GTSRepository.Assessment` sent `price = s.BetSelection.Odds`,
  which for a boosted bet is the boosted price the player is paid, with the player's `priceChangeRule` (`AcceptNone`
  for no auto-accept option). A normal bet at 1.50 on the same selection was accepted. There was no boost handling
  for GTS at all, unlike `MtsBetProvider.cs:381-422`, which sends the original odds (`OddsRef`) as the leg odds, the
  boosted price with `SetBoostedOdds` and the difference as an "OddsBooster" bonus. `GetSelectionPayout` computes an
  `isBoosted` flag it never uses.
- **Three calls carry a leg price, and all three sent the boosted one** (`GTSRepository.cs`): `GetMaxStake`
  (`price = s.DecimalOdds`, rule `AcceptNone`, posted to the same `/assessment` URL, asked first by
  `BettingRepository.PlaceBet`), `Assessment` (`price = BetSelection.Odds`) and `SendBet` (`price = s.Odds`, the
  bet feed). `GetMaxStake` answers a non-success response with a max stake of 0 and no log line, and
  `GetMaxStakeForSingleBetWithMTS` reads a provider max stake of 0 as "no provider limit", so its refusal was
  silent: that is why the betting-api log had no "Assessment" or error line. The refusal text came from the
  assessment of the placement (a different service from betting-api). For a boosted bet the stake limit is the
  price boost config anyway (`BettingRepository` takes `priceboostConfig.MaxStake` last).
- **Files:** `Platform.Shared/Repositories/GTSRepository.cs` (`GetMaxStake`, `Assessment`, `SendBet`, new
  `GetProviderPrice`).
- **Fix:** for a boosted bet (`BetRequestDTO.IsBoosted` for the max stake, `OnBetPlacement.IsBoosted` or
  `Bet.BetTypeId == PriceBoost` for the assessment, `Bet.BetTypeId == PriceBoost` for `SendBet`) the leg carries
  the original odds (`OddsRef`, which TD-25 keeps on the selection); a bet with no reference price falls back to its
  odds. What the bet pays does not change: `payoutPrice` of a settled leg is still computed from the boosted odds.
  `priceChangeRule` and the cashout assessment (`AcceptAny`, quote-time price) are unchanged. Covered by
  `Platform.Shared.Test/Repositories/GTSRepositoryAssessmentPriceTests.cs`.
- **Flag, original odds vs client odds:** `OddsRef` is the original odds only while Sportsbook.API prices clients
  with the original odds (`IsOEEnable` off). Under client odds it holds the client's price, so what Genius Sports is
  shown for a boosted bet has to be decided again. The tripwire in `BoostedOddsPricingTests` names this.
- **Open / unknown:** the Genius Sports API specification is not in our repos and its Confluence pages are behind a
  partner login, so these are assumptions to confirm with Genius Sports: that the bet feed wants the placement
  `price` (the original one) with the boosted payout in `payoutPrice` for a settled leg, and how the liability
  of a boosted bet is computed (their risk view sees the original price, not what the bet pays). A
  "1026 - Failed sending Bet to GTS" on the next DEV boosted bet would show a wrong assumption. The Max Bet button
  (`PlatformRepository.GetMaxStake`) builds its selections without `OddsRef`, so it still sends the boosted price
  and gets the silent 0 described above; the stake limit of a boosted bet is the price boost config, so nothing is
  lost today **[Verified]** in code, not observed.
- **Confidence:** **[Verified]** in code and by unit tests; behaviour on DEV after deploy **[Unknown]**.

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
