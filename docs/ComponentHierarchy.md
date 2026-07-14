# Component Hierarchy (platform-web)

> Phase 7. The Blazor component tree of `Platform.Child.Web`. All **[PW]** (verified in platform-web
> source/docs). Subject and provenance per [UIArchitecture.md](UIArchitecture.md).

## Two entry points, one root, diverging at layout `[PW]`
`_Host.cshtml` (path-based layout select) → `App.razor` (`CascadingValue<ClientInfo>`) → `Router` →
**MainLayout (AL)** or **Euro2Layout (EL2)** → page → components.

## AL tree (MainLayout → Index.razor `/`) `[PW]`
```
MainLayout (ErrorBoundary)
  Index.razor  — owns ALL chrome (the layout is just a div)
    Preloader · SessionExpiredMessage · CurrencyCodeMismatchMessage · UnexpectedErrorAlert
    AppHeader (Header/AL)
      Search → InputSearch · SearchResult · ConfirmModal
      UserInfo (AL) · BettingRulesModal
    ToastComponent · AnnouncementsModal
    SideBar
      BetSection
        Single → BetSelectionItem · CustomValidation · InputStake · BorderedMessage
        Parlay → BetSelectionItemParlay · InputStake · CustomValidation · BorderedMessage
        Preloader · MyBetsSingleBetItem → MyBetsCashoutButton
        MyBetsParlayBetItem → MyBetsCashoutButton · BorderedMessage
      SportsMenu (RenderFragment)
    SubHeader (AL) → [announcements banner]
    BetRecommendation → BetRecommendationItem → BetRecommendationSelection
    SportsCarousel (AL) → SportItem · SegmentItem · LeagueItem · ConfirmModal
    TableHeader (AL) → ConfirmModal
    EarlyPresetDate · FilterGroup → LeagueFilter
    — view-conditional blocks —
      Favorites:   Sport → League → AL/Fixture → AL/Selection ; EmptyFavoritesMessage
      Priceboost:  BoostedFixture → BoostedSelection
      Today/Live/Early/SegmentLeague:  League → AL/Fixture (Virtualize) → AL/Selection
      Outrights:   Outrights → OutrightsMarket → OutrightsSelection
      FixtureDetails: FixtureDetails → FixtureDetailsMarket → FixtureDetailsSelection
    BetHistory → Filter → DatePicker ; SingleBetItem · ParlayBetItem · BetBuilderItem
                 PartialCashoutModal · BetHistoryPlaceholder · Preloader
    AnnouncementsModal · MatchEndedModal · MyFavoritesLimitMessage
  Other AL pages: OGAuth (→Preloader) · Result (→Single/Parlay/BetBuilderItem) ·
                  AuthenticationErrorPage · NotFoundPage
```

## EL2 tree (Euro2Layout) `[PW]`
```
Euro2Layout (ErrorBoundary)
  HeaderContent (Header/EL2)
    UserBalance
    [ChildContent] → SportsMenu (SportsTree) → SportItem → SegmentItem → LeagueItem ; GenericModal
  EL2 pages:
    EL2/Live  (/el2/live)
      BetRecommendation → …Item → …Selection
      MainNavPill → PillButton ; SportsCarousel (EL2) ; MatchEndedModal
      DropdownMultiSelect · Dropdown · PillButton
      CascadingValue<OddsUpdateType> → EL2/League → EL2/Fixture → EL2/Selection
    EL2/FixtureDetails  (/el2-fixture-details)
      [LMT / livestream / virtual-stream iframes] · BetBuilderSwitch · EL2/DetailsSelection
    EL2/BossBoosts (/el2/boss-boosts) [stub] · EL2/Upcoming (/el2/upcoming) [stub]
  Index2.razor (/el, /{CurrentScope}, /el/segment)
    Shared/FixtureList/League → Shared/FixtureList/Fixture (Virtualize)
```

## Component families `[PW]`
| Family | Folder | AL / EL2 / Shared |
|--------|--------|-------------------|
| Pages | `Pages/`, `Pages/EL2/` | Index (AL), Index2/EL2.* (EL2) |
| Layouts | `Shared/` | MainLayout (AL), Euro2Layout (EL2) |
| Header | `Components/Header/{AL,EL2}` | AppHeader/SubHeader/TableHeader/UserInfo (AL); HeaderContent/UserBalance (EL2) |
| Fixture list | `Components/MainMarketsTable/{AL,EL2}`, `Components/Shared/FixtureList` | Sport/League/Fixture/Selection per variant |
| Sports nav | `Components/SportsCarousel/{AL,EL2}`, `Components/SportsTree`, `Components/Shared/SportsCarousel` | three carousel variants + tree |
| Bet placement | `Components/BetSection` (**Shared**) | BetSection/Single/Parlay/BetSelectionItem*/InputStake |
| My bets / history | `Components/MyBets`, `Components/BetHistory` | MyBets*BetItem, MyBetsCashoutButton, BetHistory + items |
| Outrights / boost | `Components/OutrightsTable`, `Components/PriceBoost` | Outrights*, Boosted* |
| Common primitives | `Components/Common`, `Components/Shared` | Preloader, ConfirmModal, GenericModal, Dropdown, PillButton, DropdownMultiSelect, placeholders |

## Composition patterns `[PW]`
- **Parent pages own the data** (fixture lists, betslip) and pass it to children via `[Parameter]`;
  children return decisions via `EventCallback`. **Siblings never talk directly.** See
  [ComponentDataFlow.md](ComponentDataFlow.md).
- **`RenderFragment` slots** decouple structure from content: `SideBar.Content`, `League`'s
  `LeagueHeaderContent`/`FixtureContent`, `Sport`'s `SportHeaderContent`/`LeagueContent`,
  `HeaderContent.ChildContent`, `Euro2Layout` `@Body` — pages inject the row content into generic
  containers (so the same `League` container renders Favorites vs Today rows).
- **Base classes** factor shared behavior: `BasePage`, `LayoutBase`, `LeagueBase`, `MyBetsBase`,
  `IRenderComponent` (the `_shouldRender` contract).
- **Per-variant components** (AL/EL2/Shared) instead of a runtime theme switch — the layout's pages
  reference the right variant directly.

## Notable leaf components `[PW]`
- **`AL/Selection` / `EL2/Selection`** read `[CascadingParameter] OddsUpdateType` to animate odds
  changes; they are the highest-frequency re-render target (driven by `OnOddsChange`).
- **`MyBetsCashoutButton`** reflects the live cashout offer (`OnCashOutChange`) with a 7 s
  cashed-out display window.
- **Stubs:** `EL2/BossBoosts`, `EL2/Upcoming`, `UserBalance` (placeholder), EL2 bet-section
  (placeholder text) — incomplete EL2 surface. **[PW]** (work-in-progress).

Full per-component parameter/EventCallback catalogue: platform-web `docs/ComponentHierarchy.md`
(source of this summary). Data sources per component: [ComponentDataFlow.md](ComponentDataFlow.md).
