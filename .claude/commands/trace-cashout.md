# /trace-cashout

Trace cashout (quote → confirm → settle), full and partial.

## Steps
1. **Live value:** `CashOutStake.Servicev2` preloads active bets and recomputes cashout value on
   `OnOddsChange`; pushes `OnCashOutChange` (`r.bet.cashoutchange.s88`) → betslip.
2. **Request:** `POST v1/bet/cashout` (`BetCashoutRequestDTO`: BetId, Channel, CashoutStake,
   `CashoutStakePercent?` null=full) → `Betting.API` → `OnBetFullCashOut`/`OnBetPartialCashOut`.
3. **Process:** `CashOut.Service` (100-thread, MTS SDK) → writes `PartialCashoutAudit`, updates `Bet`
   (`IsCashout`/`IsPartialCashout`, recalculated payout for parlay remainder). Errors → `r.bet.cashout.error`.
4. **Status codes:** `141 CashOut_Is_Unavailable`, `142 CashOut_Stake_Changed_From_X_To_Y` (reconfirm);
   `CashOutStatus{Available=1,Unavailable=-1,Closed=-2}`, `CashoutType{Full=1,Partial=2}`.

## Reference
[docs/EntityLifecycle.md](../../docs/EntityLifecycle.md) §6 · [docs/SequenceDiagrams.md](../../docs/SequenceDiagrams.md) §5
· [docs/BusinessRules.md](../../docs/BusinessRules.md) §H.
