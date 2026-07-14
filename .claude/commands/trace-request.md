# /trace-request

Trace an inbound HTTP request end-to-end through platform-final (and across repo boundaries).

## When to use
Understanding how a gateway call is handled: which controller, filter, repository, downstream
service, and datastore it touches.

## Steps
1. **Entry:** find the route in `Platform.API/Controllers/IntegrationController.cs` (public) or
   `InternalController.cs` (back office). Catalogue: [docs/knowledge/PlatformApiEndpoints.md](../../docs/knowledge/PlatformApiEndpoints.md).
2. **Filter:** identify the `[ServiceFilter(typeof(<Filter>))]` (`APIKeyFilter`/`InternalAPIKeyFilter`/
   `AuthorizationFilter`/`PassKeyFilter`) and what it validates.
3. **Repository:** the controller calls `IPlatformRepository`/`PlatformRepository`; trace the method.
4. **Fan-out:** note typed `HttpClient` calls to `Betting.API`/`Sportsbook.API`/`Wallet.API`/QTech and
   any `IMessageBroker`/`IRabbitMQRepository` publishes.
5. **Datastore:** SQL (`AsNoTracking`/proc) or Mongo (`.Find().Project<T>()`, read-only) or cache
   (`IMemCache.GetCache`).
6. **Response:** `BaseResponseDTO<T>` + business `StatusCode`; success/maintenance → 200 else 400.

## Reference
[docs/RequestLifecycle.md](../../docs/RequestLifecycle.md) §1–2 · [docs/PlatformApi.md](../../docs/PlatformApi.md)
· [docs/MiddlewarePipeline.md](../../docs/MiddlewarePipeline.md). Output a step list with file:line cites;
mark unverified hops Unknown.
