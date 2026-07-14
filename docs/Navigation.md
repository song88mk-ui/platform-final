# Navigation & Routing (platform-web)

> Phase 7. Routing, route tables, navigation patterns, and parent-frame navigation for
> `Platform.Child.Web`. All **[PW]**. Subject/provenance per [UIArchitecture.md](UIArchitecture.md).

## Routing model `[PW]`
Every URL → `app.MapFallbackToPage("/_Host")` → Blazor `<Router>` scans `@page` directives.
`_Host` picks the layout by path (`/el2` → EL2). Soft navigation (circuit stays open) is the default.

## Child route table `[PW]`
| Route | Component | Layout | Notes |
|-------|-----------|--------|-------|
| `/`, `/{ParamScope}`, `/segment`, `/league`, `/fixture`, `/maintenance` | `Index` | AL | `ParamScope` ∈ Today/Live/Early/Outrights/Favorites/Priceboost |
| `/el`, `/{CurrentScope}`, `/el/segment` | `Index2` | EL2 | EL2 prematch/live list |
| `/el2/live` | `EL2/Live` | EL2 | live fixtures |
| `/el2/upcoming`, `/el2/boss-boosts` | `EL2/Upcoming`, `EL2/BossBoosts` | EL2 | **stubs** |
| `/el2-fixture-details` | `EL2/FixtureDetails` | EL2 | fixture detail |
| `/play` | `OGAuth` | AL | Oriental Gaming HMAC entry |
| `/result` | `Result` | AL | standalone bet result (query: clientId, transactionId, token, languageCode) |
| `/unauthorized` | `AuthenticationErrorPage` | AL | 401 |
| `/notfound` | `NotFoundPage` | AL | 404 (also Router `NotFound`) |

Parent (`Platform.Parent.Web`): `/` (`Index`), `/glive` (`GLive`), `/fetchdata` (dev scaffold).

## Route parameters `[PW]`
- Navigation params are **query-string** values (except `/{ParamScope}` and `/{CurrentScope}`).
- `BasePage` reads via **`[SupplyParameterFromQuery]`**: `clientId`, `currencyCode`, `languageCode`,
  `minuteOffset`, `operatorToken`, `userId`, `betLimit`, `poolId`, `timestamp`, `refCurrencyCode`,
  `signature`, `gameCode`, `theme`, `sportId`, `fixtureId`, `segmentId`, `leagueId`, `date`.
  These are exactly the headers/identity platform-final's gateway expects (see
  [PlatformApi.md](PlatformApi.md), [AuthenticationIntegration.md](AuthenticationIntegration.md)).

## Internal navigation `[PW]`
`NavigationManagerExtensions` provides typed `NavigateToPath` overloads:
```
NavigateToPath(path, sportId[, segmentId[, leagueId[, fixtureId]]])
NavigateToPath(path, sportId, date)
NavigateToPath(path, Dictionary<string,string> parameters)
NavigateToPath(paramName, paramValue)   // single query-param update on current path
```
- `HandleNavigate` **preserves existing query params** except `sportId/segmentId/leagueId/fixtureId/
  date` (which it replaces) — so identity/auth params survive every in-app navigation.
- All overloads call `NavigationManager.NavigateTo` **without `forceLoad`** (soft nav; circuit
  persists). Scope/sport/league changes drive a fixture re-fetch + SignalR channel re-subscribe
  (see [RequestLifecycle.md](RequestLifecycle.md) §6).

## Layout & navigation interplay `[PW]`
- AL pages inherit `MainLayout`; EL2 pages carry `@layout Euro2Layout`. `PageState.Model.Url`/`Layout`
  track the active route/layout. `ErrorBoundary.Recover()` fires on each `OnParametersSetAsync` so
  navigation clears a prior error without a reload.

## Parent-frame (external) navigation `[PW]`
platform-web is embedded; navigation can be driven by the host page via the **`platform-event`
postMessage protocol**:
- `jsUtilities.v33.js` listens for a `navigate-to` event → `indexReference.invokeMethodAsync(
  'HandleNavigateTo', payload)` → `[JSInvokable] HandleNavigateTo` on `Index`.
- platform-web → parent: `platformEventsSender` posts events (page-height change, bet placed,
  betslip count, errors, navigation). See [JavaScriptInterop.md](JavaScriptInterop.md).

## Special / hard navigations `[PW]`
| Trigger | Navigation |
|---------|-----------|
| Connection cap exceeded (`LayoutBase.ValidateConnection`) | `NavigateTo("/landing-page.html?{query}", forceLoad: true)` |
| OG HMAC auth failure (`OGAuth`) | `NavigateTo("/unauthorized")` |
| Unexpected JS error (`Error` component) | `NavigateTo(currentPath, forceLoad: true)` (reload) |
| Missing ClientId/CurrencyCode (`Index.OnInitializedAsync`) | `NavigateTo("/notfound")` |

## Focus & a11y `[PW]`
`App.razor` uses `<FocusOnNavigate RouteData Selector="h1" />` to move focus to the page heading on
navigation.

## Notes
- Deep-linking works via query params (a URL fully reconstructs operator/fixture context).
- `forceLoad` is reserved for error/limit escapes and parent-driven full reloads; normal nav is soft.
