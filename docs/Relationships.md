# Entity Relationships

> Phase 3. How the domain entities relate, and the **ownership / source-of-truth / create-update-
> consume-render** matrix the brief asks for. Confidence **High [Verified]** for cardinalities and
> stores; **[Unknown]** flagged for unlocated authoring paths.

## 1. Catalogue relationship graph (read model — MongoDB)
```
SportGroup 1───* Sport
   Sport 1───* Segment
   Segment 1───* League
   League 1───* Fixture
   Fixture 1───2 Participant (Home, Away — embedded, not a separate doc)
   Fixture 1───* Market
   Market 1───* Selection
   Market *───1 MarketTemplate     (Market.MaTeId → SelectorMarketTemplate)
   Market 1───1 MarketMapping      (provider specifier for odds/settlement routing)
   Selection *───1 SelectionTemplate (Selection.SeTeId)
```
Keys are the abbreviated `FiId / SpId / SegId / LeId / MaId / SeId` chain; every level also carries
inline translation arrays. **[Verified]**

## 2. Wager relationship graph (SQL — BettingContext)
```
Bet 1───* BetSelection            (Single = 1 leg; Parlay/BetBuilder = N legs)
Bet 1───* BetAudit                (snapshot per state change)
BetSelection 1───* BetSelectionAudit
Bet 1───0..1 BetPriceBoost        (only boosted bets)
Bet 1───0..1 BetInfo              (device/UA metadata)
Bet 1───0..* PartialCashoutAudit  (one row per (partial) cashout)
Bet *───1 BetConfig               (by ClientId + CurrencyCode)
Bet *───0..1 UserBetConfig        (by UserId)
Bet/BetSelection → StakeLimitFixture (by UserId + FixtureId; liability)
League *───1 Tier                 (League.Tier → Tier limits)
Parlay settlement ──▶ ParlayBonusAudit (if qualifying)
Bet/Market ──▶ ClientTopMarket    (per-market bet-count rollup)
```
**[Verified]** (cardinalities from entity keys/FKs) / settlement-derived rows **[Likely]**.

## 3. Cross-store links (by value, not FK)
The read model (Mongo) and wagers (SQL) are **not** joined by database FKs — they link by **shared
ids carried in messages/DTOs**: **[Verified]**
- `BetSelection.FixtureId / MarketId / SelectionId` reference Mongo `FiId/MaId/SeId`.
- `CorrelationId` ties a placed selection back through the provider/settlement flow.
- `MarketMapping.MaSp` (provider specifier) links a market to odds-engine-2 settlement events.
- Curation tables (`ClientTopFixture.FixtureId`, `ClientTopLeague.LeagueId`, …) reference catalogue
  ids by value.

> Consequence: there is **no referential integrity across stores**; consistency is eventual and
> id-based. A bet can reference a fixture that no longer exists in the live Mongo read model.

## 4. Ownership & lifecycle matrix (the brief's per-entity questions)

| Entity | Store / Source of truth | Created where | Updated where | Consumed where | Rendered where |
|--------|-------------------------|---------------|---------------|----------------|----------------|
| **Sport / Segment / League** | Mongo (read model) ← odds-engine-2 | feed | feed (`OnMarketChange`) | Sportsbook.API lookups | gateway sports-tree, WebSocket |
| **Fixture** | Mongo `Fixture` ← odds-engine-2 | feed | feed (`OnOddsChange`, status) | Sportsbook.API | gateway fixture lists/details, WebSocket |
| **Market** | Mongo `Market` ← odds-engine-2 | feed | feed (`OnMarketChange`, bet-stop) | Sportsbook.API | fixture details, WebSocket |
| **MarketMapping** | Mongo `MarketMapping` ← feed | feed | feed | odds/settlement routing | (internal) |
| **Selection / Odds** | Mongo `Selection` ← odds-engine-2 | feed | feed (`OnOddsChange`) | Sportsbook.API | fixture details, betslip, WebSocket |
| **Participant (Home/Away)** | embedded on Fixture | feed | feed | with fixture | with fixture |
| **MarketTemplate / Group** | SQL `SportsbookContext` (global) | **[Unknown]** authoring | **[Unknown]** | Sportsbook.API | market ordering/grouping |
| **SportGroup / Country / TierLeagueOrder** | SQL (global) | **[Unknown]** | **[Unknown]** | Sportsbook.API / gateway | sport grouping/geo |
| **ClientTopFixture / ClientTopLeague** | SQL (per-client) | BO `v1/client/topfixtures`,`topleagues` | same (full-replace) | gateway top* reads | featured content |
| **Client\*Order / ClientMarketTemplateGroup** | SQL (per-client) | **[Unknown]** (admin/config) | **[Unknown]** | Sportsbook.API | per-client ordering |
| **UserFavorite** | SQL (per-user) | gateway `POST userfavorites` | gateway add/remove | gateway favorites reads | favorites UI |
| **Announcement (+Client/Language)** | SQL (global+junctions) | BO `announcements/post` | BO post/delete | gateway `announcements` | banners |
| **BetConfig / UserBetConfig** | SQL `BettingContext` | BO `v1/config/client` → Betting.API | same | Betting.API at placement | (limits applied) |
| **Bet / BetSelection** | SQL `BettingContext` (truth) | Betting.API → Placement.Servicev2 | Settlement.Service, CashOut.Service, Job.Resettlement | gateway bet-history, reporting | bet history / betslip |
| **BetPriceBoost** | SQL | placement (boosted) | settlement | reporting | (boost flag) |
| **PartialCashoutAudit** | SQL | CashOut.Service | — (append-only) | reporting | cashout history |
| **ParlayBonusAudit** | SQL | settlement (qualifying parlay) | — | reporting | bonus display |
| **BetAudit / BetSelectionAudit** | SQL | every bet state change | append-only | audit/reporting | BO audit views |
| **ClientTopMarket** | SQL | `Client.TopMarket` worker | worker | reporting | top-markets feed |
| **StakeLimitFixture / Tier / League** | SQL | config/placement | placement | placement (liability) | — |
| **SportsbookDynamic / Static (translations)** | SQL `TranslationContext` | translation pipeline | pipeline / `recovery` | `v1/sportsbook/translations` | all localized text |
| **Wallet balance** | SQL `WalletContext` (Wallet.API) | Wallet.API | Wallet.API (credit/debit) | gateway `v1/wallet/balance` | balance display |

(Real-time rendering = SignalR push; see [WebSocketContracts.md](WebSocketContracts.md). REST
rendering = `Platform.API` gateway; see [PublicApiSurface.md](PublicApiSurface.md).)

## 5. The two "join keys" that matter most
1. **`FixtureId` (`FiId`)** — the spine linking catalogue (Mongo), bets (SQL), curation (SQL),
   favorites, and real-time channels (`fixturedetails:{fixtureId}`). **[Verified]**
2. **`ClientId` / `ClientCode`** — the tenant key scoping config, curation, bets, top-markets, and
   per-client routing keys (`r.wallet.{clientCode}`, `r.bet.onbetstatus.{clientId}`). **[Verified]**

## 6. Relationship risks / notes
- **No cross-store FKs** → orphan references are possible (bet → missing fixture). Handle defensively. **[Verified]**
- **Denormalization is pervasive**: `BetSelection` copies the entire Sport→Selection path and names
  so bets are self-describing without joining the (volatile) catalogue. Names can therefore be
  **stale** vs current translations. **[Verified]**
- **Participants are not entities** — anything needing team-level data must read denormalized
  fixture fields, not a Team table. **[Verified]**
- Several per-client ordering tables have **no traced authoring path** (`[Unknown]`); see
  [Unknowns.md](Unknowns.md).
