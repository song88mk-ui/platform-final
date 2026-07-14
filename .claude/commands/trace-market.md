# /trace-market

Trace a **market** (a betting market on a fixture).

## Steps
1. **Storage:** Mongo `Market` collection — `Platform.Shared/Data/Sportsbook/Entities/Market.cs`
   (`MaId/MaTeId/FiId/MaStId/Li/MaNa…`); `MarketMapping` maps to the provider specifier for
   odds/settlement routing. **Read-only** in platform-final (odds-engine-2 writes).
2. **Template/grouping:** `SelectorMarketTemplate` (`SelectorMarketTemplate2` table) +
   `MarketTemplateGroup` (`MarketTemplateGroup2`) in SQL `SportsbookContext`; per-client overrides
   `ClientMarketTemplateGroup`/`ClientSelectorMarketTemplate`.
3. **Status lifecycle:** `MarketStatus{Active=1,Inactive=0,Suspended=-1,HandedOver=-2,Settled=-3,
   Cancelled=-4,NotMapped=99}` ([docs/EntityLifecycle.md](../../docs/EntityLifecycle.md) §2).
4. **Read/serve:** `Sportsbook.API` projections (`MarketLimitedProjection`) → `v1/sportsbook/fixturedetails`,
   `markettemplategroups`, `betbuilder/markettemplates`.
5. **Real-time:** `OnOddsChange.ma[]` / `OnBetStopFixture` via `Websocket.API2`.

## Reference
[docs/DomainModel.md](../../docs/DomainModel.md) · [docs/knowledge/CommonModels.md](../../docs/knowledge/CommonModels.md).
