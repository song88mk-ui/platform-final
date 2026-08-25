# Anti-Patterns (Observed)

> Phase 8. Recurring problematic patterns **present in the code** — documented so they are recognised
> and **not propagated** (the brief asks to document existing conventions, including the bad ones).
> Each notes evidence + impact; remediation direction lives in [TechnicalDebt.md](TechnicalDebt.md)
> (cross-referenced by ID). Confidence **High [Verified]** unless noted.

## Security
### AP-1 — Committed plaintext secrets (TD-01)
Base `appsettings.json` files carry DB passwords / API keys / partner creds. In-cluster they are
overridden by SSM, so these are dev/staging fallbacks — but they ship in the image and live in git
history. **[Verified]**

### AP-2 — String-interpolated `FromSqlRaw` (SQL injection) (TD-01b)
`AgentReports.API/ReportsRepository` builds raw SQL by interpolating params into the string passed to
`FromSqlRaw` (6 report procs). Contrast the parameterized calls in `Wallet.API`/`Betting.API`.
**[Verified]**

### AP-3 — Exception detail leaked to clients (TD-04)
Several controller/repository catches return `ex.Message`/`ex.StackTrace` inside `BaseResponseDTO`.
**[Verified]**

### AP-4 — Hardcoded credentials/endpoints (TD-03)
`BetPlacement.Console` hardcodes a staging gateway URL + `x-client-id`/`x-operator-token`/`x-api-key`/IP.
**[Verified]**

### AP-5 — Over-permissive edge defaults
`AllowAllOrigins` CORS (AnyOrigin/Method/Header) + `ForwardedHeaders` trusting all upstreams
(KnownProxies cleared) + Swagger exposed in all environments. Acceptable only behind the controlled
ingress. **[Verified]** (TD-12)

### AP-6 — No global authorization fallback
Auth is opt-in per controller via `[ServiceFilter]`; a controller that omits it is **completely open**
(`UseAuthorization()` is inert). Coarse shared internal key opens all back-office endpoints. **[Verified]**

### AP-7 — Unguarded header parsing in `APIKeyFilter`
OG `x-og-*` headers are parsed with `int.Parse`/`long.Parse`/`bool.Parse` without null/format guards →
a malformed header throws → bare 500 (no exception middleware). **[Verified]**

## Architecture / structure
### AP-8 — Duplicated/parallel implementations (TD-07)
`WebSocket.API` vs `Websocket.API2`; `*.Service` vs `*.Servicev2`; `oddsengine` vs `odds-engine-2`;
and a **duplicated `IMessageBroker`/`MessageBroker`** copied into some services' own `Utility/`
(e.g. `BetStatus.Service`) instead of using `Platform.Shared`'s. Unclear source of truth; double
maintenance. **[Verified]**

### AP-9 — Convention-only cross-repo contracts (no shared types/tests)
Message contracts, routing keys, Mongo/SQL schemas, and SignalR method names are **duplicated per
repo** and aligned only by field-name/key convention — no shared package, no compiler check, no
contract test. A rename is an up-to-4-repo silent breaking change. **[Verified]** — [Contracts.md](Contracts.md).

### AP-10 — Logic in repositories instead of services
Business logic concentrates in large repository classes (`PlatformRepository`/`BettingRepository` run
to thousands of lines) rather than focused domain services — only `ParlayBonusService` is a true
domain service. Hard to unit-test. **[Verified]/[Likely]** (size observed).

### AP-11 — No `IOptions<T>` / silent config typos
App config is read via `IConfiguration["A:B"]` string indexer; a mistyped key returns `null` with no
startup validation. **[Verified]**

## Runtime / concurrency
### AP-12 — Hand-rolled 100-thread pools (TD-06)
`CashOut.Service`, `Placement.Servicev2`, `Operator.Servicev2`, `Operator.Cashout.Service`,
`Client.TopMarket` create raw `new Thread` + `BlockingCollection` round-robin instead of
`System.Threading.Channels` / consumer concurrency. Hard to tune/reason about. **[Verified]**

### AP-13 — Singleton repo holding a scoped `DbContext` (potential)
Worker services register repositories as Singleton while the hosted `Worker` is singleton; a repo that
caches a `DbContext` would be an EF lifetime hazard. Verify each worker creates a context per message.
**[Likely]** — [PersistencePatterns.md](PersistencePatterns.md).

### AP-14 — Per-pod caches with no distributed coherence
`MemoryCache.Default` is per-pod; cross-pod consistency depends entirely on the RabbitMQ eviction
broadcast — a missed message serves stale data until TTL. Hour-granularity TTLs are coarse.
**[Verified]** — [Caching.md](Caching.md).

### AP-15 — No global exception handler
Only `UseDeveloperExceptionPage` (dev). Error shaping depends on per-controller `try/catch`; an
unguarded action leaks a bare 500. **[Verified]**

## Platform / lifecycle
### AP-16 — EOL runtime + build/runtime SDK mismatch (TD-02/TD-05) — **Resolved** (2026-08-20, GM-744)
~~About 36 projects on EOL `netcoreapp3.1`; Docker builds on SDK 6.0 but runs on aspnet 3.1; ad-hoc partial
moves to net7/net8.~~ 36 projects now target `net10.0`; Platform.Shared + 3 Mongo services target `net8.0` (MongoDB.Driver 2.11.6 incompatible with net10 - see TechnicalDebt); EF Core 8.0.30 unified.

### AP-17 — Shipping integrations absent from CI (TD-09)
QTech/RedCat*/Oddin/AgentReports*/ESportOperator have no CI image build;
deploy path for some is unverified. **[Verified]/[Unknown]** (Note: `Operator.Cashout.Service`
**is** in the CI matrix; `Cancellation.Service` is a bin/obj-only orphan dir, not a project.)

### AP-18 — Minimal automated tests
Only `Platform.Shared.Test` and `Platform.Sportsbook.API.Test` (both run by a PR-only CI test job);
broad coverage not evident — refactoring safety relies on the `/Reference`
snapshot + manual care. **[Likely]**

## Hygiene
### AP-19 — Misspelled identifiers baked into contracts
`InserOrUpdateClientMarketSortOrder`/`…TemplateGroup` (procs), `Custom:Fibonnaci`,
`ProcesssBetPlacement`, `OnBOBetCancellaton`, `OnMarketDeactivationMesssage`, filename
`ParlayBonusService .cs` (trailing space). These are **real identifiers** — fixing requires
coordinated schema/contract changes, don't rename casually. **[Verified]**

### AP-20 — UI: JS-global `DotNetObjectReference` coupling `[PW]`
platform-web stores component refs in JS globals keyed by method name, coupling `jsUtilities.v33.js`
to C# method names with no compiler check (UI analogue of AP-9). **[PW]** — [JavaScriptInterop.md](JavaScriptInterop.md).

---
> These are recorded as **observed reality**, not endorsed patterns. New code should follow
> [BestPractices.md](BestPractices.md) / [ImplementationPatterns.md](ImplementationPatterns.md) and
> avoid extending the above. Remediation priorities: [TechnicalDebt.md](TechnicalDebt.md).
