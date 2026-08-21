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
- **Decision:** one `netstandard2.1` library holds entities, all 8 `DbContext`s, DTOs, enums,
  message contracts, provider integrations, and utilities. **[Verified]**
- **Rationale:** maximize reuse; single source of truth for contracts and data models;
  netstandard2.1 lets 3.1/7.0/8.0 consumers all reference it.
- **Consequences:** very high reuse **and** very large blast radius — any change rebuilds every
  image (CI affected-files includes `Platform.Shared/**`). No clean Domain/Infra separation.

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

### ADR-10 — Containerize on build-SDK 6.0, run on ASP.NET 3.1
- **Decision:** multi-stage Docker — build with `dotnet/sdk:6.0`, run on `dotnet/aspnet:3.1`;
  private GitHub NuGet feed; AWS RDS/DocumentDB CA bundle installed; strip env appsettings. **[Verified]**
- **Consequences:** newer SDK builds an EOL target runtime; code must remain 3.1-compatible.

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

---

## Decisions that appear *absent* (worth a future ADR)
- No central **secrets management** decision visible in-repo (secrets committed). **[Verified]**
- No committed **message-contract versioning** strategy. **[Unknown]**
- No repo-level **runtime-upgrade** plan off netcoreapp3.1. **[Unknown]**
