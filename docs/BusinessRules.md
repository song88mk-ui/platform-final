# Business Rules

> Phase 3. The domain rules enforced by the platform, reconstructed from the validation
> `StatusCode` enum, bet config, the parlay-bonus service, cashout/settlement modeling, and the
> auth filters. Confidence **High [Verified]** where a code/enum/field encodes the rule;
> **[Likely]** where the rule is inferred from a status code whose enforcement path wasn't traced;
> **[Unknown]** flagged. Rules are grouped by lifecycle stage. Status codes cross-ref
> [PublicApiSurface.md](PublicApiSurface.md) §StatusCode.

## A. Access / request validation (pre-bet)
1. **API key required.** `x-api-key` must match `Custom:APIKey` (or `WebSocketAPIKey`), else
   `101`. Internal endpoints require `Custom:InternalAPIKey`. **[Verified]** — see
   [Authentication.md](Authentication.md).
2. **Client must be known & active.** `x-client-id` must be a Guid resolving to an **active**
   `Client` row, else `102`. (ClientSportsbook flow.) **[Verified]**
3. **Language must be supported.** `x-language-code` must be a defined `LanguageCode`, else `106`. **[Verified]**
4. **Operator token** for user-scoped actions is validated **downstream** (Operator/Wallet); a
   missing/invalid token yields `103` / `Operator_Error 1001`. **[Likely]** (not enforced in
   platform-final filters).
5. **IPv4 required** in some bet paths: non-IPv4 → `122 IPAddress_Must_Be_IPv4`. **[Verified]** (code exists).

## B. Bet placement — stake & odds rules
Encoded by `StatusCode` 104–117 and `BetConfig`. **[Verified]** (codes) / **[Likely]** (exact thresholds live in `BetConfig` JSON):
1. **Sufficient funds** — insufficient balance → `104 Insufficient_Funds`.
2. **Currency** must be supported and match the wallet → `105`, `111`.
3. **Odds must be > 1.0** → `112 Odds_Must_Be_Greater_Or_Higher_To_1`.
4. **Stake > 0** → `113`.
5. **Stake within min/max** (from `BetConfig.DefaultConfig` / per-sport `OverrideConfig`) →
   `114` (below min), `115` (above max).
6. **Per-fixture max** — total exposure per game capped (`MaxPerGame`, `StakeLimitFixture`) →
   `116 Maximum_Bet_Amount_For_The_Game_Has_Been_Reached`.
7. **Max payout** — potential payout capped (`ParlayConfig.MaxPayout` etc.) → `117`.
8. **Odds type / channel / fixture type / fixture status** must be supported → `107/108/109/110`.

## C. Bet placement — market/odds state rules
1. **Market must be offered & active.** Suspended market → `124 Market_Is_Suspended`; not offered
   → `125 Market_Is_Not_Offered`; inactive odds → `128 Odds_Is_Inactive`. **[Verified]** (codes).
2. **Odds freshness.** If the selection's odds moved since the client quoted them →
   `126 Odds_Or_Lines_Have_Changed` (consumer alias `Odds_Is_Stale`). The client may re-confirm
   (esp. BetBuilder, which returns `TotalOdds` for reconfirmation). Tolerance governed by
   `OddsChangeType{Any, Higher}`: with `Higher`, only **downward** moves block; upward (better)
   odds auto-accept. **[Verified]** (enum + consumer behavior).
3. **Bet stop.** Fixture-level bet stop → `129`; global bet stop → `130 Betstop_On_Global`. Driven
   by the `BettingStatus`/`Maintenance` Mongo flags + `OnBetStopFixture`/`OnBetStopGlobal`. **[Verified]**.
4. **Betting restrictions** (risk/compliance): global `118`, per-sport `119`, per-segment `120`,
   per-league `121`. **[Verified]** (codes).
5. **Liability/tier** — if no max-liability tier resolves → `131 Max_Liability_By_Tier_Not_Found`;
   MTS provider error → `132 Mts_Error`. **[Verified]** (codes).

## D. Parlay rules
1. **Virtuals cannot be parlayed with other sports** → `145`. **[Verified]** (code).
2. **Parlay bonus eligibility** (`ParlayBonusService` / `ParlayBonusAudit`): awarded when a parlay
   has **≥ minimum selections (3+)** that won, **at ≥ `ParlayBonusMinOdds`** per the client's
   `ParlayBonusAudit` config, capped at `ParlayBonusMaxWinnings`. Bonus % scales with selection
   count (`ParlayBonusConfig[]`: SelectionCount → percent). Evaluated at **settlement** in a
   `ReadCommitted` `TransactionScope`. **[Verified]** (service logic) / threshold values **[Likely]** (config-driven).
3. **Bet builder** counts as a single combined selection on one fixture; eligible markets must be
   in `BetBuilderMarketTemplates` (Prematch/Live flags). Combined odds from
   `v1/sportsbook/betbuilder/selections/odds`. **[Verified]**.

## E. Price-boost rules
1. **No duplicate boost** — placing multiple bets on the same boosted odds is disallowed →
   `147`. **[Verified]** (code).
2. **Boost cannot be parlayed** → `148 Boosted_Odds_Cannot_Be_Parlayed`. **[Verified]**.
3. **Boost expires at kickoff** — boosted odds not allowed once a fixture goes live → `149`. **[Verified]**.
4. **Boost ∉ BetBuilder** → `156 Boosted_Odds_Cannot_Be_Combined_With_Bet_Builder`. **[Verified]**.
5. **A boosted bet is priced at the client's boosted odds** (BO-10). The slip sends the boosted price and
   `GetBetFixtures2` now prices the selection at the client's entry in `Selection.ClBoOd`
   (`CODE:odds|CODE:odds`, written by odds-engine-2 `OnPriceBoost`), so the stale-odds check
   (`OddsChangeType.None` for a boosted bet) passes and the bet is stored at that price. A selection the
   client no longer has a boost on keeps its original odds, so the bet is refused as an odds change.
   The client is matched on its whole code. **[Verified]** (code, unit tests); DEV end to end **[Unknown]**
   until deployed.
6. **Original odds vs client odds.** The boosted price replaces the ORIGINAL odds of the selection. That
   holds while `SportsbookRepository.IsOEEnable` is hardcoded off (no margin: clients are priced with the
   original odds). If clients are priced with CLIENT odds (`IsOEEnable` true, margin from `ClCoMa`) a boost
   has to be defined again against them. `BoostedOddsPricingTests.ClientOddsTripwire_...` fails the day
   `IsOEEnable` can return true. See TD-25. **[Verified]**.

## F. Bet Builder rules
1. **2–10 selections** → `154 Bet_Builder_Must_Have_2_Or_More_Selections`,
   `155 …Cannot_Be_More_Than_10_Selections`. **[Verified]** (codes).
2. **Same fixture only** → `157 Bet_Builder_Must_Be_On_The_Same_Fixture`. **[Verified]**.

## G. Livestream rules
1. **Livestream may be unavailable** for a fixture → `123 Fixture_Has_No_Livestream`. **[Verified]**.
2. **Geo restriction** — livestream blocked by region (`GeAl` on fixture) →
   `146 Livestream_Is_Not_Available_In_Your_Region`. **[Verified]**.

## H. Cashout rules
1. **Availability** — cashout only on **running** bets when liquidity/odds permit
   (`CashOutStatus{Available, Unavailable, Closed}`); unavailable → `141 CashOut_Is_Unavailable`. **[Verified]**.
2. **Value can move** — between quote and confirm the cashout value may change →
   `142 CashOut_Stake_Changed_From_X_To_Y`; client must re-confirm. **[Verified]** (code).
3. **Full vs partial** (`CashoutType`): full closes the bet (`BetStatusId=CashedOut` (8000),
   selections `CashedOut`; note `Bet.IsCashout` is **not** set by `CashOut.Service` — only copied
   during resettlement); partial (`CashoutStakePercent` < 100) cashes a fraction, leaves residual
   stake, sets `IsPartialCashout=true`, and **recalculates remaining payout**; each event writes a
   `PartialCashoutAudit` row. **[Verified]** (code, GM-398 review).
4. Cashout value is maintained live by `CashOutStake.Servicev2` reacting to `OnOddsChange`, and
   pushed to clients via `OnCashOutChange`. **[Verified]**.
5. **Provider gate (pre-acceptance)** — before accepting, `CashOut.Service` resolves the bet's
   provider (`IBetProviderFactory.Resolve(OnCashoutBetSettlement.ChannelTypeId)`, default BG) and
   calls `ValidateCashoutAsync`: BG → GTS Bet Assessment (read-only, `cashout-`-prefixed betId,
   `totalStake=1`); SR → MTS ticket cashout (executes). Decline (including provider auth/API
   errors — fail-fast) → cashout declined, no money moves, `OnBetStatus` pushed to the client.
   Providers self-gate on `Custom:IsProviderEnabled`. **[Verified]** (GM-398).
6. **Provider bet update (post-settlement)** — `Provider.Service` consumes
   `OnBetFullCashOut`/`OnBetPartialCashOut` from `r.bet.operator` and reports the bet to GTS
   with `cashedOutTotalStake`/`cashedOutAmount`/`payout`; legs stay `Open` with `payoutPrice=null`
   (per Genius Sports guidance). `CashOut.Service` stamps cumulative figures
   (`OriginalTrueStake`/`CashedOutTotalTrueStake`/`CashedOutTotalAmount`) on both messages from
   the `PartialCashoutAudit` rows: a partial cashout is reported as an `Open` update (no payout),
   a full cashout as `Settled` with `totalStake` restored to the original stake and `payout` equal
   to the total paid across all cashouts. Messages lacking the cumulative fields (pre-enrichment,
   in flight during deploy) are skipped with a warning. **[Verified]** (GM-398, GM-790).

## I. Settlement rules
1. **Grading** sets `BetSelectionStatusId` per leg, then the `Bet` status:
   Won/HalfWon/Lost/HalfLost/Void. A parlay loses if **any** leg loses; wins only if **all**
   non-void legs win. **[Likely]** (standard parlay semantics; per-leg fields confirmed).
2. **Void factor** (`VoidFactor{Zero,Half,One}`) and **dead heat** (`DeadHeatFactor`) reduce a
   selection's effective stake/return. Void legs are removed from a parlay (odds recomputed). **[Verified]** (fields) / math **[Likely]**.
3. **Half-won/half-lost** (`3001/4001`) handle Asian-handicap push-quarters. **[Verified]** (enum).
4. **Tax** — `StakeTax`/`WinningsTax` applied per `BetConfig.TaxConfig`, with sport-type
   exclusions (`ExcludeStakeSportTypes`, `ExcludeWinningsSportTypes`). **[Verified]** (config fields).
5. **Resettlement** — settled bets can be re-graded (`IsResettled`) when upstream corrects a
   result; failed settlements are retried by `Job.Resettlement` (Fibonacci backoff, `RetryCount<8`)
   and BO can `resendsettlement`. **[Verified]**. For a Cancelled bet `resendsettlement` replays the
   cancellation to the operator only. To tell the provider (GTS) a bet is Cancelled again, BO calls
   `v1/bet/resendprovidercancellation` (BO-125): it reads the bet from the betting database, refuses
   anything that is not Cancelled, and publishes `OnBetCancellation` with an `OnBetSettlement`
   payload (`IsAudit = true`) on `r.bet.cancellation`, the key only `Provider.Service` binds, so
   the operator is not sent a second cancellation. `Provider.Service` then cancels the reservation
   and syncs the Cancelled status with `SendBet`, as it does for a manual cancellation (GM-837).
   **[Verified]** in code and unit tests; GTS accepting the update is **[Unknown]** until observed.
6. **Audit** — every state change snapshots `BetAudit`/`BetSelectionAudit`. **[Verified]**.
7. **GTS forwarding only on bet change** - `Settlement.Service` publishes `OnBetSettlement` with
   `IsAudit = true` only when the settlement pass changed the bet (new status, or new payout in
   `ApplyAllSettled`); a leg settling on a bet whose outcome is already decided (a Won leg on a Lost
   parlay, or the final leg that leaves it Lost) publishes with `IsAudit = false`.
   `GTSBetProvider.ProcessSettlement` forwards to GTS only when `IsAudit` is true, so GTS receives the
   first decisive settlement and any later status change (Lost to Won on resettlement), not the legs
   in between. `Operator.Servicev2` keys its own behaviour on the same flag. **[Verified]** (GM-800
   follow-up).

8. **Test-operator wallet resettlement** (`Platform.Operator.API`, the in-house wallet behind
   `Platform.Operator` tokens) - every placement, settlement, cashout and cancellation writes an
   `OperatorAudit` row with `PostBalance == PreBalance + Amount`. When a bet that already has a
   settled row (Won/HalfWon/Lost/HalfLost/Void/CashedOut) is settled again, the LATEST row for the
   bet is reversed before the new outcome is booked, including the same status arriving again with
   a different payout. Rows after a Void are booked net of the stake (`payout - stake`) because
   the Void refunded it; Running (placement) rows are never reversed. Rules are stated once in
   `Platform.Operator.API/Utility/OperatorAuditReversal.cs` and covered pair-by-pair in
   `Platform.Operator.API.Test`. **[Verified]** (GM-815).

## J. History / query rules
1. **Date window** — `FromDate < ToDate` → else `152`; **max 24h per request** → `153`. **[Verified]** (codes).
2. **Search text ≥ 3 chars** → `133`. **[Verified]**.
3. **Waiting bets hidden** — consumer filters `BetStatus==Waiting(1000)` out of history lists. **[Verified: consumer]**.

## K. Curation rules (back office)
1. **Top fixtures/leagues ≤ 50** entries; **no duplicate** fixture/league ids or sort orders →
   `134–138`. Updates are **full-replace** within `(ClientId[, SportId])` scope under a
   `ReadCommitted` transaction. **[Verified]**.
2. **Announcements** are global content with per-client visibility (`ClientAnnouncements`) and
   per-language text (`LanguageAnnouncements`), gated by `StartDate`/`EndDate`/`IsActive`/`IsDeleted`. **[Verified]**.

## L. Maintenance
1. A **maintenance** flag (`Maintenance` collection / `OnMaintenance`) yields
   `205 Maintenance`; consumers treat it as "retry/continue" and surface a maintenance state
   rather than an error. **[Verified]** (code + consumer behavior).

## Rules NOT owned here `[Verified]`
- **Odds/price setting, trading, and risk model** — owned by **odds-engine-2** (and bet-genius);
  platform-final consumes the resulting odds/markets. No trading logic in this repo.
- **Wallet debit/credit accounting** — owned by `Wallet.API` (stored procedures).
- **Operator-token / user authentication** — owned by the `Auth`/Operator layer.

> Many exact thresholds (min/max stake, max payout, parlay-bonus %, tax %) are **data**, stored as
> JSON in `BetConfig` per client+currency — not constants in code. Treat specific numbers as
> configuration, not rules. **[Verified]** (config is JSON blobs).
