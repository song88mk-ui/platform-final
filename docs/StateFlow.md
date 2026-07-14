# State Flow (End-to-End)

> Phase 4. How state lives and propagates across the path. Three distinct "state" tiers, each in a
> different place. Provenance: **[PW]** platform-web (client/session UI state), **[PF]** platform-final
> (server caches + datastores + real-time fan-out), **[OE]** odds-engine-2.

## The three state tiers
```
┌─ UI / session state ───────────────┐   ┌─ Server runtime state ─────────────┐   ┌─ System of record ─┐
│ platform-web, per Blazor circuit    │   │ platform-final, per service        │   │ datastores          │
│ scoped DI state containers [PW]     │   │ IMemCache (in-proc) + Redis [PF]   │   │ Mongo + SQL [PF]    │
│ + in-memory FixtureModel lists      │   │ + worker in-memory caches          │   │ + odds-engine-2 [OE]│
└─────────────────────────────────────┘   └─────────────────────────────────────┘   └─────────────────────┘
```

## Tier 1 — platform-web UI/session state (per circuit) **[PW]**
- **Pattern:** Blazor Server **scoped DI** state containers; **no Redux/Flux/MediatR**. One instance
  of each per circuit (browser tab). `BaseState<T> : INotifyPropertyChanged`; setting `.Model` raises
  `PropertyChanged`.
- **Containers:** `OperatorState` (ClientId, CurrencyCode, LanguageCode, UserId, MinuteOffset, auth
  flags), `TranslationState` (`Dictionary<string,string>` + `GetValue`), `PageState` (URL, active
  page, layout, sidebar), `DeviceState` (mobile/medium/large).
- **Propagation:** only **layout** components subscribe to `PropertyChanged` →
  `InvokeAsync(StateHasChanged)`; pages/children read state by injection and re-render via the
  layout cascade. Handlers detached on `Dispose`.
- **Non-container state:** the **fixture lists / betslip / MyBets** live as **instance fields** on
  the page (`List<FixtureModel>`), mutated in place by SignalR handlers + `Counter` bumps.
- **Lifetime:** reset to fresh instances in `LayoutBase.InitializeStates()`; destroyed when the
  circuit ends. **Nothing persists server-side per user** except `ProtectedSessionStorage`
  (line-view preference) and `ConnectedUserList` (a **singleton** connection counter for the
  `MaximumConnections` cap).

### Tier-1 update patterns **[PW]**
1. **SignalR push → model → re-render:** hub handler mutates a `FixtureModel`, `InvokeAsync(StateHasChanged)`.
2. **API fetch → state → re-render:** repo call result assigned to `TranslationState.Model` /
   `OperatorState.Model`, `PropertyChanged` cascades.
3. **JS interop → state:** screen size / timezone offset → `DeviceState` / `OperatorState`.

## Tier 2 — platform-final server runtime state **[PF]**
- **`IMemCache`** (in-process `MemoryCache`) — the default cache in every API/service. Examples:
  `AuthorizationFilter` caches Guid→ClientCode for **24 h** (`cc:{clientId}`); providers cache MTS/GTS
  auth tokens (`mts:auth` 55 min, `bg:auth`); repositories cache lookups with TTL/lazy-load.
- **Redis** (`StackExchange.Redis`) — only in `ClientSportsbook.API`.
- **Worker in-memory state** — `CashOutStake.Servicev2` **preloads active bets** and maintains live
  cashout stakes in memory; the 100-thread services hold per-thread `BlockingCollection` queues.
- **No distributed session / sticky user state** in the APIs — they are stateless request handlers;
  identity arrives per-request via headers (see [Authentication.md](Authentication.md)).
- **Cache invalidation is event-driven:** `CacheSync.Service` consumes 7 `On*Change` /
  `OnDeleteCacheMessage` events (market/sport sort order, favorites, maintenance, parlay bonus,
  client, dynamic translation) and evicts affected `IMemCache` entries. So Tier-2 state is kept
  fresh by RabbitMQ, not by TTL alone. **[PF]**

## Tier 3 — systems of record **[PF]**/**[OE]**
- **MongoDB `Sportsbook`** — live catalogue (Fixture/Market/Selection), owned by **odds-engine-2**.
- **SQL `BettingContext`** — bets/cashout/audit (truth for wagers).
- **SQL `SportsbookContext`** — curation/config.
- **SQL `TranslationContext` / `WalletContext`** — translations / wallet.
See [DomainModel.md](DomainModel.md) for the three-store split.

## End-to-end state propagation (how a change reaches the screen)
### Odds change (Tier 3 → Tier 1) **[OE]**→**[PF]**→**[PW]**
```
odds-engine-2 recomputes ─▶ Mongo write + RabbitMQ OnOddsChange (r.oe.sb.engine.*)
   ─▶ Websocket.API2 hub Publish* ─▶ platform-web HubConnection.On(OnOddsChange)
   ─▶ mutate FixtureModel + Counter++ ─▶ InvokeAsync(StateHasChanged) ─▶ DOM diff
```
No platform-web Tier-1 *container* is involved — the fixture list (instance field) is mutated directly.

### Wallet change (settlement → balance display)
```
Settlement.Service credits Wallet.API ─▶ OnWalletChange (r.wallet.s88)
   ─▶ hub ─▶ platform-web OnWalletChangeAsync ─▶ refresh balance ─▶ Header re-render
```
**[PF]**→**[PW]**

### Config/curation change (back office → live reads)
```
Back office writes SportsbookContext ─▶ OnClientChange/OnMarketSortOrderChange/…
   ─▶ CacheSync.Service evicts IMemCache ─▶ next Sportsbook.API read repopulates ─▶ next platform-web fetch
```
**[PF]**

## Concurrency notes
- **platform-web:** SignalR handlers run off the render thread; `InvokeAsync` marshals
  `StateHasChanged` onto the Blazor synchronization context. Per-circuit isolation means no
  cross-user state bleed. **[PW]**
- **platform-final:** `IMemCache` is process-local → **each API replica has its own cache**; only
  RabbitMQ invalidation keeps replicas roughly consistent. A cache write on one pod is not seen by
  another except via the broadcast eviction. **[PF]** (consistency consideration — see
  [TechnicalDebt.md](TechnicalDebt.md)).

## State ownership quick-reference
| State | Tier | Owner | Lifetime | Invalidation |
|-------|------|-------|----------|--------------|
| OperatorState/PageState/DeviceState | 1 | platform-web circuit | circuit | reset on layout init |
| TranslationState | 1 | platform-web circuit | circuit | refetch on language change (30 s API cache) |
| Fixture list / betslip | 1 | platform-web page | circuit | SignalR push + scope reload |
| ClientCode / provider tokens | 2 | platform-final API | TTL (24 h / 55 min) | TTL + `CacheSync` events |
| Active-bet cashout stakes | 2 | CashOutStake.Servicev2 | process | OnOddsChange / bet events |
| Catalogue (Fixture/Market/Selection) | 3 | odds-engine-2 → Mongo | persistent | feed updates |
| Bets / cashout / audit | 3 | platform-final SQL | persistent | settlement/cashout writes |
