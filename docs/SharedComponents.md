# Shared Components (platform-web)

> Phase 7. The layout shell, reusable UI primitives, headers, modals, and cascading values of
> `Platform.Child.Web`. All **[PW]**. Subject/provenance per [UIArchitecture.md](UIArchitecture.md).

## Layout shell (`Shared/`) `[PW]`
The `Shared/` folder holds **only layouts**; general UI lives under `Components/`.

### `LayoutBase` (abstract)
Injects `IConfiguration`, `NavigationManager`, `ConnectedUser`, `ConnectedUserList`, and the four
state containers (`DeviceState`/`OperatorState`/`PageState`/`TranslationState`). Behaviour:
- `InitializeStates()` — resets each state to a fresh instance and subscribes its `PropertyChanged`
  → `InvokeAsync(StateHasChanged)` (so any state mutation re-renders the layout).
- `ValidateConnection()` — if `ConnectedUserList.Users.Count` > `Custom:MaximumConnections`, sets
  `IsAccessible=false` and hard-navigates to `/landing-page.html`; else appends `ConnectedUser`.
- `Dispose()` — unsubscribes handlers, removes the `ConnectedUser`.

### `MainLayout` (AL)
`<div><ErrorBoundary>@if(IsAccessible){ @Body }</ErrorBoundary></div>`. No header/sidebar in the
layout (the AL page renders chrome). Sets `PageState.Layout = AL`; logs version via JS; prerender-guarded.

### `Euro2Layout` (EL2)
Max-width 1366px shell: `HeaderContent` (with `SportsMenu` slot) + a desktop `SportsMenu` sidebar +
`@Body` (wrapped in `CascadingValue Name="Layout"`) + a bet-section placeholder. **Owns SignalR**:
`ConnectToChannel`/`DisconnectToChannel`/`DisconnectAllChannels` (tracks `_connectedChannels`,
re-subscribes on reconnect). Public props consumed by EL2 pages via `[CascadingParameter(Name="Layout")]`:
`MarketTemplates`, `CurrentSportModel`, `Sports`, `DeviceModel`. `LoadPage()` fetches device/
translations/templates/sports tree on first mount.

## Header components `[PW]`
| Component | Layout | Purpose |
|-----------|--------|---------|
| `AppHeader` | AL | top-right action bar: search toggle, balance, `UserInfo`, bet-history + betting-rules shortcuts; `IRenderComponent` (re-renders only on balance/currency/bet-builder/language change) |
| `SubHeader` | AL | announcements ticker + dark/light theme toggle; exposes `[JSInvokable] SetToDarkMode/SetToLightMode` |
| `TableHeader` | AL | scope tabs (Early/Today/Live/Outrights) / breadcrumbs + odds-format & line-view dropdowns + favorites/price-boost/filter buttons; raises scope/odds/line callbacks |
| `UserInfo` | AL | avatar + user id panel |
| `HeaderContent` | EL2 | header slot wrapper + mobile side-menu toggle; renders `UserBalance` + `ChildContent` |
| `UserBalance` | EL2 | **placeholder balance** (no code-behind) — stub |

## Reusable UI primitives (`Components/Common`, `Components/Shared`) `[PW]`
- **Modals/overlays:** `ConfirmModal` (navigate-with-unsaved-bets confirmation), `GenericModal`,
  `AnnouncementsModal`, `PartialCashoutModal`, `MatchEndedModal`, `BettingRulesModal`.
- **Inputs/controls:** `InputStake` (two-way stake bind), `InputSearch`, `Dropdown`,
  `DropdownMultiSelect`, `PillButton`, `DatePicker`.
- **Feedback/state:** `Preloader`, `ToastComponent` (via `ToastService`), `BorderedMessage`,
  `CustomValidation`, `EmptyFavoritesMessage`, `MyFavoritesLimitMessage`, `SessionExpiredMessage`,
  `CurrencyCodeMismatchMessage`, `UnexpectedErrorAlert`, `*Placeholder` (skeletons).
- **Bet placement (shared across AL+EL2):** `BetSection`, `Single`, `Parlay`, `BetSelectionItem`,
  `BetSelectionItemParlay` — the betslip primitives, used by both layouts.

## Cascading values `[PW]`
| Value | Source | Consumers | Notes |
|-------|--------|-----------|-------|
| `ClientInfo` | `App.razor` (`IsFixed`) | any component (`[CascadingParameter]`) | IP/UA/country + operator config flags; never changes |
| `Euro2Layout` (Name="Layout") | `Euro2Layout` (`IsFixed`) | EL2 pages (`BasePage`) | exposes layout methods/props |
| `OddsUpdateType` | `Index.razor` | AL selection cells | odds-animation mode (None/Higher/Any) |

## Error & empty states `[PW]`
- `ErrorBoundary` in both layouts → `Error` fallback (logs via JS, offers force-reload);
  `Recover()` on each navigation. Inline `UnexpectedErrorAlert` for independent force-reloads.
- Empty/limit/session states are dedicated message components (above), shown by the page.

## RenderFragment-slot pattern `[PW]`
Generic container components expose `RenderFragment` slots that pages fill, so one container serves
many views: `SideBar.Content`, `League.{LeagueHeaderContent,FixtureContent}`,
`Sport.{SportHeaderContent,LeagueContent}`, `HeaderContent.ChildContent`,
`Shared/FixtureList/League.{LeagueHeaderContent,FixtureContent}`. This is the main reuse mechanism
for the fixture-list tables. See [ComponentHierarchy.md](ComponentHierarchy.md).
