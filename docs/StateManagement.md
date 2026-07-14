# State Management (platform-web)

> Phase 7. UI state containers, DI, and component communication in `Platform.Child.Web`. All **[PW]**.
> The end-to-end / cross-repo state view (incl. platform-final server caches) is in
> [StateFlow.md](StateFlow.md); this doc is the **UI-tier** focus.

## Pattern: scoped DI state containers + `INotifyPropertyChanged` `[PW]`
No Redux/Flux/MediatR/store library. Each Blazor **circuit** (browser tab) gets its **own scoped
instance** of every state class. State change → `PropertyChanged` → **layout** re-renders → cascades.

```csharp
public abstract class BaseState<T> : INotifyPropertyChanged {
    private T _state;  protected T State => _state;
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name="") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```
Each concrete state holds a `Model` whose setter calls `OnPropertyChanged()`.

## The four state containers `[PW]`
| State | Model | Holds | Updated by |
|-------|-------|-------|-----------|
| `OperatorState` | `OperatorModel` | ClientId, CurrencyCode, LanguageCode, UserId, MinuteOffset, OG flags, IsAuthorized | `Index.OnInitializedAsync` (from query params), language change |
| `TranslationState` | `Dictionary<string,string>` (+ `GetValue(key)`) | active-language translations | `LoadTranslations` / language switch |
| `PageState` | `PageModel` | Url, active `IPage`, `Layout` (AL/EL2), sidebar flags | layout init, page init, navigation |
| `DeviceState` | `DeviceModel` | IsMobile/IsMedium/IsLarge | `ScreenSize` JS on init + resize |

Registration: `AddScoped<DeviceState/OperatorState/PageState/TranslationState>()`
(`WebApplicationBuilderExtensions`). **[PW]**

## Subscription & propagation `[PW]`
Only **layout** components subscribe (`LayoutBase.InitializeStates`):
```csharp
DeviceState.PropertyChanged     += async (s,p) => await InvokeAsync(StateHasChanged);
OperatorState.PropertyChanged   += async (s,p) => await InvokeAsync(StateHasChanged);
PageState.PropertyChanged       += async (s,p) => await InvokeAsync(StateHasChanged);
TranslationState.PropertyChanged += async (s,p) => await InvokeAsync(StateHasChanged);
```
Pages/children read state by **injection** and re-render via the layout cascade — they do **not**
subscribe themselves. Handlers detached in `Dispose`. `InvokeAsync` marshals onto the Blazor render
thread (SignalR handlers run off it).

## Non-container state `[PW]`
- **Fixture lists / betslip / MyBets** live as **page instance fields** (`List<FixtureModel>`),
  mutated in place by SignalR handlers (+ a `Counter` bump) — not in a state container. See
  [Rendering.md](Rendering.md), [ComponentDataFlow.md](ComponentDataFlow.md).
- **`ConnectedUserList`** is a **Singleton** (cross-circuit) connection counter for the
  `MaximumConnections` cap; `ConnectedUser` is scoped per circuit.
- **`ProtectedSessionStorage`** persists UI prefs (active scope / line-view) across reloads.
- **Lifetime:** state reset to fresh instances in `LayoutBase.InitializeStates()`; destroyed with
  the circuit. No server-side per-user persistence beyond the above.

## Dependency injection (UI tier) `[PW]`
- **Global `@inject`** (every `.razor`, via `_Imports`): `IConfiguration`, `IJSRuntime`,
  `IPlatformRepository2`, `IPreRenderService`, `CurrencyService`, `ModalService`, the four states,
  `NavigationManager`, `HubConnection`.
- **Lifetimes:** state + `IPlatformRepository`/`PlatformRepository` (typed HttpClient) +
  `ApiUrlResolver` + `ToastService`/`ModalService`/`CurrencyService` + `ConnectedUser` +
  `HubConnection` + `IPreRenderService` = **Scoped** (per circuit); `ConnectedUserList` = **Singleton**.
  (Contrast platform-final's mostly-Singleton infra DI — see [DependencyInjection.md](DependencyInjection.md).)

## Component communication `[PW]`
1. **Down:** `[Parameter]` / `[CascadingParameter]` (`ClientInfo`, `Layout`, `OddsUpdateType`).
2. **Up:** `EventCallback` (children raise; parent page handles). Siblings never communicate directly.
3. **Cross-cutting:** shared state containers (read by injection) + the `HubConnection` (real-time).
4. **External (parent frame):** `window.postMessage` ↔ `[JSInvokable]` on `Index`
   (see [JavaScriptInterop.md](JavaScriptInterop.md), [Navigation.md](Navigation.md)).

## Update patterns `[PW]`
- **SignalR push → model → re-render:** handler mutates fixture model, `InvokeAsync(StateHasChanged)`.
- **API fetch → state → re-render:** result assigned to `TranslationState.Model`/`OperatorState.Model`
  → `PropertyChanged` cascade.
- **JS interop → state:** screen size / timezone → `DeviceState`/`OperatorState`.

## Notes / risks `[PW]`
- Per-circuit isolation = no cross-user state bleed, but **state is volatile** (lost on circuit drop;
  reconnect re-initialises and re-fetches).
- Layout-only subscription keeps the model simple but means **all** re-renders funnel through the
  layout cascade — `ShouldRender` gates on leaves are essential (see [Rendering.md](Rendering.md)).
