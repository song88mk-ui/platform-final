# Hosting & Process Model

> Phase 2. Confidence **High [Verified]** unless noted. How platform-final processes are hosted,
> bound, threaded, and shut down. Complements [StartupFlow.md](StartupFlow.md) and
> [StartupProjects.md](StartupProjects.md).

## Host builders in use
| Host type | Builder | Projects |
|-----------|---------|----------|
| ASP.NET Core (Kestrel) | `Host.CreateDefaultBuilder` + `ConfigureWebHostDefaults` + `UseStartup<Startup>` | all `*.API` except `Websocket.API2` |
| ASP.NET Core minimal hosting | `WebApplication.CreateBuilder` (top-level) | `Platform.Websocket.API2` |
| Generic Host (worker) | `Host.CreateDefaultBuilder` + `ConfigureServices` + `AddHostedService<Worker>` | 18 services/jobs `[Verified]` |
| None (console) | `static Main` | `Platform.BetPlacement.Console` |

## Web server & network binding
- **Server:** Kestrel (the `ConfigureWebHostDefaults` default). No `UseKestrel`/`ConfigureKestrel`/
  `UseUrls`/`ListenAnyIP` anywhere — **zero custom Kestrel configuration**. **[Verified]**
- **Port:** comes from the environment, not code. Since GM-744 (2026-08-20) the runtime image
  is `mcr.microsoft.com/dotnet/aspnet:10.0`, which defaults to port **8080** — so every
  Dockerfile now sets `ENV ASPNETCORE_URLS=http://+:80` explicitly, plus `EXPOSE 80`. So
  containers still listen on **:80**. **[Verified]** (Dockerfile).
- TLS termination and the `:80→:3000` Service mapping are handled by the ingress / K8s layer
  (sibling `argocd`), not by these processes. **[Likely]** — see [Configuration.md](Configuration.md).
- **`ForwardedHeaders`** middleware is enabled so the apps trust `X-Forwarded-For`/`-Proto` from
  the ingress (KnownNetworks/Proxies cleared = trust all upstreams). **[Verified]**

## HTTP/2 cleartext switch
11 projects call `AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true)`
in `Main`. **[Verified]** Purpose: allow the **OpenTelemetry OTLP exporter** to push traces/metrics
over **gRPC (HTTP/2) without TLS** to the in-cluster collector. See [Logging.md](Logging.md).

## Background-service hosting (`IHostedService` / `BackgroundService`)
- 18 `AddHostedService<Worker>` registrations. Each `Worker : BackgroundService`. **[Verified]**
- `BackgroundService.ExecuteAsync` performs the real init (RabbitMQ connect, handler wiring,
  state preload) — see [StartupFlow.md](StartupFlow.md) Archetype B.
- **Windows Service hosting:** 4 services call `.UseWindowsService()` — `BetForwarder.Service`,
  `CacheSync.Service`, `ESportOperator.Service`, `AgentReports.Service`. **[Verified]** This is
  benign on Linux/containers (`UseWindowsService` no-ops when not run as a Windows service), but
  it signals these were originally Windows-hosted; only `BetForwarder` & `CacheSync` are in the
  CI/Docker matrix. **[Verified]**

## Concurrency model inside workers
- **Default:** a single RabbitMQ consumer callback per worker; concurrency bounded by
  `MessageBroker:PrefetchCount` (~1000). **[Verified]**
- **Dedicated-thread fan-out:** 5 workers create raw `new Thread(...)` pools with
  `BlockingCollection<>` queues and round-robin distribution: **[Verified]**
  - `CashOut.Service`, `Placement.Servicev2`, `Operator.Servicev2`, `Operator.Cashout.Service`
    — high-throughput (≈100 threads on the hottest two).
  - `Client.TopMarket` — 3 threads (placement/settlement/cancellation).
  - This hand-rolled scheme is flagged in [TechnicalDebt.md](TechnicalDebt.md) (TD-06); the
    modern equivalent would be `System.Threading.Channels` / consumer concurrency.

## EF Core hosting choices
- Standard: `AddDbContext<T>` (scoped). **[Verified]**
- **Pooled** (`AddDbContextPool` / `DbContextPool<>`): only the 3 RedCat projects
  (`RedCat.API`, `RedCatBetting.API`, `RedCatOperator.API`) — chosen for their hot
  request/callback paths. **[Verified]**
- `Job.Resettlement` sets a **30-minute** DbContext command timeout. **[Verified]**

## Health & probes
- RedCat APIs enable ASP.NET **health checks**. **[Verified]** (Phase 1)
- `Websocket.API2` exposes `v1/heartbeat` and `v1/connections`. **[Verified]**
- A platform-wide liveness/readiness convention is **[Unknown]** (likely defined in `argocd`
  probes against `:80`) — tracked in [Unknowns.md](Unknowns.md).

## Shutdown
- Generic Host handles `SIGTERM`/Ctrl-C → cancels the `stoppingToken` → `Worker.ExecuteAsync`
  unwinds; `MessageBroker`/DbContext are `IDisposable` and disposed by the container. **[Verified]**
- No custom `IHostApplicationLifetime` hooks or graceful-drain logic were found. Workers using
  manual ack rely on RabbitMQ redelivery if killed mid-process. **[Verified]**

## Runtime image (recap; updated 2026-08-20, GM-744) `[Verified]`
Multi-stage Docker: build `dotnet/sdk:10.0` → run `dotnet/aspnet:10.0`,
`ENV ASPNETCORE_URLS=http://+:80` + `EXPOSE 80` (aspnet:8.0+ defaults to :8080 — the pin
preserves the argocd Service `:3000 → :80` mapping), `CMD ["dotnet","Platform.<Name>.dll"]` (Placement.Servicev2: `entrypoint.sh`; Websocket.API2: `ENTRYPOINT`)
(portable form), private GitHub NuGet feed, AWS RDS/DocumentDB CA bundle installed
(fetched via Dockerfile `ADD <url>` + `update-ca-certificates` where needed — curl is not
in the aspnet:10.0 image), env-specific `appsettings.*.json` stripped from the published output.
