# Logging & Observability

> Phase 2. Confidence **High [Verified]** — read from `Program.cs`, `appsettings.json` `Serilog`
> sections, `Telemetry/Instrumentation.cs`, and `MessageBroker.cs`. Covers the logging framework,
> structured logging, correlation, enrichment, sinks, and the tracing/metrics stack.

## Framework: Serilog `[Verified]`
- The logging provider is **Serilog**, wired in `Program.cs` via
  `UseSerilog((ctx,_,log) => log.ReadFrom.Configuration(ctx.Configuration))` — configuration is
  **entirely data-driven** from the `Serilog` section of `appsettings.json`. **[Verified]**
- Packages: `Serilog` 4.3.0, `Serilog.AspNetCore` 10.0.0, `Serilog.Settings.Configuration` 10.0.0,
  `Serilog.Sinks.Console` 6.1.1, `Serilog.Formatting.Compact` 3.0.0. **[Verified]**
- A few projects (`Client.API`, `ClientSportsbook.API`) **do not** call `UseSerilog` — they fall
  back to the default `Microsoft.Extensions.Logging` console provider. **[Verified]** (gap, TD-08).

## Configuration (representative `Serilog` section) `[Verified]`
```json
"Serilog": {
  "Using": ["Serilog.Sinks.Console"],
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "System.Net.Http.HttpClient.OtlpMetricExporter": "Warning",
      "System.Net.Http.HttpClient.OtlpTraceExporter": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "WriteTo": [ { "Name": "Console" } ],
  "Enrich": ["FromLogContext"]
}
```

## Sinks `[Verified]`
- **Console is the only sink** for almost all services — correct for containers (stdout scraped by
  the cluster log pipeline). **[Likely]** the console output is JSON via
  `CompactJsonFormatter` (the `Serilog.Formatting.Compact` package is referenced); confirm the
  formatter binding per service.
- **File sink with retention** in `BetStatus.Service` (`LogSettings:DirectoryPath`,
  `RetentionInDays`, AppData fallback). **[Verified]**
- No Seq/Elasticsearch/CloudWatch Serilog sink is configured in-repo; downstream aggregation is
  handled by the platform log pipeline (`infrastructure`/`argocd`). **[Likely]**

## Structured logging `[Verified]`
- Logs use Serilog **message templates with named properties**, e.g.
  `_logger.LogInformation("{event} {@properties}", "PublishOddsChangeForMainMarkets", properties)`
  (`Websocket.API2/WebSocketRepository.cs`). The `{@properties}` destructures a dictionary of
  request context (ipAddress, x-api-key, x-client-id, statusCode, message). **[Verified]**
- Controllers/handlers attach the same context as **OpenTelemetry tags** on the active `Activity`
  in parallel with logging (see Tracing). **[Verified]**

## Log level control `[Verified]`
- Two parallel level configs exist: the `Logging:LogLevel` section (MEL) **and** the `Serilog`
  `MinimumLevel` section. Since Serilog replaces the provider, the **`Serilog` section is
  authoritative** where `UseSerilog` is present; `Logging:LogLevel` governs the MEL fallback in
  the non-Serilog services. **[Verified]**
- The OTLP exporter HttpClient categories are pinned to `Warning` to avoid feedback-loop log noise
  from the telemetry pipeline itself. **[Verified]**

## Correlation & enrichment `[Verified]`
- **Enrichment:** `Enrich:[FromLogContext]` only — no machine/thread/environment enrichers, no
  request-id enricher. **[Verified]**
- **There is no HTTP correlation-id middleware** and no `X-Correlation-Id` propagation. Correlation
  is achieved two other ways:
  1. **OpenTelemetry trace context** — each request/handler opens an `Activity`; the trace/span IDs
     are the correlation backbone, and `MessageBroker` **injects W3C trace context into RabbitMQ
     message headers** so a trace continues across async hops (e.g. `Settlement.Service` extracts
     it on receive). **[Verified]**
  2. **Domain `CorrelationId`** — bet flows carry a `CorrelationId` on `BetRequest`/`BetSelection`
     DTOs and on RabbitMQ `BasicProperties.CorrelationId` (used by `BetForwarder.Service`,
     `AgentReports.Service`). This is a **business** correlation id, distinct from the trace id.
     **[Verified]**
- Net effect: cross-service correlation in logs relies on **trace IDs (OTel)** and **business
  CorrelationId**, not on a logging-framework correlation enricher. **[Verified]**

## Tracing & metrics (OpenTelemetry) `[Verified]`
- Each instrumented project has `Telemetry/Instrumentation.cs` defining an `ActivitySource` and a
  `Meter`, both named `Platform.<Project>`, registered `AddSingleton<Instrumentation>()`. **[Verified]**
- Wired via the private **`Infra.Extensions.Telemetry`** package's `AddTelemetry(new
  TelemetrySettings(ServiceName, ActivitySourceName, MeterName))`. **[Verified]**
- Export is **OTLP over gRPC (HTTP/2 cleartext)** — hence the `Http2UnencryptedSupport` AppContext
  switch in 11 `Main` methods. **[Verified]**
- `Telemetry:Enabled` config gates it (committed default `"false"`; enabled per environment).
  **[Verified]**
- Usage idioms: `using var activity = _activitySource.StartActivity(path)`; `activity?.SetTag(...)`;
  `activity?.SetStatus(ActivityStatusCode.Error, ex.Message)`; counters via the `Meter` for
  throughput (e.g. bet-placement counters in `Placement.Servicev2`). **[Verified]**
- Coverage is **not uniform**: `Client.TopMarket` and the non-Serilog client APIs lack OTel
  instrumentation. **[Verified]** (TD-08).
- `Infra.Extensions.Telemetry` 0.1.1-5 also registers ASP.NET Core, HttpClient, runtime and process
  instrumentation, so every route already has `http.server.request.duration` tagged with
  `http.route`. It has **no MongoDB driver instrumentation**: Mongo time appears in a trace only
  where the code times it itself. **[Verified]** (package assembly references)

### Top leagues latency breakdown (`Sportsbook.API`, 2026-10-26)
`SportsbookRepository.GetTopLeagues` (`POST v1/sportsbook/topleagues`) records, through
`Telemetry/TopLeaguesMetrics.cs` on the service `Meter`, where a cache miss spends its time and how
often the response cache misses, finds nothing, or repeats a miss already in flight:

| Instrument | Type, unit | Tags | Answers |
|---|---|---|---|
| `sportsbook.topleagues.requests` | counter | `cache` (hit/miss), `outcome` (results/empty/error, misses only), `concurrent_miss` (true when another miss for the same response cache key was already in flight on the pod), `sport.id`, `language` (en/other) | hit ratio, empty-result misses, duplicate Mongo reads |
| `sportsbook.topleagues.phase.duration` | histogram, ms | `phase` (`mongo_fetch`, `translations`, `league_config`, `ranking`, `total`), `sport.id`, `language` | time per phase on a miss |
| `sportsbook.topleagues.fixtures` | histogram, count | `sport.id`, `language` | fixtures read per miss |

The same values are set as `topleagues.*` tags on the `GetTopLeagues` span. `league_config`
includes the SQL reads of `ClientLeagueOrder` and `ClientTopLeague` when their caches are cold.
The histograms use the default OTel buckets (0, 5, 10, 25 … 10000 ms), so sub-5 ms CPU phases
need the mean (`_sum / _count`) or the span tags rather than a percentile. Tags are
deliberately low-cardinality: never add the client id or cache key.

Example PromQL. **[Assumption]** The names follow the collector's usual OTLP→Prometheus translation
(dots to underscores, unit suffix); check them in Grafana Explore first.
```
# p95 per phase, football, last 15m
histogram_quantile(0.95, sum by (le, phase) (rate(sportsbook_topleagues_phase_duration_milliseconds_bucket{sport_id="1"}[15m])))
# mean per phase (use for the sub-5 ms phases)
sum by (phase) (rate(sportsbook_topleagues_phase_duration_milliseconds_sum[15m])) / sum by (phase) (rate(sportsbook_topleagues_phase_duration_milliseconds_count[15m]))
# share of misses that duplicated an in-flight miss, and empty-result misses
sum(rate(sportsbook_topleagues_requests_total{cache="miss",concurrent_miss="true"}[15m])) / sum(rate(sportsbook_topleagues_requests_total{cache="miss"}[15m]))
sum by (outcome) (rate(sportsbook_topleagues_requests_total{cache="miss"}[15m]))
# end-to-end, hits included
histogram_quantile(0.95, sum by (le) (rate(http_server_request_duration_seconds_bucket{http_route="v1/sportsbook/topleagues"}[15m])))
```

## What is absent `[Verified]`
- No request-logging middleware (`UseSerilogRequestLogging` is **not** called) — per-request HTTP
  logging relies on controller-level logs + OTel spans, not an access log.
- No audit-log framework at the logging layer (domain audit is in DB tables like `BetAudit`).
- No PII scrubbing/redaction in log config — and controllers may log header values (`x-api-key`)
  as tags/properties, which can leak the API key into logs. **[Verified]** — hardening item.

## Observability summary
| Concern | Mechanism | Where |
|---------|-----------|-------|
| App logs | Serilog → Console (JSON) | `Program.cs` + `Serilog` config |
| Service file logs | Serilog File sink + retention | `BetStatus.Service` |
| Traces | OTel `ActivitySource` → OTLP/gRPC | `Telemetry/Instrumentation.cs` |
| Metrics | OTel `Meter` counters → OTLP/gRPC | per service |
| Cross-async correlation | W3C trace context in RabbitMQ headers | `MessageBroker.cs` |
| Business correlation | `CorrelationId` on DTOs / MQ `BasicProperties` | bet flow |
| Aggregation/storage | external (cluster log pipeline + OTel collector) | `infrastructure`/`argocd` `[Likely]` |
