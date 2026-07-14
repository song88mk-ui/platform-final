# Authorization

> Phase 2. Confidence **High [Verified]**. Companion to [Authentication.md](Authentication.md).
> platform-final has **no policy/role/claims-based authorization**; access control is **coarse,
> key-based, and filter-enforced**, with audience separation by controller.

## Model: there is no ASP.NET authorization subsystem `[Verified]`
- No `AddAuthorization`, no `AddPolicy` (auth), no `[Authorize]`, no roles, no claims, no
  `IAuthorizationHandler`/`IAuthorizationRequirement`. **[Verified]** (grep — the `AddPolicy`
  hits are all **CORS** policies named `AllowAllOrigins`).
- `UseAuthorization()` is present in the pipeline but **inert** because no scheme/policy exists —
  see [MiddlewarePipeline.md](MiddlewarePipeline.md). It neither blocks nor decorates requests.

## Authorization = audience + key, enforced by ActionFilters `[Verified]`
Authorization decisions reduce to **"which key opens which door"**, applied per controller:

| Audience | Filter | Key checked | Effect |
|----------|--------|-------------|--------|
| Public players/front-end | `APIKeyFilter` | `Custom:APIKey` or `Custom:WebSocketAPIKey` | access to `IntegrationController` public endpoints |
| Internal / back office | `InternalAPIKeyFilter` | `Custom:InternalAPIKey` | access to `InternalController` (client/config/settlement/cancel) |
| Per-client sportsbook | `AuthorizationFilter` | `Custom:APIKey` + valid active `x-client-id` + language | access to ClientSportsbook endpoints, with tenant context |
| QTech partner | `PassKeyFilter` | `Custom:PassKey` | casino callback endpoints |
| RedCat partner | shared-key check | `Custom:SharedKey` | RedCat callback endpoints |

There are **no finer-grained permissions** (no per-endpoint scopes, no operator-role checks) at
the platform-final layer. **[Verified]**

## Tenant / scoping authorization `[Verified]`
The closest thing to data-scoped authorization is in `AuthorizationFilter`
(`ClientSportsbook.API`): it resolves the caller's `ClientCode` from an **active** `Client` row
(`PlatformContext`, 24 h cached) and rejects unknown/inactive clients (102). Downstream queries
are then scoped by that `ClientCode`/`ClientId`. So **tenant isolation is enforced by data
filtering keyed on the validated client**, not by an authorization policy. **[Verified]**

## Operator-token (end-user) authorization `[Verified] / [Likely]`
- The end-user is represented by the opaque `x-operator-token`. platform-final's filters **do not
  validate it**; it is passed downstream where the operator/wallet layer authorizes the user and
  may return `StatusCode.Missing_Or_Unsupported_OperatorToken_In_Request_Headers` (103) or
  `Operator_Error` (1001). **[Verified]** (filters don't check it) / **[Likely]** (downstream
  enforcement point). Consumers (platform-web) treat 103/1001 as "redirect to login".

## Failure responses `[Verified]`
- All filter rejections return **HTTP 400** (`BadRequestObjectResult`) with a body
  `{ StatusCode, Message }` (business code, underscores→spaces). **Not** HTTP 401/403 — consistent
  with the platform's "business status in body" convention. **[Verified]**

## Key risks / observations `[Verified]`
- **No global fallback policy.** Authorization is opt-in per controller via `[ServiceFilter]`. A
  controller/action that omits the attribute is **completely open**. There is no
  `RequireAuthorization()` / fallback `AuthorizationPolicy` to catch omissions. (Highlighted in
  [Authentication.md](Authentication.md).)
- **Shared static keys** mean authorization is only as strong as key secrecy; keys are committed
  (TD-01) and shared across many callers. Revocation = rotate the key everywhere.
- **Coarse internal key**: any holder of `Custom:InternalAPIKey` can call **all** back-office
  endpoints (cancel bets, manual settlement, client config). No segregation of internal duties.
  **[Verified]**
- `UseAuthorization()` being inert can mislead a reader into assuming framework authorization is
  active. Documented here to prevent that mistake. **[Verified]**

## If extending (to keep consistent with the codebase)
- Add the appropriate `[ServiceFilter(typeof(<Filter>))]` to any new controller; do not assume
  the pipeline protects it.
- Keep returning `BaseResponseDTO`/business `StatusCode` rather than introducing HTTP 401/403,
  unless deliberately changing the contract.
