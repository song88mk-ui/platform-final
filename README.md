# platform-final

Core .NET sportsbook backend of the Song88 platform - the "Platform API" consumed by the
sibling `platform-web` Blazor sportsbook frontend. It is a headless, microservice,
message-driven backend: a 40-project solution (`Platform/Platform.sln`) containing REST
APIs, ~18 RabbitMQ background workers, and a SignalR websocket hub. There is no UI in
this repository.

This README is a practical local run/test guide. The deep reference lives in
[`docs/`](docs/README.md) (start at `docs/README.md`) and the agent operating manual is
[`CLAUDE.md`](CLAUDE.md).

## What is in here (main entry points)

| Project | Kind | Role |
|---|---|---|
| `Platform.API` | REST API (gateway) | The single public gateway; fans out over HTTP to the internal APIs and over RabbitMQ. This is the API `platform-web` calls (`API:PlatformAPI`). Swagger UI at `/swagger`. |
| `Platform.Sportsbook.API` | REST API | Sportsbook catalogue reads (MongoDB) + curation config (SQL `Sportsbook` DB) |
| `Platform.Betting.API` | REST API | Bet placement/config (SQL `Betting` DB); talks to MTS/GTS provider APIs |
| `Platform.Wallet.API` | REST API | Money movements (stored-proc only, SQL `Wallet` DB) |
| `Platform.Operator.API` | REST API | Operator-facing API |
| `Platform.RabbitMQ.API` | REST API | HTTP-to-RabbitMQ publish bridge |
| `Platform.Websocket.API2` | SignalR hub | Real-time hub at `/platform`, MessagePack; consumed by `platform-web` (fed by `odds-engine-2`) |
| `Platform.*.Service`, `Platform.Job.*` | Workers | ~18 `BackgroundService` RabbitMQ consumers (settlement, cashout, cache sync, ...) |
| `Platform.Shared` | Class library | Shared kernel: entities, `DbContext`s, message contracts, enums. Platform-wide blast radius - treat as public API. |
| `Platform.Shared.Test`, `Platform.Sportsbook.API.Test` | xUnit tests | See "Running tests" |

Note: some services exist in duplicated variants (`*.Service` vs `*.Servicev2`,
`WebSocket.API` vs `Websocket.API2`). Confirm the live variant before working on one -
the CI matrix in `.github/workflows/ci.yml` lists what is actually built and deployed
(e.g. `Websocket.API2` and the `v2` services are in the matrix; `WebSocket.API` is not).

## Target frameworks / prerequisites

- Almost all projects (37) target `netcoreapp3.1` (EOL). `Platform.Shared` is
  `netstandard2.1`, `Platform.Websocket.API2` is `net7.0`, `Platform.Shared.Test` is
  `net8.0`.
- Docker images are built with the .NET SDK 6.0 image and run on the ASP.NET Core 3.1
  runtime image (see any `Platform/*/Dockerfile`).
- To build and run everything locally you therefore need:
  - A recent .NET SDK (8.0 or later) to build/run `Platform.Websocket.API2` and
    `Platform.Shared.Test`.
  - The .NET Core 3.1 runtime (or SDK) installed to run the `netcoreapp3.1` services and
    `Platform.Sportsbook.API.Test`.
- Docker (optional) if you prefer container builds, plus local SQL Server / RabbitMQ /
  MongoDB containers (see "Dependent services").
- Private NuGet feed: the Dockerfiles register the GitHub Packages feed
  `https://nuget.pkg.github.com/FlyingCockatoo/index.json` (build arg `NUGET_PASSWORD`,
  a GitHub token). At least `Infra.Extensions.Telemetry` (referenced by
  `Platform.Websocket.API2`) comes from a non-nuget.org source, so configure that feed
  locally before a full-solution restore:

  ```
  dotnet nuget add source --username <github-user> --password <github-token> \
    --store-password-in-clear-text \
    --name github "https://nuget.pkg.github.com/FlyingCockatoo/index.json"
  ```

  There is no `nuget.config` committed in the repo, so this is per-machine setup.

## Configuration

Standard ASP.NET Core configuration (`Host.CreateDefaultBuilder`): `appsettings.json`,
then `appsettings.{Environment}.json` (most projects ship an
`appsettings.Development.json`; the launch profiles set
`ASPNETCORE_ENVIRONMENT=Development`), then environment variables. Override any key via
env var with the double-underscore separator, e.g. `ConnectionStrings__Platform=...` or
`Custom__APIKey=...`.

Common sections (per service `appsettings.json`):

- `ConnectionStrings` - SQL Server and MongoDB connection strings. Names seen per
  service: `Platform` (Platform.API, Websocket.API2), `Sportsbook`, `Translation` and
  `MongoDB-Sportsbook` (Sportsbook.API), `Betting` (Betting.API), `Wallet` (Wallet.API).
- `API` - base URLs of the other services this one calls over HTTP, e.g. Platform.API
  has `API:PlatformBettingAPI`, `API:PlatformSportsbookAPI`; several services have
  `API:RabbitMQAPI`. Point these at wherever you run the target service locally.
- `MessageBroker` - RabbitMQ connection (`Host`, `Port`, `Username`, `Password`,
  `VirtualHost`, `Exchange` - the topic exchange is `e.central`).
- `Custom` - per-service feature flags and API keys (`Custom:APIKey`,
  `Custom:InternalAPIKey`, `Custom:WebSocketAPIKey`, provider credentials such as
  `Custom:MTS*` / `Custom:GTS*` in Betting.API, client-code lists in Sportsbook.API).
- `Serilog` / `Logging` - console JSON logging.
- `Telemetry` - OpenTelemetry on/off (`Telemetry:Enabled`, Platform.API).

Secrets: the committed `appsettings.json` files contain dev/staging fallback values
(some point at staging AWS RDS / cloud hosts); production values come from AWS SSM at
deploy time (per `CLAUDE.md`, TD-01). Do not commit new secrets; get current dev/staging
credentials from the team, or point the connection strings at your own local containers.
Key inventory (no values): `docs/knowledge/ConfigurationKeys.md`.

Auth note: there is no framework authentication - access control is API-key action
filters per controller. Requests to protected endpoints need the key configured under
`Custom:*` for that service.

## Running locally

There is no docker-compose; you run the specific services your scenario needs directly
with `dotnet run`. You rarely need all 30+ processes.

A minimal read-path slice for driving `platform-web` locally:

1. Infrastructure: SQL Server + RabbitMQ (+ MongoDB with a populated `sportsbook`
   database if you need the live catalogue - it is written by `odds-engine-2`, this repo
   only reads it).
2. `Platform.Sportsbook.API`
3. `Platform.Betting.API` (for bet placement)
4. `Platform.RabbitMQ.API` (HTTP publish bridge, needed by APIs that emit events)
5. `Platform.API` (the gateway `platform-web` talks to)
6. `Platform.Websocket.API2` (only for real-time odds pushes; fed by odds-engine-2)

Ports: every `Properties/launchSettings.json` "Project" profile binds
`http://localhost:5000` (Websocket.API2 uses `5050`), so profiles collide if you start
several services as-is. However, the committed `API:*` cross-service URLs use each
project's IIS Express port. The path of least resistance is to run each service on the
port its consumers already expect:

| Service | Port expected by committed config |
|---|---|
| Platform.API | 5000 (its own profile; `platform-web` config points here) |
| Platform.Sportsbook.API | 63752 |
| Platform.Betting.API | 58167 |
| Platform.RabbitMQ.API | 55866 |
| Platform.Wallet.API | 4863 |
| Platform.Operator.API | 51366 |
| Platform.Websocket.API2 | 5050 (https 7092) |

Example:

```
cd Platform/Platform.Sportsbook.API
dotnet run --urls http://localhost:63752

cd Platform/Platform.Betting.API
dotnet run --urls http://localhost:58167

cd Platform/Platform.API
dotnet run            # profile binds http://localhost:5000
```

Then browse `http://localhost:5000/swagger` (Platform.API exposes Swagger in all
environments). Alternatively adjust the `API:*` URLs via env vars instead of pinning
ports (e.g. `API__PlatformSportsbookAPI=http://localhost:5001`).

Background workers (`Platform.Settlement.Service`, `Platform.CashOut.Service`, etc.) are
plain `dotnet run` console hosts; they need only their `MessageBroker` and connection
strings configured, and start consuming from `e.central`.

### Running with Docker

Each deployable project has its own `Dockerfile` that copies `Platform.Shared` plus the
project, so the build context is the repository root:

```
docker build -f Platform/Platform.API/Dockerfile \
  --build-arg NUGET_PASSWORD=<github-token> \
  -t platform-api .

docker run -p 5000:80 \
  -e ConnectionStrings__Platform="<your-conn-string>" \
  platform-api
```

Containers listen on port 80. `appsettings.*.json` overrides are deleted from the
published image, so all environment-specific config must come from env vars. This
mirrors CI (`.github/workflows/ci.yml`), which builds these images and, on release,
pushes them to AWS ECR for EKS deployment via the sibling `argocd` repo.

## API endpoints

Endpoint inventory per API service, taken from the controller source (none of the
controllers use a class-level `[Route]` prefix, so the action attribute template is the
full route). Every API also serves Swagger UI at `/swagger` in all environments - use it
for request/response schemas. Reminder: there is no framework auth anywhere; protection,
where present, is API-key action filters reading the `x-api-key` header.

### Platform.API (gateway) - 94 endpoints

Two controllers (`Platform/Platform.API/Controllers/`):

**IntegrationController** (61 endpoints) - the public surface `platform-web` calls.
Class-level `APIKeyFilter`: `x-api-key` must equal `Custom:APIKey` (or
`Custom:WebSocketAPIKey`); OrientalGaming clients are additionally identified via
`x-client-id` / `x-og-*` headers. Routes by area:

- Authentication: `POST v1/authentication/login`, `POST v1/authentication/logout`,
  `GET v1/authentication/og`
- Wallet: `GET v1/wallet/balance`
- Betting: `POST v2/bet/placement`, `POST v1/bet/maxstake`, `GET v1/bet/config`,
  `GET v1/bet/status`, `GET v1/bet/history`, `POST v2/bet/history`,
  `GET v1/bet/history/settled`, `GET v1/bet/history/running`, `POST v1/bet/cashout`
- Sportsbook catalogue (all `GET`): `v1/sportsbook/sports`, `v1/sportsbook/search`,
  `v1/sportsbook/livestream`, `v1/sportsbook/fixturedetails`,
  `v1/sportsbook/selectiondetails`, `v1/sportsbook/activefixtures`,
  `v1/sportsbook/activefixtures/details`, `v1/sportsbook/livefixturecount`,
  `v1/sportsbook/todayfixturecount`, `v2/sportsbook/topfixtures`,
  `v2/sportsbook/topfixtures/fixtureId`, `v1/sportsbook/boostedfixtures`,
  `v1/sportsbook/sports/topleagues`, `v1/sportsbook/sports/allleagues`,
  `v1/sportsbook/sports/topleaguefixtures`,
  `v1/sportsbook/sports/{live|today|upcoming|daily|outright}fixtures`,
  `v1/sportsbook/sports/segments/fixtures`,
  `v1/sportsbook/sports/segments/leagues/{live|today|upcoming|daily|outright}fixtures`,
  `v1/sportsbook/sports/segments/leagues/fixtures`,
  `v1/sportsbook/markettemplategroups`, `v1/sportsbook/selectormarkettemplates`,
  `v1/sportsbook/marketsortorder`, `v1/sportsbook/fixturesortorder`,
  `v1/sportsbook/client/markettemplateplayerprops`, `v1/sportsbook/highlights`,
  `v1/sportsbook/highlights/fixtures`, `v1/sportsbook/betrecommendations`,
  `v1/sportsbook/announcements`, `v1/sportsbook/translations`, `v1/sportsbook/ccf`
- User favorites: `GET v1/sportsbook/userfavorites`,
  `GET v1/sportsbook/userfavorites/fixtures`, `POST v1/sportsbook/userfavorites`,
  `DELETE v1/sportsbook/userfavorites`
- Bet builder: `POST v1/sportsbook/betbuilder/markettemplates`,
  `POST v1/sportsbook/betbuilder/selections/odds`
- Casino / esports: `GET v1/casino`, `GET v1/casino/live`, `GET v1/casino/virtualgames`,
  `GET v1/esport/config`

**InternalController** (33 endpoints) - back-office/internal operations. Class-level
`InternalAPIKeyFilter` (`x-api-key` = `Custom:InternalAPIKey`); hidden from Swagger
(`ApiExplorerSettings(IgnoreApi = true)`). Routes:

- Client admin: `GET v1/client`, `GET v1/clients`, `POST v1/client`,
  `POST v1/client/status`, `POST v1/config/client`, `GET v1/client/sports`,
  `GET v1/client/sportgroups`, `GET v1/client/allleagues`, `GET v2/client/allleagues`,
  `GET v1/client/exchangerate`, `GET v1/client/exchangerates`,
  `GET v1/client/parlaybonus`, `POST v1/client/parlaybonus`
- Curation: `GET v2/client/topfixtures`, `GET v1/client/topfixtures/sportstree`,
  `POST v1/client/topfixtures`, `POST v1/client/topfixtures/list`,
  `POST v1/client/topfixtures/copy`, `GET v1/client/topleagues`,
  `POST v1/client/topleagues`, `POST v1/client/topleagues/list`,
  `POST v1/client/topleagues/copy`
- Bets: `POST v1/bet/cancel`, `POST v1/bet/manualsettlement`, `POST v1/bet/details`,
  `POST v1/bet/resendsettlement`, `POST v1/bet/resendsettlement/details`
- Announcements: `POST v1/client/announcements`, `POST v1/client/announcements/post`,
  `POST v1/client/announcements/delete`
- Cache / recovery: `DELETE v1/cache/client/{clientId}`, `POST v1/cachepreload`,
  `POST v1/translation/recovery`

### Platform.Sportsbook.API - 82 endpoints

Single `SportsbookController`; no auth filter (anonymous - internal service, keep it off
the public network). Almost everything is `POST` (request DTO in the body) even for
reads. Routes by area; see `/swagger` for schemas:

- Catalogue reads: `v1/sportsbook/{livestream|topleagues|allleagues|topfixtures|search|activefixtures|activefixtures/count|announcements|translations|boostedfixtures}`,
  `v1/sportsbook/topfixtures/fixtureId`, `v1/sportsbook/sports/details`,
  `v1/sportsbook/lookup/{sports|segments|leagues}`,
  `v1/sportsbook/{markettemplategroups|selectormarkettemplates|marketsortorder|fixturesortorder}`,
  `v1/sportsbook/{betselectionprobability|betfixture|betfixture/translation|betsettlementscore}`,
  `v2/sportsbook/{allleagues|topfixtures|sports|fixtures|topleaguefixtures|segments/fixtures}`,
  `v2/sportsbook/{live|today}fixturecount`,
  `v2/sportsbook/{live|today|upcoming|daily|outright}fixtures`,
  `v2/sportsbook/sports/segments/leagues/{live|today|upcoming|daily|outright}fixtures`,
  `v2/sportsbook/fixtures/details`, `v2/sportsbook/fixtures/details/limited`,
  `v2/sportsbook/fixtures/markets/selections/details`, `v2/sportsbook/highlights`,
  `v2/sportsbook/highlights/fixtures`, `v2/sportsbook/betrecommendations`
- User favorites: `v1/sportsbook/userfavorites/{get/all|get|insert|remove}`
- Manual settlement: `v1/sportsbook/manualsettlement/{won|lost|halfwon|halflost|void}`
- Bet builder: `v1/sportsbook/betbuilder/markettemplates`,
  `v1/sportsbook/betbuilder/fixtures/totalodds`
- Client curation (SQL `Sportsbook` DB config): `v1/sportsbook/client/topfixtures`,
  `v2/sportsbook/client/topfixtures`, `v1/sportsbook/client/topfixtures/{post|copy}`,
  `v1/sportsbook/client/topleagues`, `v1/sportsbook/client/topleagues/{post|copy}`,
  `v1/sportsbook/client/{sports|sportgroups|markettemplategroups|markettemplates|markettemplatesorder|markettemplateplayerprops}`,
  `v1/sportsbook/client/add/markettemplategroup`,
  `v1/sportsbook/client/markettemplategroup/{post|delete|copy}`,
  `v1/sportsbook/client/announcements`,
  `v1/sportsbook/client/announcements/{post|delete}`,
  `GET v1/sportsbook/client/asian`
- Cache / recovery: `DELETE v1/cache/maintenance`, `POST v1/cachepreload`,
  `POST v1/sportsbook/translation/recovery`

### Platform.Betting.API - 19 endpoints

`BetController` + `ConfigController`; no auth filter (anonymous). All `POST`.

| Method | Route | Purpose |
|---|---|---|
| POST | `v1/bet/placement` | Place a bet |
| POST | `v1/bet/maxstake` | Max stake for a prospective bet |
| POST | `v1/bet/history` | Bet history |
| POST | `v1/bet/history/settled` | Settled bet history |
| POST | `v1/bet/history/running` | Running (open) bet history |
| POST | `v1/bet/config` | Client bet configuration |
| POST | `v1/bet/cancel` | Cancel bets |
| POST | `v1/bet/manualsettlement` | Manually settle a bet |
| POST | `v1/bet/details` | Bet details (manual-settlement view) |
| POST | `v1/bet/selections` | Selection details for bets |
| POST | `v1/bet/resendsettlement` | Re-publish a settlement message |
| POST | `v1/bet/cashout` | Cash out a bet |
| POST | `v1/bet/cashout/state` | Current cashout state/value |
| POST | `v1/bet/status` | Bet status lookup |
| POST | `v1/bet/recommendations` | Bet recommendations |
| POST | `v1/cachepreload` | Warm caches |
| POST | `v1/config/client` | Apply back-office client bet config |
| POST | `v1/config/parlaybonus` | Apply parlay bonus config |
| POST | `v1/config/parlaybonus/list` | List client parlay bonus config |

### Platform.Wallet.API - 4 endpoints

`IntegrationController`; an `APIKeyFilter` class exists and is registered in DI
(`Startup.cs`) but is not applied to any controller or action, so the endpoints are
effectively anonymous.

| Method | Route | Purpose |
|---|---|---|
| POST | `v1/credit` | Credit a user wallet |
| POST | `v1/debit` | Debit a user wallet |
| POST | `v1/userwallettransfer` | Transfer between user wallets |
| GET | `v1/userwallet` | Get user wallet/balance |

### Platform.Operator.API - 39 endpoints

Three controllers; anonymous except where noted (one action uses
`InternalAPIKeyFilter`).

**IntegrationController** (26):

| Method | Route | Purpose |
|---|---|---|
| POST | `v1/authentication` | Authenticate (see controller) |
| POST | `v1/cashout/status` | Cashout result callback |
| POST | `v1/wallet/balance` | Wallet balance |
| POST | `v1/bet/placement` | Bet placement |
| POST | `v1/bet/vs/placement` | Bet placement (RedCat virtual sports) |
| POST | `v1/bet/full/cashout` | Full cashout |
| POST | `v1/bet/partial/cashout` | Partial cashout |
| POST | `v1/bet/settlement` | Bet settlement |
| POST | `v1/bet/vs/settlement` | Bet settlement (RedCat virtual sports) |
| POST | `v1/bet/cancellation` | Bet cancellation |
| POST | `v1/fixture/score` | Fixture score update |
| POST | `v1/user` | Create user (requires `x-api-key` = `Custom:InternalAPIKey`) |
| POST | `v1/negativetest/add` | Add client to negative-test list |
| POST | `v1/negativetest/remove` | Remove client from negative-test list |
| POST | `v1/negativetest/get` | List negative-test clients |
| POST | `v1/testconfig/add` | Add test config (free bet / stake split) |
| POST | `v1/testconfig/remove` | Remove test config |
| POST | `v1/testconfig/get` | List test configs |
| POST | `v1/casino/bet/placement` | Casino bet placement |
| POST | `v1/casino/bet/settlement` | Casino bet settlement |
| POST | `v1/esports/debit` | Esports debit |
| POST | `v1/esports/credit` | Esports credit |
| POST | `/og/api/v2/platform/security/authenticate` | OrientalGaming authenticate |
| GET | `/og/api/v2/platform/player/balance` | OrientalGaming player balance |
| POST | `/og/api/v2/platform/transaction/bulk/debit` | OrientalGaming bulk debit (bet placement) |
| POST | `/og/api/v2/platform/transaction/credit` | OrientalGaming credit (settlement) |

**OperatorController** (11) - same operations under an `operator/` prefix; hidden from
Swagger (`ApiExplorerSettings(IgnoreApi = true)`): `POST operator/v1/authentication`,
`POST operator/v1/authentication/{login|logout}`, `POST operator/v1/bet/{placement|settlement|full/cashout|cancellation}`,
`POST operator/v1/casino/bet/{placement|settlement}`, `POST operator/v1/esports/{debit|credit}`.

**OddsFeedController** (2): `POST bet/status/placement`, `POST bet/status/cancellation` -
odds-feed bet status callbacks.

### Platform.RabbitMQ.API - 1 endpoint

`IntegrationController`; no auth filter (anonymous).

| Method | Route | Purpose |
|---|---|---|
| POST | `v1/broker/publish` | Publish a message to the RabbitMQ exchange (HTTP-to-broker bridge) |

### Platform.Websocket.API2 (SignalR)

Hub mapped at `/platform` (`Program.cs`; transports WebSockets, SSE and long polling;
MessagePack protocol when `Custom:UseMessagePack` is true). No auth on the hub - the
`ClientId` sent with each subscription is validated as a GUID against the `Client` table
in the `Platform` DB (a client with a `ParentClientCode` is mapped to its parent's
stream, BO-93).

Client-invokable hub methods (`Repository/WebSocketRepository.cs`):

- `GetConnectionId()`
- `SubscribeToEndpoint` / `UnsubscribeToEndpoint` (odds groups by channel key)
- `SubscribeToBetslip` / `UnsubscribeToBetslip` (per-fixture)
- `SubscribeToTopMarkets` / `UnsubscribeToTopMarkets` (per-fixture)
- `SubscribeToBetStatus` / `UnsubscribeToBetStatus` (per-user)
- `SubscribeToCashOutChange` / `UnsubscribeToCashOutChange` (per-user)
- `SubscribeToWalletChange` / `UnsubscribeToWalletChange` (per-user)
- `SubscribeToPayableChange` / `UnsubscribeToPayableChange` (per-user)
- `SubscribeToFavorites` / `UnsubscribeToFavorites` (per-user)
- `PublishOddsChangeForMainMarkets`, `PublishOddsChangeForAllMarkets`,
  `PublishBetStopFixture`, `PublishBetStopGlobal`, `PublishBetStatus`,
  `PublishCashOutChange`, `PublishWalletChange`, `PublishPayableChange` - fan-out
  entry points invoked by the upstream feeder (odds-engine-2), not by browsers; note
  they are callable by any connected client since the hub has no auth

Server-to-client events (`IPlatformEvent.cs` - these names are the contract
`platform-web` registers handlers for): `OnOddsChange`, `OnBetStopFixture`,
`OnBetStopGlobal`, `OnBetslipOddsChange`, `OnBetslipBetStopFixture`,
`OnTopMarketsOddsChange`, `OnTopMarketsBetStopFixture`, `OnBetStatus`,
`OnCashOutChange`, `OnWalletChange`, `OnPayableChange`.

Plain REST endpoints on the same host (`Controllers/SportsbookController.cs`):

| Method | Route | Purpose |
|---|---|---|
| GET | `v1/heartbeat` | Liveness check |
| GET | `v1/connections` | List current SignalR connection ids and count |

## Running tests

Two xUnit test projects exist; they target different frameworks and must be run
separately (see the comment in `Platform.Sportsbook.API.Test.csproj`):

```
# net8.0 - pure unit tests over the shared kernel
dotnet test Platform/Platform.Shared.Test

# netcoreapp3.1 - repository tests with EF Core InMemory; needs the 3.1 runtime
dotnet test Platform/Platform.Sportsbook.API.Test
```

No external services are required (Sportsbook tests use
`Microsoft.EntityFrameworkCore.InMemory`). CI does not run tests - it only builds Docker
images - so run them locally before pushing. New services are expected to ship with a
matching xUnit test project (see `CLAUDE.md`).

## Dependent services

| Dependency | Used for | Where configured |
|---|---|---|
| SQL Server | `Platform`, `Sportsbook`, `Translation`, `Betting`, `Wallet` databases | `ConnectionStrings:*` per service. Locally, a SQL Server container on `localhost,1433` works (Betting/Wallet configs already point at localhost). |
| MongoDB | `sportsbook` live catalogue (fixtures/markets/odds). Owned and written by `odds-engine-2`; this repo reads only - never write to it from here. | `ConnectionStrings:MongoDB-Sportsbook` (Sportsbook.API) |
| RabbitMQ | Async spine: topic exchange `e.central`, routing keys `r.bet.*` / `r.wallet.*` / `r.oe.*` | `MessageBroker:*` per service |
| platform-web | Downstream consumer (Blazor frontend) - calls Platform.API and the `/platform` SignalR hub | Sibling repo; its `UrlMappings`/`API` config must point at your local Platform.API / Websocket.API2 |
| odds-engine-2 | Upstream: writes the Mongo catalogue, publishes `r.oe.*` messages, feeds the websocket hub | Sibling repo |
| Sportradar MTS / Genius GTS / QTech / Oddin | External betting/casino providers | `API:*` and `Custom:*` in Betting.API / Platform.API (committed values point at test/UAT endpoints) |
| Azure SignalR (optional) | Websocket.API2 backplane | Websocket.API2 config |

## Database schema and migrations

There are no EF Core migrations in this repository (no `Migrations/` folders). Schema is
managed outside the codebase; hand-run SQL scripts live in `scripts/sql/` and must be
idempotent (guarded `IF NOT EXISTS` / `COL_LENGTH` checks - see
`scripts/sql/add-parentclientcode-client.sql` for the pattern). To get a working local
database you need a schema/backup from the team or staging; the repo does not create it.

## House rules (short version)

- Production is read-only by default; never run queries or deploy without explicit
  instruction.
- `Platform.Shared` changes rebuild every image and can break sibling repos - contracts
  (message models, entities, enums, routing keys, SignalR method names) are duplicated
  across repos by convention.
- Match existing patterns (repository pattern, `BaseResponseDTO`, manual mapping, action
  filter auth). See `docs/ImplementationPatterns.md` and `CLAUDE.md` before coding.

## Keeping this README up to date

This README is part of the codebase. Update it in the same PR whenever anything it documents changes - project structure (projects/services added, renamed, or removed), configuration keys, run commands, ports, endpoints, or external dependencies. An outdated run guide is worse than none.
