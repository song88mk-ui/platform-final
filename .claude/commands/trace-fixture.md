# /trace-fixture

Trace how a **fixture** is produced, stored, served, and rendered.

## Key fact
The fixture is a **MongoDB read model owned by odds-engine-2** — platform-final never writes it.

## Steps
1. **Origin (sibling, read-only):** odds-engine-2 ingests the UOF feed → writes Mongo `Fixture` and
   publishes `r.oe.sb.engine.*`. ([docs/OddsEngineIntegration.md](../../docs/OddsEngineIntegration.md))
2. **Storage:** `Platform.Shared/Data/Sportsbook/Entities/Fixture.cs` (abbreviated fields
   `FiId/PrFiId/SpId/SegId/LeId/FiTyId/FiStId/…`). Mongo collection `Fixture` in DB `Sportsbook`.
3. **Read:** `Platform.Sportsbook.API/Repository/SportsbookRepository.cs` (`.Find().Project<…Projection>()`,
   secondary read preference) → gateway `v1/sportsbook/...fixtures` endpoints.
4. **Curation overlay:** SQL `SportsbookContext` (top fixtures/ordering) merged in.
5. **Real-time:** changes pushed via `OnOddsChange`/`OnBetStop*` through `Websocket.API2`.
6. **Render (sibling):** platform-web maps to `FixtureModel`. ([docs/ComponentDataFlow.md](../../docs/ComponentDataFlow.md))

## Reference
[docs/DomainModel.md](../../docs/DomainModel.md) §2 · [docs/MongoDb.md](../../docs/MongoDb.md) ·
[docs/EntityLifecycle.md](../../docs/EntityLifecycle.md) §1. `FixtureType{Match=0,Outright=1}`.
