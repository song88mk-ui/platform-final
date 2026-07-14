# Skill: SQL Investigation

Investigate the relational data (bets/config/curation/reports/wallet).

## Knowledge
- **9 SQL DBs / EF Core 5 DbContexts** ([docs/Database.md](../../docs/Database.md)): `BettingContext`,
  `PlatformContext`, `SportsbookContext`, `TranslationContext`, `AgentReportsContext`,
  `CasinoBetting`, `ESportBetting`, `RedCatBetting`, + empty `WalletContext`.
- **Stored procs:** wallet (`CreditOrDebitUserWallet`/`TransferUserWallet`/`GetUserWallet`), reports
  (6 `Get*Summary/Details` via keyless DbSets), `GetTopMarketBets`, curation `InserOrUpdate…` (sic).
- **DB-first** (no EF migrations); money `decimal(26,12)`; only `TranslationContext` has explicit EF indexes.
- Reads use `AsNoTracking`; writes use `TransactionScope` (ReadCommitted); curation = full-replace.

## Method
1. Identify the context + entity ([docs/knowledge/CommonModels.md](../../docs/knowledge/CommonModels.md))
   and the repository method ([docs/RepositoryPatterns.md](../../docs/RepositoryPatterns.md)).
2. Note table remaps (`SelectorMarketTemplate2` etc.) and composite keys.
3. **Security:** any `FromSqlRaw`/`ExecuteSqlRaw` must be parameterized — AgentReports interpolates
   (TD-01b/AP-2). Stored-proc bodies live in the DB/`DatabaseScripts` (not C# — U).
4. Remember the **shared `Sportsbook` DB is also written by backoffice-final** (R-12).

## Don't
Don't string-interpolate SQL. Don't assume an index exists outside `TranslationContext`.
