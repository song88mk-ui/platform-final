# Middleware Pipeline

> Phase 2. Confidence **High [Verified]** — read from `Startup.Configure` across all web hosts.
> This is the **runtime HTTP request pipeline**; order here is behavioral and significant.

## Canonical pipeline (Platform.API, `Startup.Configure`, L100–127) `[Verified]`
```
Request
  │
  ├─ if (env.IsDevelopment()) UseDeveloperExceptionPage()   ← DEV ONLY
  ├─ UseResponseCompression()        (Platform.API + the 2 WebSocket apps only)
  ├─ UseForwardedHeaders()           (rewrite scheme/remote IP from X-Forwarded-*)
  ├─ UseSwagger() + UseSwaggerUI()   (OpenAPI JSON + UI)
  ├─ UseRouting()                    (endpoint selection)
  ├─ UseCors("AllowAllOrigins")      (AllowAny origin/method/header)
  ├─ UseAuthorization()              (NO-OP — see below)
  └─ UseEndpoints(MapControllers())  (controller actions; ActionFilters run here)
Response
```

Most other APIs use the **same order minus response compression**:
`[dev exception page] → UseForwardedHeaders → Swagger → UseRouting → UseCors → UseAuthorization
→ UseEndpoints(MapControllers)`. **[Verified]** (`Sportsbook.API`, `Betting.API`, `Operator.API`,
`Wallet.API`, `ClientSportsbook.API`, `RedCat*`, `QTech.API`, `Oddin.API`, `RabbitMQ.API`,
`AgentReports.API`, …).

`Websocket.API2` (net7.0 minimal hosting) follows the same order and additionally
`MapHub<…>("/platform")`. **[Verified]**

## Component-by-component

### Exception handling — minimal `[Verified]`
- The **only** exception middleware is `UseDeveloperExceptionPage()`, gated on
  `env.IsDevelopment()`. **There is no `UseExceptionHandler`, no global exception middleware, and
  no `IExceptionFilter`** registered anywhere. **[Verified]** (grep across all `*.cs`).
- Consequence in **production** (non-Development): an unhandled exception that escapes a
  controller returns Kestrel's default **HTTP 500** with an empty body. In practice controllers
  wrap handlers in `try/catch` and return `BaseResponseDTO` with `StatusCode.Error` (HTTP 400),
  sometimes including `ex.Message`/`ex.StackTrace` — see [TechnicalDebt.md](TechnicalDebt.md) TD-04.
- `ASPNETCORE_ENVIRONMENT` therefore materially changes error verbosity. **[Verified]**

### Response compression `[Verified]`
- Only `Platform.API`, `WebSocket.API`, `Websocket.API2`. Brotli + Gzip at
  `CompressionLevel.Fastest`, MIME types = defaults + `image/svg+xml`.
- Placed **first** (after the dev exception page) so all downstream output is compressed.

### Forwarded headers `[Verified]`
- `UseForwardedHeaders()` with `XForwardedFor | XForwardedProto`; `KnownNetworks`/`KnownProxies`
  **cleared** → trusts forwarded headers from **any** upstream. Acceptable behind a controlled
  ingress, risky if directly exposed. Needed so `Request.Scheme`/client IP reflect the real
  client behind Cloudflare/Nginx.

### Swagger `[Verified]`
- `UseSwagger()` + `UseSwaggerUI()` are enabled **unconditionally** (not dev-gated) on the APIs.
  So the OpenAPI document and UI are reachable in all environments. Note as an
  exposure/hardening consideration. **[Verified]**

### Routing & CORS `[Verified]`
- `UseRouting()` → `UseCors("AllowAllOrigins")`. CORS is the broad allow-all policy
  (`AllowAnyOrigin/Method/Header`, `Platform.API` adds 24 h preflight cache). Correct relative
  order (CORS after routing, before endpoints).

### Authorization — present but inert `[Verified]`
- `UseAuthorization()` is in every web pipeline (16 occurrences), **but no authentication scheme
  or authorization policy is registered** (`AddAuthentication`/`AddAuthorization`/`AddPolicy`/
  `[Authorize]` are absent — the `AddPolicy` calls are **CORS** policies). So `UseAuthorization()`
  is effectively a **no-op pass-through**. **[Verified]** Real access control happens in
  **ActionFilters** during `MapControllers` execution — see [Authentication.md](Authentication.md)
  and [Authorization.md](Authorization.md).
- `UseAuthentication()` is **not** called anywhere. **[Verified]**

### Endpoints & ActionFilters `[Verified]`
- `UseEndpoints(MapControllers())` is the terminal stage. The real per-request gatekeeping —
  `APIKeyFilter`, `InternalAPIKeyFilter`, `AuthorizationFilter`, `PassKeyFilter` — runs as **MVC
  action filters** (`OnActionExecuting`) inside this stage, not as middleware. They short-circuit
  with `BadRequestObjectResult` carrying a business `StatusCode`.

## What is NOT in the pipeline `[Verified]`
- No `UseHttpsRedirection` (TLS terminated upstream).
- No `UseStaticFiles` (pure APIs; `AddControllers`, not `AddControllersWithViews`).
- No `UseRequestLocalization` / localization middleware (no `AddLocalization` anywhere).
- No `UseRateLimiter` (not available on 3.1) / no custom throttling middleware.
- No custom `UseMiddleware<>` or inline `app.Use(...)` — the pipeline is 100% built-in components.
- No request/correlation-id middleware — correlation is carried by **OpenTelemetry**
  `Activity`/trace context and by RabbitMQ `CorrelationId`, not by an HTTP middleware. See
  [Logging.md](Logging.md).

## Ordering risks (observations) `[Verified]`
- `UseResponseCompression` before `UseStaticFiles` is moot (no static files).
- `UseForwardedHeaders` is correctly **before** routing/CORS so downstream sees real scheme/IP.
- Because there is no exception-handler middleware, **all** error shaping depends on
  per-controller `try/catch` discipline; an unguarded action leaks a bare 500. (TD-04)
