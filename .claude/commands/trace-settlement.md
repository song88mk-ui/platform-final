# /trace-settlement

Trace bet settlement / resettlement.

## Steps
1. **Trigger:** odds-engine-2 publishes `OnBetSettlement` (`r.oe.bet.settlement`) with results/void
   factors/dead-heat. ([docs/OddsEngineIntegration.md](../../docs/OddsEngineIntegration.md))
2. **Grade:** `Settlement.Service/Repository/BettingRepository.cs` consumes it (OpenTelemetry trace
   from MQ headers) → sets each `BetSelection.BetSelectionStatusId`, then `Bet` status (Won/HalfWon/
   Lost/HalfLost/Void); applies `VoidFactor`/`DeadHeatFactor`; computes parlay bonus
   (`ParlayBonusService`); writes `BetAudit`/`BetSelectionAudit`.
3. **Payout:** credits `Wallet.API` (stored proc) on win; emits `OnBetStatus` + reporting.
4. **Failure/retry:** `Job.Resettlement` (Fibonacci backoff, `OperatorSettlementRetry`, `RetryCount<8`);
   BO `v1/bet/resendsettlement`. `IsResettled` marks re-graded bets.

## Open
Exact void/dead-heat/parlay math lives in `Settlement.Service` (U-21, not fully line-traced) — verify.

## Reference
[docs/EntityLifecycle.md](../../docs/EntityLifecycle.md) §4–5 · [docs/BusinessRules.md](../../docs/BusinessRules.md) §I.
