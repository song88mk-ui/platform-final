# Unknowns — Explicitly Unverified Items

> Per the analysis charter: **never guess**. Anything not traceable to concrete evidence is recorded
> here as **Unknown**, with **Suggested investigation** (the *resolve by* line). Each item carries
> evidence/files inline. Nothing here should be treated as fact until verified.

> **Phase 10 consolidation.** This is the authoritative open-questions register across Phases 1–10.
> Many items were resolved in later phases using verified sibling-repo reads (platform-web, odds-engine-2,
> Auth, backoffice-final, argocd) — resolved lines are struck through with the resolving evidence.
> Explicit **assumptions** (distinct from unknowns) live in [KnowledgeValidation.md](KnowledgeValidation.md) §Assumptions.
>
> **Tally (Phase 10):** Resolved → U-07, U-08, U-09, U-19, U-20 (+ U-06 partial).
> Still open (verify-before-rely) → U-01 (partial), U-02, U-03, U-04, U-05, U-06, U-10, U-11, U-12,
> U-13, U-14, U-21, U-22, U-23, U-24, U-25, U-26, U-27, U-28, U-29, U-30.
> Highest-value open items: **U-21** (settlement math), **U-24** (Mongo `Maintenance` writer),
> **U-26** (OE2-vs-oddsfeed authority), **U-01** (full routing-key↔queue topology).

## Runtime message flows
- **U-01** Exact RabbitMQ **routing-key ↔ queue ↔ consumer** binding for every service. We
  verified the message **types** each `Worker` handles and several routing keys, but not the full
  binding topology. *Resolve by:* reading each service's `MessageBroker:RoutingKey`/`Queue` config
  + `Connect` call, or inspecting the live RabbitMQ topology.
  *Partially resolved:* concrete real-time routing keys now confirmed —
  `r.oe.sb.engine.onoddschange|onbetstopfixture|onbetstopglobal`, `r.bet.betstatus.s88`,
  `r.bet.cashoutchange.s88`, `r.wallet.s88` (per platform-web). Full topology still open.
- **U-02** The precise **ordering/causality** of the end-to-end bet lifecycle across services
  (documented as **[Likely]** in [Architecture.md](Architecture.md)). *Resolve by:* a trace/log
  walk-through of one real bet.
- **U-03** Whether platform consumes odds from **both** `odds-engine-2` and `oddsfeed`, or one is
  primary. *Resolve by:* the consuming services' routing-key config + ops knowledge.

## Services not fully traced
- **U-04** `Cancellation.Service`, `ESportOperator.Service`, `Operator.Cashout.Service` —
  `Worker.cs` not read in depth; consumed/produced message types are **[Likely]/Unknown**.
- **U-05** `Job.ForexEUR` and `Job.Operator.Score` — loop interval and exact processing
  **Unknown** (only the data sources are known).

## Build / deploy
- **U-06** Why ~13 projects (QTech, RedCat*, Oddin, AgentReports*, Cancellation, ESportOperator,
  Operator.Cashout, WebSocket.API, etc.) are **absent from the CI matrix** — separate pipeline,
  manual deploy, or inactive? *Resolve by:* checking `argocd` apps + any other CI in the org.
- ~~**U-07** K8s Service port mapping (container :80 vs :3000).~~ **RESOLVED (Phase 5, argocd):**
  K8s service exposes **:3000** (`platform-app` chart), container Kestrel on :80.
- ~~**U-08** Runtime config injection mechanism.~~ **RESOLVED (Phase 5, argocd/infrastructure):**
  **AWS SSM Parameter Store + External Secrets Operator** (ClusterSecretStore `parameterstore`),
  injected as `Section__Key` env vars. See [CrossRepositoryDependencies.md](CrossRepositoryDependencies.md).
- ~~**U-06** (partial) Non-CI projects' deploy path.~~ **Partially resolved (Phase 5):** the
  `platform` argocd namespace deploys the CI images; several non-CI projects are deployed by other
  pipelines/namespaces. Exact mapping for QTech/RedCat/Oddin/AgentReports still **[Unknown]**.

## WebSocket / odds push (added continued Phase 1)
- ~~**U-19** Whether the production odds WebSocket at `websocket-api.*/platform` is served by
  platform-final's `Websocket.API2` or an odds-engine-2-owned hub.~~ **RESOLVED (maintainer
  confirmed):** the `websocket-api/platform` hub **is platform-final's `Platform.Websocket.API2`**
  (net7.0); `WebSocket.API` is the superseded 3.1 original. See
  [WebSocketContracts.md](WebSocketContracts.md). *Remaining sub-detail (now narrowed):*
  `Websocket.API2` has **no in-process feeder** (verified — no RabbitMQ consumer/BackgroundService;
  controller only `v1/heartbeat`+`v1/connections`), so the `r.oe.sb.engine.*` → `Publish*` feeder is
  **external — most likely odds-engine-2's `OE.Websocket.Service` via the Azure SignalR backplane**.
  *Confirm by:* reading odds-engine-2's websocket service source.

## Sportsbook domain (added Phase 3)
- **U-20** **Authoring path for the per-client ordering / template tables** (`ClientSportOrder`,
  `ClientSegmentOrder`, `ClientLeagueOrder`, `ClientMarketTemplateOrder`, `ClientMarketTemplateGroup`,
  `ClientSelectorMarketTemplate`) and the global template/group/tier tables. They are **read** on
  catalogue queries, but no create/update endpoint was located in `InternalController`/Sportsbook.API.
  *Resolve by:* checking back-office (`backoffice-final`) admin endpoints or a config service.
- **U-21** Exact **settlement math** for void factor + dead heat + half-win/half-loss on parlays
  (fields are confirmed; the computation lives in `Settlement.Service`, not line-traced).
- **U-22** Contents of the `Provider` enum (`Platform.Shared/Enums/Provider.cs`) — file is listed
  but its values were not confirmed; routing is by `ChannelType{BG,EM,OD,SR}`.
- **U-23** Exact JSON schemas of `BetConfig` blobs (`DefaultConfig`/`OverrideConfig`/`ParlayConfig`/
  `TaxConfig`/`PriceboostConfig`/`BetBuilderConfig`) — stored as strings; shapes inferred from the
  consumer (`BetConfigModel`) but not from a schema in this repo.

## Cross-repository integration (added/updated Phase 5)
- ~~**U-09** Whether message contracts are physically shared or duplicated.~~ **RESOLVED:**
  **duplicated per-repo, aligned by convention** (field names + routing keys); platform-final
  references no shared contracts package. See [Contracts.md](Contracts.md).
- ~~**U-20** Authoring path for per-client ordering/template tables.~~ **RESOLVED (Phase 5):**
  **backoffice-final** writes them — partly via platform-final's `InternalController`, partly by
  **direct SQL writes** to the shared `Sportsbook` DB. See [CrossRepositoryDependencies.md](CrossRepositoryDependencies.md).
- **U-24** Who **writes the Mongo `Maintenance` collection** — odds-engine-2 was not found to write it,
  yet platform-final reads it. *Resolve by:* grep other producers (back office? a platform service?).
- **U-25** The exact **Auth endpoint** platform-final's `Operator.API` calls to validate the operator
  token (`IAuthRepository` client exists; **[Likely]** `v1/authentication/operator`, not line-confirmed).
- **U-26** Which odds source (**odds-engine-2 vs oddsfeed**) is authoritative per fixture/brand
  (both reach MTS; both share `e.central`). *Resolve by:* per-brand routing/config + ops knowledge.

## Persistence & caching (added Phase 6)
- **U-27** Who **writes/populates the per-client Redis** instances read by `ClientSportsbook.API`
  (`RedisCache` only does `StringGetAsync`). *Resolve by:* grep odds-engine-2 / a cache-writer service.
- **U-28** **Mongo index definitions** for the `Sportsbook` collections (defined by odds-engine-2 /
  DocumentDB, not in platform-final). *Resolve by:* odds-engine-2 index code / DocumentDB inspection.
- **U-29** Whether Redis is invalidated by `CacheSync.Service` (only `IMemCache` eviction observed).
- **U-30** Explicit SQL index definitions outside `TranslationContext` (DB-side, not in EF config).

## Shared contracts & data
- **U-09 (legacy entry, superseded above)** Whether RabbitMQ **message-contract classes are physically shared** with sibling repos
  (a common package) or **duplicated** per repo. *Resolve by:* comparing `MessageModels` here with
  `odds-engine-2`/`bet-genius` equivalents.
- **U-10** Exact **field-level semantics** of many `Platform.Shared` entities/DTOs (189 models, 88
  data files) — categorized, not individually traced.
- **U-11** Casino/ESport/RedCat connection-string **key names** (`ConnectionStrings:Casino*` etc.)
  inferred from context naming; not all confirmed against `appsettings.json`.
- **U-12** Whether the `*.Service.Repository` / `*.Service.Telemetry` units are **separate
  projects** or **folders** for each of the 13 services (mixed; not exhaustively verified).

## Quality / testing
- **U-13** Actual **test coverage** — only `Platform.Shared.Test` exists; its contents and what it
  covers were not enumerated.
- **U-14** Whether `Platform.Client.API` is **deprecated/dead** in favor of `ClientSportsbook.API`
  / `ClientPlatform.API`. Strongly **[Likely]** but not confirmed by usage/traffic.

## Security
- **U-15** Whether committed secrets are **real/active** or placeholders/staging-only. Treat as
  real until proven otherwise. *Resolve by:* asking the team / checking rotation status.
- **U-16** How API keys validated by `APIKeyFilter` relate to the sibling **Auth** service's key
  issuance (config value source). *Resolve by:* Auth service + deploy config.

## Sibling repos (intentionally shallow)
- **U-17** `oddsengine` and `Reference` were **not analyzed** by directive — their internals are
  Unknown by design.
- **U-18** `infrastructure` specifics (exact RDS/Mongo/RabbitMQ endpoints, regions) are **Referenced**
  via README/structure only.

---

### How to use this list
When a later phase needs one of these facts, resolve it from the suggested source, then move the
confirmed statement into the relevant doc with a **[Verified]** tag and a citation — and delete the
corresponding line here.
