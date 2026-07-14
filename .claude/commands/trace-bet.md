# /trace-bet

Trace the **bet lifecycle** (placement → settlement → cashout) — the platform's spine.

## Steps
1. **Placement (sync):** `POST v2/bet/placement` → `Platform.API` → `Betting.API/BettingRepository`:
   validates (stake/odds/market/limits → `StatusCode`), runs provider (`IBetProviderFactory` by
   `ChannelType`), persists `Bet`+`BetSelection` (SQL `BettingContext`), publishes `OnBetPlacement`.
2. **Async fan-out:** `Placement.Servicev2` (provider dispatch, 100-thread), `BetInfo.Service`,
   `AgentReports.Service`, `BetForwarder.Service`, `Client.TopMarket`.
3. **Status:** `BetStatus{Waiting=1000→Running=2000→Won=3000/Lost=4000/Void=5000/CashedOut=8000,
   Rejected=6000,Cancelled=7000}`; pushed via `OnBetStatus` (`r.bet.betstatus.s88`).
4. **Settlement:** `Settlement.Service` consumes `OnBetSettlement` → grades selections, parlay bonus
   (`ParlayBonusService`), `BetAudit`, credits `Wallet.API`. Retries via `Job.Resettlement`.
5. **Cashout:** `/trace-cashout`.

## Reference
[docs/EntityLifecycle.md](../../docs/EntityLifecycle.md) §4 · [docs/SequenceDiagrams.md](../../docs/SequenceDiagrams.md)
§4,6 · [docs/BusinessRules.md](../../docs/BusinessRules.md). Audit-append + `CorrelationId` throughout.
