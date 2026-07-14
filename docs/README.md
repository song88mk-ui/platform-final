# Platform-Final — Repository Knowledge Base

> **Phase 1 — Repository Discovery & Knowledge Acquisition**
> Status: Analysis-only baseline. No source code was modified to produce this documentation.
> Generated: 2026-06-24. Repo version: `3.35.0` (see [.version](../.version)).
> Continued 2026-06-24: enriched the API/WebSocket contracts by cross-referencing the
> **platform-web** sibling (the Blazor front-end whose `docs/` documents this backend).

This `/docs` folder is the durable knowledge base for the `platform-final` repository — the
core .NET sportsbook platform within the larger Song88 betting workspace.

## How to read this

| Document | What it answers |
|----------|-----------------|
| [Architecture.md](Architecture.md) | Overall architecture style, layers, communication patterns, data flow |
| [RepositoryMap.md](RepositoryMap.md) | One-line map of every project and where things live |
| [ProjectOverview.md](ProjectOverview.md) | Per-project purpose, responsibilities, consumers |
| [SolutionStructure.md](SolutionStructure.md) | Solution layout, project types, target frameworks |
| [DependencyGraph.md](DependencyGraph.md) | Project references, package references, runtime dependency direction |
| [StartupProjects.md](StartupProjects.md) | Which projects are deployable entry points and how they boot |
| **Phase 2 — Startup & Runtime** | |
| [StartupFlow.md](StartupFlow.md) | Ordered boot sequence for each process archetype (web / worker / console) |
| [Hosting.md](Hosting.md) | Process model: host builders, Kestrel/ports, threads, Windows service, shutdown |
| [DependencyInjection.md](DependencyInjection.md) | Container, lifetimes (singleton/scoped/typed-HttpClient), options & factory registrations |
| [MiddlewarePipeline.md](MiddlewarePipeline.md) | Runtime HTTP pipeline order; exception handling; the inert `UseAuthorization` |
| [Configuration.md](Configuration.md) | Config sources/precedence, env vars, feature flags, secrets handling (no values) |
| [Authentication.md](Authentication.md) | Custom API-key/header auth via ActionFilters (no JWT/cookie/OIDC) |
| [Authorization.md](Authorization.md) | Coarse key+audience access control; no policies/roles/claims |
| [Logging.md](Logging.md) | Serilog structured logging, OpenTelemetry traces/metrics, correlation |
| **Phase 10 — Validation & Technical Debt** | |
| [KnowledgeValidation.md](KnowledgeValidation.md) | Confidence-by-topic, explicit assumptions, validated negatives, knowledge gaps, readiness statement |
| [TechnicalDebt.md](TechnicalDebt.md) | Consolidated debt register (TD-01…TD-17), each with Evidence·Files·Confidence·Suggested investigation |
| [ArchitectureRisks.md](ArchitectureRisks.md) | Systemic risks (R-1…R-16) + circular-dependency check + risk heat map |
| [Unknowns.md](Unknowns.md) | Open-questions register (U-01…U-30) with resolved/open tally |
| **Phase 9 — Repository Intelligence** (`knowledge/` quick-reference) | |
| [knowledge/CommonServices.md](knowledge/CommonServices.md) | Reach-for-these services/abstractions |
| [knowledge/CommonDtos.md](knowledge/CommonDtos.md) | Most-used request/response DTOs + the envelope |
| [knowledge/CommonModels.md](knowledge/CommonModels.md) | Core entities + enum cheat-sheet + where each lives |
| [knowledge/ConfigurationKeys.md](knowledge/ConfigurationKeys.md) | Config section/key + feature-flag cheat-sheet (no secrets) |
| [knowledge/PlatformApiEndpoints.md](knowledge/PlatformApiEndpoints.md) | Gateway route lookup + StatusCode quick map |
| [knowledge/WebSocketMessages.md](knowledge/WebSocketMessages.md) | Subscribe/push events + abbreviated field maps |
| [knowledge/Utilities.md](knowledge/Utilities.md) | Utility/helper/extension inventory + "which util?" |
| [knowledge/Troubleshooting.md](knowledge/Troubleshooting.md) | Symptom → cause → where-to-look |
| [knowledge/FrequentlyModifiedAreas.md](knowledge/FrequentlyModifiedAreas.md) | Git-history churn hotspots + change-coupled clusters |
| **Phase 8 — Implementation Patterns** (how the team writes software) | |
| [ImplementationPatterns.md](ImplementationPatterns.md) | Pattern catalogue (repository/factory/service/extensions/messaging…) + what's deliberately absent |
| [CodingStandards.md](CodingStandards.md) | Observed style rules: language, files, async, DI, pipeline, error handling |
| [NamingConventions.md](NamingConventions.md) | Project/type/wire/config/route naming, incl. real misspelled identifiers |
| [BestPractices.md](BestPractices.md) | The team's consistent "house rules" to mirror when adding code |
| [AntiPatterns.md](AntiPatterns.md) | Recurring problematic patterns present in the code (not to propagate) |
| **Phase 7 — UI Architecture** (the platform's UI tier = the `platform-web` Blazor app) | |
| [UIArchitecture.md](UIArchitecture.md) | Blazor Server entry points, routing, AL vs EL2 layouts, theming, error boundaries |
| [ComponentHierarchy.md](ComponentHierarchy.md) | The full Child.Web component tree (AL + EL2) + composition patterns |
| [SharedComponents.md](SharedComponents.md) | Layout shell, reusable primitives, headers, modals, cascading values |
| [StateManagement.md](StateManagement.md) | Scoped state containers (`INotifyPropertyChanged`), DI, component communication |
| [Navigation.md](Navigation.md) | Route tables, query-param navigation, parent-frame postMessage navigation |
| [Rendering.md](Rendering.md) | Blazor lifecycle, `ShouldRender` optimisation, `<Virtualize>`, CSS/theming |
| [JavaScriptInterop.md](JavaScriptInterop.md) | JS interop inventory, `[JSInvokable]`/`DotNetObjectReference`, `platform-event` bus, LMT |
| **Phase 6 — Persistence & Caching** | |
| [Database.md](Database.md) | SQL Server: 9 DbContexts, tables/keys/indexes, stored procs, `decimal(26,12)` |
| [MongoDb.md](MongoDb.md) | The `Sportsbook` read model — collections, read preferences, **platform-final never writes** |
| [Caching.md](Caching.md) | `IMemCache` (per-pod), Redis (per client-code), client-side storage; cache-key catalogue |
| [CacheInvalidation.md](CacheInvalidation.md) | Event-driven eviction via `CacheSync.Service` + TTL fallback; cross-tier latency |
| [PersistencePatterns.md](PersistencePatterns.md) | Storage-mechanism patterns: transactions, audit-append, full-replace, schema mgmt, sync |
| [RepositoryPatterns.md](RepositoryPatterns.md) | The `IXxxRepository`+impl abstraction across EF/Mongo/proc/HTTP |
| **Phase 5 — Cross-Repository Integration** | |
| [CrossRepositoryDependencies.md](CrossRepositoryDependencies.md) | Authoritative integration map for every sibling (classified Verified / Verified-sibling / Unknown) |
| [PlatformApi.md](PlatformApi.md) | `Platform.API` as a provider contract — who consumes it, with which key |
| [OddsEngineIntegration.md](OddsEngineIntegration.md) | odds-engine-2 (Mongo + RabbitMQ + WS feeder) + bet-genius (GTS) + oddsfeed (MTS) |
| [AuthenticationIntegration.md](AuthenticationIntegration.md) | Auth service (JWT/API-key issuer) and how platform-final relates (API keys, operator token, OG HMAC) |
| [WebSocket.md](WebSocket.md) | SignalR producer→hub→consumer: OE.Websocket.Service → Websocket.API2 → platform-web |
| [ExternalSystems.md](ExternalSystems.md) | Third-party + cloud infra (MTS, GTS, Ably, QTech, RedCat, AWS, Cloudflare, SSM) |
| [Contracts.md](Contracts.md) | The contract-sharing model (duplicated-by-convention) + routing-key registry + blast radius |
| **Phase 4 — End-to-End Data Flow** (cross-repo: platform-web ↔ platform-final ↔ odds-engine-2) | |
| [RequestLifecycle.md](RequestLifecycle.md) | Browser→Blazor→gateway→datastore→odds-engine traces for every major flow |
| [SequenceDiagrams.md](SequenceDiagrams.md) | Mermaid sequence diagrams (load, fixtures, odds, placement, cashout, settlement, auth, translations) |
| [RenderingPipeline.md](RenderingPipeline.md) | How platform-final data becomes pixels in the Blazor render tree |
| [StateFlow.md](StateFlow.md) | The three state tiers (circuit UI state / server caches / systems of record) and propagation |
| [ComponentDataFlow.md](ComponentDataFlow.md) | Per-component data sources mapped to platform-final endpoints/messages + backend-change blast radius |
| **Phase 3 — Sportsbook Domain** | |
| [DomainModel.md](DomainModel.md) | The domain entities + the three-store/three-owner split (Mongo read model vs SQL config vs SQL bets) |
| [BusinessGlossary.md](BusinessGlossary.md) | Domain vocabulary as the code uses it + abbreviated field decoder |
| [BusinessRules.md](BusinessRules.md) | Validation/placement/parlay/cashout/settlement rules (from StatusCode + config) |
| [EntityLifecycle.md](EntityLifecycle.md) | Fixture / Market / Selection / Bet / Cashout state machines and producers |
| [Relationships.md](Relationships.md) | Relationship graph + ownership/source-of-truth/create-update-consume matrix |
| **Phase 1 — Discovery** | |
| [PublicApiSurface.md](PublicApiSurface.md) | `Platform.API` gateway endpoint catalogue + StatusCode enum (cross-checked with platform-web) |
| [WebSocketContracts.md](WebSocketContracts.md) | SignalR hub subscribe/push events + message contracts |
| [SharedLibraries.md](SharedLibraries.md) | Deep dive on `Platform.Shared` (the one shared library) |
| [ImplementationPatterns.md](ImplementationPatterns.md) | DI, repository, factory, messaging, telemetry patterns in use |
| [CodingStandards.md](CodingStandards.md) | Conventions actually observed in the code |
| [ConfigurationOverview.md](ConfigurationOverview.md) | Config sections, key names, secrets handling (no secret values) |
| [CrossRepositoryDependencies.md](CrossRepositoryDependencies.md) | How this repo integrates with sibling repos |
| [KnowledgeBase.md](KnowledgeBase.md) | Reusable patterns, glossary, domain concepts |
| [ArchitectureDecisions.md](ArchitectureDecisions.md) | Inferred/observed architectural decisions |
| [TechnicalDebt.md](TechnicalDebt.md) | Identified debt, inconsistencies, duplication, risks |
| [Unknowns.md](Unknowns.md) | Explicitly unverified items requiring clarification |

## Confidence convention

Every non-trivial claim is tagged:

- **[Verified]** — confirmed by reading the file(s) cited.
- **[Likely]** — strongly implied by naming + partial evidence, not fully traced.
- **[Unknown]** — could not be verified; tracked in [Unknowns.md](Unknowns.md).

## Evidence convention

Claims cite `Project/Path/File.cs` and, where known, a line number. Sibling-repo claims
cite the sibling path. Where a fact originates in another repository it is marked as such
and never presented as belonging to this one.

## Secrets notice

Several `appsettings.json` files in this repository contain committed plaintext secrets.
This documentation **never reproduces secret values** — only key names. See
[TechnicalDebt.md](TechnicalDebt.md) → "Committed secrets".
