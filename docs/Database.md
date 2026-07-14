# Database (SQL Server)

> **Phase 6 — Persistence & Caching.** Analysis-only; no code modified. Confidence **High [Verified]**
> (read from `OnModelCreating` + entity attributes + repositories) unless noted. Companion:
> [MongoDb.md](MongoDb.md), [PersistencePatterns.md](PersistencePatterns.md),
> [RepositoryPatterns.md](RepositoryPatterns.md). Entity semantics: [DomainModel.md](DomainModel.md),
> [SharedLibraries.md](SharedLibraries.md).

## Engine & topology
- **SQL Server on AWS RDS** (`…rds.amazonaws.com:1433`, `TrustServerCertificate=True`). **[Verified]**
  (committed staging DSN) / **[Verified-sibling]** (infrastructure: RDS, not yet in Terraform).
- **9 databases, one EF Core `DbContext` each** (EF Core 5; `WalletContext` is empty — see below).
  Connection keys `ConnectionStrings:{Platform|Betting|Sportsbook|Translation|Wallet|AgentReports|
  Casino*|ESportBetting|RedCatBetting}`. **[Verified]**
- **Schema is DB-first / externally managed:** no EF `Migrations` folder was found; schema + stored
  procedures live outside EF (the `Platform.Shared/DatabaseScripts` folder is the likely home).
  **[Likely]** — evidenced by `[Table("…2")]` remaps, stored-proc reliance, and absent migrations.

## Per-context schema

### BettingContext (`Data/Betting`) — wagers (system of record) `[Verified]`
13 DbSets (default table names). Keys: `Bet`(BetId), `BetSelection`(BetSelectionId),
`BetConfig`(ClientId, CurrencyCode), `StakeLimitFixture`(UserId, FixtureId), `Tier`(TierId),
`ClientTopMarket`(FixtureId, MarketId, ClientId, CurrencyCode), audit tables on `Id`. No explicit
indexes in `OnModelCreating` (relies on DB-defined indexes). Money columns `decimal(26,12)`
(seen in entities/procs).

### PlatformContext (`Data/Platform`) — clients/operators/config `[Verified]`
10 DbSets. Keys: `Client`(ClientId), `Operator`(UserId), `ForexEUR`(CurrencyCode),
`ConfidenceFactor`(Timestamp, UserId), `OperatorSettlementRetry`(BetId), `NegativeTest`/`TestConfig`(ClientId).

### SportsbookContext (`Data/Sportsbook`) — curation/config `[Verified]`
18 DbSets, all **composite-keyed** by tenant + hierarchy. **Table remaps via `[Table]`:**
`MarketTemplateGroup→MarketTemplateGroup2`, `SportGroup→SportGroup2`,
`SelectorMarketTemplate→SelectorMarketTemplate2`, `ClientSelectorMarketTemplate→ClientSelectorMarketTemplate2`
(the `2` suffix = a schema-version revision retained for compatibility). Per-client tables keyed on
`ClientId(+SportId+…)`; global tables on natural keys. See [DomainModel.md](DomainModel.md) §3.

### TranslationContext (`Data/Translation`) — the only explicitly-indexed schema `[Verified]`
- `SportsbookDynamic` (PK `Id`, constraint `PK_SportsbookDynamic_v2`) with **4 indexes**:
  - `UX_SBD_STRING_LANG` **unique** (`StringId`, `TargetLanguageId`)
  - `IX_SBD_STRING_LANG` (same cols, non-unique lookup)
  - `IX_SBD_HASH_LANG` (`SourceHash`, `SourceLanguageId`, `TargetLanguageId`, `GlossaryVersion`)
  - `IX_SBD_MISSING` **filtered** `WHERE [TARGET_TEXT] IS NULL` (find untranslated strings)
- Column config: `String_ID`(100), `SOURCE/TARGET_LANGUAGE_ID`(10), `SOURCE_HASH`
  (`ValueGeneratedOnAddOrUpdate`), `GLOSSARY_VERSION` default 1, `CREATED_AT` default `GETDATE()`,
  `PROCESSED` default false, `TARGET_TEXT` nullable.
- `SportsbookStatic` (PK `LanguageCode, Name`).

### AgentReportsContext (`Data/Reports`) — reporting (read-heavy) `[Verified]`
14 keyed entities (heavy composite keys: `WinLoss`(ParentUserId, DirectUserId, IndirectUserId,
TransactionDate, BetId), etc.) **+ 6 keyless (`HasNoKey`) DbSets** that map **stored-procedure
result sets**: `ReadWinLossSummary/Details`, `ReadProfitLossSummary/Details`, `ReadGGRSummary/Details`.

### CasinoBettingContext / ESportBettingContext / RedCatBettingContext `[Verified]`
2/2/4 DbSets respectively; `*Bet`(BetId), `*BetAudit`(Id). RedCat is **DbContextPool**-registered
(hot path — see [Hosting.md](Hosting.md)).

### WalletContext (`Platform.Wallet.API/Data`) — empty `[Verified]`
**No DbSets, no `OnModelCreating`.** Wallet persistence is **100% stored procedures** (below). The
context exists only to provide a connection.

## Stored procedures `[Verified]`
SQL Server stored procs are used for money, reporting, and some curation writes:

| Proc | Caller | Mechanism |
|------|--------|-----------|
| `CreditOrDebitUserWallet` | `Wallet.API/WalletRepository` | `ExecuteSqlRawAsync` (in/out params, `decimal(26,12)`) |
| `TransferUserWallet` | Wallet.API | `ExecuteSqlRawAsync` |
| `GetUserWallet` | Wallet.API | `ExecuteSqlRawAsync` |
| `GetWinLossSummary/Details`, `GetProfitLossSummary/Details`, `GetGGRSummary/Details` | `AgentReports.API/ReportsRepository` | `FromSqlRaw` → keyless DbSets |
| `GetRunningBalance` | AgentReports.API | `ExecuteSqlRawAsync` (output params) |
| `GetTopMarketBets` | `Betting.API/BettingRepository` | `FromSqlRaw` → `ClientTopMarket` |
| `InserOrUpdateClientMarketSortOrder`, `InserOrUpdateClientMarketTemplateGroup` (sic — "Inser") | `Sportsbook.API/SportsbookRepository` | `ExecuteSqlRaw` (curation writes; some call sites commented out) |

> Note the **misspelled proc names** (`InserOrUpdate…`) — a real schema-object name, do not "fix" casually. **[Verified]**
> `AgentReports`/`Reports` proc SQL is **built by string interpolation** of parameters into
> `FromSqlRaw` — a **SQL-injection exposure** if any parameter is attacker-controlled; tracked in
> [TechnicalDebt.md](TechnicalDebt.md). **[Verified]**

## Read vs write patterns `[Verified]`
- **Read:** EF LINQ; **`AsNoTracking`** used in 14 files for read-only queries; reporting + top-market
  reads via stored procs. `Find()` for cached single-key lookups (e.g. `AuthorizationFilter`).
- **Write:** EF `Add`/`Update`/`SaveChanges`; **money + wallet exclusively via stored procs**.
- **Bulk/curation writes use a full-replace pattern** (delete existing rows for a `(ClientId[,SportId])`
  scope, insert new) inside a `TransactionScope` (`ReadCommitted`). **[Verified]** — see
  [PersistencePatterns.md](PersistencePatterns.md).
- **Transactions:** `TransactionScope`/`BeginTransaction` in ~10 repos (Betting, CashOut, BetInfo,
  AgentReports, Operator, Oddin, ESportOperator, Platform.API). **[Verified]**

## Connection & pooling `[Verified]`
- Standard `AddDbContext` (scoped) everywhere except **RedCat** (`AddDbContextPool`). **[Verified]**
- `Job.Resettlement` sets a **30-minute** command timeout. **[Verified]**
- Some services/repos **new up a `DbContext` per operation** (e.g. `ParlayBonusService` opens a
  `BettingContext` with `ConnectionStrings:Betting`). **[Verified]**

## Cross-store note
Bets reference catalogue ids (`FixtureId`, `MarketId`, `SelectionId`) that live in **MongoDB**, not
SQL — there are **no cross-store FKs** (see [Relationships.md](Relationships.md)). The `Sportsbook`
SQL DB is **also written by backoffice-final** directly (curation ordering tables) — see
[CrossRepositoryDependencies.md](CrossRepositoryDependencies.md).

## Unknowns
- Explicit index definitions outside `TranslationContext` (rely on DB-side indexes not visible in
  EF config) — **[Unknown]** from source.
- Exact stored-proc bodies (live in the DB / `DatabaseScripts`, not in C#) — **[Unknown]**.
