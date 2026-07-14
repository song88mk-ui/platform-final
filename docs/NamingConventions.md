# Naming Conventions

> Phase 8. Naming as the code **actually** uses it (descriptive, not prescriptive). Confidence
> **High [Verified]** across Phases 1–7. Companion: [CodingStandards.md](CodingStandards.md),
> [ImplementationPatterns.md](ImplementationPatterns.md).

## Projects & solution
- **`Platform.<Area>.<Kind>`** — `.API` (web), `.Service` / `.Servicev2` (workers), `.Job.<Name>`
  (timed loops), `.Console`, `.Shared` (kernel), `.Shared.Test`. **[Verified]**
- **`…v2` / `…2` suffix = a rewrite/revision** that runs beside the original: `Operator.Servicev2`,
  `CashOutStake.Servicev2`, `Placement.Servicev2`, `Websocket.API2`; SQL tables `SelectorMarketTemplate2`,
  `MarketTemplateGroup2`, `SportGroup2`, `ClientSelectorMarketTemplate2`; constraint `PK_SportsbookDynamic_v2`.
  **[Verified]** — see [Database.md](Database.md), [TechnicalDebt.md](TechnicalDebt.md) (duplication).
- Companion sub-projects: `*.Service.Repository`, `*.Service.Telemetry`. **[Verified]**

## C# types & members `[Verified]`
- **Interfaces** `I`-prefixed; implementation drops the `I` (`IBettingRepository`/`BettingRepository`).
- **Block-scoped namespaces** mirroring folders: `Platform.<Project>.<Folder>`. No file-scoped namespaces.
- **PascalCase** types/methods/properties; **camelCase** locals/params; **`_camelCase`** private fields
  (e.g. `_configuration`, `_platformRepository`, `_messageBroker`, `_activitySource`).
- One primary type per file; several files carry a UTF-8 BOM.
- **Async methods are usually NOT `Async`-suffixed** (e.g. `PlaceBet`, `GetSportsTree`) — the suffix is
  inconsistent (some `…Async` exist, esp. EF/Mongo calls). **[Verified]** (minor inconsistency).

## Domain & layer naming `[Verified]`
- **Repositories:** `IXxxRepository`/`XxxRepository` in `Repository/`.
- **Message contracts:** `On<Event>` (`OnBetPlacement`, `OnBetSettlement`, `OnWalletChange`, …) in
  `MessageModels/`.
- **DTOs:** `<Thing>RequestDTO` / `<Thing>ResponseDTO`; envelope `BaseResponseDTO<T>`; some `BaseModel<T>`.
- **Mongo projections:** `<Thing>Projection` (`ActiveFixtureProjection`) or `<Thing>Filter`
  (`CashOutMarketFilter`).
- **Providers/factories:** `XxxBetProvider` / `IBetProviderFactory`; **Helpers:** `XxxHelper`;
  **Extensions:** `XxxExtensions`; **ActionFilters:** `XxxFilter`; **Telemetry:** `Instrumentation`.
- **Enums:** PascalCase type; values often carry explicit numeric codes (`BetStatus.Won = 3000`).
- **Workers:** the hosted class is always `Worker`.

## Wire / persistence naming `[Verified]`
- **MongoDB + SignalR messages use abbreviated field names** to shrink payloads: `Fi`=Fixture,
  `Sp`=Sport, `Seg`=Segment, `Le`=League, `Ma`=Market, `Se`=Selection, `Ho/Aw`=Home/Away, `Od`=Odds
  (`DeOd/MaOd/HoOd/InOd`), `St`=Status, `Ty`=Type, `Na`=Name, `Tr`=Translations, `So Or`=SortOrder,
  `Is…`=bool, `Pr`=Probability, `Co`=Correlation, `LaCo`=LanguageCode, `ClCo`=ClientCode. Full decoder:
  [BusinessGlossary.md](BusinessGlossary.md).
- **SQL columns** are mostly default EF naming; `TranslationContext` uses **UPPER_SNAKE_CASE** columns
  (`SOURCE_TEXT`, `TARGET_LANGUAGE_ID`) and named indexes (`UX_SBD_STRING_LANG`, `IX_SBD_MISSING`).
- **Money** type is consistently `decimal(26,12)`.

## Config keys `[Verified]`
Sections: `ConnectionStrings:*`, `MessageBroker:*` / `ExternalMessageBroker:*`, `API:*` (downstream
URLs), `Custom:*` (flags/secrets), `Serilog:*`, `Telemetry:*`. In-cluster the env-var form is
`Section__Key` (double underscore). See [Configuration.md](Configuration.md).

## Routing & messaging keys `[Verified]`
- **REST routes:** lowercase, path-versioned (`v1/sportsbook/sports/livefixtures`, `v2/bet/placement`).
- **RabbitMQ routing keys:** dotted, namespaced by producer: `r.bet.*`/`r.wallet.*` (platform),
  `r.oe.*` (odds engines), `r.bg.*`, `r.of.*`; `.s88` = brand/client-code suffix. See
  [Contracts.md](Contracts.md).
- **Cache keys:** prefixed (`cc:{clientId}`, `mts:auth`, `bg:auth`).
- **SignalR methods:** `SubscribeTo<X>` / `On<X>` / `Publish<X>`. See [WebSocketContracts.md](WebSocketContracts.md).

## HTTP headers `[Verified]`
`x-`-prefixed kebab-case: `x-api-key`, `x-operator-token`, `x-client-id`, `x-language-code`,
`x-country-code`, `x-user-ip`, `x-og-*` (Oriental Gaming). See [Authentication.md](Authentication.md).

## Known naming defects (do not "fix" casually — they are real identifiers) `[Verified]`
- Misspelled stored procs: **`InserOrUpdateClientMarketSortOrder`/`…TemplateGroup`** (missing "t").
- Misspelled config key **`Custom:Fibonnaci`**; method **`ProcesssBetPlacement`** (triple-s).
- Message-model typos: **`OnBOBetCancellaton`**, **`OnMarketDeactivationMesssage`** (file/class names).
- Filename with trailing space: **`ParlayBonusService .cs`**.
These are baked into schema/contracts — see [AntiPatterns.md](AntiPatterns.md) / [TechnicalDebt.md](TechnicalDebt.md).
