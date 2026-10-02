# Authentication

> Phase 2. Confidence **High [Verified]** — grepped the whole solution for auth schemes and read
> every ActionFilter. **The headline finding:** platform-final uses **no ASP.NET Core
> authentication framework**. Authentication is **custom, header/API-key based**, enforced in MVC
> ActionFilters. Read alongside [Authorization.md](Authorization.md).

## What is NOT used `[Verified]`
Grepping all `*.cs`:
- **No `AddAuthentication` / `AddJwtBearer` / `AddCookie` / `AddOpenIdConnect` / `AddIdentity`.**
- **No `UseAuthentication()`** in any pipeline.
- **No `[Authorize]` attributes**, no authorization policies, no auth handlers.
- `JwtParser` exists in `Platform.Shared/Utility/JwtParser.cs` but is **not referenced** anywhere
  in platform-final (dead/unused within this repo). **[Verified]**

So there are no JWT bearer schemes, cookies, OIDC flows, claims principals, or token-refresh
mechanics **inside platform-final**. (The sibling **`Auth`** service issues JWTs/API keys, and
`Operator.API` holds an `IAuthRepository` HTTP client to it — but platform-final does not itself
validate JWTs in a middleware/scheme.) **[Verified] / [Likely]** for the Auth-service relationship.

## What IS used — API-key + token headers in ActionFilters
Every protected controller is decorated `[ServiceFilter(typeof(<Filter>))]`. The filter runs in
`OnActionExecuting` and short-circuits with `BadRequestObjectResult` carrying a business
`StatusCode` when validation fails. Four filter types: **[Verified]**

### 1. `APIKeyFilter` (public APIs) — `Platform.API/ActionFilters/APIKeyFilter.cs`
- Reads `x-api-key`; rejects unless it equals `Custom:APIKey` **or** `Custom:WebSocketAPIKey`
  → else `StatusCode.Missing_Or_Unsupported_APIKey_In_Request_Headers` (101). (L27–38)
- If the key is the **WebSocket** key, it returns early (trusted server-to-server). (L40)
- Otherwise `x-client-id` must be present and not blank → else
  `StatusCode.Missing_Or_Unsupported_ClientId_In_Request_Headers` (102). (L46–55, GM-873)
- Then, for **Oriental Gaming** client-ids (`Custom:OrientalGaming:ClientIds`), it parses
  the `x-og-*` headers (player code, game code, pool id, bet limit, timestamp, signature,
  currency, language, SB-launch) into `BaseController.OGParameters`. (L57–75)
- **Note:** the "operator token" (`x-operator-token`) used by the front-end is **not validated by
  this filter** — it is forwarded downstream to the operator/wallet layer for validation. The
  `StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers` (103) /
  `Operator_Error` (1001) codes are produced **downstream**, not here. **[Verified]** (filter has
  no operator-token check) / **[Likely]** for where 103/1001 originate.

### 2. `InternalAPIKeyFilter` (back-office/internal) — `Platform.API/ActionFilters/InternalAPIKeyFilter.cs`
- Reads `x-api-key`; rejects unless it equals `Custom:InternalAPIKey` → else 101. (L19–29)
- That's the entire check — internal endpoints trust the shared internal key. **[Verified]**

### 3. `AuthorizationFilter` (client sportsbook) — `Platform.ClientSportsbook.API/ActionFilters/AuthorizationFilter.cs`
A heavier, DB-backed check: **[Verified]**
1. `x-client-id` must be a valid `Guid` → else 102.
2. Resolve `ClientCode` from `PlatformContext.Client` (active only), **cached 24 h in `IMemCache`**
   under `cc:{clientId}` → else 102.
3. `x-api-key` must equal `Custom:APIKey` → else 101.
4. `x-language-code` (default `EN`) must be a defined `LanguageCode` enum value → else 106.
5. Populates `IntegrationControllerBase.ClientKeyFilter` { ClientId, ClientCode, IpCountryCode
   (`CF-IPCountry`), LanguageCode, IpAddress (`X-Forwarded-For`) } for the controller.

### 4. `PassKeyFilter` (QTech partner) — `Platform.QTech.API/ActionFilters/PassKeyFilter.cs`
- Validates a partner `Pass-Key` header against config (`Custom:PassKey`). **[Verified]** (Phase 1).

## Identity & client context model `[Verified]`
- There is **no `ClaimsPrincipal`/`HttpContext.User`**. "Identity" is conveyed entirely through
  **request headers** and materialized into plain DTOs on the controller:
  - `x-client-id` → tenant/operator identity (Guid → ClientCode).
  - `x-operator-token` → end-user session token (validated downstream by Operator/Wallet).
  - `x-og-*` → Oriental Gaming player context.
  - `x-language-code`, `x-country-code`/`CF-IPCountry`, `x-user-ip`/`X-Forwarded-For`.
- The header set a caller must send is documented from the consumer side in
  [PublicApiSurface.md](PublicApiSurface.md) (verified against platform-web).

## Server-to-server & partner auth `[Verified]`
- Internal hops use shared API keys (`Custom:APIKey` / `InternalAPIKey` / `WebSocketAPIKey`) in
  `x-api-key`, set on the typed `HttpClient`s.
- Partner inbound callbacks use partner-specific keys: QTech `Pass-Key`, RedCat
  `Custom:SharedKey`, Oddin brand token, OG public/private keys. **[Verified]**

## Token refresh / sessions `[Verified] (negative)`
- No token-refresh, session store, or sliding-expiry logic exists in platform-final. Operator
  tokens are opaque pass-through values; their lifecycle is owned by the **Auth**/Operator layer.
  Tracked in [Unknowns.md](Unknowns.md) (operator-token validation source).

## Security implications (observations) `[Verified]`
- Shared static API keys (committed in `appsettings.json`) are the primary gate → see
  [TechnicalDebt.md](TechnicalDebt.md) TD-01.
- `APIKeyFilter` rejects a missing or blank `x-client-id` with 102 (GM-873, 2026-10-02). Before
  that it called `clientId.ToUpper()` on null, and PRD logged about 960 NullReferenceExceptions a
  day as 500s. It still parses `x-og-*` headers with `int.Parse`/`long.Parse`/`bool.Parse`
  **without null/format guards** → a malformed OG header on an OG client-id can throw, surfacing
  as a 500 (no exception middleware). Robustness/DoS consideration. **[Verified]**
- Auth checks are **per-action filters**; a new controller that forgets the `[ServiceFilter]`
  attribute is **unprotected** (no global fallback policy). **[Verified]** — see
  [Authorization.md](Authorization.md).
