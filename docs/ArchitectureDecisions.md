# Architecture Decisions (Observed / Inferred)

> These are **reconstructed** decision records — there is no committed ADR log in the repo.
> Each entry states the decision as evidenced by the code, the rationale we can infer, and the
> consequences. Confidence is tagged. These are **descriptive, not prescriptive** (Phase 1 does
> not recommend changes).

---

### ADR-01 — Microservices over a monolith
- **Decision:** split the platform into ~26 independently deployed services/APIs sharing one
  library, rather than a single app. **[Verified]**
- **Rationale (inferred):** independent scaling of hot paths (placement, cashout, settlement),
  isolation of partner integrations, independent deploy cadence.
- **Consequences:** operational complexity (many images, K8s, GitOps); cross-service contracts
  must be managed via RabbitMQ + HTTP; `Platform.Shared` becomes a shared-kernel coupling point.

### ADR-02 — RabbitMQ topic bus (`e.central`) as the integration spine
- **Decision:** asynchronous, event-driven core over a single shared topic exchange with
  per-domain/per-client routing keys. **[Verified]**
- **Rationale:** decouple producers from consumers; fan-out the bet lifecycle to many
  independent consumers (reporting, forwarding, cashout, websocket).
- **Consequences:** eventual consistency; message-contract versioning matters; a central broker
  is a shared dependency across **all** sibling repos.

### ADR-03 — Shared kernel (`Platform.Shared`) instead of layered assemblies
- **Decision:** one shared library holds entities, all 8 `DbContext`s, DTOs, enums,
  message contracts, provider integrations, and utilities. **[Verified]**
- **Rationale:** maximize reuse; single source of truth for contracts and data models;
  originally `netstandard2.1` so 3.1/7.0/8.0 consumers could all reference it (since
  GM-744 it targets `net8.0` — the lowest TFM among its consumers, forced by the three MongoDB services held at net8, see ADR-14).
- **Consequences:** very high reuse **and** very large blast radius — any change rebuilds every
  image (CI has no affected-files detection — all 26 matrix images build on every push to main/PR). No clean Domain/Infra separation.

### ADR-04 — Database-per-bounded-context (8 SQL DBs) + polyglot persistence
- **Decision:** separate SQL databases per domain (`Platform`, `Betting`, `Sportsbook`,
  `Translation`, `Wallet`, `AgentReports`, `Casino`, `ESport`, `RedCat`), plus MongoDB (sportsbook
  read model / casino status) and Redis (client sportsbook), plus in-proc `MemCache`. **[Verified]**
- **Rationale:** isolate domains; use Mongo for fast fixture/market reads; Redis/MemCache for hot reads.
- **Consequences:** no cross-DB transactions; consistency handled via events; multiple
  connection strings to manage; back office reads several of these DBs directly.

### ADR-05 — Provider abstraction via factory + strategy
- **Decision:** `IBetProvider` strategy implementations (MTS, GTS) resolved by `ChannelType`
  through `IBetProviderFactory`. **[Verified]**
- **Rationale:** plug new betting providers without touching call sites; route per channel.
- **Consequences:** adding a provider = implement interface + register; `ChannelType` enum is a
  contract that must stay stable.

### ADR-06 — Publish-to-RabbitMQ-over-HTTP for APIs
- **Decision:** APIs publish messages by calling `Platform.RabbitMQ.API` (HTTP) rather than
  opening AMQP connections; services connect to AMQP directly. **[Verified]**
- **Rationale (inferred):** keep stateless request/response APIs free of long-lived AMQP
  connection management; centralize broker config/credentials.
- **Consequences:** extra network hop + a single-point publish service; broker creds centralized.

### ADR-07 — SignalR + MessagePack + Azure SignalR for client real-time
- **Decision:** push odds/bet-status/wallet/cashout to clients via SignalR hubs using MessagePack;
  Azure SignalR Service in production. **[Verified]**
- **Rationale:** managed scale-out for many concurrent client connections; compact binary payloads.
- **Consequences:** Azure dependency; two implementations now coexist (see ADR-12).

### ADR-08 — Observability via OpenTelemetry + Serilog (private tooling package)
- **Decision:** standardize on `Infra.Extensions.Telemetry` (private NuGet) for OTel and Serilog
  console-JSON for logs; trace context propagated through RabbitMQ headers. **[Verified]**
- **Consequences:** dependency on a private GitHub feed (build needs `NUGET_PASSWORD`); consistent
  tracing across services (where adopted — not uniform).

### ADR-09 — Hand-rolled 100-thread fan-out for hottest queues
- **Decision:** `CashOut.Service` and `Placement.Servicev2` use 100 threads + 100
  `BlockingCollection`s with round-robin distribution and manual ack. **[Verified]**
- **Rationale (inferred):** maximize throughput / ordering control on the busiest queues under 3.1.
- **Consequences:** complex, hard-to-tune concurrency code; a modern alternative would be
  `System.Threading.Channels` / consumer concurrency. Tracked as debt.

### ADR-10 — Containerize on build-SDK 6.0, run on ASP.NET 3.1 — **Superseded** (2026-08-20, GM-744, by ADR-14)
- **Decision:** multi-stage Docker — build with `dotnet/sdk:6.0`, run on `dotnet/aspnet:3.1`;
  private GitHub NuGet feed; AWS RDS/DocumentDB CA bundle installed; strip env appsettings. **[Verified]**
- **Consequences:** newer SDK builds an EOL target runtime; code must remain 3.1-compatible.
- **Status:** superseded by the .NET 10 upgrade (ADR-14); build images are sdk:10.0, runtime aspnet:10.0 (aspnet:8.0 for the two Mongo services with Dockerfiles).

### ADR-11 — Deploy via AWS ECR + ArgoCD GitOps to EKS
- **Decision:** CI pushes per-app images to environment-specific ECR accounts (dev/stg/prd) via
  OIDC; `argocd` syncs to EKS; `infrastructure` provisions substrate. **[Verified] (CI)** /
  **[Likely] (GitOps wiring, in sibling repos)**.

### ADR-12 — Incremental "v2" rewrites alongside originals
- **Decision:** introduce `Servicev2` / `Websocket.API2` rewrites that run beside the originals
  rather than in-place migration. **[Verified]**
- **Rationale (inferred):** de-risk rewrites; cut over gradually.
- **Consequences:** transient duplication; need to track which version is authoritative
  (CI ships `Websocket.API2`).

### ADR-13 — Business status in payload, transport status minimal
- **Decision:** uniform `BaseResponseDTO<T>` with a business `StatusCode` enum; HTTP layer mostly
  uses 200 (success/maintenance) vs 400 (everything else). **[Verified]**
- **Consequences:** clients must inspect the body's `StatusCode`; HTTP semantics under-used.

### ADR-14 — Upgrade to .NET 10, with Mongo services held at .NET 8 (GM-744; amended 2026-08-24)
- **Decision:** 36 projects target `net10.0`; `Platform.Shared` and the three
  MongoDB-reading services (`Sportsbook.API`, `QTech.API`, `CashOutStake.Servicev2`)
  target `net8.0`. Every Dockerfile builds on `mcr.microsoft.com/dotnet/sdk:10.0`; runtime
  is `mcr.microsoft.com/dotnet/aspnet:10.0` except Sportsbook.API and
  CashOutStake.Servicev2 which run on `aspnet:8.0` (QTech.API has no Dockerfile).
  Each runtime stage sets `ENV ASPNETCORE_URLS=http://+:80` explicitly (aspnet:8.0+ images
  default to port 8080; the pin preserves the argocd Service `:3000 → container :80`
  mapping) and uses the portable `CMD ["dotnet", "<App>.dll"]` form (exceptions: Placement.Servicev2 keeps its `entrypoint.sh` wrapper; Websocket.API2 uses `ENTRYPOINT ["dotnet", ...]`). **[Verified]**
- **Why the .NET 8 carve-out (amendment, 2026-08-24):** the first all-net10 deployment
  failed in staging: MongoDB.Driver 2.11.6's LINQ partial evaluator reflection-invokes a
  method .NET 10 no longer allows, throwing `NotSupportedException: Specified method is
  not supported` when rendering any filter containing `Contains` over an *array* (e.g.
  `fixtureTypeIds.Contains(c.FiTyId)` in `SportsbookRepository.GetSports`). Reproduced in
  isolation: the same expression renders correctly on net8.0 and throws on net10.0.
  `Platform.Shared` (MongoDB.Bson) must stay `net8.0` so the net8 services can reference
  it. Path back to full net10: upgrade MongoDB.Driver to a modern release (2.19+/3.x,
  LINQ3 provider) — tracked as technical debt, requires cross-team validation because
  odds-engine-2 owns the catalogue.
- **Supporting decisions:** EF Core unified at **8.0.30 (LTS)** across the graph — EF 10
  requires net10 and cannot be used by the net8 `Platform.Shared`, which owns 8 of the 11
  DbContexts; one EF version everywhere avoids compile-vs-runtime assembly mismatches
  (note: EF 8 LTS support ends 2026-11-10 — revisit with the driver upgrade).
  Microsoft.Extensions.* stay at 10.0.11 and Swashbuckle at 10.2.3 (both ship net8.0
  assets); wire-format-sensitive packages deliberately left
  unchanged for cross-repo safety (RabbitMQ.Client 5.2.0, MongoDB.Driver 2.11.6/2.13.1,
  Newtonsoft.Json 12.0.3, Sportradar.MTS.SDKCore 2.4.1.2, Serilog stack,
  Infra.Extensions.Telemetry 0.1.0-3; Newtonsoft.Json later moved to 13.0.4 for
  GHSA-5crp-9r3c-p9vr, see TD-02). Committed `appsettings*.json` SQL connection
  strings gained `TrustServerCertificate=True` (Microsoft.Data.SqlClient 4+ defaults
  `Encrypt=true`).
- **Deployment prerequisites (must happen before the first stg/prd release from this
  code — owner: devops):**
  1. **SQL TLS:** production connection strings in AWS SSM will fail the TLS handshake
     under Microsoft.Data.SqlClient 5.x (pulled by EF Core 8; Encrypt defaults to true since 4.0) unless changed. Two options: (a) quick — append
     `TrustServerCertificate=True` in SSM (disables server-cert validation; matches the
     committed dev/staging fallbacks); (b) better — bake the RDS CA bundle into all
     runtime images (the `ADD https://truststore.pki.rds.amazonaws.com/... +
     update-ca-certificates` pattern already used for DocumentDB in
     `Platform.Sportsbook.API/Dockerfile` and `Platform.CashOutStake.Servicev2/Dockerfile`)
     and keep `Encrypt=true` with real validation. Option (b) needs a follow-up PR.
  2. **SQL compatibility level:** EF Core 8+ translates parameterized `Contains` to
     `OPENJSON`, which requires database compatibility level >= 130 (SQL Server 2016+).
     Verify every target database's compat level before rollout, or pin
     `UseSqlServer(..., o => o.UseCompatibilityLevel(<n>))`. **[Unknown: actual compat
     levels of stg/prd databases]**
- **Consequences:** supported runtime; single SDK (10.0.x) for local dev and CI; code no
  longer needs to stay 3.1-compatible. Supersedes ADR-10.

---

## Decisions that appear *absent* (worth a future ADR)
- No central **secrets management** decision visible in-repo (secrets committed). **[Verified]**
- No committed **message-contract versioning** strategy. **[Unknown]**
- ~~No repo-level **runtime-upgrade** plan off netcoreapp3.1. **[Unknown]**~~ Resolved
  (2026-08-20, GM-744): the upgrade to .NET 10 happened — see ADR-14.
