# Dependency Injection

> Phase 2. Confidence **High [Verified]**. The container, registration styles, lifetimes,
> options/factory usage. Extends the DI summary in
> [ImplementationPatterns.md](ImplementationPatterns.md) §1 with lifetime-by-lifetime evidence.

## Container
- **Built-in `Microsoft.Extensions.DependencyInjection` only.** No third-party container
  (no Autofac/Lamar/etc.). **[Verified]** — no such packages in any `.csproj`.
- Registration locations: **web** → `Startup.ConfigureServices`; **workers** →
  `Program.ConfigureServices((ctx,services)=>…)`; **net7.0 Websocket.API2** → inline in
  `Program.cs`. **[Verified]**

## Lifetimes — observed conventions

### Singleton `[Verified]`
Process-wide, thread-safe shared state and infrastructure:
- `IMemCache → MemCache` (in-proc cache over `System.Runtime.Caching`).
- `Instrumentation` (OpenTelemetry `ActivitySource` + `Meter`).
- `IMessageBroker → MessageBroker` (long-lived RabbitMQ connection) — in workers & `RabbitMQ.API`.
- `IBettingRepository → BettingRepository` in **worker** services (e.g. `Settlement.Service`),
  because the hosted `Worker` is itself effectively a singleton and resolves dependencies once.
- Redis cache (`ClientSportsbook.API`), `IWebSocketRepository` (WebSocket hubs).

### Scoped `[Verified]`
Per-request (web) lifetime:
- `AddDbContext<T>` registers the context as **Scoped** by default.
- DB-backed repositories: `AddScoped<IPlatformRepository, PlatformRepository>` etc.
- **ActionFilters** are registered `AddScoped<APIKeyFilter>()` / `AddScoped<InternalAPIKeyFilter>()`
  and applied via `[ServiceFilter(typeof(...))]` so the container injects their dependencies
  (`IConfiguration`, `PlatformContext`, `IMemCache`). **[Verified]**

### Typed HttpClient (Transient handler, managed lifetime) `[Verified]`
- `AddHttpClient<IXxxRepository, XxxRepository>` — the repository is created per-resolution but
  the underlying `HttpMessageHandler` is pooled/rotated by `IHttpClientFactory`.
- Used for **every outbound HTTP integration**: gateway→Betting/Operator/Sportsbook/Casino;
  Sportsbook→BetGenius/BetBuilder/RabbitMQ; Betting→MTS/GTS/Sportsbook/RabbitMQ; etc.
- Base address + default headers are usually set in the **repository constructor** (from
  `API:*`/`Custom:*` config), not at registration. **[Verified]** (Phase 1 + platform-web consumer doc).

### Transient
- No explicit `AddTransient<...>` of note was observed; the platform leans on Singleton/Scoped/
  typed-HttpClient. **[Likely]** (not exhaustively grepped).

## Options pattern — minimal
- The `Options` pattern is used **only for framework options**, never for app config:
  `Configure<ForwardedHeadersOptions>` (most web hosts), `Configure<BrotliCompressionProviderOptions>`
  / `Configure<GzipCompressionProviderOptions>` (`Platform.API`). **[Verified]**
- **There is no `services.Configure<TAppSettings>(Configuration.GetSection(...))` / `IOptions<T>`
  binding for application settings.** App config is read at point-of-use via the
  `IConfiguration["Section:Key"]` indexer. **[Verified]** This is a deliberate, consistent
  (if dated) convention — see [Configuration.md](Configuration.md) and
  [CodingStandards.md](CodingStandards.md).

## Factory registrations
- **Bet/livestream provider factories** are the main factory pattern: multiple `IBetProvider`
  implementations are registered, and `IBetProviderFactory` (registered scoped) selects one by
  `ChannelType`. Same for `ILiveStreamFactory`. **[Verified]** — registered via the
  `AddBettingProviders()` / `AddProviders()` extension methods. See
  [SharedLibraries.md](SharedLibraries.md) §3.
- **Grouping extension methods** (`IServiceCollection` extensions) are the idiom for cohesive
  registration sets: `AddBettingProviders` (Betting.API), `AddProviders` (Sportsbook.API),
  RedCat `ServiceExtension.AddContexts/AddHttpClients/AddRepositories`,
  `AddTelemetry` (from the private `Infra.Extensions.Telemetry` package). **[Verified]**
- `IHttpClientFactory` is used directly in `PlatformRepository2`-style repos (the
  `BaseRepository` pattern) instead of a typed client. **[Verified: consumer]** (platform-web)
  / **[Likely]** the same pattern exists in some platform-final repos.

## Resolution into ActionFilters & controllers `[Verified]`
- Controllers inherit `BaseController`/`IntegrationControllerBase`; the framework injects ctor
  dependencies (repositories, `IConfiguration`, `IMemCache`, `Instrumentation`).
- `[ServiceFilter(typeof(APIKeyFilter))]` resolves the filter (and its deps) from the scoped
  container per request — this is why filters can do DB lookups (`AuthorizationFilter` →
  `PlatformContext`). See [Authorization.md](Authorization.md).

## Anti-patterns / notes (do not replicate) `[Verified]`
- `Settlement.Service` registers `IBettingRepository` as **Singleton** while the repo may use a
  DbContext — singleton-holding-scoped is a known EF lifetime hazard; verify the repo creates
  contexts per-operation rather than holding one. (Tracked as a review item, not confirmed.)
- `BuildServiceProvider()` mid-registration (seen in platform-web's webprops bootstrap) is an
  anti-pattern; not observed in platform-final but worth watching if config sources are added.
