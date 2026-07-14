# Configuration

> Phase 2. Confidence **High [Verified]** for sources/precedence/keys (read from `Program.cs`,
> `appsettings.json`, Dockerfiles, `.gitignore`). **No secret values are reproduced.** This is the
> runtime-focused companion to Phase 1's [ConfigurationOverview.md](ConfigurationOverview.md)
> (which catalogues sections/keys); here we trace the **hierarchy, sources, and how config is
> consumed at runtime**.

## Configuration sources & precedence `[Verified]`
All hosts use `Host.CreateDefaultBuilder`, which establishes the standard ASP.NET Core chain
(later overrides earlier):

```
1. appsettings.json                    (committed; base values + — problematically — secrets)
2. appsettings.{Environment}.json      (env-specific; appsettings.Development.json is GITIGNORED)
3. User Secrets                        (Development only, by default — NOT used here; see below)
4. Environment variables               (the production override mechanism; __ → : nesting)
5. Command-line args                   (passed to Main; not used in containers)
```

- `ASPNETCORE_ENVIRONMENT` selects step 2 and toggles dev-only behavior
  (`UseDeveloperExceptionPage`). **[Verified]**
- **No additional/custom configuration providers** are registered in platform-final — no
  `AddJsonStream`, no remote config source, no `AddClientConfiguration`. (The CDN/webprops config
  provider documented for the platform exists in the **platform-web** sibling, not here.)
  **[Verified]** (grep) — contrast with platform-web in [CrossRepositoryDependencies.md](CrossRepositoryDependencies.md).

## How config is consumed at runtime `[Verified]`
- Read via the **`IConfiguration["Section:Key"]` string indexer** at point-of-use (constructors,
  filters, repositories). Example: `_configuration["Custom:APIKey"]`,
  `Configuration["ConnectionStrings:Platform"]`, `_configuration["Custom:OrientalGaming:ClientIds"].Split(',')`.
- **No `IOptions<T>` binding for app settings** (only framework options like `ForwardedHeadersOptions`).
  See [DependencyInjection.md](DependencyInjection.md). Implication: typos in keys fail silently
  (return `null`), and there is no startup validation of configuration. **[Verified]**

## Committed `appsettings.json` contents (key names only) `[Verified]`
From `Platform.API/appsettings.json` (representative):
- `Logging:LogLevel` + `Serilog` (Console sink, level overrides, `Enrich:[FromLogContext]`).
- `AllowedHosts: "*"`.
- `ConnectionStrings:Platform` — **contains a live staging RDS DSN with embedded user id +
  password** (value redacted here). This is the clearest instance of TD-01.
- `Custom:` — `APIKey`, `InternalAPIKey`, `WebSocketAPIKey`, `IsOddsString`,
  `Oddin:{BaseUrl,BrandToken}`, `OrientalGaming:{VendorName,PublicAPIKey,PrivateAPIKey,ClientIds}`.
  Several are **plaintext secrets**.
- `API:` — `PlatformBettingAPI`, `PlatformSportsbookAPI`, `CasinoAPI` (downstream base URLs;
  committed values point at `localhost`/preprod and are env-overridden).
- `Telemetry:Enabled` — `"false"` in the committed base file (OTel off by default; enabled per env).

> Full section/key catalogue (MessageBroker, ExternalMessageBroker, etc.) is in
> [ConfigurationOverview.md](ConfigurationOverview.md). **Secrets are never printed in these docs.**

## Environment-specific config `[Verified]`
- `appsettings.Development.json` is **gitignored** (`.gitignore` → `**/appsettings.Development.json`),
  so local dev secrets stay out of git.
- In the Docker build, **all `appsettings.*.json` are deleted** from the published output
  (`rm -f appsettings.*.json`). The base `appsettings.json` is **not** matched by that glob and
  **remains in the image** (carrying its committed secrets). **[Verified]** — this is why the
  committed base secrets matter operationally.
- Runtime overrides are therefore expected via **environment variables** injected by the platform
  (K8s/`argocd`), using the `Section__Key` double-underscore convention (e.g.
  `ConnectionStrings__Platform`, `MessageBroker__Host`, `Custom__APIKey`). **[Likely]** — the
  env-var convention is observed in sibling repos' manifests, not in this repo's committed files.

## Secrets management — current state `[Verified] / [Likely]`
- **No User Secrets**, **no AWS Secrets Manager / Parameter Store SDK usage**, **no Azure Key
  Vault provider** is referenced in platform-final source. **[Verified]** (grep — none present).
- Secrets are either (a) committed in `appsettings.json` (bad — TD-01) or (b) injected as env
  vars at deploy time by `argocd`/`infrastructure`. **[Likely]** for (b).
- Remediation direction (not a Phase-2 action): move to a secrets store + purge from history +
  rotate. See [TechnicalDebt.md](TechnicalDebt.md) TD-01.

## Feature flags `[Verified]`
There is **no feature-flag framework** (no `Microsoft.FeatureManagement`, no LaunchDarkly).
"Feature flags" are plain boolean/string `Custom:*` config keys read via the indexer:
- `Custom:IsProviderEnabled` — gate bet providers (MTS/GTS).
- `Custom:IsMTSEnabled` — gate the Sportradar MTS SDK in `CashOut.Service` / providers.
- `Custom:IsProduction` — `WebSocket.API` local vs Azure SignalR.
- `Custom:UseMessagePack` — `Websocket.API2` protocol toggle.
- `Custom:TestBetClientCodes` — test-mode client allow-list.
- `Telemetry:Enabled` — OTel on/off.
- `Custom:Fibonnaci` (sic) — `Job.Resettlement` backoff schedule.

These are read at startup or per-operation; **toggling requires a config change + restart** (no
hot reload is wired, though `appsettings.json` reload-on-change is on by `CreateDefaultBuilder`
default — but nothing subscribes to `IOptionsMonitor`/change tokens). **[Verified]**

## Configuration hierarchy diagram
```
cmd args  ▸  env vars (Section__Key)  ▸  appsettings.{Env}.json  ▸  appsettings.json
  (unused)     (prod override path)        (Dev gitignored)          (committed; has secrets)
                         │
                         ▼
                IConfiguration  ──string indexer──▶  ctors / filters / repositories
                  (no IOptions<T> binding for app settings)
```
