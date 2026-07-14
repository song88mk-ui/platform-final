# Business Glossary

> Phase 3. Domain vocabulary as the **code actually uses it** — including where the platform's
> terms differ from generic sportsbook language. Confidence **High [Verified]** unless noted.

## Catalogue hierarchy
| Term | Meaning in this platform | Notes |
|------|--------------------------|-------|
| **Sport** | Top-level discipline (Soccer, Tennis, …). `SpId/SpNa`. | |
| **Segment** | Sub-grouping under a sport — region/category (e.g. "Europe", "International"). `SegId/SegNa`. | The generic "**Competition**" concept maps here or to League. No `Competition` entity. **[Verified]** |
| **League** | A competition/tournament (Premier League, La Liga). `LeId/LeNa`; `LeBeId` = Betradar id. | The generic "**Tournament**" maps here. **There is no `Tournament` entity.** **[Verified]** |
| **Fixture** | A single event — a **Match** *or* an **Outright**. `FiId`; `PrFiId` = provider id. | `FixtureType{Match=0, Outright=1}`. **[Verified]** |
| **Participant / Team** | Home/Away competitors. `HoId/AwId` (+ `HoBeId/AwBeId`). | **Denormalized onto the fixture; not a first-class entity.** **[Verified]** |
| **Market** | A betting market on a fixture (Match Winner, Over/Under). `MaId`; `Li` = line. | Typed by a **Market Template**. **[Verified]** |
| **Market Template** | Reusable market *type* definition per sport (`SelectorMarketTemplate`, `MaTeId/MarketTemplateCode`). | Drives naming/ordering of markets. **[Verified]** |
| **Market Group / Market Template Group** | Display grouping of market templates by sport + fixture status (e.g. "Main Markets"). `MarketTemplateGroup`. | Per-client override: `ClientMarketTemplateGroup`. **[Verified]** |
| **Selection** | A bettable outcome within a market (Home, Over 2.5, Player X 2+). `SeId`; `SeTeId` = template. | Carries odds + active flag. **[Verified]** |
| **Outright** | A long-term market not tied to one match (League Winner, Top Scorer). | A `Fixture` with `FiTyId=Outright(1)`. **[Verified]** |

## Bet products & promotions
| Term | Meaning |
|------|---------|
| **Bet** | A placed wager (`BetId`). `BetType`: **Single**(0), **Parlay**(1), **PriceBoost**(2), **BetBuilder**(3). |
| **BetSelection** | One leg of a bet. Single = 1; Parlay/BetBuilder = N. |
| **Single** | One-selection bet. |
| **Parlay** (accumulator) | Multi-selection bet; odds multiply; all must win. May earn a **Parlay Bonus**. |
| **Parlay Bonus** | Extra winnings when a qualifying parlay (≥ min selections at ≥ min odds) wins; capped by `ParlayBonusMaxWinnings`. Modeled in `ParlayBonusAudit` / `ParlayBonusService`. |
| **Bet Builder** | Combine multiple selections **from the same fixture**; eligible markets gated by `BetBuilderMarketTemplates`. |
| **Price Boost** | Operator-enhanced odds on a selection; tracked via `BetPriceBoost`. |
| **Player Props** | Markets on individual player stats; gated by `MarketTemplatePlayerProps`. |
| **Top Fixtures / Top Leagues** | Operator-curated featured content per client (`ClientTopFixture`, `ClientTopLeague`). |
| **Top Market** | Per-market bet-volume rollup (`ClientTopMarket`); also a real-time feed (`SubscribeToTopMarkets`). |
| **Bet Recommendations / Boosted Fixtures** | Curated promotional fixture lists served by gateway read endpoints. |

## Money, odds & risk
| Term | Meaning |
|------|---------|
| **Stake** | Amount wagered. Breakdown: `Stake` (face), `TrueStake`, `StakeReal` vs `StakeBonus`, `StakeTax`/`TaxedStake`. |
| **Payout** | Potential return; `WinLoss` = realized result; `WinningsTax`/`TaxedWinnings` applied per `TaxConfig`. |
| **Odds** | Price of a selection. Stored as **decimal**; displayed in 4 formats. `OddsRef` = price at placement. |
| **Odds Type** | `Decimal=0, Malaysian=1, HongKong=2, Indonesian=3` (`OddsType`). No American/Fractional. Conversion in `OddsTypeHelper`. |
| **Odds Change Type** | `None=0, Any=1, Higher=2` (`OddsChangeType`) — user's tolerance for odds movement at placement. |
| **Dead Heat** | Tie among winners → stake split via `DeadHeatFactor` on the selection. |
| **Void Factor** | Settlement reduction: `Zero=0` (full void/refund), `Half=1`, `One=2` (`VoidFactor`). |
| **Cashout** | Settle a running bet early for a computed value. `CashoutType{Full=1, Partial=2}`. Partial leaves residual stake. |
| **Tier / Liability** | `Tier` (MaxStake/MaxPerGame) mapped per `League`; `StakeLimitFixture` tracks remaining exposure per user+fixture; `CompanyShare` on a bet. |
| **CCF / Confidence Factor** | Risk parameter recomputed by `Job.CCF` (`PlatformContext.ConfidenceFactor`). |
| **Free Bet / Bonus stake** | `IsFreeBet`, `StakeBonus`, `BonusId` on a bet. |

## Status vocabulary (see [EntityLifecycle.md](EntityLifecycle.md) for transitions)
| Term | Values |
|------|--------|
| **Fixture Status** | `Prematch, Live, Suspended, Ended, Closed, Cancelled, Abandoned, Delayed, Unknown, Postponed, Interrupted, MatchAboutToStart` |
| **Fixture Status Group** | `All=0, Prematch=1, Live=2` |
| **Fixture Scope** | `Early=-1, Today=0, Live=1` (list-filtering buckets) **[Verified]** |
| **Match Status** | sport-specific period/period-end codes (`Ended=100`, `AET=110`, `AP=120`, …) |
| **Market Status** | `Active=1, Inactive=0, Suspended=-1, HandedOver=-2, Settled=-3, Cancelled=-4, NotMapped=99` |
| **Bet Status** | `Waiting=1000, Running=2000, Won=3000, HalfWon=3001, Lost=4000, HalfLost=4001, Void=5000, Rejected=6000, Cancelled=7000, CashedOut=8000` |
| **Settlement Status** | `Pending, Lost, Won, Void, HalfWon, HalfLost, Cancelled` |
| **Cashout Status** | `Available=1, Unavailable=-1, Closed=-2` |
| **Selection "status"** | No dedicated enum: live = `IsAc` (active) bool + `MaStId`; placed leg = `BetSelectionStatusId` (uses BetStatus-style values). **[Verified]** |

## Channels, products, routing
| Term | Meaning |
|------|---------|
| **ChannelType** | **Provider/feed routing**: `BG=0` (BetGenius/GTS), `EM=1`, `OD=2` (OddsFeed), `SR=3` (Sportradar/MTS). Drives `IBetProviderFactory`. **[Verified]** |
| **Channel** | **Placement channel**: `Internet, Retail, Terminal, Mobile, Sms, CallCentre, TvApp, Agent`. **[Verified]** |
| **Provider** | A bet validation/risk backend (MTS, GTS). A `Provider` enum file exists but its contents were not confirmed here — **[Unknown]**; routing is by `ChannelType`. |
| **Product** | `All=0, Sportsbook=1, VirtualGames=2, Casino=3, LiveCasino=4`. |
| **Sport Group** | `All, Sports, eSports, VirtualSports, SportsAndVirtualSports, SimulatedRealitySports`. |
| **Client / ClientCode** | Operator/brand tenant. `ClientId` (Guid) ↔ `ClientCode` (string); resolved + cached by `AuthorizationFilter`. |
| **Operator Token** | Opaque end-user session token (`x-operator-token`); validated downstream, not by platform-final. |
| **OG (Oriental Gaming)** | A special client integration with extra `x-og-*` headers and player context. |

## Translation
| Term | Meaning |
|------|---------|
| **Dynamic translation** | Versioned, hashed, per-language string translations (`SportsbookDynamic`); supports "missing target" detection + glossary versions. |
| **Static translation** | Fixed label map (`SportsbookStatic`: `LanguageCode`+`Name`→`Value`). |
| **Inline translations** | Per-language name arrays embedded in odds/fixture messages (`SpNaTr/SegNaTr/LeNaTr/FiNaTr/MaNaTr/SeNaTr`), each `{ LaCo, name }`. |
| **Glossary Version** | Integer version on dynamic translations enabling staged rollout/rollback. |

## Abbreviated field decoder (MongoDB / WebSocket wire names)
`Fi`=Fixture, `Sp`=Sport, `Seg`=Segment, `Le`=League, `Ma`=Market, `Se`=Selection,
`Ho`=Home, `Aw`=Away, `St`=Status, `Ty`=Type, `Da`=Date, `Na`=Name, `Id`=Id, `Tr`=Translations,
`Od`=Odds (`DeOd`/`MaOd`/`HoOd`/`InOd` = Decimal/Malay/HK/Indo), `Li`=Line/Livestream,
`So Or`=SortOrder, `Pr`=Probability, `Is…`=boolean flag, `Cu`=Current, `Sc`=Score,
`Co`/`CoId`=CorrelationId, `LaCo`=LanguageCode, `ChTyId`=ChannelTypeId, `ClCo`=ClientCode.
**[Verified]** — see [WebSocketContracts.md](WebSocketContracts.md) for the full odds-message map.
