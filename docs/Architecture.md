# Architecture

> Confidence on the overall conclusions in this document: **High [Verified]** for structure,
> layering, and messaging; **Medium [Likely]** for end-to-end runtime message flows that cross
> into sibling repositories.

## 1. Architecture style

`platform-final` is a **microservice / message-driven distributed system** built on
**.NET 10** (all projects target `net10.0` since GM-744, 2026-08-20 — see
[SolutionStructure.md](SolutionStructure.md)). It is **not** a single deployable monolith:
the solution contains **40 projects** that compile into ~26 independently deployed Docker
images (see [.github/workflows/ci.yml](../.github/workflows/ci.yml)).

Two complementary communication styles coexist: **[Verified]**

1. **Synchronous HTTP/REST** between APIs (`AddHttpClient<IXxxRepository, XxxRepository>`),
   used for request/response flows (login, placement gateway, wallet, sportsbook reads).
2. **Asynchronous messaging over RabbitMQ** (`Platform.Shared/Utility/MessageBroker.cs`),
   used for the bet lifecycle (placement → provider → settlement → reporting) and for
   cache invalidation and real-time fan-out.

A third style — **real-time push to clients via SignalR** — sits at the edge
(`Platform.WebSocket.API`, `Platform.Websocket.API2`).

## 2. Layering

The platform layers per-service rather than as one global onion. Within each deployable
project the layering is consistent: **[Verified]**

```
HTTP edge          Controllers/ (ASP.NET Core controllers, ActionFilters/)
   │  or
Message edge       Worker.cs (BackgroundService consuming RabbitMQ)
   ▼
Application/Access Repository/ (IXxxRepository + XxxRepository)
   ▼
Shared kernel      Platform.Shared  (entities, DbContexts, message contracts,
                                     providers, MessageBroker, MemCache, enums)
   ▼
Infrastructure     SQL Server (EF Core 10), MongoDB, RabbitMQ, Redis, external HTTP
```

- The **only shared library** is `Platform.Shared` (`net10.0`). Every other project
  references it. **[Verified]** — see [DependencyGraph.md](DependencyGraph.md).
- There is **no separate Domain/Application/Infrastructure assembly split** — `Platform.Shared`
  is a broad "shared kernel" containing entities, EF Core `DbContext`s, DTOs, enums, message
  contracts, provider integrations, and utilities all together. **[Verified]**

## 3. Dependency direction

```
Every project ───────────────▶ Platform.Shared        (compile-time, one-way)

APIs ─── HTTP ──▶ other APIs (Platform.API ▶ Betting/Operator/Sportsbook/Casino)
APIs ─── HTTP ──▶ Platform.RabbitMQ.API ─── AMQP ──▶ RabbitMQ
Services ─── AMQP ──▶ RabbitMQ ─── AMQP ──▶ Services   (event bus)
```

- Compile-time dependencies point **inward to `Platform.Shared` only**; there are no
  project-to-project references between the deployable services/APIs. **[Verified]** — each
  `.csproj` references only `Platform.Shared` (and per-service `*.Repository`/`*.Telemetry`
  sub-projects where present). Cross-service communication is exclusively HTTP or RabbitMQ.
- Notable indirection: many APIs publish to RabbitMQ **over HTTP** by calling
  `Platform.RabbitMQ.API` (`AddHttpClient<IRabbitMQRepository,…>`), rather than opening an
  AMQP connection directly. Background services connect to RabbitMQ **directly** via
  `MessageBroker`. **[Verified]**

## 4. The bet lifecycle (primary domain flow)

This is the spine of the platform. Confidence **[Likely]** for the exact ordering across
services (each leg is [Verified] individually, the stitched-together sequence is inferred
from message-type producers/consumers):

```
Client ──HTTP──▶ Platform.API (/v2/bet/placement)
                    │  ServiceFilter APIKeyFilter
                    ▼
                 Platform.Betting.API (/v1/bet/placement)
                    │  validates via provider (MTS/GTS), persists Bet (BettingContext)
                    │  publishes OnBetPlacement ──▶ RabbitMQ (e.central)
                    ▼
      ┌───────────── RabbitMQ exchange e.central ─────────────┐
      ▼              ▼                ▼               ▼        ▼
 Placement.       BetInfo.        Provider.       AgentReports BetForwarder
 Servicev2        Service         Service         .Service     .Service
 (100-thread      (persist bet    (route to       (reporting)  (forward to
  provider        info)           bet provider,                 EXTERNAL
  dispatch)                       Ably assess)                  broker)
                                     │
 Odds Engine 2 ─OnOddsChange─▶ CashOutStake.Servicev2 / WebSocket.API
 (sibling repo)                (recompute cashout, push odds to clients)
                                     │
                          OnBetSettlement / OnBetCancellation
                                     ▼
                          Settlement.Service ──▶ Wallet.API (credit)
                                     │            AgentReports.Service (winloss)
                                     ▼
                          Job.Resettlement (Fibonacci-backoff retry of failures)
```

Key message contracts live in `Platform.Shared/MessageModels/` (`OnBetPlacement`,
`OnBetSettlement`, `OnBetCancellation`, `OnBetFullCashOut`, `OnWalletChange`,
`OnOddsChange`/`OnMarketChange`, etc.). **[Verified]** that these classes exist; see
[SharedLibraries.md](SharedLibraries.md).

## 5. Real-time edge

`Platform.WebSocket.API` and `Platform.Websocket.API2` (both `net10.0`) are
**SignalR hubs** (`Hub<IPlatformEvent>`) using **MessagePack** binary protocol and, in
production, **Azure SignalR Service**. They expose a `/platform` hub with `SubscribeTo*` /
`UnsubscribeTo*` methods (betslip odds, bet status, cashout, wallet, payable, favorites) and
`Publish*` server→client pushes. `API2` is the newer re-implementation. **[Verified]** —
`Platform.WebSocket.API/Repository/WebSocketRepository.cs`,
`Platform.Websocket.API2/Program.cs`. Full event/contract catalogue:
[WebSocketContracts.md](WebSocketContracts.md).

**Primary consumer:** the **platform-web** sibling (a .NET 7 Blazor Server BFF) consumes the
`Platform.API` REST gateway and this SignalR hub. Its docs gave us verified consumer-side
contracts — see [PublicApiSurface.md](PublicApiSurface.md) and
[CrossRepositoryDependencies.md](CrossRepositoryDependencies.md).

## 6. Data architecture

**Polyglot, database-per-bounded-context.** `Platform.Shared/Data/` defines **8 EF Core
DbContexts**, each in its own namespace/folder and mapped to its own SQL Server database via
a distinct `ConnectionStrings:*` key: **[Verified]**

| DbContext | Folder | Connection string key | Domain |
|-----------|--------|------------------------|--------|
| `PlatformContext` | `Data/Platform` | `ConnectionStrings:Platform` | Clients, operators, config, retries |
| `BettingContext` | `Data/Betting` | `ConnectionStrings:Betting` | Bets, selections, configs, parlay bonus |
| `SportsbookContext` | `Data/Sportsbook` | `ConnectionStrings:Sportsbook` | Fixtures/markets/leagues ordering |
| `TranslationContext` | `Data/Translation` | `ConnectionStrings:Translation` | Multilingual content |
| `WalletContext` | (Wallet.API project) | `ConnectionStrings:Wallet` | Wallet ledger (stored procs) |
| `AgentReportsContext` | `Data/Reports` | `ConnectionStrings:AgentReports` | Agent P&L / reconciliation |
| `CasinoBettingContext` | `Data/CasinoBetting` | `ConnectionStrings:Casino*` | Casino bets |
| `ESportBettingContext` | `Data/ESportBetting` | `ConnectionStrings:ESportBetting` | ESports bets |
| `RedCatBettingContext` | `Data/RedCatBetting` | `ConnectionStrings:RedCatBetting` | RedCat provider bets |

> Note: `WalletContext` is defined inside the `Platform.Wallet.API` project, not in
> `Platform.Shared`. **[Verified]** via `Platform.Wallet.API/Startup.cs`.

Beyond SQL: **MongoDB** (sportsbook fixtures/markets read model, casino betting status;
`MongoDB.Driver`), **Redis** (`StackExchange.Redis` in `Platform.ClientSportsbook.API`),
and **in-process `MemCache`** (`System.Runtime.Caching`). **[Verified]**

## 7. Cross-cutting concerns

| Concern | Mechanism | Evidence |
|---------|-----------|----------|
| Logging | **Serilog**, console JSON (`CompactJsonFormatter`), `ReadFrom.Configuration` | `*/Program.cs`, `*/appsettings.json` |
| Tracing/metrics | **OpenTelemetry** via private `Infra.Extensions.Telemetry` pkg + per-project `Telemetry/Instrumentation.cs` | `*/Startup.cs`, `*/Telemetry/` |
| AuthN/Z (edge) | API-key `ActionFilters` (`APIKeyFilter`, `InternalAPIKeyFilter`, `AuthorizationFilter`, `PassKeyFilter`) | `*/ActionFilters/` |
| CORS | `AllowAllOrigins` policy (AllowAny origin/method/header) | `*/Startup.cs` |
| Reverse-proxy | `ForwardedHeaders` (X-Forwarded-For/Proto), proxies cleared | `*/Startup.cs` |
| Caching | `IMemCache` (in-proc), Redis (one API) | `Platform.Shared/Utility/MemCache.cs` |
| Response shape | `BaseResponseDTO<T>` { `StatusCode`, `Message`, `Result` } with a **business** `StatusCode` enum | `Platform.Shared/Models/BaseResponseDTO.cs` |

See [ImplementationPatterns.md](ImplementationPatterns.md) and
[CodingStandards.md](CodingStandards.md) for detail.

## 8. Deployment topology

- CI builds each app as a Docker image and pushes to **AWS ECR** (per-environment account
  IDs for dev/stg/prd) via GitHub Actions OIDC. **[Verified]** — `.github/workflows/ci.yml`.
- Images are deployed to **EKS** via the sibling **`argocd`** GitOps repo; the central
  RabbitMQ cluster and databases are provisioned by the sibling **`infrastructure`** repo.
  **[Likely]** — see [CrossRepositoryDependencies.md](CrossRepositoryDependencies.md).
- Multi-stage Dockerfile: **build on `dotnet/sdk:10.0`**, **run on `dotnet/aspnet:10.0`**
  with an explicit `ENV ASPNETCORE_URLS=http://+:80` (the `aspnet:8.0+` images default to
  port 8080; the pin preserves the argocd Service `:3000 → container :80` mapping),
  private GitHub NuGet feed (`FlyingCockatoo`), AWS RDS/DocumentDB CA bundle installed.
  **[Verified]** — `Platform.Sportsbook.API/Dockerfile` (updated GM-744, 2026-08-20).

## 9. Architectural risks (summary — detail in TechnicalDebt.md)

- ~~**EOL runtime**: .NET Core 3.1 and netstandard2.1 are out of support.~~ **Resolved**
  (2026-08-20, GM-744): everything now targets `net10.0`.
- ~~**Build/runtime SDK mismatch**: build SDK 6.0, runtime ASP.NET 3.1.~~ **Resolved**
  (2026-08-20, GM-744): build and runtime images are both 10.0.
- **Committed plaintext secrets** in many `appsettings.json`.
- **Two parallel WebSocket implementations** (`WebSocket.API` vs `Websocket.API2`).
- **Two parallel odds engines** in the ecosystem (`odds-engine-2` preferred; `oddsengine` obsolete).
- **Hand-rolled 100-thread pools** in `CashOut.Service` / `Placement.Servicev2`.
