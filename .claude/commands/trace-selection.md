# /trace-selection

Trace a **selection** (a bettable outcome + its odds).

## Steps
1. **Storage:** Mongo `Selection` — `Platform.Shared/Data/Sportsbook/Entities/Selection.cs`
   (`SeId/MaId/SeTeId/Od` decimal + `SelectionExtension` `MaOd/HoOd/InOd/OddsRef` + probabilities
   `Pr/WoPr/LoPr/VoPr/HaWoPr/HaLoPr` + `IsAc`). Read-only here.
2. **Odds formats:** stored as **decimal**; Malay/HK/Indo computed via
   `Platform.Shared/Helpers/OddsTypeHelper.GetOddsByOddsType(...)`. `OddsType{Decimal=0,Malaysian=1,
   HongKong=2,Indonesian=3}`.
3. **At placement:** snapshotted into `BetSelection` (SQL `BettingContext`) — `Odds`+`OddsRef`,
   `BetSelectionStatusId`, `DeadHeatFactor`, `CorrelationId`, `IsBinary`.
4. **Read/serve:** `Sportsbook.API` → `v1/sportsbook/selectiondetails`.
5. **Real-time:** `OnOddsChange…se[]` (`deOd/maOd/hoOd/inOd/isAc`) via `Websocket.API2`.

## Reference
[docs/DomainModel.md](../../docs/DomainModel.md) · [docs/BusinessGlossary.md](../../docs/BusinessGlossary.md)
(odds + abbreviations) · [docs/knowledge/WebSocketMessages.md](../../docs/knowledge/WebSocketMessages.md).
