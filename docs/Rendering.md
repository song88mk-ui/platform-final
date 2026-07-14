# Rendering & Lifecycle (platform-web)

> Phase 7. The Blazor rendering lifecycle, `ShouldRender` optimisation, virtualization, and CSS for
> `Platform.Child.Web`. All **[PW]**. The cross-repo "how platform-final data becomes pixels" view is
> [RenderingPipeline.md](RenderingPipeline.md); this is the **UI-tier lifecycle** focus.

## Blazor Server render model `[PW]`
Components render **server-side**; Blazor diffs the render tree and streams DOM patches to the
browser over the `/blazor` circuit. Re-render happens only on `StateHasChanged`, driven by exactly
two sources (both rooted in platform-final data — see [StateFlow.md](StateFlow.md)):
1. **State container change** → `INotifyPropertyChanged` → layout `InvokeAsync(StateHasChanged)` → cascade.
2. **SignalR message** → `HubConnection.On<T>` handler mutates a `FixtureModel` (+ `Counter`) →
   `InvokeAsync(StateHasChanged)`.

## Initial render (SSR → circuit) `[PW]`
1. `_Host.cshtml` runs server-side **before any circuit** (theme/layout/CDN config, `ClientInfo`,
   `ServerPrerendered` `App`).
2. **Pre-render guard** (`PreRenderService.IsPreRendering`) → page lifecycle methods return early →
   **no API/JS calls during SSR**.
3. Circuit opens → scoped DI → layout `OnInitializedAsync` (`InitializeStates`, connection cap, hub
   start) → page `OnInitializedAsync` (parallel fetches) → first full render → DOM shipped.

## Lifecycle hooks (per component type) `[PW]`
| Hook | Typical use |
|------|-------------|
| `OnInitializedAsync` | hub connect, operator model, parallel data load (translations/sports/bet-config/favorites/boosts), register `HubConnection.On` handlers, register JS references |
| `OnParametersSetAsync` | scope/sport/segment/league routing; `ValidateParameters` change-detection gate |
| `OnAfterRenderAsync` | JS interop that needs the DOM (swipe/scroll/resize listeners, LMT init, reference registration); usually `firstRender`-gated |
| `ShouldRender` | the `_shouldRender`/`RenderComponent` flag (below) |
| `Dispose`/`DisposeAsync` | unsubscribe hub channels, dispose `CancellationTokenSource`s, detach state handlers |

Key pages/bases: `BasePage` (AL `Index`, EL2 pages), `Index2`, `LeagueBase`, `MyBetsBase`,
`LayoutBase`. `Index` is `IAsyncDisposable`.

## Render optimisation — the core performance mechanism `[PW]`
- **`ShouldRender()` gating:** nearly every component overrides `ShouldRender()` to return a private
  `_shouldRender` (or `RenderComponent`) flag, set `true` only on a meaningful parameter change
  (`ValidateParameters`) and reset `false` after render (`OnAfterRenderAsync`). This **suppresses the
  default "re-render on every parent `StateHasChanged`"**, so a burst of `OnOddsChange` messages
  re-renders **only the affected selection cells**, not the whole list. This is the single most
  important UI-perf decision given the high-frequency odds feed.
- **`<Virtualize>`:** large fixture lists (`League`/`Fixture` in AL, EL2, and `Shared/FixtureList`)
  render only visible rows.
- **Odds-cell animation:** `Selection` cells read `[CascadingParameter] OddsUpdateType` and flash
  up/down (~4 s, `CancellationTokenSource`-bounded) on change.

## Error boundaries & recovery `[PW]`
Both layouts wrap `@Body` in `ErrorBoundary` → `Error` fallback (logs via JS, force-reload link).
`ErrorBoundary.Recover()` on each navigation resets the boundary without a reload. Inline
`UnexpectedErrorAlert` triggers independent force-reloads. Circuit drop → SignalR auto-reconnect
(3×5 s) → `location.reload()` on exhaustion.

## CSS & styling `[PW]` / `[Likely]`
- **Global theme CSS from CDN** (`{CDN}/css/{al|el2}/{theme}.css`), selected at runtime in `_Host`
  before any component renders; plus a Blazor styles bundle and icon font.
- **No evidence of Blazor scoped CSS isolation (`.razor.css`)** — styling is global/theme-driven.
  **[Likely]** (component-level CSS isolation not used; theming is the styling strategy).
- Theme is a **runtime** decision (query param / `IsEuroLayout` config); both AL and EL2 ship in one
  build — no compile-time branching.

## Render-correctness notes `[PW]`
- SignalR handlers run **off** the render thread → must use `InvokeAsync(StateHasChanged)` (they do).
- A stale fixture (platform-final returned one later removed upstream) renders from the in-memory
  model until the next load/odds message — consistent with the no-cross-store-FK property
  ([Relationships.md](Relationships.md)).
- Heavy reliance on `ShouldRender` flags means a **missing/incorrect `ValidateParameters`** can cause
  a component to *not* update on real data changes — a subtle correctness risk inherent to the pattern.
