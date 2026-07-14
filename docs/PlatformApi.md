# Platform API — Integration Contract (Provider View)

> Phase 5. platform-final's `Platform.API` gateway **as a contract that siblings depend on**. The
> full route catalogue is in [PublicApiSurface.md](PublicApiSurface.md) (verified in
> `Platform.API/Controllers/*`); this document is about **who consumes it, with which key, and what
> they rely on**. Classification: **[Verified]** platform-final · **[Verified-sibling]** consumer repo.

## Consumers of Platform.API
| Consumer | Surface used | Auth key | Evidence |
|----------|--------------|----------|----------|
| **platform-web** (`Platform.Child.Web`) | `IntegrationController` public endpoints (sportsbook reads, bet, wallet, auth) | `Custom:APIKey` (+ `x-operator-token`, `x-client-id`) | [Verified-sibling] |
| **platform-web** (`Platform.Parent.Web`) | `ClientSportsbook.API` (active fixtures, market subscribe) | `Custom:APIKey` + `x-client-id` | [Verified-sibling] |
| **backoffice-final** | `InternalController` (curation, settlement, cancel) | **internal** `x-api-key` (`Custom:InternalAPIKey`) | [Verified-sibling] |
| **oddsfeed** (`OF.PlatformClient.API`) | `Platform.API` client/bet-config/exchange-rate reads | `x-api-key` + `x-client-id` (+ internal key) | [Verified-sibling] |
| **odds-engine-2 / others** | (none direct — integrate via RabbitMQ/Mongo, not REST) | — | [Verified-sibling] |

## The two contracts: public vs internal
`Platform.API` exposes **two audiences** (see [Authorization.md](Authorization.md)): **[Verified]**
- **`IntegrationController`** — `APIKeyFilter` (`x-api-key` == `Custom:APIKey`/`WebSocketAPIKey`).
  Player-facing reads + bet/wallet/auth. Consumed by platform-web.
- **`InternalController`** — `InternalAPIKeyFilter` (`x-api-key` == `Custom:InternalAPIKey`).
  Client config, curation (top fixtures/leagues, announcements, parlay bonus), bet cancel/manual
  settlement/resend. Consumed by **backoffice-final**.

> A single shared **internal key** opens *all* internal endpoints — backoffice holds it. There is no
> per-operation scoping. **[Verified]** (security note — [TechnicalDebt.md](TechnicalDebt.md)).

## What consumers rely on (the de-facto contract)
These are stable expectations sibling code is built against — changing them is a **breaking change**:
1. **Envelope:** every response is `BaseResponseDTO<T>` `{ StatusCode, Message, Result }`; success/
   maintenance → HTTP 200, everything else → HTTP 400 **with a body**. Consumers branch on the
   business `StatusCode`, not HTTP status. **[Verified]** + **[Verified-sibling]** (platform-web retry).
2. **`StatusCode` enum values** (100–157, 205, 1000/1001) — platform-web maps these to retry /
   maintenance / unauthorized behavior. Renumbering breaks consumers. **[Verified]**
3. **Header contract:** `x-api-key`, `x-operator-token`, `x-client-id`, `x-language-code`,
   `x-country-code`, `x-user-ip`, plus `x-og-*` for OG. **[Verified]** (filters) / **[Verified-sibling]**.
4. **Route + query/body shapes** per [PublicApiSurface.md](PublicApiSurface.md) — e.g.
   `POST v2/bet/placement` (`BetRequestDTO`), `GET v1/sportsbook/sports/livefixtures?sportId=`,
   `v1/wallet/balance`. **[Verified]** both sides.
5. **OddsFeed callback contract:** `Operator.API/OddsFeedController` accepts
   `/bet/status/placement` and `/bet/status/cancellation` from oddsfeed. **[Verified]**.

## Internal fan-out behind the gateway `[Verified]`
`Platform.API` is itself a client: it delegates over typed HttpClients to `Betting.API`,
`Operator.API`, `Sportsbook.API`, and the casino (QTech) API (`Startup.cs`), and publishes to
RabbitMQ via `RabbitMQ.API`. Sportsbook reads ultimately hit **MongoDB `Sportsbook`** (owned by
odds-engine-2). So a consumer's REST call can transitively depend on odds-engine-2's Mongo writes.
See [RequestLifecycle.md](RequestLifecycle.md).

## Versioning & compatibility `[Verified]` / `[Unknown]`
- Versioning is **in the path** (`v1/`, `v2/`) — e.g. `v2/bet/placement`, `v2/bet/history` coexist
  with `v1`. **[Verified]**
- There is **no published OpenAPI contract package** shared with consumers; Swagger is generated and
  exposed at runtime, but consumers hand-write their DTOs (platform-web's `PlatformRepository`).
  Compatibility is therefore **convention-based** — see [Contracts.md](Contracts.md). **[Verified]**
- No formal deprecation policy for old routes was found. **[Unknown]**

## Deployment identity `[Verified-sibling: argocd]`
`Platform.API` ships as image **`platform-platform-api`**, K8s service **`platform-api.platform.svc:3000`**
(container port 80 → service 3000). Internal callers use that DNS; external via
`platform-api.{env}.song88.com` (ALB + ACM TLS).
