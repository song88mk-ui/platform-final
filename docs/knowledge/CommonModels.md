# Common Models (Quick Reference)

> Phase 9. The domain entities + enums you touch most. Authoritative: [DomainModel.md](../DomainModel.md),
> [SharedLibraries.md](../SharedLibraries.md), [Database.md](../Database.md). All **[Verified]**.

## Where each model lives
| Model family | Store | Folder |
|--------------|-------|--------|
| Bet / BetSelection / audit / config | SQL `BettingContext` | `Platform.Shared/Data/Betting/Entities` |
| Client / Operator / ConfidenceFactor | SQL `PlatformContext` | `Data/Platform` |
| Curation (top fixtures/leagues, ordering, templates) | SQL `SportsbookContext` | `Data/Sportsbook/SQLEntities` |
| Fixture / Market / Selection / MarketMapping (read model) | **MongoDB** | `Data/Sportsbook/Entities` |
| Translations | SQL `TranslationContext` | `Data/Translation` |
| Casino / ESport / RedCat bets | SQL (own contexts) | `Data/{CasinoBetting,ESportBetting,RedCatBetting}` |
| Reports / WinLoss / GGR | SQL `AgentReportsContext` | `Data/Reports` |
| Wallet | SQL (stored procs; **no entities**) | `Platform.Wallet.API` |

## Core betting entities `[Verified]`
- **`Bet`** (PK `BetId` Guid): stake breakdown (`Stake/TrueStake/StakeReal/StakeBonus/StakeTax/TaxedStake`),
  `Odds`(decimal)+`OddsTypeId`+`OddsRef`+`OddsChangeTypeId`, payout (`Payout/WinLoss/WinningsTax`,
  parlay bonus), `BetStatusId`, `BetTypeId` (Single/Parlay/PriceBoost/BetBuilder), cashout flags,
  `IsResettled`, currency/channel/dates.
- **`BetSelection`** (PK `BetSelectionId`, FK `BetId`): full Sport→Selection path + names,
  `Odds`+`OddsRef`, `Line`, `BetSelectionStatusId`, `DeadHeatFactor`, probabilities, `CorrelationId`, `IsBinary`.
- **Config:** `BetConfig` (ClientId+CurrencyCode; JSON blobs), `UserBetConfig`.
- **Audit:** `BetAudit`, `BetSelectionAudit`, `ParlayBonusAudit`, `PartialCashoutAudit` (append-only).
- **Limits:** `StakeLimitFixture`, `Tier`, `League`.

## Catalogue read-model entities (Mongo, abbreviated fields) `[Verified]`
- **`Fixture`** (`FiId/PrFiId/SpId/SegId/LeId/FiTyId/FiStId/FiStDa/Sc/Ho*/Aw*/…Tr`),
  **`Market`** (`MaId/MaTeId/FiId/MaStId/Li/MaNa…`), **`Selection`** (`SeId/MaId/Od` + `MaOd/HoOd/InOd`
  + probabilities + `IsAc`), **`MarketMapping`**, **`BettingStatus`**, **`Maintenance`**.
  platform-final **reads only** (odds-engine-2 writes). See [MongoDb.md](../MongoDb.md).

## Most-used enums `[Verified]`
| Enum | Values |
|------|--------|
| `BetStatus` | Waiting=1000, Running=2000, Won=3000/HalfWon=3001, Lost=4000/HalfLost=4001, Void=5000, Rejected=6000, Cancelled=7000, CashedOut=8000 |
| `MarketStatus` | Active=1, Inactive=0, Suspended=-1, HandedOver=-2, Settled=-3, Cancelled=-4, NotMapped=99 |
| `FixtureStatus` | Prematch, MatchAboutToStart, Live, Suspended, Ended, Closed, Cancelled, Abandoned, Delayed, Postponed, Interrupted, Unknown |
| `FixtureType` | Match=0, Outright=1 · `FixtureScope` Early=-1/Today=0/Live=1 |
| `BetType` | Single=0, Parlay=1, PriceBoost=2, BetBuilder=3 |
| `OddsType` | Decimal=0, Malaysian=1, HongKong=2, Indonesian=3 · `OddsChangeType` None=0/Any=1/Higher=2 |
| `ChannelType` | BG=0, EM=1, OD=2, SR=3 (provider routing) · `Channel` Internet…Agent (placement) |
| `StatusCode` | 100 Success … 157 BetBuilder rules, 205 Maintenance, 1000 Error, 1001 Operator_Error |
| `CashOutStatus` | Available=1, Unavailable=-1, Closed=-2 · `CashoutType` Full=1/Partial=2 · `VoidFactor` Zero/Half/One |
| `SettlementStatus`, `Product`, `SportGroup`, `LanguageCode` (21), `TransactionType`, `UserFavoriteHierarchy` | see [BusinessGlossary.md](../BusinessGlossary.md) |

Full enum semantics + lifecycle: [BusinessGlossary.md](../BusinessGlossary.md), [EntityLifecycle.md](../EntityLifecycle.md).

## Key relationships `[Verified]`
`Bet 1─* BetSelection`; catalogue `Sport→Segment→League→Fixture→Market→Selection`. **Cross-store links
are by value** (`FixtureId`, `ClientId`, `CorrelationId`) — no FKs between SQL and Mongo. See
[Relationships.md](../Relationships.md).
