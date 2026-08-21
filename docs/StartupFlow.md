# Startup Flow

> **Phase 2 — Startup & Runtime Analysis.** Analysis-only; no code modified. Confidence
> **High [Verified]** unless noted — derived from `Program.cs`/`Startup.cs`/`Worker.cs` across
> projects. Builds on Phase 1's [StartupProjects.md](StartupProjects.md) (which classifies the
> boot shapes); this document traces the **ordered sequence** of what happens at process start.

platform-final has **three** process archetypes. Each has a distinct startup sequence.

---

## Archetype A — ASP.NET Core Web host (the APIs)

Representative: `Platform.API` (`Program.cs`, `Startup.cs`). Identical shape across all `*.API`
projects except `Websocket.API2` (see Archetype A′).

### Sequence
```
1. Main()
   └─ AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true)
        → enables HTTP/2 cleartext (h2c) for the OTLP gRPC trace/metric exporter   [Verified]
   └─ CreateHostBuilder(args).Build().Run()

2. Host.CreateDefaultBuilder(args)        [framework]
   ├─ loads configuration (see Configuration.md): appsettings.json
   │   → appsettings.{Environment}.json → env vars → command line
   ├─ configures default console logging (replaced below)
   └─ sets content root / DI container

3. .UseSerilog((ctx,_,log) => log.ReadFrom.Configuration(ctx.Configuration))
        → Serilog becomes the logging provider, configured from the "Serilog" section   [Verified]

4. .ConfigureWebHostDefaults(web => web.UseStartup<Startup>())
        → Kestrel server selected; binds to ASPNETCORE_URLS (port 80 in container)   [Likely]

5. Startup.ConfigureServices(IServiceCollection)   — service registration (see DI order below)

6. Startup.Configure(IApplicationBuilder, IWebHostEnvironment)   — middleware pipeline
        (see MiddlewarePipeline.md)

7. Host.Run() → Kestrel listens; requests flow through the pipeline.
```

### `ConfigureServices` registration order (Platform.API, lines cited) `[Verified]`
1. `AddResponseCompression` (Brotli + Gzip, +`image/svg+xml`) — L33
2. `Configure<BrotliCompressionProviderOptions/GzipCompressionProviderOptions>` (Fastest) — L42/47
3. `AddCors("AllowAllOrigins")` (AnyOrigin/Method/Header, 24 h preflight) — L52
4. `AddSingleton<Instrumentation>()` + `AddTelemetry(...)` (OpenTelemetry) — L65/66
5. `Configure<ForwardedHeadersOptions>` (XFwdFor/Proto; KnownNetworks/Proxies cleared) — L72
6. `AddScoped<APIKeyFilter>()`, `AddScoped<InternalAPIKeyFilter>()` — L79/80
7. `AddControllers()` — L82
8. `AddSingleton<IMemCache, MemCache>()` — L84
9. `AddScoped<IPlatformRepository, PlatformRepository>()` — L85
10. `AddHttpClient<I…, …>()` × 4 (Betting, Operator, Sportsbook, Casino) — L86–89
11. `AddSwaggerGen(...)` — L91
12. `AddDbContext<PlatformContext>(UseSqlServer(...))` — L96

> Registration order is mostly irrelevant to behavior (DI resolves lazily); the **pipeline**
> order in `Configure` is what matters at runtime — see [MiddlewarePipeline.md](MiddlewarePipeline.md).

### Initialization dependencies
- No eager/blocking init in web hosts: DbContexts, repositories, and `Instrumentation` are
  resolved **on first request** (scoped/singleton lazy creation). **[Verified]** — nothing in
  `Configure` opens a DB connection or warms a cache.
- The only process-global side effect before host build is the **HTTP/2 cleartext AppContext
  switch** (for OTLP export). **[Verified]**

### Archetype A′ — `Websocket.API2` (minimal hosting)
Same logical steps, but expressed in **top-level `Program.cs`** (`WebApplicationBuilder` /
`WebApplication`) — no `Startup` class. Adds SignalR (`AddSignalR`/MessagePack, optional Azure
SignalR) and maps the `/platform` hub. **[Verified]** — `Platform.Websocket.API2/Program.cs`.

---

## Archetype B — Generic Host background worker (the services)

Representative: `Platform.Settlement.Service/Program.cs`. 18 projects register
`AddHostedService`. **[Verified]**

### Sequence
```
1. Main()
   └─ AppContext.SetSwitch(Http2UnencryptedSupport, true)   [11 projects do this — for OTLP]
   └─ CreateHostBuilder(args).Build().Run()

2. Host.CreateDefaultBuilder(args)            [config + DI]
3. [.UseSerilog(ReadFrom.Configuration)]      [most services]
4. [.UseWindowsService()]                      [4 services: BetForwarder, CacheSync,
                                                 ESportOperator, AgentReports.Service]
5. .ConfigureServices((ctx, services) => {
      [AddSingleton<Instrumentation>() + AddTelemetry(...)]   [OTel-enabled services]
      AddSingleton<IMessageBroker, MessageBroker>();
      AddSingleton/Scoped<IXxxRepository, XxxRepository>();
      [AddDbContext<...>(UseSqlServer(...))]                  [EF-backed services]
      AddHostedService<Worker>();
   })
6. Host.Run() → host starts → Worker.StartAsync → Worker.ExecuteAsync(stoppingToken)
```

### `Worker.ExecuteAsync` runtime init (the real startup work) `[Verified]`
The meaningful initialization happens **inside the hosted service**, not in `Program`:
1. `_messageBroker.Connect()` / `ConnectWithManualAck()` — opens the RabbitMQ connection,
   declares queue/bindings, sets prefetch (see [Hosting.md](Hosting.md) and
   [SharedLibraries.md](SharedLibraries.md) §2).
2. Hooks the `MessageReceivedEvent` handler that `switch`es on `MessageType`.
3. Some workers **preload state** on startup (e.g. `CashOutStake.Servicev2` preloads active
   bets) — this is the only blocking init in the platform. **[Verified]**
4. High-throughput workers (`CashOut.Service`, `Placement.Servicev2`, `Operator.Servicev2`,
   `Operator.Cashout.Service`, `Client.TopMarket`) spin up **dedicated threads** +
   `BlockingCollection` queues here. **[Verified]** — see [Hosting.md](Hosting.md).

### Archetype B′ — Jobs (timed loops)
`Job.CCF/ForexEUR/Operator.Score/Resettlement` follow Archetype B but the `Worker` body is a
`while(!stoppingToken)` loop with `Task.Delay` between iterations rather than a message handler.
`Job.Resettlement` connects to RabbitMQ to **republish** retried settlements. **[Verified]**

---

## Archetype C — Plain console

`Platform.BetPlacement.Console/Program.cs` — no Generic Host. `Main` builds a static
`HttpClient` and runs nested loops. No DI, no config layering, hardcoded endpoint. **[Verified]**

---

## Startup order across the platform (operational)
There is **no in-code orchestration** of inter-service startup order — each container starts
independently. Ordering is handled externally:
- RabbitMQ, SQL, Mongo, Redis must be reachable (provisioned by sibling `infrastructure`).
- `MessageBroker` uses `NetworkRecoveryInterval = 5s` and `RequestedHeartbeat = 60s`, so a
  worker that starts before RabbitMQ is up will **auto-recover** rather than crash-loop.
  **[Verified]** — `Platform.Shared/Utility/MessageBroker.cs`.
- Web hosts that depend on downstream APIs (`API:*` base URLs) fail **per-request**, not at
  startup, since `HttpClient`s are lazy. **[Verified]**

## Cross-references
- Boot-shape classification & deploy entry points → [StartupProjects.md](StartupProjects.md)
- Service lifetimes & factory registrations → [DependencyInjection.md](DependencyInjection.md)
- Pipeline order → [MiddlewarePipeline.md](MiddlewarePipeline.md)
- Process model, threads, Windows service → [Hosting.md](Hosting.md)
