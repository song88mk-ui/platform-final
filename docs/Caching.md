# Caching

> Phase 6. Every caching layer platform-final uses, plus the client-side storage tiers (platform-web).
> Confidence **High [Verified]** (read from `MemCache.cs`, `RedisCache.cs`, repositories) unless noted.
> Invalidation is its own doc: [CacheInvalidation.md](CacheInvalidation.md). State tiers overview:
> [StateFlow.md](StateFlow.md).

## Layers at a glance
| Layer | Where | Scope | Backing | Used for |
|-------|-------|-------|---------|----------|
| **`IMemCache`** | every API/service | **per-pod, process-global** | `System.Runtime.Caching.MemoryCache.Default` | tokens, client lookups, catalogue reads |
| **Redis** | `ClientSportsbook.API` only | per **client-code** instance | `StackExchange.Redis` | client sportsbook reads |
| **Worker in-memory** | `CashOutStake.Servicev2` | per process | plain fields | active-bet cashout stakes |
| **Browser/Session/Local storage** | platform-web (Blazor) | per circuit/browser | `ProtectedSessionStorage` etc. | UI prefs (line-view), session |

## `IMemCache` — the primary cache `[Verified]`
`Platform.Shared/Utility/MemCache.cs` wraps the **static singleton `MemoryCache.Default`**:
- `AddOrUpdateCache<T>(key, obj, int hours)` — absolute expiry `Now.AddHours(hours)`.
- `AddOrUpdateCache<T>(key, obj, DateTimeOffset)` — absolute expiry at a timestamp.
- `GetCache<T>(key)` — simple get (null if absent/expired).
- `GetCache<T>(key, Func<T> loader, int hours)` — **lazy-load**: on miss, call `loader`, cache result.
- `GetCache(key) : KeyValuePair<bool,long>` — integer-value cache (present-flag + value).
- `DeleteCache(key)`, `DeleteCacheByPrefix(prefix)` (iterates `MemoryCache.Default` keys,
  case-insensitive `StartsWith`).

**Critical properties [Verified]:**
- Registered `AddSingleton<IMemCache, MemCache>()`, **but `MemoryCache.Default` is a process-wide
  static** — so *every* `MemCache` instance in a pod shares **one** cache, and **each pod/replica has
  its own** independent cache. There is **no distributed cache**; cross-pod consistency relies on the
  RabbitMQ invalidation broadcast (see [CacheInvalidation.md](CacheInvalidation.md)).
- TTL granularity is **whole hours** for the int overload (a coarse default).

### Cache-key catalogue `[Verified]`
| Key / prefix | TTL | Owner | Contents |
|--------------|-----|-------|----------|
| `cc:{clientId}` | 24 h | `AuthorizationFilter` (ClientSportsbook) | Guid→ClientCode (active client) |
| `mts:auth` | 55 min | `MTSAuthRepository` | MTS OAuth token (expires 60) |
| `bg:auth` | (token TTL) | `BetGeniusRepository` | BetGenius auth token |
| catalogue/lookup keys | varies | Sportsbook/Betting repos | sports tree, market templates, translations, configs |
| cache-type prefixes | — | `CacheSync.Service` | evicted by `DeleteCacheByPrefix` on `On*Change` |

> platform-web has its **own** 30-second `IMemoryCache` in front of these endpoints (sports/templates/
> bet-config/favorites/translations) and a 3 s bet-details cache — a **second cache tier** outside
> platform-final. **[Verified-sibling: platform-web]** — see [RequestLifecycle.md](RequestLifecycle.md).

## Redis — client sportsbook only `[Verified]`
`Platform.ClientSportsbook.API/Repository/RedisCache.cs`:
- **Multiple Redis instances keyed by client code**: reads `ConnectionStrings:RedisInstances`
  (a section of `code → connection string`), builds a `Dictionary<clientCode, IDatabase>` at
  construction (singleton), connecting each via `ConnectionMultiplexer.Connect`.
- Lookup: `GetValue(clientCode, key)` → `redisDbs[clientCode.ToUpper()].StringGetAsync(key.ToLower())`.
  **Keys are lowercased; client codes uppercased.**
- **Read path only** observed here (`StringGetAsync`); **who writes/populates the Redis values is
  not in this repo** — **[Likely]** an external/odds-engine process populates per-client Redis. **[Unknown]** writer.
- Registered `AddSingleton<RedisCache>()`. Provisioning/host of the Redis instances is **[Unknown]**.

## Worker in-memory cache `[Verified]`
`CashOutStake.Servicev2` **preloads active bets** on startup and maintains live cashout stakes in
process memory, updated by `OnOddsChange`/bet events — a hot in-RAM working set, not a general cache.
See [StartupFlow.md](StartupFlow.md).

## Client-side storage (platform-web) `[Verified-sibling]`
The brief's "Browser/Session/Local Storage" live in **platform-web**, not platform-final:
- **`ProtectedSessionStorage`** — persists the user's active **line-view preference** / active scope
  across reloads within a session.
- **Blazor circuit state** (scoped DI containers) is the de-facto session store — see
  [StateFlow.md](StateFlow.md) Tier 1.
- No evidence of `localStorage`-based persistence of platform data beyond UI prefs; tokens are
  **never** stored client-side (server-side BFF). **[Verified-sibling]**.

## Cache strategy summary `[Verified]`
- **Read-through / lazy-load** is the dominant pattern (`GetCache(key, loader, ttl)`): fetch from
  source on miss, cache, serve from cache until TTL or eviction.
- **Auth tokens** cached just under their real expiry (55 min for a 60-min MTS token).
- **Tenant resolution** (`cc:{clientId}`) cached 24 h — long-lived, invalidated on `OnClientChange`.
- **Catalogue** cached short, refreshed by feed-driven invalidation + platform-web's 30 s tier.

## Risks (see [TechnicalDebt.md](TechnicalDebt.md))
- **Per-pod caches** + broadcast invalidation: a missed/failed invalidation message leaves a pod
  serving stale data until TTL. No distributed cache to guarantee coherence. **[Verified]**
- Hour-granularity TTLs are coarse for fast-moving catalogue data (mitigated by event invalidation).
- Redis value **writer is unverified** from this repo (U-27, [Unknowns.md](Unknowns.md)).
