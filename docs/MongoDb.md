# MongoDB

> Phase 6. Confidence **High [Verified]** (read from the Mongo repositories). The single most
> important fact: **platform-final is a read-only MongoDB consumer** — it never writes. Companion:
> [Database.md](Database.md), [OddsEngineIntegration.md](OddsEngineIntegration.md).

## Instance & database
- **AWS DocumentDB** (MongoDB-compatible), database **`Sportsbook`**, connection key
  `ConnectionStrings:MongoDB-Sportsbook` (+ `MongoDB-Casino` for QTech). `MongoClient` constructed
  per repository; the Docker image installs the AWS RDS/DocumentDB **CA bundle** for TLS. **[Verified]**
- **Owner/writer: odds-engine-2** (Phase 5). platform-final only reads. **[Verified]** (no writes here)
  + **[Verified-sibling]** (OE2 writes).

## Collections (in `Sportsbook`) `[Verified]`
`Fixture`, `Market`, `MarketMapping`, `Selection`, `BettingStatus`, `Maintenance`.
(`MongoDB-Casino` exposes a `BettingStatus` collection read by QTech.API.) Document shapes use
abbreviated field names — see [DomainModel.md](DomainModel.md) §2.

## Who reads what `[Verified]`
| Reader (platform-final) | Collections | Read preference |
|-------------------------|-------------|-----------------|
| `Sportsbook.API/SportsbookRepository` | Fixture, Market, MarketMapping, Selection, BettingStatus, Maintenance | **Secondary** (conditional on config), with a **Primary** copy of each for consistency-sensitive reads |
| `CashOutStake.Servicev2/BettingRepository` | Market, Selection | default (primary) |
| `QTech.API/CasinoRepository` | BettingStatus (`MongoDB-Casino`) | **Secondary** |

## Read patterns `[Verified]`
- **Dual read-preference strategy** in `Sportsbook.API`: it opens **both** a secondary-preference and
  a primary-preference handle for `Fixture`/`Market`/`Selection`/`BettingStatus`. The secondary
  (`ReadPreferenceMode.Secondary`) is used for high-volume catalogue reads to offload the primary;
  the primary handle is used where freshness matters. The secondary vs primary choice is
  **config-gated** (a flag selects Secondary, else Primary). **[Verified]**
  (`SportsbookRepository.cs:100-124`).
- **Projection-first:** queries use `.Find(filter).Project<TProjection>(projection)` to return slim
  projection types (e.g. `ActiveFixtureProjection`, `CashOutMarketFilter`, `CashOutSelectionFilter`)
  rather than whole documents — minimizing payload and deserialization. **[Verified]**
- **Single-document reads** via `.Find(filter).FirstOrDefault[Async]` (e.g. `BettingStatus`,
  `Maintenance`, cashout market/selection lookups). **[Verified]**
- Status/maintenance flags (`BettingStatus`, `Maintenance`) are read to gate betting before serving
  the catalogue. **[Verified]**

## Write patterns `[Verified]`
- **None.** A repo-wide grep for `InsertOne/InsertMany/ReplaceOne/UpdateOne/UpdateMany/DeleteOne/
  DeleteMany/BulkWrite` across platform-final returns **zero hits**. platform-final reads Mongo
  exclusively; all writes are performed by **odds-engine-2** (`OE.Sportsbook.Service`/`OE.Job.*`).
  **[Verified]** (platform-final) + **[Verified-sibling]** (OE2).

## Indexes `[Unknown from platform-final]`
- Mongo indexes are defined by the **writer (odds-engine-2)** / DocumentDB, **not** by platform-final
  — no index creation code exists here. Effective query performance depends on indexes provisioned
  on `FiId/PrFiId/SpId/SegId/LeId/MaId/SeId/ClCo/FiStId` by OE2/infrastructure. Exact index set is
  **[Unknown]** from this repo (resolvable in odds-engine-2 / DocumentDB). 

## Consistency model `[Verified]`
- Reads are **eventually consistent** relative to the live feed: a secondary-preference read may lag
  the primary; the config flag lets ops force primary reads when needed. **[Verified]**
- There is **no transaction** spanning Mongo and SQL — a bet (SQL) can reference a fixture (Mongo)
  that has since changed/disappeared. Defensive handling required. See
  [Relationships.md](Relationships.md), [StateFlow.md](StateFlow.md).

## Caching in front of Mongo `[Verified]`
Catalogue reads are wrapped by `IMemCache` (per-pod) and, for client sportsbook, Redis — and
invalidated by `CacheSync.Service` on `On*Change`/`OnDeleteCacheMessage`. See
[Caching.md](Caching.md) and [CacheInvalidation.md](CacheInvalidation.md).
