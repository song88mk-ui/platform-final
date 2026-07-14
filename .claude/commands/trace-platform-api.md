# /trace-platform-api

Map a `Platform.API` gateway endpoint to its downstream fan-out and contract.

## Steps
1. **Route → handler:** locate in `IntegrationController.cs`/`InternalController.cs` (filter, verb,
   path). Catalogue: [docs/knowledge/PlatformApiEndpoints.md](../../docs/knowledge/PlatformApiEndpoints.md),
   line-cited in [docs/PublicApiSurface.md](../../docs/PublicApiSurface.md).
2. **Repository method:** `PlatformRepository.cs` — note request/response DTO + `StatusCode` branches.
3. **Downstream:** which typed `HttpClient` (Betting/Sportsbook/Wallet/Operator/Casino) and/or
   `IRabbitMQRepository` publish. Base URLs from `API:*` config.
4. **Consumer contract:** what platform-web expects (headers, retry on `StatusCode`, 30s cache) —
   [docs/PlatformApi.md](../../docs/PlatformApi.md).
5. **Auth:** filter + key (`Custom:APIKey` vs `InternalAPIKey`); OG `x-og-*` for OG clients.

## Output
Endpoint → filter → repo method → downstream call(s) → datastore, with the `StatusCode`s it can return.
