# Best Practices (Observed Conventions)

> Phase 8. The conventions this team applies **consistently and deliberately** — the "house rules" to
> follow when adding code so it matches the codebase. **Descriptive** (what the team does well /
> consistently), not external recommendations. Confidence **High [Verified]**. Counterpart:
> [AntiPatterns.md](AntiPatterns.md).

## 1. Always go through the repository + uniform envelope `[Verified]`
Data/integration access is **always** an `IXxxRepository`+`XxxRepository` pair returning
`Task<BaseResponseDTO<T>>` (business `StatusCode` + Message + Result). Controllers stay thin: open an
`Activity`, call the repo, `switch` on `StatusCode`, return `Ok`/`BadRequest`. **Follow this** — don't
put data access or business branching in controllers. See [RepositoryPatterns.md](RepositoryPatterns.md).

## 2. Reuse the shared kernel; group DI with extension methods `[Verified]`
Common contracts/entities/utilities live in `Platform.Shared` and are reused, not re-implemented.
Cohesive registration sets are exposed as `IServiceCollection` extension methods
(`AddBettingProviders`, `AddProviders`, RedCat `ServiceExtension`, `AddTelemetry`). **Add new provider
sets the same way.** See [SharedLibraries.md](SharedLibraries.md), [DependencyInjection.md](DependencyInjection.md).

## 3. Extend providers via the factory, not call-site `if`s `[Verified]`
New bet/livestream providers implement `IBetProvider`/`ILiveStreamProvider` with a distinct
`ChannelType` and register; `IBetProviderFactory.Resolve` dispatches. No channel `switch` at call sites.

## 4. Cache reads through `IMemCache.GetCache(key, loader, ttl)` `[Verified]`
Read-through caching with a prefixed key and explicit TTL is the standard for tokens/lookups/catalogue.
Invalidation is event-driven via `CacheSync.Service`. **Use the prefix + `DeleteCacheByPrefix`
convention** so eviction works. See [Caching.md](Caching.md), [CacheInvalidation.md](CacheInvalidation.md).

## 5. Parameterize SQL; use stored procs for money `[Verified]` (mostly)
Wallet mutations and top-market/running-balance reads use **stored procedures** with `SqlParameter`/
positional `FromSqlRaw("proc {0}", arg)`. Money is always `decimal(26,12)`. EF reads use `AsNoTracking`.
Multi-write operations run in a `TransactionScope` (ReadCommitted). **Match this** — and parameterize
(the AgentReports string-interpolation is the exception to avoid, see [AntiPatterns.md](AntiPatterns.md)).

## 6. Treat Mongo as a read model; project, don't fetch whole docs `[Verified]`
Read-only `.Find(filter).Project<TProjection>(...)`; use the **secondary** read preference for
high-volume catalogue reads, primary where freshness matters. Never write Mongo from platform-final.
See [MongoDb.md](MongoDb.md).

## 7. Instrument everything: OTel + Serilog + trace propagation `[Verified]`
Register `Instrumentation` + `AddTelemetry`; open an `Activity` per request/handler and `SetTag`/
`SetStatus`; log via Serilog console-JSON; **propagate W3C trace context through RabbitMQ headers** so
async hops stay correlated. **Wire telemetry on new services** (two client APIs that skipped it are the
gap, not the norm). See [Logging.md](Logging.md).

## 8. Gate the edge with an ActionFilter `[Verified]`
Every controller carries `[ServiceFilter(typeof(<Filter>))]` (API-key / client / pass-key). **Never add
a controller without one** — there is no global fallback policy. See [Authorization.md](Authorization.md).

## 9. Messaging: explicit contracts, manual ack where loss matters, idempotent retries `[Verified]`
Define `On*` POCO contracts; consume via the `MessageReceived` event + `switch(MessageType)`; use
**manual ack** (`ConnectWithManualAck`) for loss-sensitive consumers (BetInfo, Placement, AgentReports);
carry a `CorrelationId`; retry failed settlements (`Job.Resettlement`, `IsResettled`,
`OperatorSettlementRetry`). See [ImplementationPatterns.md](ImplementationPatterns.md) §8.

## 10. Append-only audit + full-replace curation `[Verified]`
Every bet state change snapshots `BetAudit`/`BetSelectionAudit`; cashouts append `PartialCashoutAudit`.
Curation writes **delete-the-scope-then-insert** inside a transaction. **Preserve the audit trail**
when touching bet writes. See [EntityLifecycle.md](EntityLifecycle.md), [PersistencePatterns.md](PersistencePatterns.md).

## 11. Make services resilient to dependency startup order `[Verified]`
`MessageBroker` uses heartbeat + network-recovery (auto-reconnect); `HttpClient`s are lazy so missing
downstreams fail per-request, not at boot. Workers preload state in `ExecuteAsync`. **Don't add
blocking boot-time dependency checks** — the platform expects independent container start. See
[StartupFlow.md](StartupFlow.md).

## 12. Keep config externalizable; flags as `Custom:*` keys `[Verified]`
Read config via `IConfiguration["A:B"]`; expose toggles as `Custom:*` booleans; let the cluster inject
real values via SSM/`Section__Key` env vars. **Don't hardcode endpoints/keys** (the load-test console
is the counter-example to avoid). See [Configuration.md](Configuration.md).

## 13. Render perf (UI tier, platform-web) `[PW]`
Gate component re-render with `ShouldRender`/`_shouldRender` + `ValidateParameters`, and `<Virtualize>`
long lists, so the high-frequency odds feed only repaints changed cells. See [Rendering.md](Rendering.md).

---
**How to use this doc:** before adding code, find the matching practice above and mirror it. If a
practice doesn't fit, check [ImplementationPatterns.md](ImplementationPatterns.md) for the established
pattern rather than introducing a new one (per the team's convention of consistency over novelty).
