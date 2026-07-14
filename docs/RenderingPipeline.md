# Rendering Pipeline (End-to-End)

> Phase 4. How data becomes pixels. The rendering tier lives in **platform-web** (Blazor Server);
> **platform-final** is upstream of it (REST + SignalR data). Provenance: **[PW]** platform-web
> (verified in its source/docs), **[PF]** platform-final. This documents the render path from
> platform-final's perspective — i.e. *what platform-final must deliver for rendering to work*.

## Where rendering happens
- **platform-final renders nothing UI.** It returns JSON `BaseResponseDTO<T>` and pushes SignalR
  messages. It is a headless backend (`AddControllers`, no `AddControllersWithViews`, no static
  files — see [MiddlewarePipeline.md](MiddlewarePipeline.md)). **[PF]**
- **platform-web is the renderer:** Blazor **Server** — components render server-side; the DOM diff
  is shipped to the browser over the persistent `/blazor` circuit WebSocket. No SPA/WebAssembly. **[PW]**

## Component tree (platform-web) **[PW]**
```
_Host.cshtml                       ← Razor Page; SSR; ClientInfo cascade root; theme/CSS choice
  └── App.razor                    ← Router; cascades OperatorState, TranslationState
        └── Router → RouteView
              └── Layout (MainLayout = AL  |  Euro2Layout = EL2)
                    └── Page (Index/Index2  |  EL2/Live, EL2/FixtureDetails)
                          ├── Header (wallet, search, betslip icon)
                          ├── SportsCarousel / SportsTree (sport→segment→league nav)
                          ├── MainNavPill (Live/Early/Today/Outrights/PriceBoost)
                          ├── LeagueGroup × N → Fixture × N → (Market/Selection cells)
                          ├── BetRecommendation → …Item → …Selection (top-markets)
                          └── BetSection → BetSlip / MyBets(…CashoutButton) / BetHistory
```

## The two render triggers **[PW]**
Blazor re-renders only on `StateHasChanged`. In this app it is driven by exactly two sources —
both ultimately fed by **platform-final data**:

1. **State container change** → `INotifyPropertyChanged` → layout `InvokeAsync(StateHasChanged)`
   → cascades to children. Used for translations, operator, page, device, wallet. (See
   [StateFlow.md](StateFlow.md).)
2. **SignalR message** → `HubConnection.On<T>` handler mutates an in-memory `FixtureModel` and
   bumps a `Counter`, then `InvokeAsync(StateHasChanged)`. Used for odds/bet-stop/bet-status/
   cashout/wallet pushes from platform-final. **[PF]** is the producer; **[PW]** renders.

## Initial render sequence (SSR → circuit) **[PW]**
1. `_Host.cshtml` runs server-side **before any circuit**: resolves layout (AL/EL2 from `clientId`),
   loads operator CDN config, picks theme CSS, renders `App` with `ServerPrerendered`.
2. A **pre-render guard** (`PreRenderService.IsPreRendering`) makes every page lifecycle method
   return early during SSR — so **no platform-final API calls happen during pre-render**; they run
   once the circuit is live. **[PW]**
3. Circuit opens → scoped DI → `Layout.OnInitializedAsync` (`InitializeStates`, connection-limit
   check, hub `StartAsync`) → `Page.OnInitializedAsync` (parallel data fetch from platform-final) →
   first full render → DOM shipped to browser.

## Blazor lifecycle hooks (platform-web) **[PW]**
| Component | OnInitializedAsync | OnParametersSetAsync | ShouldRender gate |
|-----------|--------------------|----------------------|-------------------|
| `BasePage` | hub connect, operator model, wallet, translations, sports, bet config, favorites, boosts | scope routing, sport/segment/league selection | — |
| `Index` | + betslip/parlay/bet-builder init | `LoadPage` per scope; param validation | — |
| `EL2/Live` | sports list, dropdowns, hub handlers, layout load | live fixture fetch, channel subscribe | — |
| `Fixture`/`Selection`/`BetRecommendation*` | — | param-diff (`ValidateParameters`) | `_shouldRender` flag |
| `MyBetsCashoutButton` | repo header prep | stake sync, new-offer detect, 7 s cashed-out display | — |

Performance pattern: leaf components implement a `_shouldRender` flag + `ValidateParameters()` so a
high-frequency odds push only re-renders the cells whose `FixtureModel`/odds actually changed,
not the whole list. **[PW]**

## Scope → data-source map (what platform-final must serve per view) **[PW]**→**[PF]**
| Scope | platform-web method | platform-final endpoint |
|-------|---------------------|--------------------------|
| Live | `GetLiveFixtures` | `GET v1/sportsbook/sports/livefixtures` |
| Early/Upcoming | `GetEarlyFixtures`/`GetDailyFixtures` | `…/upcomingfixtures` / `…/dailyfixtures` |
| Today | `GetTodayFixtures` | `…/todayfixtures` |
| Outrights | `GetOutrightFixtures` | `…/outrightfixtures` |
| PriceBoost | `GetPriceboostFixtures`/`GetBoostedFixtures` | `v1/sportsbook/boostedfixtures` |
| Favorites | `GetFavoriteFixtures` | `v1/sportsbook/userfavorites/fixtures` |
| SegmentLeague | `GetFixturesByLeague` | `…/segments/leagues/fixtures` |
| Fixture detail | `GetFixtureDetails` | `v1/sportsbook/fixturedetails` |

## Odds formatting at render **[PF]** semantics, **[PW]** display
- platform-final stores/serves **decimal** odds; the WebSocket `OutgoingSelection` carries all four
  formats (`DeOd/MaOd/HoOd/InOd`) precomputed (see [WebSocketContracts.md](WebSocketContracts.md)).
- platform-web picks the format per the user's odds-type selection and renders the matching field;
  the up/down flash compares new vs previous odds. **[PW]**

## Theme rendering **[PW]**
Theme is a **runtime** decision in `_Host.cshtml` (from `clientId`/`theme`/`IsEuroLayout` CDN flag);
AL themes from `wwwroot/css/al/*`, EL2 from `wwwroot/css/el2/white.css`. Both ship in one build; no
compile-time branching. platform-final is theme-agnostic. **[PW]**

## Render resilience **[PW]**
- Circuit drop → SignalR auto-reconnect (3 × 5 s) → full-page reload on exhaustion.
- A stale fixture (platform-final returns a fixture later removed upstream) renders from the
  in-memory model until the next load/odds message — consistent with the **no-cross-store-FK**
  property in [Relationships.md](Relationships.md). **[PF]**
