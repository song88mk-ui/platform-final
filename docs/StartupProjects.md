# Startup Projects & Boot Behavior

> Confidence: **High [Verified]** for the boot patterns of the projects whose `Program.cs`/
> `Startup.cs` were read; **[Likely]** where inferred from SDK type + naming.

There is **no single startup project** — this is a multi-service solution. Each deployable
project is its own process/container. They fall into three boot shapes.

## A. ASP.NET Core Web hosts (`Sdk.Web`) — Startup + Program split

Pattern (`Program.cs`): **[Verified]**
```csharp
Host.CreateDefaultBuilder(args)
    .UseSerilog((ctx, _, log) => log.ReadFrom.Configuration(ctx.Configuration))
    .ConfigureWebHostDefaults(web => web.UseStartup<Startup>());
```
`Startup.ConfigureServices` then registers: telemetry (`Instrumentation` + `AddTelemetry`),
CORS `AllowAllOrigins`, `ForwardedHeaders`, controllers, typed HttpClients, repositories,
Swagger, and `DbContext`s. `Startup.Configure` wires the pipeline: dev exception page →
forwarded headers → Swagger → routing → CORS → authorization → endpoints. **[Verified]**
(`Platform.Sportsbook.API/Startup.cs`, `Platform.API/Startup.cs`, etc.)

Projects: `Platform.API`, `Betting.API`, `Sportsbook.API`, `ClientSportsbook.API`,
`ClientPlatform.API`, `Client.API`, `Operator.API`, `Wallet.API`, `Oddin.API`, `QTech.API`,
`RedCat.API`, `RedCatBetting.API`, `RedCatOperator.API`, `AgentReports.API`, `RabbitMQ.API`,
`WebSocket.API`. **Exception:** `Websocket.API2` uses the **minimal-hosting** style
(everything in `Program.cs`, no `Startup.cs`). **[Verified]**

> Some web hosts (`Client.API`, `ClientSportsbook.API`) do **not** configure Serilog in
> `Program.cs`. **[Verified]**

## B. Generic Host background workers (`Sdk.Worker`) — Program-only

Pattern (`Program.cs`): **[Verified]**
```csharp
Host.CreateDefaultBuilder(args)
    [.UseSerilog(...)]            // most, not all
    [.UseWindowsService()]        // BetForwarder, CacheSync, ESportOperator, AgentReports.Service
    .ConfigureServices((ctx, services) => {
        services.AddSingleton<IMessageBroker, MessageBroker>();
        services.AddSingleton/Scoped<IXxxRepository, XxxRepository>();
        [services.AddDbContext<...>(...);]   // CashOutStake, Operator.Score, Resettlement
        services.AddHostedService<Worker>();
    });
```
The `Worker : BackgroundService.ExecuteAsync` typically: connects `MessageBroker`
(`Connect` / `ConnectWithManualAck`), registers a `MessageReceivedEvent` handler that
`switch`es on `MessageType`, and processes. Several preload caches on startup. **[Verified]**

Projects: all `*.Service` / `*.Servicev2`, `AgentReports.Service`, `Client.TopMarket`,
`BetForwarder/BetInfo/BetStatus/CacheSync/CashOut/CashOutStake/Cancellation/Provider/Operator*/ESportOperator`.

### High-throughput variant (100-thread pools)
`Platform.CashOut.Service` and `Platform.Placement.Servicev2` create **100 threads + 100
`BlockingCollection` queues** and round-robin incoming messages across them (manual ack).
**[Verified]**. This is a hand-rolled work-distribution scheme — see
[TechnicalDebt.md](TechnicalDebt.md) and [ImplementationPatterns.md](ImplementationPatterns.md).

### Job variant (timed loops)
`Job.CCF`, `Job.ForexEUR`, `Job.Operator.Score`, `Job.Resettlement` run an infinite loop with
`Task.Delay` between iterations (CCF: 300s; ForexEUR: 1 h, 1 min after a failure; Resettlement: Fibonacci backoff). **[Verified]** for
CCF/ForexEUR/Resettlement.

## C. Plain console (`Sdk`) — `static Main`

`Platform.BetPlacement.Console` — no Generic Host; a `Main` that sets up a static `HttpClient`
and runs nested loops placing test bets. **[Verified]**

## Deployment entry points

The set of projects that ship as their own container is the **CI matrix** in
`.github/workflows/ci.yml` (26 apps, including `Operator.Cashout.Service`), each with a
`Dockerfile` whose `CMD` runs
`Platform.<Name>.dll`. **[Verified]**. Projects not in the matrix (QTech, RedCat*, Oddin,
AgentReports*, ESportOperator, WebSocket.API, Client/sample,
console, Shared, tests) are either deployed by another pipeline, deployed manually, or
inactive — **[Unknown]**; see [Unknowns.md](Unknowns.md).

## Container boot (runtime image) `[Verified]` (updated 2026-08-20, GM-744)
- Base runtime image: `mcr.microsoft.com/dotnet/aspnet:10.0` (`aspnet:8.0` for Sportsbook.API and CashOutStake.Servicev2), with an explicit
  `ENV ASPNETCORE_URLS=http://+:80` (the aspnet:8.0+ images default to port 8080),
  `EXPOSE 80`, `WORKDIR /app`.
- Entry: `CMD ["dotnet", "Platform.<Name>.dll"]` (portable form; was `/usr/bin/dotnet`). Exceptions: Placement.Servicev2 runs `entrypoint.sh` (MTS config injection); Websocket.API2 uses `ENTRYPOINT`.
- Web hosts therefore still listen on **port 80** in-container. Internal service DNS (per
  sibling `argocd`/`backoffice-final` config) targets port **3000** — meaning the K8s Service
  maps 3000→80. **[Likely]** (the 3000 figure comes from sibling config, not this repo).
- `appsettings.*.json` (environment overrides) are **deleted** from the published output in
  the build stage; runtime config is expected from environment/mounted config. **[Verified]**
