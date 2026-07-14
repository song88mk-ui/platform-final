# Template: New API Endpoint (Platform.API gateway)

Follow the existing controller/repository shape. UI work is platform-web — this is the backend contract.

## 1. Controller action (`Platform.API/Controllers/IntegrationController.cs` or `InternalController.cs`)
- Class already has `[ApiController, EnableCors("AllowAllOrigins"), ServiceFilter(typeof(<Filter>))]`
  (`APIKeyFilter` for public, `InternalAPIKeyFilter` for back office). **Don't add an unprotected action.**
- `[HttpGet/Post("vN/area/...")]` — lowercase, path-versioned, consistent with neighbors.
- Open a telemetry `Activity`; call the repo; `switch (result.StatusCode)` → `Ok(result)` for
  `Success`/`Maintenance`, else `BadRequest(result)`. Don't return `ex.StackTrace`.

## 2. Repository method (`PlatformRepository` + `IPlatformRepository`)
```csharp
Task<BaseResponseDTO<TResponse>> DoThing(TRequest request, CancellationToken ct = default);
```
- Map manually with `.Select` (no AutoMapper). Set the business `StatusCode`.
- Downstream: typed `HttpClient` (Betting/Sportsbook/Wallet/Operator/Casino) or `IRabbitMQRepository`.

## 3. DTOs (`Platform.Shared/Models`)
`<Thing>RequestDTO` / `<Thing>ResponseDTO`; wrap in `BaseResponseDTO<T>`. If a `StatusCode` is new,
remember consumers map it (R-2) — coordinate with platform-web.

## 4. Checklist
`/impact-analysis` · `[ServiceFilter]` present · parameterized SQL · no secrets · 3.1-compatible ·
`/update-docs` (`PublicApiSurface.md` + `knowledge/PlatformApiEndpoints.md`).
