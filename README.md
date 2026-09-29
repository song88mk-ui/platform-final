# platform-final

Core .NET sportsbook backend of the Song88 platform - the "Platform API" consumed by the
sibling `platform-web` Blazor sportsbook frontend. It is a headless, microservice,
message-driven backend: a 46-project solution (`Platform/Platform.sln`) containing REST
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
| `Platform.Shared.Test`, `Platform.Sportsbook.API.Test`, `Platform.Operator.API.Test`, `Platform.BetInfo.Service.Test`, `Platform.CacheSync.Service.Test`, `Platform.Operator.Servicev2.Test`, `Platform.Betting.API.Test`, `Platform.API.Test`, `Platform.CashOutStake.Servicev2.Test` | xUnit tests | See "Running tests" |

Note: some services exist in duplicated variants (`*.Service` vs `*.Servicev2`,
`WebSocket.API` vs `Websocket.API2`). Confirm the live variant before working on one -
the CI matrix in `.github/workflows/ci.yml` lists what is actually built and deployed
(e.g. `Websocket.API2` and the `v2` services are in the matrix; `WebSocket.API` is not).

## Target frameworks / prerequisites

- 36 projects target `net10.0`; `Platform.Shared` + the 3 MongoDB-reading services (`Sportsbook.API`, `QTech.API`, `CashOutStake.Servicev2`) target `net8.0` because MongoDB.Driver 2.11.6 fails on .NET 10 (since GM-744; previously a
  netcoreapp3.1/netstandard2.1/net7.0/net8.0 spread).
- Docker images are built with the .NET SDK 10.0 image and run on the ASP.NET Core 10.0 (8.0 for Sportsbook.API/CashOutStake.Servicev2)
  runtime image (see any `Platform/*/Dockerfile`).
- To build and run everything locally you therefore need:
  - The .NET SDK 10.0.x (builds all TFMs; running the net8 Mongo services locally also needs the ASP.NET Core 8.0 runtime).
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
  Platform.API's `API:WebProps:Uri` / `API:WebProps:Security:ApiKey` (webprops-api, read
  by the `CashoutBet` guardrail) ship **empty** and must be set per environment
  (`API__WebProps__Uri=...`); while unset, every cashout is denied (fails closed).
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

Containers listen on port 80 (each Dockerfile sets `ENV ASPNETCORE_URLS=http://+:80`
explicitly, because the `aspnet:8.0+` base images default to port 8080; the pin keeps
the argocd Service `:3000 -> container :80` mapping working). `appsettings.*.json`
overrides are deleted from the
published image, so all environment-specific config must come from env vars. This
mirrors CI (`.github/workflows/ci.yml`), which builds these images and, on release,
pushes them to AWS ECR for EKS deployment via the sibling `argocd` repo.

## Quick start (concrete example)

The fastest path from a clean checkout to something visible in a browser. The smallest
single service worth running is `Platform.Sportsbook.API`: it serves Swagger UI in all
environments and has one endpoint (`GET /v1/sportsbook/client/asian`) that answers
purely from configuration, so you get a real JSON response before any database exists.

Startup dependencies, traced from `Platform/Platform.Sportsbook.API/Startup.cs` and
`appsettings.json` (verified in source, not by execution - see status note below):

- **To start: nothing external.** The two SQL `DbContext`s (`Sportsbook`,
  `Translation`) are registered in `ConfigureServices` but EF Core only opens a
  connection on first query. The MongoDB client is created inside the
  `SportsbookRepository` constructor, which runs on the first controller request, and
  the `MongoClient` constructor performs no network I/O. There is no RabbitMQ broker
  connection in this service - only an HTTP client pointed at `API:RabbitMQAPI`
  (`http://localhost:55866`), used by publish/curation endpoints. Nothing in
  `Program.cs`/`Startup.cs` touches SQL, MongoDB or RabbitMQ at boot, so the process
  starts and Swagger renders with zero infrastructure running.
- **At first data request:** catalogue endpoints need MongoDB
  (`ConnectionStrings:MongoDB-Sportsbook`); curation/translation endpoints need the
  `Sportsbook`/`Translation` SQL databases; a handful of write endpoints need the
  RabbitMQ.API bridge. Missing infrastructure fails per-request, not at startup.

### Steps

1. One-time machine setup - register the private GitHub Packages feed
   (`Platform.Sportsbook.API` references `Infra.Extensions.Telemetry`, which is not on
   nuget.org):

   ```
   dotnet nuget add source --username <github-user> --password <GITHUB_PAT> \
     --store-password-in-clear-text \
     --name github "https://nuget.pkg.github.com/FlyingCockatoo/index.json"
   ```

2. Run the service (needs the .NET SDK 10.0.x - see "Target frameworks /
   prerequisites"):

   ```
   dotnet run --project Platform/Platform.Sportsbook.API --urls http://localhost:63752
   ```

3. Open `http://localhost:63752/swagger`. You should see the Swagger UI titled
   "Sportsbook API V1" listing the ~82 `v1/sportsbook/*` and `v2/sportsbook/*`
   endpoints. OpenTelemetry exporter warnings in the console are expected and harmless
   when no local OTLP collector is running.

4. In Swagger, execute `GET /v1/sportsbook/client/asian` (or just browse to
   `http://localhost:63752/v1/sportsbook/client/asian`). No database is touched; the
   values come from `Custom:AsianClientCodes` in the committed `appsettings.json` plus a
   hardcoded template list. Expected response:

   ```json
   {
     "statusCode": 100,
     "message": "Success",
     "result": {
       "clientCodes": ["S88", "G01"],
       "marketTemplateIds": ["16", "18", "66", "68"]
     }
   }
   ```

   (`statusCode` 100 is `StatusCode.Success`; responses use default System.Text.Json
   camelCase with enums as numbers.)

Status: **should work, unverified.** The machine this guide was written on had neither
the required .NET runtime (then ASP.NET Core 3.1; .NET 10 since GM-744) nor a feed PAT,
so steps 2-4 are traced from
`Startup.cs`, `Properties/launchSettings.json` and the controller/repository source
rather than executed. If a step fails, please fix this section in your PR.

### Option A - real catalogue data via the committed staging defaults

The committed `appsettings.json` points at shared staging infrastructure: MongoDB at
`10.49.100.55:27017` (an RFC1918 private address) and SQL Server at
`stg-song88-mssql-server.cz8wmg0s28pd.ap-south-1.rds.amazonaws.com` (publicly resolvable
in DNS, but to a 100.64.0.0/10 VPC-internal address - checked 2026-08-05). Neither host
speaks HTTP, so no health-endpoint check was possible; both are only reachable from
inside the staging network/VPN.

If you are on that network, the run command above should serve real data with no config
changes ("should work, unverified"). Try it in Swagger with
`POST /v1/sportsbook/lookup/sports` and body:

```json
{ "clientCode": "S88", "languageCode": 1 }
```

(`languageCode` 1 = EN.) A populated staging catalogue returns
`{"statusCode": 100, "message": "Success", "result": {"sports": [...]}}` with one entry
per sport that currently has active fixtures; an unreachable Mongo host instead surfaces
a 500/timeout on this request while `/swagger` and `client/asian` keep working.

### Option B - fully local infrastructure

Passwords below are examples for LOCAL-ONLY containers - never reuse them anywhere
shared. Start the containers:

```
docker run -d --name mssql -p 1433:1433 \
  -e ACCEPT_EULA=Y -e SA_PASSWORD="YourStrong@Passw0rd123" \
  mcr.microsoft.com/mssql/server:2019-latest

docker run -d --name mongo -p 27017:27017 mongo:4.4

docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

(MongoDB 4.4 matches the `MongoDB.Driver` 2.11.6 the project uses; RabbitMQ is only
needed once you also run `Platform.RabbitMQ.API` or the workers - Sportsbook.API itself
never opens a broker connection.)

Then override the connection strings via environment variables and run:

```
ConnectionStrings__Sportsbook="Data Source=localhost,1433;Database=Sportsbook;User Id=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=True"
ConnectionStrings__Translation="Data Source=localhost,1433;Database=Translation;User Id=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=True"
ConnectionStrings__MongoDB-Sportsbook="mongodb://localhost:27017/sportsbook"

dotnet run --project Platform/Platform.Sportsbook.API --urls http://localhost:63752
```

(Set them in your shell before `dotnet run`; note the literal dash in
`ConnectionStrings__MongoDB-Sportsbook` - in PowerShell use
`${env:ConnectionStrings__MongoDB-Sportsbook} = "..."`, in bash use `env` or Docker
`-e`, since `export` rejects dashes in names.)

Honest gaps with fully local infra:

- **SQL schema provisioning is a gap.** There are no EF migrations and, on this branch,
  no provisioning scripts for the `Sportsbook`/`Translation` databases (the only SQL
  files in the repo are `Platform/Platform.Shared/Data/Betting/Scripts/` and
  `Platform/Platform.Shared/DatabaseScripts/`, which assume existing schema). You need a
  schema/backup from the team or staging; until then, curation/translation endpoints
  will fail at first query. See "Database schema and migrations".
- An empty local Mongo does not error - catalogue endpoints simply return empty results
  (`"statusCode": 100` with an empty list), because the `sportsbook` database is
  populated by `odds-engine-2`, not by this repo. Swagger and `client/asian` behave
  exactly as in the zero-infrastructure case.

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

Nine xUnit test projects exist; all target `net10.0` except `Platform.CashOutStake.Servicev2.Test` (`net8.0`, matching the service it covers):

```
# pure unit tests over the shared kernel
dotnet test Platform/Platform.Shared.Test

# pure unit tests over the selection-name composer
dotnet test Platform/Platform.Sportsbook.API.Test

# repository tests against the EF in-memory provider, plus pure tests over the audit reversal rules
dotnet test Platform/Platform.Operator.API.Test

# repository and host-registration tests against the EF in-memory provider
dotnet test Platform/Platform.BetInfo.Service.Test

# translation relay tests against a fake message broker
dotnet test Platform/Platform.CacheSync.Service.Test

# cashout wallet-retry tests against the EF in-memory provider and a loopback operator stub
dotnet test Platform/Platform.Operator.Servicev2.Test

# GM-861 cashout ownership check: a UserId/ClientId mismatch is rejected before anything
# else runs, against the EF in-memory provider
dotnet test Platform/Platform.Betting.API.Test

# BO-106 webprops cashout guardrail: the lookup key and each FullCashout/PartialCashout
# combination, against the EF in-memory provider and a fake WebPropsRepository/HttpClient
dotnet test Platform/Platform.API.Test

# GM-864 preload regression (an excluded bet no longer stops the rest of the list from
# loading) and partial-cashout audit history, against the EF in-memory provider
dotnet test Platform/Platform.CashOutStake.Servicev2.Test
```

No external services are required, including for the repository tests: they run against
`Microsoft.EntityFrameworkCore.InMemory`, not a database. CI runs the whole solution in a
PR-only test job (`dotnet test Platform/Platform.sln`, `.github/workflows/ci.yml`), so a new
test project is picked up as soon as it is added to `Platform.sln`. Still run them locally
before pushing. New services are expected to ship with a matching xUnit test project (see
`CLAUDE.md`).

### GM-865 - live database concurrency test (not part of the standard suite)

`Platform.CashOut.Service.Test` (`GM865ConcurrentPartialCashoutTest.cs`) reproduces two
concurrent partial cashouts on the same bet against a real SQL Server - the row-lock race this
covers cannot be reproduced with EF InMemory. It is deliberately **not** added to
`Platform.sln` and needs a live database, so it never runs as part of `dotnet test
Platform/Platform.sln` or CI:

```
GM865_DEV_BETTING_CONNECTION_STRING="Server=<dev host>,1433;Database=Betting;User Id=sa;Password=<dev sa password>;TrustServerCertificate=true;Encrypt=false;" \
  dotnet test Platform/Platform.CashOut.Service.Test
```

Needs Twingate access to the dev database NLB; the host and `sa` password are the same pair
used for ad-hoc `sqlcmd` access to the dev SQL Server (helmfile `sapassword` value - ask the
team if you do not already have it). Seeds one bet, cleans it up afterward either way.

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
