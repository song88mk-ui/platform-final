# Entity Lifecycle

> Phase 3. State machines and create→update→terminal flows for the core domain entities.
> Confidence **High [Verified]** for status enums and producers; **[Likely]** for exact transition
> triggers not line-traced. Status values cross-ref [BusinessGlossary.md](BusinessGlossary.md).

## 1. Fixture lifecycle (owned upstream by odds-engine-2; read model here)
```
                 (feed creates)
   ─────────────▶ Prematch ───────────────▶ Live ───────────▶ Ended ──▶ Closed
                    │  │                      │  ▲               │
   MatchAboutToStart┘  │           Suspended ◀┘  │ (resume)      │
                       │            (temp)  ─────┘               │
                       ├──▶ Postponed / Delayed ─(reschedule)─▶ Prematch
                       └──▶ Cancelled / Abandoned / Interrupted ─(→ void bets)
```
- **Statuses** (`FixtureStatus`): `Prematch, MatchAboutToStart, Live, Suspended, Ended, Closed,
  Cancelled, Abandoned, Delayed, Postponed, Interrupted, Unknown`. **[Verified]**
- **Created/updated:** entirely by the **odds-engine-2 feed** → MongoDB `Fixture` collection; the
  platform never authors fixtures. `MatchStatus` (sport-specific period codes; `Ended=100`,
  `AET=110`, `AP=120`) tracks in-play progress. **[Verified]**
- **List scope** for reads is bucketed by `FixtureScope{Early=-1, Today=0, Live=1}`. **[Verified]**
- **Terminal:** `Closed` (settled & done), `Cancelled`/`Abandoned` (→ bets void). **[Likely]** for the
  cancel→void linkage.

## 2. Market lifecycle (read model)
```
   Inactive ──▶ Active ⇄ Suspended ──▶ Settled
        │          │                      
        │          └────────────────▶ Cancelled (→ void)
        └─▶ HandedOver / NotMapped (operational/edge)
```
- **Statuses** (`MarketStatus`): `Inactive=0, Active=1, Suspended=-1, HandedOver=-2, Settled=-3,
  Cancelled=-4, NotMapped=99`. **[Verified]**
- **Active** = bets accepted; **Suspended** = temporarily locked (odds in flux / in-play event);
  **Settled** = outcome applied; **Cancelled** = void & refund. Driven by `OnMarketChange` /
  bet-stop messages from odds-engine-2. **[Verified]** (enum) / triggers **[Likely]**.

## 3. Selection lifecycle (read model)
- No dedicated status enum. A live selection's state is `IsAc` (active) + its market's `MaStId`
  + current odds (`Od`/`OddsRef`). When odds move, `OnOddsChange` updates the selection and pushes
  to clients. A selection becomes ungradeable/inactive when `IsAc=false` or its market suspends.
  **[Verified]** (fields).
- At **bet placement**, the live selection is **snapshotted** into a `BetSelection` (odds frozen as
  `Odds`, reference kept in `OddsRef`); from then the *placed* leg follows the **bet** lifecycle. **[Verified]**.

## 4. Bet lifecycle (owned by platform-final)
```
            place                accept                settle / cashout
 Client ─────────▶ (validate) ─────────▶ Waiting ──▶ Running ──┬─▶ Won
   │                  │                     │                   ├─▶ HalfWon
   │              Rejected(6000)            │                   ├─▶ Lost
   │              (validation/limits)       │                   ├─▶ HalfLost
   │                                        │                   ├─▶ Void
   │                                        │                   └─▶ CashedOut(8000)
   └─ Cancelled(7000)  ◀── BO/user cancel ──┘
                                  └─ Resettled (re-grade after settle; IsResettled)
```
- **Statuses** (`BetStatus`): `Waiting=1000 → Running=2000 → {Won=3000 | HalfWon=3001 | Lost=4000 |
  HalfLost=4001 | Void=5000 | CashedOut=8000}`; plus `Rejected=6000`, `Cancelled=7000`. **[Verified]**
- **Waiting** = accepted but fixture/market not started; **Running** = in play, ≥1 leg unresolved;
  terminal = the win/lose/void/cashout set. **[Verified]** (enum) / transition triggers **[Likely]**.
- **Producers:**
  - **Create** (Waiting/Running, or Rejected): `Betting.API` `POST v2/bet/placement` →
    `Placement.Servicev2` (persists `Bet`+`BetSelection`, runs provider validation). **[Verified]**
  - **Settle** (→ Won/Lost/Void/Half…): `Settlement.Service` (consumes `OnBetSettlement`,
    writes audit, computes parlay bonus, calls Wallet credit). **[Verified]**
  - **Cashout** (→ CashedOut / residual): `CashOut.Service` + `CashOutStake.Servicev2`. **[Verified]**
  - **Cancel** (→ Cancelled): BO `v1/bet/cancel` / `OnBetCancellation`. **[Verified]**
  - **Resettle:** `Job.Resettlement` retries failures; `IsResettled` marks re-graded bets. **[Verified]**
- **Audit:** each transition snapshots `BetAudit`/`BetSelectionAudit`. **[Verified]**

## 5. BetSelection (leg) lifecycle
- Created with the bet; `BetSelectionStatusId` graded at settlement (Won/HalfWon/Lost/HalfLost/Void
  using BetStatus-style values). `DeadHeatFactor` and `VoidFactor` adjust effective return; a
  **void** leg is removed from a parlay and the combined odds recomputed. `IsSettled`/`IsResettled`
  track per-leg state. **[Verified]** (fields) / settlement math **[Likely]**.

## 6. Cashout lifecycle
```
 Running bet ──quote──▶ CashOutStatus.Available ──confirm──▶ Full  ─▶ Bet.CashedOut (residual 0)
        │                       │                          └▶ Partial ─▶ residual stake, payout recalced,
        │                  Unavailable(141) /                          IsPartialCashout=true, loops
        │                  value changed(142)                          (multiple PartialCashoutAudit rows)
        └─ value tracked live by CashOutStake.Servicev2 (reacts to OnOddsChange) ─▶ push OnCashOutChange
```
- `CashOutStatus{Available=1, Unavailable=-1, Closed=-2}`, `CashoutType{Full=1, Partial=2}`.
  Each cashout writes `PartialCashoutAudit` (pre/post stake, percent, amount, tax). **[Verified]**.

## 7. Config & curation lifecycle (platform-final SQL)
- **BetConfig / UserBetConfig:** created/updated via BO `v1/config/client` →
  `Betting.API` (`ProcessBetConfig`); read at placement. JSON blobs (default/override/parlay/tax/
  boost/builder). **[Verified]**.
- **Top fixtures/leagues, announcements:** created/updated via BO InternalController →
  `Sportsbook.API` repo (full-replace, transactional); read by gateway/Sportsbook reads. **[Verified]**.
- **Sort-order & template tables** (`Client*Order`, `*SelectorMarketTemplate`, groups): consumed on
  catalogue reads; **authoring endpoint not located** — **[Unknown]** (likely admin tooling / config flow).

## 8. Translation lifecycle
- `SportsbookDynamic`: a source string is hashed (`SourceHash`); a per-language `TargetText` is
  filled (else flagged missing) under a `GlossaryVersion`; served bulk via
  `v1/sportsbook/translations` and inline on odds messages. `SportsbookStatic` holds fixed labels.
  `v1/translation/recovery` backfills missing entries. **[Verified]**.

## Lifecycle ↔ message map (who drives transitions)
| Transition | Message / trigger | Service |
|------------|-------------------|---------|
| Fixture/Market/Selection change | `OnOddsChange`/`OnMarketChange`/`OnBetStop*` (`r.oe.sb.engine.*`) | odds-engine-2 → Mongo → WebSocket |
| Bet created | `OnBetPlacement` | Placement.Servicev2 |
| Bet settled | `OnBetSettlement` | Settlement.Service → Wallet/Reports |
| Bet cancelled | `OnBetCancellation`/`OnBOBetCancellation` | Settlement/Provider |
| Cashout | `OnBetFullCashOut`/`OnBetPartialCashOut`/`OnCashOutChange` | CashOut.Service / CashOutStake.Servicev2 |
| Resettle | retry loop | Job.Resettlement |
See [DependencyGraph.md](DependencyGraph.md) §3 and [WebSocketContracts.md](WebSocketContracts.md).
