# JavaScript Interop (platform-web)

> Phase 7. The JS interop surface of `Platform.Child.Web` — what crosses the Blazor↔browser boundary
> and why. All **[PW]**. Subject/provenance per [UIArchitecture.md](UIArchitecture.md).

## Model `[PW]`
Blazor **Server** ⇒ every JS interop call crosses the SignalR circuit (relatively expensive), so
interop is reserved for **browser-only APIs** Blazor can't reach: DOM measurement/scroll, event
listeners, storage, clipboard, parent-frame `postMessage`, and third-party widgets (Sportradar LMT).
`IJSRuntime` is injected globally (`_jsRuntime`) and per-component. JS files: `jsUtilities.v33.js`
(primary, 812 lines; v25–v33 versions exist), `lmt.1.3.js` (Live Match Tracker), `boot.js`
(reconnection).

## Three interop directions `[PW]`
1. **.NET → JS** (`IJSRuntime.InvokeAsync`/`InvokeVoidAsync`): measure DOM, scroll, register
   listeners, manipulate classes, post to parent.
2. **JS → .NET** (`DotNetObjectReference` + `[JSInvokable]`): browser events call back into components.
3. **.NET ↔ parent frame** (`window.postMessage`): the embedding host page integration.

## .NET → JS calls (inventory) `[PW]`
| JS function | Caller | Returns | Purpose |
|-------------|--------|---------|---------|
| `getTimeZoneOffset` | `Index.OnInitializedAsync` | `int` | browser UTC offset (minutes) for date math |
| `ScreenSize` | `Index` init + resize | `DeviceModel` | classify device tier (mobile/medium/large) → `DeviceState` |
| `ScreenWidthSize` | `Index` init | `int` | raw `innerWidth` |
| `detectSwipeRight` | `Index.OnAfterRenderAsync` | `bool` | attach Hammer.js swipe to betslip slider |
| `getCashoutSliderWidth` | `MyBetsBase` | `double` | `getBoundingClientRect().width` of cashout slider |
| `import("./js/lmt.js")` + `initLMT` | `FixtureDetails2.OnAfterRenderAsync` | module ref / void | mount Sportradar LMT widget (`SIR("addWidget",…)`) |
| `defaultUrlQueryParameterENLanguageCode` | `Index.OnAfterRenderAsync` | void | normalise `languageCode` query param; `history.replaceState` |
| `smoothScroll` | `Index`, `SportsCarousel` | void | `scrollIntoView({behavior:"smooth"})` on sport item |
| `addClassName` | `Index.ShowBetSection` | void | toggle betslip slide-in class (mobile) |
| `resizeListener` / `onscroll` / `onpopstate` | `Index.OnAfterRenderAsync` (first render) | void | register window listeners (resize→`HandleResizeScreen`; scroll→back-to-top; popstate→theme toggle) |
| `setObservedElement` | `Index`, `BetHistory` | void | `ResizeObserver` → posts `on-page-height-change` to parent |
| `platformEventsSender` | `Index`, `Parlay`, `RepositoryPlatformEventService` | void (fire-and-forget) | `window.parent.postMessage` — send events to host |
| `SetSubHeaderReference` (theme JS) | `SubHeader` | void | store ref for `onpopstate` theme toggle |

## JS → .NET (`[JSInvokable]` + `DotNetObjectReference`) `[PW]`
A `DotNetObjectReference` of a component is stored in a JS global so browser events can invoke .NET:
| Reference setter | Stored ref | JS calls back |
|------------------|-----------|---------------|
| `SetIndexReference` | `indexReference` (Index page) | `HandleNavigateTo`, `HandleResizeScreen`, `HandleUserAuthorization`, `HandleAddBetSelections`, `ShowBetSection`/`HideBetSection` |
| `SetSubHeaderReference` | SubHeader | `SetToDarkMode` / `SetToLightMode` |
| `SetClipboardReference` | BetHistory | `HandleCopyToClipboard` |
| `SetDatePickerReference` | BetHistory `Filter` | `HandleShowDatePickerModal` |

These references are set in `OnAfterRenderAsync`/`OnInitialized` (first render). `[JSInvokable]`
methods on components are the entry points for swipe, resize, parent-frame navigation, OG auth, and
betslip manipulation.

## Parent-frame protocol (`platform-event`) `[PW]`
platform-web is embeddable; it talks to its host page via `window.postMessage`:
- **Outbound** (`platformEventsSender` → `window.parent.postMessage`): page load, bet placed/removed,
  odds-format change, navigation, betslip-count change, session clear, API errors,
  `on-page-height-change` (so the host can resize the iframe via the `ResizeObserver`).
- **Inbound** (`jsUtilities.v33.js` listens; calls `indexReference.invokeMethodAsync`): e.g.
  `navigate-to` → `HandleNavigateTo`. See [Navigation.md](Navigation.md).
- `Platform.Parent.Web` loads `platform-event.v1.js` as the host side of this bus.

## Third-party widget interop `[PW]`
- **Sportradar LMT** (Live Match Tracker): dynamic ES-module `import("./js/lmt.js")` → `initLMT
  (selector, providerFixtureId, collapseTo, expanded)` → `SIR("addWidget",…)`. Mounted in
  `OnAfterRenderAsync` of the EL2 fixture-details view.
- **Hammer.js**: swipe gestures (betslip slider).

## Browser storage interop `[PW]`
- **`ProtectedSessionStorage`** (Blazor) — UI prefs (active scope, line-view); not raw JS.
- Other storage/clipboard access goes through `jsUtilities` helpers via interop.
- **No platform data or tokens are stored client-side** — platform-web is a server-side BFF
  ([RequestLifecycle.md](RequestLifecycle.md)).

## Lifecycle & cleanup `[PW]`
- DOM-dependent interop (listeners, LMT, references) runs in `OnAfterRenderAsync` (`firstRender`-gated).
- `CancellationTokenSource`s (odds-flash, cashout timers) and stored references are disposed in
  `Dispose`/`DisposeAsync`; `BetHistory` re-targets its `ResizeObserver` from `"bethistory"` to
  `"content"` on dispose.

## Cost & risk notes `[PW]`
- Each interop call is a **circuit round-trip** — hence batching at init and `firstRender` gating.
  Fire-and-forget (`InvokeVoidAsync` un-awaited) is used for non-critical posts (`platformEventsSender`,
  `setObservedElement`, `smoothScroll`).
- Heavy reliance on **JS globals holding `DotNetObjectReference`s** couples JS file versions
  (`jsUtilities.v33`) to component method names — a rename is a cross-file (C#↔JS) breaking change
  with no compiler check (a UI analogue of the contract blast-radius in [Contracts.md](Contracts.md)).
