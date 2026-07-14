# Authentication Integration

> Phase 5. How authentication works **across** platform-final and the **Auth** sibling. Internal
> (within-platform-final) auth is in [Authentication.md](Authentication.md); this doc is the
> **cross-repo** view. Classification: **[Verified]** platform-final · **[Verified-sibling]** Auth/
> backoffice · **[Likely]**/**[Unknown]** flagged.

## The actors
| Actor | Role |
|-------|------|
| **Auth** (`Auth.Api`, net8) | issues + validates **JWTs** and **API keys**; owns users/roles/permissions | 
| **platform-final** | **does not validate JWTs**; gates with **API keys**; forwards opaque **operator token** downstream |
| **platform-web** | sends API key + operator token + client id on every call; never holds user credentials |
| **backoffice-final** | logs operators in via Auth (JWT); calls platform-final with the **internal API key** |

## What Auth issues `[Verified-sibling]`
- **JWT** (`Auth.Api/Repositories/AuthRepository.cs`): **HMAC-SHA256**, symmetric key `JWT:Key`,
  issuer/audience `JWT:Issuer` = **`bo.auth.api`**, ~3-month expiry. Claims include `UserId`,
  `Provider`, `RoleId/RoleName`, granular permission claims (e.g. `sbc.clients.access`), and
  **`ClientCodes`** (the operators a user may access). Validation endpoint `POST v1/authentication/access`.
- **Operator token** (`POST v1/authentication/operator`, `x-operator-token`): a secondary
  session token stored in `AspNetUserTokens`, 1-day cached.
- **API keys:** every Auth endpoint is gated by an `x-api-key` filter (`Security:ApiKey`).
- **`Auth.Shared`** is **models only** (`TokenModel`, `ClaimTypes`, `StatusCode`) — **no shared
  validation library** for consumers.

## How platform-final fits — three distinct mechanisms `[Verified]`
platform-final has **no `AddAuthentication`/JWT bearer** (Phase 2). Auth participation is indirect:

1. **Edge API keys (the real gate).** `APIKeyFilter`/`InternalAPIKeyFilter`/`AuthorizationFilter`/
   `PassKeyFilter` validate `x-api-key` against `Custom:APIKey`/`InternalAPIKey`/`WebSocketAPIKey`.
   These key **values** are provisioned per-environment (ultimately governed by the org's key
   management, surfaced via Parameter Store — see below). The keys are *issued/known* outside
   platform-final; platform-final only **compares** them. **[Verified]**
2. **Operator token pass-through.** platform-final's filters **do not** validate `x-operator-token`;
   it is forwarded to the operator/wallet layer. `Operator.API` registers an `IAuthRepository`
   HTTP client — the validation point against Auth's `v1/authentication/operator`. Failures surface
   as `StatusCode 103` / `Operator_Error 1001`, which platform-web treats as "redirect to login".
   **[Verified]** (client + status codes) / **[Likely]** (that it calls Auth's operator endpoint).
3. **OG (Oriental Gaming) HMAC.** For OG client-ids, `APIKeyFilter` ingests `x-og-*` headers; the
   **`x-og-signature`** is an HMAC-MD5 validated upstream in platform-web's `OGAuth.razor` before
   launch. This is a **separate partner auth** path, not Auth/JWT. **[Verified]** + **[Verified-sibling: platform-web]**.

> `JwtParser` exists in `Platform.Shared/Utility` but is **unused** in platform-final. **[Verified]**

## backoffice-final ↔ Auth ↔ platform-final `[Verified-sibling]`
- backoffice authenticates operators against **Auth** (`v1/login` → JWT; `v1/authentication/access`
  to validate). It configures `AuthenticationAPI:{Uri, Security:{APIKey, Key, Issuer="bo.auth.api"}}`.
- backoffice then calls **platform-final's `InternalController`** with the **internal `x-api-key`** —
  i.e. the *operator's* JWT is consumed by backoffice; the call to platform-final is authorized by a
  **shared service key**, not the operator's identity. So platform-final's internal endpoints trust
  "a holder of the internal key", with no per-operator attribution at that boundary. **[Verified]** +
  **[Verified-sibling]** (security note — [TechnicalDebt.md](TechnicalDebt.md)).

## Key/secret provisioning `[Verified-sibling: argocd/infrastructure]`
- API keys, JWT signing keys, DB/MQ credentials are stored in **AWS Systems Manager Parameter Store**
  and injected as **`Section__Key` env vars** via the **External Secrets Operator**
  (ClusterSecretStore `parameterstore`). So in cluster, `Custom__APIKey`, `Custom__InternalAPIKey`,
  etc. come from SSM — **overriding** the committed `appsettings.json` values (which are
  dev/staging fallbacks). This refines [TechnicalDebt.md](TechnicalDebt.md) TD-01: prod secrets are
  *not* sourced from git, but the committed dev/staging secrets still ship in the image.

## Token-refresh / sessions `[Verified]` (negative) / `[Verified-sibling]`
- platform-final has **no session store, refresh, or sliding expiry** — it is stateless per request.
- Token/session lifecycle (JWT ~3-month, operator token 1-day cache) lives in **Auth**. **[Verified-sibling]**

## Cross-repo auth flow (consolidated)
```
Operator (BO user): backoffice ──v1/login──▶ Auth ──JWT(bo.auth.api, HMAC256, ClientCodes)──▶ backoffice
                    backoffice ──InternalController + x-api-key(internal)──▶ platform-final
Player: platform-web ──x-api-key + x-operator-token + x-client-id──▶ Platform.API
        Platform.API APIKeyFilter: validate x-api-key (Custom:APIKey)        [PF]
        operator-token forwarded ──▶ Operator.API/IAuthRepository ──▶ Auth v1/authentication/operator  [Likely]
OG launch: platform-web OGAuth.razor validates x-og-signature (HMAC-MD5) ──▶ launch ──▶ APIKeyFilter parses x-og-*
```

## Open items
- **U-16/U-25:** exact endpoint platform-final's `Operator.API` calls on Auth for operator-token
  validation (client exists; the specific Auth route is **[Likely]** `v1/authentication/operator`,
  not line-confirmed in platform-final). See [Unknowns.md](Unknowns.md).
