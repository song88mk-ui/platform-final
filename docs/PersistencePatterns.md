# Persistence Patterns

> Phase 6. The **storage-mechanism** patterns (how data is stored, transacted, and accessed) across
> SQL/Mongo/Redis/cache. The repository **code-organization** abstraction is in
> [RepositoryPatterns.md](RepositoryPatterns.md). Confidence **High [Verified]** unless noted.

## 1. Polyglot persistence, by ownership `[Verified]`
| Store | Access | platform-final role |
|-------|--------|---------------------|
| SQL Server (9 DBs, EF Core 5) | EF LINQ + stored procs | **owns** bets/config/curation/reports/wallet |
| MongoDB (`Sportsbook`) | `MongoDB.Driver` | **read-only consumer** (odds-engine-2 owns writes) |
| Redis (`ClientSportsbook.API`) | `StackExchange.Redis` | read (per client-code) |
| In-proc cache | `MemoryCache.Default` | per-pod read-through |
See [Database.md](Database.md), [MongoDb.md](MongoDb.md), [Caching.md](Caching.md).

## 2. Four data-access flavors `[Verified]`
A repository uses exactly one (sometimes mixed) of:
1. **EF Core LINQ** — most SQL access; `AsNoTracking` for reads (14 files); `Add/Update/SaveChanges` for writes.
2. **Stored procedures** — money (`Wallet.API`, `ExecuteSqlRawAsync`), reporting (`AgentReports.API`,
   `FromSqlRaw` → keyless DbSets), top-markets (`Betting.API`), some Sportsbook curation
   (`ExecuteSqlRaw`). See [Database.md](Database.md).
3. **MongoDB driver** — `.Find(filter).Project<T>(...)` read/projection only.
4. **HttpClient** — cross-service/external integrations (not local persistence).

## 3. DbContext lifecycle `[Verified]`
- **Scoped `AddDbContext`** by default (per request/operation).
- **`AddDbContextPool`** for RedCat (hot callback paths).
- **Manual per-operation contexts**: some shared services construct a `DbContext` directly with a
  connection string (e.g. `ParlayBonusService` → new `BettingContext`) rather than DI — so they own
  the connection lifetime per call.
- **Worker caveat**: services register repos as **Singleton** (the hosted `Worker` is singleton). A
  singleton repo holding a singleton-scoped `DbContext` would be an EF lifetime hazard — repos that
  do SQL in workers should create a context per message (verify per service; flagged in
  [DependencyInjection.md](DependencyInjection.md)). **[Likely]** risk.

## 4. Transactions `[Verified]`
- **`TransactionScope` with `ReadCommitted`** is the platform idiom (Betting, CashOut, BetInfo,
  AgentReports, Operator, Oddin, ESportOperator, Platform.API, ParlayBonusService, curation writes).
- No cross-store (SQL↔Mongo) transactions exist; consistency across stores is **eventual** and
  id-based (see [Relationships.md](Relationships.md)).

## 5. Write patterns
- **Append-mostly audit**: every bet state change writes a `BetAudit`/`BetSelectionAudit` snapshot;
  cashouts append `PartialCashoutAudit`; qualifying parlays append `ParlayBonusAudit`. Audit tables
  are write-once history, never updated. **[Verified]**
- **Full-replace curation**: top fixtures/leagues + ordering writes **delete the scope then insert**
  (within a `TransactionScope`), rather than diff-update — simple, idempotent, but rewrites the whole
  set each save. **[Verified]**
- **Stored-proc money mutations**: credit/debit/transfer are **never** EF writes — always the wallet
  procs (atomic balance update + status out-params, `decimal(26,12)`). **[Verified]**
- **Idempotency / correlation**: bet flows carry a `CorrelationId`; settlement retries (`IsResettled`,
  `Job.Resettlement`, `OperatorSettlementRetry`) re-drive failed writes. **[Verified]**

## 6. Read patterns `[Verified]`
- **Read-through cache** in front of SQL/Mongo (`IMemCache.GetCache(key, loader, ttl)`).
- **Projection-first Mongo** reads (slim projection types, not full docs).
- **Secondary-read-preference** Mongo for high-volume catalogue reads, primary for freshness-sensitive.
- **`AsNoTracking`** for EF read queries.
- **Denormalized reads**: `BetSelection` stores the full Sport→Selection path + names, so bet history
  renders without joining the volatile catalogue (names may be stale). **[Verified]**

## 7. Money & precision `[Verified]`
All monetary values use **`decimal(26,12)`** (stake, payout, balance, tax, cashout). Wallet balance
mutations are stored-proc + output-param based (no read-modify-write in app code).

## 8. Schema management `[Likely]`
- **DB-first / externally managed**: no EF `Migrations` folder found; `[Table("…2")]` remaps + stored
  procs imply schema is owned outside EF (likely `Platform.Shared/DatabaseScripts` + DBA process).
- The shared `Sportsbook` SQL DB is **co-written by backoffice-final** — schema changes must consider
  both repos. **[Verified-sibling]**.

## 9. Synchronization between stores `[Verified]`
- **SQL ↔ Mongo**: no direct sync; they meet only via shared ids in messages/DTOs. odds-engine-2
  writes Mongo; platform-final writes SQL; the bet's snapshot (SQL) freezes catalogue values at
  placement.
- **Cache ↔ source**: read-through + event invalidation (`CacheSync.Service`) — see
  [CacheInvalidation.md](CacheInvalidation.md).
- **Cross-pod**: per-pod caches synchronized only by RabbitMQ eviction broadcast (no distributed cache).

## Anti-patterns observed (do not propagate) `[Verified]`
- **String-interpolated `FromSqlRaw`** in AgentReports (SQL-injection exposure) — [TechnicalDebt.md](TechnicalDebt.md).
- **Hour-granularity TTLs** for fast-moving data.
- **Singleton repo + DbContext** lifetime hazard in some workers (verify per service).
- **Misspelled stored-proc names** (`InserOrUpdate…`) baked into the schema contract.
