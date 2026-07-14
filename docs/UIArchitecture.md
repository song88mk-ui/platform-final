# UI Architecture (platform-web)

> **Phase 7 — UI Architecture Analysis.** Analysis-only; no code modified. **platform-final has no
> UI** (headless backend), so this phase documents the platform's **UI tier = the `platform-web`
> Blazor Server app** — the consumer of platform-final's gateway + WebSocket (see
> [PlatformApi.md](PlatformApi.md), [WebSocket.md](WebSocket.md), [RequestLifecycle.md](RequestLifecycle.md)).
> All facts are **[PW]** (verified in the platform-web sibling's source/docs). These docs live in
> `platform-final/docs` per the standing Phase 2 location decision; the **authoritative source** is
> `platform-web/docs`.

## Stack `[PW]`
- **Blazor Server** (no WebAssembly). UI renders server-side; the DOM diff streams to the browser
  over the persistent `/blazor` SignalR circuit (WebSockets-only, `transport: 1`, skip-negotiation).
- **.NET 7.0.** Two projects:
  - **`Platform.Child.Web`** — the embeddable sportsbook widget (all real UI).
  - **`Platform.Parent.Web`** — a thin embedding shell (iframe/postMessage host; minimal UI).

## Entry points `[PW]`
- **`Pages/_Host.cshtml`** (the only Razor Page; `app.MapFallbackToPage("/_Host")`): runs server-side
  before any circuit — reads Cloudflare headers into a `ClientInfo`, loads per-operator CDN config
  (`CdnConfigurationProvider`: `OGAuth`/`DarkMode`/`LanguageOptions`/`BettingRules`/`PartialCashout`/
  `IsEuroLayout`/themes), detects layout (`/el2` → EL2 else AL), builds the theme CSS `<link>`,
  preloads the icon font, loads `hammer.min.js` + `jsUtilities.v33.js`, and starts the Blazor circuit
  with a 3×5 s reconnect handler (then `location.reload()`).
- **`App.razor`**: root component. Child.Web wraps the tree in a **fixed** `CascadingValue<ClientInfo>`
  + a `<Router>` (`DefaultLayout = MainLayout`, `NotFound → NotFoundPage`). Parent.Web is a plain Router.
- **`_Imports.razor`**: global `@using` + **global `@inject`** of 12 services into every `.razor`
  (see [StateManagement.md](StateManagement.md)).

## Routing `[PW]`
- Every URL hits `_Host` → Blazor `<Router>` scans `@page` directives. Full route table in
  [Navigation.md](Navigation.md). Highlights: `/` + `/{ParamScope}` → `Index` (AL); `/el`,
  `/{CurrentScope}` → `Index2` (EL2); `/el2/live` → `EL2/Live`; `/el2-fixture-details` →
  `EL2/FixtureDetails`; `/play` → `OGAuth`; `/result` → `Result`; `/unauthorized`, `/notfound`.
- Navigation params are mostly **query-string** values (`clientId`, `currencyCode`, `operatorToken`,
  `sportId`, `fixtureId`, …) supplied via `[SupplyParameterFromQuery]` on `BasePage`.

## Two layouts (the central UI split) `[PW]`
```
LayoutComponentBase → LayoutBase → { MainLayout (AL) , Euro2Layout (EL2) }
```
- **`LayoutBase`** (abstract): injects the state containers + connection tracking; `InitializeStates()`
  subscribes each state's `PropertyChanged` → `StateHasChanged`; `ValidateConnection()` enforces
  `Custom:MaximumConnections` (redirect to `/landing-page.html` if exceeded).
- **`MainLayout` (AL — "Asian Layout")**: a single full-viewport `div` + `ErrorBoundary` wrapping
  `@Body`. **No chrome in the layout** — the AL page (`Index.razor`) renders its own header/sidebar/
  bet-section. Sets `PageState.Layout = AL`.
- **`Euro2Layout` (EL2 — "Euro Layout 2")**: max-width 1366px centred shell with a `HeaderContent`,
  a `SportsMenu` sidebar, and a `bet-section` placeholder; **cascades itself** (`Name="Layout"`) so
  EL2 pages call `Layout.LoadPage()`/`ConnectToChannel()` directly; **owns the SignalR connection**.
  Sets `PageState.Layout = EL2`.
- Layout chosen at `_Host` (path-based) + `@layout Euro2Layout` directive on EL2 pages. AL and EL2
  have **parallel component folders** (`Components/.../AL/` vs `.../EL2/` vs `.../Shared/`).

## Theme architecture `[PW]`
- **Global CSS files from CDN**, not per-component CSS. `_Host.GetCssUrl()` resolves
  `{CDN}/css/{al|el2}/{theme}.css` from the `theme` query param / operator config (`red`, `blue`,
  `gray`, `orange-*`, …; default `red`); localhost loads local CSS. Version suffixes supported.
- **CSS isolation:** there is **no evidence of Blazor scoped CSS** (`.razor.css`); styling is the
  global theme files + a Blazor styles bundle. **[Likely]** (theming is global/CDN-driven).

## Cross-cutting UI mechanisms `[PW]`
- **Cascading values:** `ClientInfo` (fixed, app-wide), `Euro2Layout` (Name="Layout", EL2),
  `OddsUpdateType` (AL odds-animation mode).
- **Error boundaries:** both layouts wrap `@Body` in `ErrorBoundary` → `Error` fallback;
  `ErrorBoundary.Recover()` on each navigation; plus an inline `UnexpectedErrorAlert`.
- **Pre-render guard:** `PreRenderService.IsPreRendering` makes page lifecycle methods return early
  during SSR — **no API/JS calls during prerender**.
- **Render optimisation:** nearly every component overrides `ShouldRender()` via a `_shouldRender`
  flag; large lists use `<Virtualize>`. See [Rendering.md](Rendering.md).

## How it relates to platform-final
- **Data:** server-side `IPlatformRepository` → `Platform.API` gateway (REST); `HubConnection` →
  `Websocket.API2` `/platform` (SignalR). The browser never holds keys. See
  [RequestLifecycle.md](RequestLifecycle.md), [ComponentDataFlow.md](ComponentDataFlow.md).
- **Companion Phase-7 docs:** [ComponentHierarchy.md](ComponentHierarchy.md),
  [SharedComponents.md](SharedComponents.md), [StateManagement.md](StateManagement.md),
  [Navigation.md](Navigation.md), [Rendering.md](Rendering.md), [JavaScriptInterop.md](JavaScriptInterop.md).
