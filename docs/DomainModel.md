# Sportsbook Domain Model

> **Phase 3 — Sportsbook Domain Analysis.** Business-knowledge focus. Analysis-only; no code
> modified. Confidence: **High [Verified]** for the hierarchy, store boundaries, Mongo
> collections, enums, and SQL entities; **[Likely]** for some individual field semantics extracted
> at scale; **[Unknown]** flagged inline. Companion docs: [BusinessGlossary.md](BusinessGlossary.md),
> [BusinessRules.md](BusinessRules.md), [EntityLifecycle.md](EntityLifecycle.md),
> [Relationships.md](Relationships.md).

## 0. The single most important domain fact: three stores, three owners

platform-final's sportsbook domain is **not one model in one database**. It splits by *who owns
the truth*: **[Verified]**

| Concern | Store | Source of truth | Authored by | Consumed/rendered by |
|---------|-------|-----------------|-------------|----------------------|
| **Live catalogue** (Fixture, Market, Selection, odds, scores, statuses) | **MongoDB** `Sportsbook` DB | **odds-engine-2** (UOF/Betradar feed) → published as `OnOddsChange`/`OnMarketChange` | external feed, **not** platform-final | `Sportsbook.API` (reads Mongo) → gateway REST + WebSocket push |
| **Curation / config** (top fixtures/leagues, sort orders, market-template groups, announcements, player props) | **SQL** `SportsbookContext` | **platform-final** | back office via `Platform.API/InternalController` | `Sportsbook.API` read endpoints |
| **Wagers** (Bet, BetSelection, cashout, audit, bet config) | **SQL** `BettingContext` | **platform-final** | `Betting.API` / `Placement.Servicev2` (create); `Settlement.Service` / `CashOut.Service` (update) | gateway bet-history endpoints; reporting |

Plus two supporting stores: **Translation** (`TranslationContext`, SQL) and **Wallet**
(`WalletContext`, SQL — owned by `Wallet.API`). **[Verified]**

> **Takeaway for engineers:** the fixture/market/selection you see in an API response is a
> **read model**. platform-final does not create or grade fixtures/odds — it ingests them from
> odds-engine-2. The platform *owns* bets, curation, and translations.

## 1. The entity hierarchy

```
SportGroup (SQL config: groups of sports — Sports / eSports / VirtualSports)
   └─ Sport
        └─ Segment            (a.k.a. region/category; the "Competition" concept maps here)
             └─ League         (the "Tournament"/"Competition" concept maps here)
                  └─ Fixture   (a match OR an Outright; FixtureType{Match=0, Outright=1})
                       ├─ Participant (Home/Away — denormalized on the fixture, not a 1st-class entity)
                       └─ Market (a betting market, typed by MarketTemplate)
                            └─ Selection (a betting outcome; carries odds in 4 formats)
```
**[Verified]** — confirmed by the lookup projections (`SportsLookupProjection`,
`SegmentsLookupProjection`, `LeaguesLookupProjection`) and the `FiId/SpId/SegId/LeId/MaId/SeId`
key chain across the Mongo entities.

> Terminology note: the codebase uses **Sport → Segment → League**, not
> "Competition/Tournament". There is **no `Tournament` entity** and only an incidental
> `Competition` reference. See [BusinessGlossary.md](BusinessGlossary.md).

## 2. Live catalogue entities (MongoDB `Sportsbook`, read model)

Collections (`Platform.Sportsbook.API/Repository/SportsbookRepository.cs:110-124`): **[Verified]**
`Fixture`, `Market`, `MarketMapping`, `Selection`, `BettingStatus`, `Maintenance` — each opened
with both **secondary** (default reads) and **primary** read preferences. Connection key
`ConnectionStrings:MongoDB-Sportsbook`; DB name `Sportsbook`.

Documents use **abbreviated field names** (to keep the feed/payload small). Defined in
`Platform.Shared/Data/Sportsbook/Entities/`. **[Verified]** (files exist) / field lists **[Likely]**:

### Fixture (`Entities/Fixture.cs`)
- **Identity:** `FiId` (long, key), `PrFiId` (provider/Betradar id e.g. `sr:match:…`), `FiSp` (specifier), `ChTyId` (channel type).
- **Classification:** `SpId/SpNa` (sport), `SegId/SegNa` (segment), `LeId/LeNa/LeBeId` (league), `FiTyId/FiTyNa` (Match/Outright).
- **State:** `FiStId/FiStNa` (fixture status), `FiStDa` (start), `Sc` (score), `CuPe/CuMi` (period/minute), `IsOfLi` (offered live), `IsSu` (suspended), `IsBo` (boosted), `LiId` (livestream), `GeAl` (geo allowed).
- **Participants:** `HoId/HoBeId` (home), `AwId/AwBeId` (away) + denormalized `HoReCa/AwReCa` (red cards), `PeSc` (period scores).
- **Translations:** inline arrays `SpNaTr/SegNaTr/LeNaTr/FiNaTr` (per `LaCo` language code).
- **Projections** (lighter read shapes in `Sportsbook.API/Models/`): `ActiveFixtureProjection`,
  `FixtureDetailsLimitedProjection`, `BetBuilderFixtureProjection`, lookup projections.

### Market (`Entities/Market.cs`)
- `MaId` (key), `MaTeId` (market-template id), `FiId` (parent), `MaNa`, `Li` (line, e.g. `2.5`),
  `MaStId/MaStNa` (market status), `InPl` (in-play), `IsBo` (boosted), `MaSoOr` (sort), `MaNaTr` (translations).
- `MarketMapping` collection maps a market to its provider specifier (`MaSp`) for odds/settlement routing. **[Verified]** (collection exists).

### Selection (`Entities/Selection.cs`)
- `SeId` (key), `MaId` (parent), `FiId`, `SeTeId` (selection-template id), `SeNa`, `SeSp` (specifier),
  `Od` (**decimal odds**, the base), `IsAc` (active), `SeSoOr` (sort), `CoId` (correlation id), `SeNaTr`.
- `SelectionExtension` adds `OddsRef` and the other formats `MaOd` (Malaysian), `HoOd` (Hong Kong),
  `InOd` (Indonesian), plus odds-engine-2 **probabilities** `Pr/WoPr/LoPr/VoPr/HaWoPr/HaLoPr`. **[Verified]** (extension class) / **[Likely]** (full list).

### Operational flags
- `BettingStatus` collection — global bet-stop flag (`IsBeSo`). `Maintenance` collection —
  maintenance + bet-stop flags. Read before serving catalogue. **[Verified]**

## 3. Curation / config entities (SQL `SportsbookContext`)

Defined in `Platform.Shared/Data/Sportsbook/SQLEntities/`. **[Verified]**. Two scopes:

- **Per-client (`ClientId`):** `ClientTopFixture`, `ClientTopLeague`, `ClientSportOrder`,
  `ClientSegmentOrder`, `ClientLeagueOrder`, `ClientMarketTemplateOrder`,
  `ClientMarketTemplateGroup`, `ClientSelectorMarketTemplate` (`…2` table), `ClientAnnouncements`.
- **Global:** `SportGroup` (`SportGroup2`), `SelectorMarketTemplate` (`…2`),
  `MarketTemplateGroup` (`…2`), `TierLeagueOrder`, `BetBuilderMarketTemplates`,
  `MarketTemplatePlayerProps`, `Country`, `Announcements`, `LanguageAnnouncements`.
- **User-generated:** `UserFavorite` (per `UserId`, `UserFavoriteHierarchy` = League/Fixture).

**Authoring:** `Platform.API/InternalController` → `Sportsbook.API` repo. **[Verified]** for
`topfixtures`, `topleagues`, `announcements`, and bet `config`; the various `*Order` tables have
**no explicit BO endpoint found** — **[Unknown]** (likely managed by an admin tool / config flow).
See [Relationships.md](Relationships.md) for the create/update/consume matrix.

## 4. Wager entities (SQL `BettingContext`)

Defined in `Platform.Shared/Data/Betting/Entities/`. **[Verified]** (files) / fields **[Likely]**:

- **`Bet`** (key `BetId` Guid): user/client identity, **stake breakdown** (`Stake`, `TrueStake`,
  `StakeReal`, `StakeBonus`, `StakeTax`, `TaxedStake`), **odds** (`Odds` decimal + `OddsTypeId/Name`
  display format + `OddsRef` + `OddsChangeTypeId`), **payout** (`Payout`, `WinLoss`, `WinningsTax`,
  parlay bonus), **status** (`BetStatusId/Name`, `Reason`), **type** (`BetTypeId` =
  Single/Parlay/PriceBoost/BetBuilder), **cashout flags** (`IsCashout`, `IsPartialCashout`),
  `IsResettled`, currency/exchange, channel, dates.
- **`BetSelection`** (key `BetSelectionId`, FK `BetId`): full Sport→…→Selection denormalized path,
  `Odds`+`OddsRef`, `Line`, `BetSelectionStatusId`, `DeadHeatFactor`, probabilities, `CorrelationId`,
  `IsBinary`, settlement score. **A Bet has 1 selection (Single) or N (Parlay/BetBuilder).**
- **Config:** `BetConfig` (per `ClientId`+`CurrencyCode`; JSON blobs `DefaultConfig`,
  `OverrideConfig`, `ParlayConfig`, `TaxConfig`, `PriceboostConfig`, `BetBuilderConfig`),
  `UserBetConfig` (per-user overrides).
- **Limits/liability:** `StakeLimitFixture` (remaining stake per user+fixture), `Tier`
  (MaxStake/MaxPerGame), `League` (league→tier mapping).
- **Promotions:** `BetPriceBoost` (boosted bets), `ParlayBonusAudit` (awarded parlay bonus).
- **Cashout:** `PartialCashoutAudit` (full & partial cashout ledger per `BetId`).
- **Audit:** `BetAudit`, `BetSelectionAudit` (snapshot per state change).
- **Other:** `BetInfo` (device/UA metadata), `ClientTopMarket` (per-market bet-count rollup).

> **Odds storage rule [Verified]:** bets store **decimal odds only**; the other formats
> (Malay/HK/Indo) are a **display concern computed at the API layer** (`OddsTypeHelper`), not
> persisted on the bet. `OddsRef` holds the placement-time reference for odds-change detection.

## 5. Cross-cutting entities

- **Translation** (`TranslationContext`): `SportsbookDynamic` (versioned, hashed, per-language
  string translations with a "missing target" filter) and `SportsbookStatic` (fixed
  `LanguageCode`+`Name`→`Value` labels). Served by `v1/sportsbook/translations`; also delivered
  **inline** on odds messages (`SpNaTr/SegNaTr/LeNaTr/FiNaTr/MaNaTr/SeNaTr`). 21 `LanguageCode`s,
  heavily SE-Asian. **[Verified]**
- **Odds** is not a standalone entity — it lives on `Selection` (live) and `BetSelection`
  (placed). Formats via `OddsType{Decimal=0, Malaysian=1, HongKong=2, Indonesian=3}`; movement via
  `OddsChangeType{None=0, Any=1, Higher=2}`; conversion in `Platform.Shared/Helpers/OddsTypeHelper.cs`.
  **[Verified]**
- **"Trading"** has **no representation in platform-final** (grep = 0). It is an **upstream**
  odds-engine/bet-genius concept (risk/odds management). **[Verified]** — see
  [CrossRepositoryDependencies.md](CrossRepositoryDependencies.md).

## 6. Special bet products

- **Outright** — a `Fixture` with `FiTyId = Outright(1)`: no live match, selections are
  season-long entrants (winner, top scorer). **[Verified]**
- **BetBuilder** — combine multiple selections from the **same fixture**; eligible markets gated by
  `BetBuilderMarketTemplates` (Prematch/Live flags); combined odds via
  `POST v1/sportsbook/betbuilder/selections/odds`. **[Verified]**
- **Price Boost** — operator-enhanced odds; tracked via `BetPriceBoost` + `BetType.PriceBoost`;
  boosted names/odds can be per-client (`ClBoSeNa…`). **[Verified]**
- **Player Props** — player-stat markets gated by `MarketTemplatePlayerProps`. **[Verified]**
- **Parlay** — multi-selection accumulator; may earn a **parlay bonus** (see
  [BusinessRules.md](BusinessRules.md)). **[Verified]**

## 7. Where each entity is created / updated / consumed / rendered
See [Relationships.md](Relationships.md) §"Ownership & lifecycle matrix" for the full table.
Lifecycle state machines are in [EntityLifecycle.md](EntityLifecycle.md).
