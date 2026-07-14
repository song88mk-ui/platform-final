# Component Data Flow (End-to-End)

> Phase 4. How data flows into and out of UI components, traced back to its **platform-final**
> origin. The components are **platform-web** Blazor components; this doc maps each to the
> platform-final endpoint/message that feeds it, so a backend change's UI blast radius is visible.
> Provenance: **[PW]** platform-web, **[PF]** platform-final.

## Data-source model (platform-web) **[PW]**
A component gets data from exactly three sources, all rooted in platform-final:
1. **REST** via `IPlatformRepository` (→ `Platform.API` gateway). [PublicApiSurface.md](PublicApiSurface.md)
2. **SignalR** via `HubConnection.On<T>` handlers in `BasePage`/`BetRecommendation`. [WebSocketContracts.md](WebSocketContracts.md)
3. **State containers** (`OperatorState`/`TranslationState`/`PageState`/`DeviceState`). [StateFlow.md](StateFlow.md)

**Ownership rule:** parent **pages** (`Index`, `Index2`, `EL2/Live`, `EL2/FixtureDetails`) own the
fixture lists and betslip; children receive data via `[Parameter]` and return decisions via
`EventCallback`. **Siblings never talk directly** — everything routes through the parent page. **[PW]**

## Component → platform-final feed map

| Component | Inputs (`[Parameter]`) | Data source (platform-final origin) | Outputs (`EventCallback`) |
|-----------|------------------------|--------------------------------------|---------------------------|
| **AppHeader / Header** | `PlayerModel`, `Device`, `IsBetBuilderActive`, `LanguageCode` | `PlayerModel.Balance` ← `GET v1/wallet/balance` + `OnWalletChange` push; labels ← `TranslationState` | `ShowBetHistory`, `OpenLiveScoreWindow`, `OnClearBetBuilder` |
| **SportsTree / SportsCarousel** | `Sports: List<SportModel>`, `SelectedSportId`, `IsAllSportsActive` | `GET v1/sportsbook/sports` (+ selector templates); curation from SQL `SportsbookContext` | `SelectedSportIdChanged`, `IsAllSportsActiveChanged`, `DisconnectChannel`, `UriPathCallback` |
| **MainNavPill / FilterGroup** | scope flags, odds-format | local UI state; odds-format drives `OddsType` display | scope/filter callbacks |
| **LeagueGroup → Fixture** | `FixtureModel` | `GET …/livefixtures`/`todayfixtures`/… (Mongo `Fixture`/`Market`/`Selection`); live `OnOddsChange`/`OnBetStopFixture` | selection-click → betslip add |
| **Selection cell** | `SelectionModel` (odds in 4 formats) | `Selection` doc (`DeOd/MaOd/HoOd/InOd`); `OnOddsChange` updates + flash | odds-click `EventCallback` |
| **BetRecommendation → Item → Selection** | top-market fixtures | `GET v1/sportsbook/betrecommendations`/`boostedfixtures`; `OnTopMarketsOddsChange` (own `SubscribeToTopMarkets`) | add-to-betslip |
| **BetSlip** | selections, stake | `POST v1/bet/maxstake`, `POST v2/bet/placement`; `OnBetslipOddsChange`/`OnBetslipBetStopFixture` | place/clear |
| **MyBets (Parlay/Single Item)** | open bets | `POST v2/bet/history` (`BetStatusFilter=Open`); `OnBetStatus` push | cashout request |
| **MyBetsCashoutButton** | bet, current offer | `POST v1/bet/cashout`; live value via `OnCashOutChange` (CashOutStake.Servicev2) | confirm cashout |
| **BetHistory** | settled bets | `POST v2/bet/history` / `GET v1/bet/history` | paging/filter |
| **EL2/FixtureDetails** | fixture + markets | `GET v1/sportsbook/fixturedetails` (Mongo); `GET v1/sportsbook/livestream` | favorite toggle, stream open |
| **Search / SearchResult** | query | `GET v1/sportsbook/search` | select fixture |
| **Result.razor** | transaction id | `POST v1/bet/details` (`PlatformRepository2`, 3 s cache) | — |
| **UserInfo / wallet** | balance | `GET v1/wallet/balance` + `OnWalletChange` | — |
| **Announcements banner** | announcements | `GET v1/sportsbook/announcements` (SQL `Announcements`) | dismiss |

## Three canonical component data flows

### A. Read flow (page → component)
```
Page.OnInitializedAsync ─ IPlatformRepository.GetXxx ─▶ Platform.API ─▶ (Sportsbook/Betting/Wallet) ─▶ store
  ◀─ BaseResponseDTO<T> ─ map to UI model ─ assign to page field/state
  ─▶ [Parameter] down to child ─ child.OnParametersSetAsync(ValidateParameters) ─ render
```
**[PW]**→**[PF]**

### B. Real-time flow (push → component)
```
odds-engine-2 / platform-final service ─ RabbitMQ ─▶ Websocket.API2 ─▶ HubConnection.On(handler in BasePage)
  ─ mutate the owned FixtureModel + Counter++ ─ InvokeAsync(StateHasChanged)
  ─▶ child re-render via parameter diff (only changed cells, gated by _shouldRender)
```
**[OE]**/**[PF]**→**[PW]**

### C. Write flow (component → page → platform-final)
```
child raises EventCallback ─▶ parent page handler ─ IPlatformRepository.Post (placement/cashout/maxstake)
  ─▶ Platform.API ─▶ Betting.API (+ provider, SQL, RabbitMQ) ─◀ BaseResponseDTO
  ─ page updates betslip/MyBets state ─ re-render; later OnBetStatus/OnWalletChange refine it
```
**[PW]**→**[PF]**

## Re-render gating (why backend message frequency matters) **[PW]**
Leaf components (`Fixture`, `Selection`, `BetRecommendationItem`, `Search`, `MyBets*`) implement
`ShouldRender` via a `_shouldRender` flag set in `OnParametersSetAsync` only when a watched value
changed (`ValidateParameters`). Consequence: a burst of `OnOddsChange` messages from platform-final
re-renders **only the affected selection cells**, not the whole list. High backend message volume is
therefore safe for the UI *as long as each message targets specific fixtures/selections*. **[PW]**

## Backend-change → UI blast radius (practical use of this doc)
| If platform-final changes… | UI components affected |
|----------------------------|------------------------|
| `Fixture`/`Market`/`Selection` Mongo shape | LeagueGroup, Fixture, Selection, FixtureDetails, BetRecommendation |
| `OnOddsChange` message fields | every odds-bearing cell + cashout value |
| `StatusCode` values | BetSlip placement errors, MyBets cashout (`142`), retry behavior |
| `v1/wallet/balance` shape | Header/UserInfo balance |
| translation keys/shape | every localized label (via TranslationState) |
| `v2/bet/history` shape | MyBets + BetHistory |

> This map is the reason Phase 1 flagged `Platform.Shared` and the message/DTO contracts as the
> highest-blast-radius area — they are the literal inputs to the render tree. See
> [DependencyGraph.md](DependencyGraph.md) §6 and [TechnicalDebt.md](TechnicalDebt.md).
