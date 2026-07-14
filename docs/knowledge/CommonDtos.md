# Common DTOs (Quick Reference)

> Phase 9. The DTOs you meet most often (the 189 in `Platform.Shared/Models` plus per-project models).
> Authoritative shapes: [PublicApiSurface.md](../PublicApiSurface.md), [DomainModel.md](../DomainModel.md).
> All **[Verified]** (structure) / **[Verified: consumer]** (field lists confirmed via platform-web).

## The envelope (every API response) `[Verified]`
```csharp
public class BaseResponseDTO<T> { StatusCode StatusCode; string Message; T Result; }
```
Also `BaseModel<T>` (consumer-side variant). Success/Maintenance → HTTP 200; else HTTP 400 with body.
Branch on `StatusCode`, **not** HTTP status. Enum values: [PublicApiSurface.md](../PublicApiSurface.md) §StatusCode.

## Request DTOs (most-used) `[Verified]`
| DTO | Used by | Key fields |
|-----|---------|-----------|
| `BetRequestDTO` | `POST v2/bet/placement`, `v1/bet/maxstake` | Stake, OddsType, OddsChangeType, Channel, IPAddress(+V6), IsBoosted, IsBetBuilder, BonusId, TotalOdds, `List<BetSelectionDTO> Selections` |
| `BetSelectionDTO` | inside BetRequestDTO | SportId, FixtureId, MarketId, SelectionId, Odds, CorrelationId |
| `BetCashoutRequestDTO` | `POST v1/bet/cashout` | BetId, Channel, IPAddress, CashoutStake, CashoutStakePercent? (null=full) |
| `BetHistoryRequestDTO` | `POST v2/bet/history` | FromDate, ToDate, BetStatusFilter, CasinoBetStatusFilter, ProductFilter, Page, PageSize, IsCurrent, BetId, `List<int>` BetType/BetStatus |
| `BetStatusRequestDTO` | `GET v1/bet/status` | BetId (Guid) |
| `BetBuilderSelectionsOddsRequestDTO` | `POST .../betbuilder/selections/odds` | `List<BetSelectionOddsRequest>` |
| `LiveStreamRequestDTO` | `GET v1/sportsbook/livestream` | ProviderFixtureId, IPAddress, IsMobile |
| `CreditOrDebitRequestDTO` | wallet ops | ClientId, UserId, Amount, CurrencyCode, Direction, TransactionType, ReferenceId |
| `MarketsSubscriptionRequestDTO` | ClientSportsbook subscribe | FixtureId, `List<string> MarketIds` |

## Response DTOs (most-used) `[Verified]`
| DTO | Returned by | Notes |
|-----|-------------|-------|
| `FixtureResponseDTO` | fixture/detail endpoints | wraps fixtures→markets→selections |
| `SportsTreeResponseDTO` | `GET v1/sportsbook/sports` | sport/segment/league tree |
| `BetConfigResponseDTO` | `GET v1/bet/config` | DefaultSportConfig/OverrideSportConfig/ParlayConfig/TaxConfig/PriceboostConfig/BetBuilderConfig |
| `BetResponseDTO` | placement | bet + per-selection state |
| `WalletBalanceResponseDTO` | `GET v1/wallet/balance` | UserId, Balance, Payable, CurrencyCode, RefCurrencyCode |
| `UserFavoriteResponseDTO` | favorites | |
| `ActiveFixturesResponseDTO` | ClientSportsbook | |
| `TranslationResponseDTO` | translations | key→value |
| `CasinoBetHistoryResponseDTO`, `MaxStakeResponseDTO`, `BoostedResponseDTO`, `SearchResultDTO`, `MarketTemplatePlayerPropsResponseDTO` | various | |

> The big aggregate `TWResponseDTO.cs` (Fixture/Market/Selection DTOs) is one of the most-changed files
> (65 changes) — see [FrequentlyModifiedAreas.md](FrequentlyModifiedAreas.md).

## Mongo projection DTOs (read model) `[Verified]`
`ActiveFixtureProjection`, `FixtureDetailsLimitedProjection`, `BetBuilderFixtureProjection`,
`MarketLimitedProjection`, `Sports/Segments/LeaguesLookupProjection`, `CashOutMarketFilter`/
`CashOutSelectionFilter`. Read via `.Find(...).Project<T>()`. See [MongoDb.md](../MongoDb.md).

## Conventions `[Verified]`
- Naming: `<Thing>RequestDTO` / `<Thing>ResponseDTO`; mapping is **manual** via `.Select` (no AutoMapper).
- Odds carried as **decimal** on bets; 4 display formats only at the edge ([BusinessGlossary.md](../BusinessGlossary.md)).
- DTOs are **not shared** cross-repo — consumers (platform-web/oddsfeed/backoffice) define their own,
  aligned by field name ([Contracts.md](../Contracts.md)).
