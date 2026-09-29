# Cross-Repository Dependencies

> **Phase 5 — Cross-Repository Integration Analysis** (supersedes the Phase 1 draft). Analysis-only;
> no files outside this repo were modified. This is the authoritative map of how **platform-final**
> integrates with its siblings in `d:\Gimli\Repositories`.
>
> **Classification** (as required): **[Verified]** in platform-final · **[Verified-sibling]** in
> the sibling repo · **[Historical: Reference]** only in `/Reference` snapshot · **[Unknown]**.
> *No external behavior is inferred* — sibling claims cite sibling files.
>
> Deep dives: [PlatformApi.md](PlatformApi.md), [OddsEngineIntegration.md](OddsEngineIntegration.md),
> [AuthenticationIntegration.md](AuthenticationIntegration.md), [WebSocket.md](WebSocket.md),
> [ExternalSystems.md](ExternalSystems.md), [Contracts.md](Contracts.md).

## Workspace inventory & stack
| Sibling | Stack | Role vs platform-final | Evidence |
|---------|-------|------------------------|----------|
| **platform-final** | .NET 10 + .NET 8 for Mongo services (GM-744) | this repo — core sportsbook backend | — |
| **platform-web** | .NET 7 Blazor Server | **consumer** (player BFF) of the gateway + WebSocket | [Verified-sibling] |
| **odds-engine-2** | .NET 8 (+ netstandard2.1) | **producer** of the live catalogue + odds (writes Mongo, feeds WS) | [Verified-sibling] |
| **bet-genius** | .NET 8/7/3.1 | Genius Sports feed; **GTS bet validation + livestream** provider | [Verified-sibling] |
| **oddsfeed** | .NET Core 3.1 | parallel **MTS/Betradar** odds+bet stack; bidirectional | [Verified-sibling] |
| **Auth** | .NET 8 | JWT + API-key issuer | [Verified-sibling] |
| **backoffice-final** | .NET Core 3.1 | operator admin; **authors curation**, settles/cancels | [Verified-sibling] |
| **argocd** | Helm/GitOps | deploys all of the above to EKS | [Verified-sibling] |
| **infrastructure** | Terraform/Terragrunt | provisions EKS/RDS/DocumentDB/MQ/ECR/SSM | [Verified-sibling] |
| **oddsengine** | .NET (legacy) | **obsolete** — prefer odds-engine-2 | noted only |
| **Reference** | snapshot | pre-AI baseline — never modify | noted only |

## The integration backbone: one central RabbitMQ
A single topic exchange **`e.central`** on **`rabbitmq-main-cluster.bet-genius-apps.svc`** (AMQP/TLS
5671, prefetch 1000) connects every repo. **[Verified-sibling: argocd]** + **[Verified]** (platform
uses `e.central`). Routing-key namespaces partition producers: `r.oe.*` (odds engines), `r.uof.*`
(UOF feed), `r.bg.*` (bet-genius), `r.of.*` (oddsfeed), `r.bet.*`/`r.wallet.*` (platform). Full
registry in [Contracts.md](Contracts.md).

## Per-sibling integration

### platform-web — player BFF (consumer) `[Verified-sibling]` / `[Verified]`
- **REST:** `PlatformRepository` (typed HttpClient) → `Platform.API` (`API:PlatformAPI`, per-domain
  `UrlMappings`); `Platform.Parent.Web` also → `ClientSportsbook.API`. Header set + retry + cache
  per [PublicApiSurface.md](PublicApiSurface.md)/[PlatformApi.md](PlatformApi.md).
- **WebSocket:** SignalR to `websocket-api/platform` (MessagePack). See [WebSocket.md](WebSocket.md).
- **Config:** also reads a `platform-webprops` CDN config service (whether part of platform-final is **[Unknown]**).
- **Auth:** never holds end-user creds beyond the opaque operator token; server-side only.

### platform-webprops (webprops-api) — per-client site config `[Verified]` / `[Verified-sibling]`
- `Platform.API`'s `WebPropsRepository` calls `GET /api/v1/config/{clientId}` (`API:WebProps:Uri`,
  `x-api-key`) on every `CashoutBet` to enforce BO's `Features.FullCashout`/`PartialCashout`
  (BO-106, fails closed).
- `API:WebProps:*` ships **empty** in `appsettings.json` and must be injected per env via argocd
  (`API__WebProps__Uri`, `API__WebProps__Security__ApiKey`) - bo-api does the same in
  `apps/corp-apps/bo-api/{env}.yaml`. Unset => `WebPropsRepository` logs
  "API:WebProps:Uri is not configured" and every cashout is denied. `apps/platform/platform-api{,-ro}/*.yaml`
  set `API__WebProps__Uri: http://platform-webprops-api:3000` (in-cluster, same as platform-web's
  `Config__BaseURL`); `API__WebProps__Security__ApiKey` is not set there. **[Verified-sibling]**
  webprops-api does not require it for this call: `GET /config/{clientId}` carries no `MustApiKey`
  middleware and its handler does no auth check, reading straight from S3
  (`webprops-api/backend/routes/v1/properties/routes.go:16`, `backend/server/apikey.go:9-22`,
  `backend/routes/v1/properties/get_config.go:20-77`). Only `POST /config/{clientId}` - bo-api's
  write path for saving Client Site Config - requires it (`routes.go:18-20`).
- **The key is the client GUID (`Client.ClientId`, uppercased) — not `ClientCode`.** Same key
  backoffice-final's Client Site Config writes (its request field is misleadingly named
  `ClientCode` but carries the GUID) and platform-web reads (`client_id`).

### odds-engine-2 — catalogue + odds producer `[Verified-sibling]`
- **Writes the MongoDB `Sportsbook` collections** platform-final reads (`Fixture`, `Market`,
  `Selection`, `MarketMapping`, `BettingStatus`; `Maintenance` not found written by OE2 —
  **[Unknown]** who writes it). `OE.Sportsbook.Service`/`OE.Job.*`.
- **Publishes** `r.oe.sb.engine.onoddschange|onbetstopfixture|onbetstopglobal`, `r.oe.bet.settlement`,
  `r.oe.adapter.on*.*`, `r.oe.uof.heartbeat` on `e.central`.
- **`OE.Websocket.Service`** consumes `r.oe.sb.engine.*` + `r.bet.betstatus.s88`/`r.bet.cashoutchange.s88`/
  `r.wallet.s88` and **pushes into platform-final's `/platform` hub** (Azure SignalR) — this is the
  feeder for `Websocket.API2`. **Resolves U-19.** `OE.API` is an empty stub (no REST). Detail:
  [OddsEngineIntegration.md](OddsEngineIntegration.md).

### bet-genius — GTS validation + livestream provider `[Verified-sibling]` / `[Verified]`
- platform-final calls BetGenius **HTTP** for auth (`Auth-v1/PROD/login`), bet **assessment**/`SendBet`
  (via `GTSBetProvider`, ChannelType BG) and **livestream tokens** (`BetGeniusLiveStream`) using
  `Custom:BetGenius:*`. **[Verified]**
- bet-genius ingests the Genius feed and publishes `r.bg.*`/`r.bg.receiver.*`; `BG.Engine.Sportsbook`
  consumes `r.bg.receiver.#` + `r.oe.mapping.bg.#`. It references the **`OE.Shared` NuGet package**
  (0.0.2-68) — platform-final does **not**. **[Verified-sibling]**

### oddsfeed — parallel MTS/Betradar stack (bidirectional) `[Verified-sibling]` / `[Verified]`
- oddsfeed (`OF.PlatformClient.API`) **calls platform-final `Platform.API`** for client/bet-config/
  exchange-rate; platform-final's **`Operator.API/OddsFeedController`** receives oddsfeed bet-status
  callbacks (`/bet/status/placement`, `/bet/status/cancellation`); contract
  `OnOddsFeedBetCancellationMessage` exists in `Platform.Shared`. **[Verified]** (controller) /
  **[Verified-sibling]** (oddsfeed callers).
- Publishes `r.of.bet.*`, `r.bet.settlement`, `r.oe.bet.settlement`. Uses MTS auth/maxstake
  (Betradar, bookmaker 32290) — same external system platform-final's `MtsBetProvider` uses.

### Auth — JWT + API-key issuer `[Verified-sibling]`
- `Auth.Api` (net8) issues **HMAC-SHA256 JWTs** (symmetric `JWT:Key`, issuer `bo.auth.api`, rich
  permission/ClientCodes claims) and validates via `POST v1/authentication/access` /
  `v1/authentication/operator`. `Auth.Shared` = **models only** (no validation logic).
- **platform-final does not validate JWTs** (Phase 2) — it relies on **API keys** at the edge and
  forwards the opaque operator token downstream. `Operator.API` holds an `IAuthRepository` HTTP
  client to Auth. **[Verified]** Detail: [AuthenticationIntegration.md](AuthenticationIntegration.md).

### backoffice-final — operator admin (authors curation) `[Verified-sibling]`
- **Calls platform-final InternalController** (`x-api-key` = internal key): `v1/client/topfixtures`,
  `topleagues(/copy)`, `announcements(/post|/delete)`, `parlaybonus`, `config/client`, plus
  `v1/bet/manualsettlement|resendsettlement|cancel|details`.
- **Also writes some Sportsbook SQL tables directly** via its own `SportsbookContext`
  (`ClientSportOrder`/`ClientSegmentOrder`/`ClientLeagueOrder`, market-template groups) — i.e. the
  per-client *ordering* tables are authored **directly in the DB**, bypassing platform-final's API.
  **Resolves Phase-3 U-20.** It then publishes cache-invalidation messages → `CacheSync.Service`.
- Reads platform `Sportsbook` + `Trading` DBs directly; calls `TradingAPI` (odds-engine), `Auth`,
  `RabbitMqApi`.

### argocd — GitOps deployment `[Verified-sibling]`
- Platform namespace apps map 1:1 to platform-final CI images (`platform-platform-api`,
  `platform-sportsbook-api`, …); **`platform-websocket-api` = `Websocket.API2`**, port 3000.
- **Secrets via AWS Parameter Store + External Secrets Operator** (ClusterSecretStore
  `parameterstore`); config as `Section__Key` env vars (e.g. `ConnectionStrings__Platform`,
  `MessageBroker__Host`). **Resolves U-08.** ImageUpdater auto-syncs tags.

### infrastructure — IaC `[Verified-sibling]`
- Provisions EKS, **DocumentDB** (Mongo `sportsbook`), **AWS MQ** (RabbitMQ), ECR repos (per-env
  AWS accounts dev `418541840646` / stg `172065883315` / prd `525375333516`), Cloudflare DNS.
  **MSSQL** is **AWS RDS** (not yet in Terraform — managed separately).

### oddsengine (obsolete) & Reference (snapshot)
- `oddsengine`: legacy; still has argocd apps; **do not reference** — prefer odds-engine-2.
- `Reference`: pre-AI baseline copy; compare-only; never modify. (Neither deep-dived.)

## Direction-of-integration summary
```
        writes Mongo + feeds WS + publishes odds            authors curation + settles
odds-engine-2 ───────────────────────────▶ platform-final ◀─────────────────────── backoffice-final
bet-genius ──HTTP assess/livestream + r.bg.*─▶   (e.central bus)   ◀──HTTP callbacks── oddsfeed (MTS)
Auth ──API keys / operator-token validate──▶     │      ▲
                                                  ▼      │ REST + SignalR
                                            platform-web (player BFF) ──▶ Browser
argocd deploys ▲           infrastructure provisions ▲ (EKS/RDS/DocumentDB/MQ/SSM/ECR)
```

## Resolved open questions (from earlier phases)
- **U-08** secrets injection → AWS SSM Parameter Store + External Secrets (env `Section__Key`).
- **U-09** contract sharing → **duplicated per-repo**, aligned by convention (see [Contracts.md](Contracts.md)).
- **U-19** WS feeder → `OE.Websocket.Service` feeds platform-final's `Websocket.API2` `/platform` hub.
- **U-20** curation authoring → backoffice-final (API + direct DB writes).
See [Unknowns.md](Unknowns.md) for what remains open.
