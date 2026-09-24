---
name: platform-api-investigation
description: Investigate the Platform.API gateway (IntegrationController and InternalController routes, filters, StatusCode envelope, downstream fan-out). Use when tracing a public or back-office endpoint to its handler, downstream call and data store.
---

# Skill: Platform API Investigation

Investigate anything about the `Platform.API` gateway.

## Knowledge
- Two controllers/audiences: `IntegrationController` (`APIKeyFilter`, public, ~62 routes) and
  `InternalController` (`InternalAPIKeyFilter`, back office, ~33 routes).
- Envelope `BaseResponseDTO<T>` + business `StatusCode`; success/maintenance→200 else 400.
- Fans out via typed `HttpClient` to Betting/Sportsbook/Operator/Casino/WebProps (no Wallet.API or
  `RabbitMQ.API` client); publishes directly via its own publish-only `MessageBroker` (`CPublish`).

## Method
1. Find the route ([docs/knowledge/PlatformApiEndpoints.md](../../../docs/knowledge/PlatformApiEndpoints.md),
   line-cited in [docs/PublicApiSurface.md](../../../docs/PublicApiSurface.md)).
2. `/trace-platform-api` to map filter → repo method → downstream → datastore.
3. Decode a `StatusCode` via the quick map; consumer behavior (retry/maintenance/redirect) in
   [docs/PlatformApi.md](../../../docs/PlatformApi.md).
4. Headers/auth: [docs/Authentication.md](../../../docs/Authentication.md) (`x-api-key`/`x-operator-token`/
   `x-client-id`/`x-og-*`).

## Output
Endpoint → handler → downstream map with `StatusCode`s and file:line evidence.
