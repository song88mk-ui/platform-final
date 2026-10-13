# Cache Invalidation

> Phase 6. How caches are kept fresh. Confidence **High [Verified]**. Builds on
> [Caching.md](Caching.md) and the messaging model in [DependencyGraph.md](DependencyGraph.md) §3.

## The model: event-driven eviction + TTL fallback `[Verified]`
platform-final caches are **per-pod** (`MemoryCache.Default`, see [Caching.md](Caching.md)), so they
cannot be invalidated by a direct call across replicas. Instead, **config/curation changes are
broadcast over RabbitMQ**, and **every pod's `CacheSync.Service` evicts locally**. TTL expiry is the
backstop if a message is missed.

```
Back office writes config/curation (SQL) ─▶ publishes On*Change / OnDeleteCacheMessage (e.central)
        │ (backoffice-final via RabbitMqApi)                         │ fan-out to every pod
        ▼                                                            ▼
  CacheSync.Service (per pod) ── switch(MessageType) ── IMemCache.DeleteCache / DeleteCacheByPrefix
        │
        └─ next read repopulates via read-through (GetCache(key, loader, ttl))
```

## `CacheSync.Service` — the invalidation worker `[Verified]`
Consumes **8 message types** and evicts the corresponding cache entries (Phase 1; the eighth added
by GM-851):
`OnMarketSortOrderChange`, `OnSportSortOrderChange`, `OnOddstableLeagueSortOrderChange`,
`OnUpdateUserFavorites`, `OnMaintenance`, `OnParlayBonusChange`, `OnClientChange`,
`OnDynamicTranslationChange`. It also handles
`OnDeleteCacheMessage` (generic eviction by key / prefix / purge-all). Calls
`ICacheSyncRepository`/`ISportsbookRepository` + `IMemCache`. **[Verified]**
(`OnTranslationChange` and `OnStaticTranslationChange` are handled in `Worker.cs` too; this list
predates them.)

### Oddstable league order (GM-851) `[Verified]`
The Early, Today and Live fixture lists order their leagues from `ClientOddstableLeagueOrder`
(written by backoffice-final, BO-107), not from the Sport Tree's `ClientLeagueOrder`. A League
Sorting save or copy publishes `OnDeleteCacheMessage` as message type
`OnOddstableLeagueSortOrderChange` on routing key `r.oe.cachesync.onoddstableleaguesortorderchange`
(`CacheType.OddstableLeagueSortOrder = 9`, `ClientId` set). `CacheSync.Service`
(`ProcessOddstableLeagueSortOrderChange`) forwards it to every Sportsbook.API pod as
`DeleteCachePreload` with `CacheKey` = the client id, and the pod runs
`OddstableLeagueSortOrder.PurgeCache`:

- the cached rows, `oleo-{clientId}` (24 h, exact key);
- that client's list responses only: `nlf`, `nlfbl`, `ntf`, `ntfbl`, `nuf`, `nufbl`, `ndf`, `ndfbl`
  keys of the form `<prefix>:{CLIENTID}:`. A message without a usable client id purges every
  client instead. (`nuf:` is shared with the user-favourites cache, so that client's favourites
  are dropped too; they reload on the next read.)

Unlike `OnSportSortOrderChange`, which purges those prefixes for every client, one client's save
leaves other clients' lists cached. If the rows cannot be read (for example the table is missing in
an environment), `GetOddstableLeagueOrder` logs the error and falls back to the Sport Tree order for
a minute instead of failing the lists; see also [TD-22](TechnicalDebt.md).

### Order stamped on pushed fixtures (GM-851) `[Verified]` / `[Verified-sibling]`
A league that first appears through a SignalR push (it goes live after the page loaded) is placed by
the fixture's `SoOr`, not by a list response. `OE.Websocket.Service` (odds-engine-2) sets `SoOr` per
client from a league order it loads by calling Sportsbook.API `GetClientFixtureSortOrder` (through
Platform.API `GET v1/sportsbook/fixturesortorder`) and caches under `{clientCode}:fso:{sport}:{segment}:{league}`
for a year, defaulting to 1000 for a league it has no row for. That endpoint used to return only the
Sport Tree order, so pushed leagues ignored the Oddstable order. It now returns the same order the lists
use (`OddstableLeagueSortOrder.BuildClientFixtureSortOrder`): Sport Tree rows, then tier rows plus 1000,
then, for a sport with Oddstable rows, every row moved to its Oddstable position and any configured
league without another row added.

Reload on a Back Office save: after the purge succeeds, `ProcessOddstableLeagueSortOrderChange`
publishes `OnOddstableLeagueSortOrderChange` to routing key
`r.oe.sb.engine.onoddstableleaguesortorderchange.<clientcode>` (payload `true`, as
`OnMarketSortOrderChange` does). The websocket service reloads that client's `fso` entries when it
receives it, which needs the routing key in its `MessageBroker__RoutingKey` (argocd,
`apps/oddsengine/websocket-service/{dev,stg,prd}.yaml`). No relay is sent if a purge call failed, because the
reload would read the old order again, or if the message carries no client code. CacheSync's own
binding is `r.oe.cachesync.#`, so it does not consume its own relay.

Limits: before the reload existed, and for any service version without the handler, the websocket
service only loaded the order at startup and for clients it newly picks up (its 5 minute client
refresh). It still does not handle `OnSportSortOrderChange`, so a Sport Tree order change reaches
pushed `SoOr` values only after a restart. A league with no row anywhere gets 1000 in a push but
`last configured + 1000` in a list. The websocket service reads `fixturesortorder` through Platform.API,
so deploy this repo before the odds-engine-2 change is relied on, and restart the websocket service once
after deploying it so the first load uses the new endpoint.

## Eviction primitives `[Verified]`
- **`DeleteCache(key)`** — exact-key removal (e.g. `cc:{clientId}` on `OnClientChange`).
- **`DeleteCacheByPrefix(prefix)`** — removes all keys starting with a prefix (e.g. all entries for a
  cache-type/sport) — case-insensitive `StartsWith` over `MemoryCache.Default`.
- TTL expiry (hours / token-lifetime) — the fallback when no event arrives.

## `OnDeleteCacheMessage` contract `[Verified]` / `[Verified-sibling]`
Carries `ClientId`, `ClientCode`, `CacheType`, optional `CacheKey`/`CacheKeys`, and `PurgeAll`. Both
platform-final (`Platform.Shared/MessageModels/OnDeleteCacheMessage.cs`) and odds-engine-2 define
their own copy — interop by convention (see [Contracts.md](Contracts.md)).

## Who triggers invalidation `[Verified]` / `[Verified-sibling]`
- **backoffice-final**: after writing curation/config (top fixtures/leagues, ordering, announcements,
  parlay bonus, client), it **publishes invalidation messages** via `RabbitMqApi`. **[Verified-sibling]**
- **platform-final internal endpoints**: `InternalController` cache ops (e.g.
  `DELETE v1/cache/client/{clientId}`, `v1/cachepreload`) and config writes also drive eviction. **[Verified]**
- **odds-engine-2**: emits `OnDeleteCacheMessage` for its own cache concerns. **[Verified-sibling]**

## Cross-tier invalidation `[Verified]`
There are **three cache tiers** to keep coherent on a change:
1. **platform-final per-pod `IMemCache`** — evicted by `CacheSync.Service` (above).
2. **`ClientSportsbook.API` Redis** — per-client; its **write/refresh path is external/unverified**
   (U-27); invalidation of Redis from `CacheSync` was **not observed** — **[Unknown]**.
3. **platform-web 30 s `IMemoryCache`** — **time-based only** (no event invalidation); a change can
   take up to 30 s to surface to players regardless of backend eviction. **[Verified-sibling]**.

So end-to-end propagation latency for a curation change ≈ (publish + per-pod evict) **+ up to 30 s**
for the platform-web tier. **[Verified]** / **[Verified-sibling]**.

## Real-time data is not "invalidated" — it is pushed `[Verified]`
Live odds/bet-stop/status do **not** go through cache invalidation. They are pushed over SignalR
(`OnOddsChange`, etc.) and applied directly to the in-memory `FixtureModel` in platform-web (see
[WebSocket.md](WebSocket.md), [StateFlow.md](StateFlow.md)). Cache invalidation governs **config/
curation/translation/auth** data, not the live feed.

## Maintenance & bet-stop flags `[Verified]`
`OnMaintenance` invalidates cached maintenance state; the authoritative flags live in the Mongo
`Maintenance`/`BettingStatus` collections read per request. A global bet-stop is both a Mongo flag and
a `OnBetStopGlobal` push.

## Risks (see [TechnicalDebt.md](TechnicalDebt.md))
- **No invalidation acknowledgement / retry**: if a pod misses an eviction message (restart, broker
  blip), it serves stale config until TTL. No distributed cache eliminates this. **[Verified]**
- **Redis tier invalidation path unverified** (U-27). **[Unknown]**
- **platform-web 30 s tier** adds fixed staleness on top of backend eviction. **[Verified-sibling]**
