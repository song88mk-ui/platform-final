# Playbook: Trace an API Call

1. **Identify the route** (`Platform.API` Integration/Internal controller) + verb + filter.
   Catalogue: [docs/knowledge/PlatformApiEndpoints.md](../../docs/knowledge/PlatformApiEndpoints.md).
2. **Filter:** which `[ServiceFilter]` and what it validates (`x-api-key`/client/pass-key/OG).
3. **Repository method:** `PlatformRepository` — request/response DTO + `StatusCode` branches.
4. **Fan-out:** typed `HttpClient` (Betting/Sportsbook/Wallet/Operator/Casino) and/or `RabbitMQ.API`.
5. **Datastore:** SQL (LINQ/proc) or Mongo (read-only projection) or `IMemCache`.
6. **Response envelope:** `BaseResponseDTO<T>`; map the `StatusCode` to consumer behavior
   ([docs/PlatformApi.md](../../docs/PlatformApi.md)).
7. **Output:** numbered trace with file:line; flag `[Likely]/[Unknown]` hops; note cross-repo deps.

Ref: [docs/RequestLifecycle.md](../../docs/RequestLifecycle.md), [docs/SequenceDiagrams.md](../../docs/SequenceDiagrams.md).
