# /trace-cashout

Trace cashout (quote → confirm → settle), full and partial.

## Steps
1. **Live value:** `CashOutStake.Servicev2` preloads active bets and recomputes cashout value on
   `OnOddsChange`; pushes `OnCashOutChange` (`r.bet.cashoutchange.{clientcode}`, e.g. `.s88`) → betslip.
2. **Request:** `POST v1/bet/cashout` (`CashoutBetRequest`: BetId, Channel, CashoutStake,
   `CashoutStakePercent?` null=full; forwarded as `CashoutBetRequestDTO`) → `Betting.API` →
   `OnBetFullCashOut`/`OnBetPartialCashOut`, published via `RabbitMQ.API` on `r.bet.cashout`;
   `CashOut.Service` binds `r.bet.cashout`/`q.bet.cashout`.
3. **Provider gate (pre-acceptance):** `CashOut.Service` resolves the provider via
   `IBetProviderFactory.Resolve(message.ChannelTypeId)` (BG→`GTSBetProvider` cashout assessment,
   read-only; SR→`MtsBetProvider` ticket cashout, executes) and calls `ValidateCashoutAsync`.
   Decline → `OnBetStatus` "Cashout request has been declined" → `r.bet.betstatus.{clientcode}` → UI.
4. **Process:** on accept, `CashOut.Service` (100-thread) updates `Bet` (full: status via `GetStatus` →
   `CashedOut`; partial: writes a `PartialCashoutAudit` row, reduces stakes, sets `IsPartialCashout`,
   recalculates payout, stays `Running`), publishes `OnBetFullCashOut`/`OnBetPartialCashOut` → `r.bet.operator`.
   Errors → `r.bet.cashout.error`.
5. **Provider bet update:** `Provider.Service` consumes `r.bet.operator` →
   `GTSBetProvider.ProcessCashout` → `POST …/bets` with `status=Settled` for full or `status=Open` for
   partial, both carrying cumulative `cashedOutTotalStake`/`cashedOutAmount`; legs stay `Open` with
   `payoutPrice=null` (Genius Sports guidance). Skipped only when the message lacks
   `CashedOutTotalTrueStake` (pre-GM-790 in-flight messages).
   `Operator.Servicev2` also consumes `r.bet.operator` (operator wallet credit; failed calls are stored
   in `OperatorSettlementRetry` for `Job.Resettlement` replay).
6. **Status codes:** `141 CashOut_Is_Unavailable`, `142 CashOut_Stake_Changed_From_X_To_Y` (reconfirm);
   `CashOutStatus{Available=1,Unavailable=-1,Closed=-2}`, `CashoutType{Full=1,Partial=2}`.

## Reference
[docs/EntityLifecycle.md](../../docs/EntityLifecycle.md) §6 · [docs/SequenceDiagrams.md](../../docs/SequenceDiagrams.md) §5
· [docs/BusinessRules.md](../../docs/BusinessRules.md) §H.
