# Solution Structure

> Confidence: **High [Verified]** — derived directly from `Platform/Platform.sln` and every `.csproj`.

## Solution file

- Single solution: [`Platform/Platform.sln`](../Platform/Platform.sln).
- **39 projects.** **[Verified]** (`find Platform -name "*.csproj" | wc -l` = 39).
- **Flat structure: the solution defines no solution folders, no nested-project grouping,
  and no "Solution Items".** **[Verified]** — `Platform.sln` contains no
  `NestedProjects` section and no folder-type `Project(...)` entries. All 39 projects sit at
  the solution root. Logical grouping exists only by project-name prefix convention
  (`Platform.*`), not by IDE folders.

## Repository root layout

```
platform-final/
├── .github/workflows/   ci.yml (build+push per app), clean.yml (branch cleanup)
├── .dockerignore        excludes bin/obj/.config/.vscode/Dockerfile
├── .gitignore           excludes bin/obj/.vs and **/appsettings.Development.json
├── .pre-commit-config.yaml  trailing-whitespace, EOF, yaml, large-files hooks
├── .version             { "version": "3.35.0" }
├── README.md            stub ("# platform-final")
├── docs/                ← this knowledge base (Phase 1 output)
└── Platform/
    ├── Platform.sln
    └── <39 project folders>
```

## Target frameworks (the real heterogeneity)

| Target | Count | Projects |
|--------|------:|----------|
| `netcoreapp3.1` | 36 | All APIs, services, jobs, console (except those below) |
| `netstandard2.1` | 1 | **`Platform.Shared`** (the shared kernel — must stay netstandard to be referenced by all) |
| `net7.0` | 1 | **`Platform.Websocket.API2`** (newer SignalR rewrite) |
| `net8.0` | 1 | **`Platform.Shared.Test`** (test project) |

**[Verified]** via `grep TargetFramework Platform/*/*.csproj`. This mix is a notable fact:
the platform is predominantly on the **end-of-life** `netcoreapp3.1`, with two projects
already moved forward. See [TechnicalDebt.md](TechnicalDebt.md).

## Project SDK types

| SDK | Meaning | Examples |
|-----|---------|----------|
| `Microsoft.NET.Sdk.Web` | ASP.NET Core web apps (APIs, WebSocket hubs) | `Platform.Sportsbook.API`, `Platform.API` |
| `Microsoft.NET.Sdk.Worker` | Generic Host background workers | `Platform.Client.TopMarket` (and most `*.Service`) |
| `Microsoft.NET.Sdk` | Library / console | `Platform.Shared`, `Platform.BetPlacement.Console` |

**[Verified]** for the cited examples; **[Likely]** that the remaining `*.Service` projects
use `Sdk.Worker` (consistent with their `Program.cs` using `Host.CreateDefaultBuilder`).

## Project classification (by role)

See [RepositoryMap.md](RepositoryMap.md) for the full annotated list and
[ProjectOverview.md](ProjectOverview.md) for per-project detail. Summary counts:

| Role | Count | Notes |
|------|------:|-------|
| Public/edge & integration **APIs** | ~16 | `*.API` projects |
| Background **services** (RabbitMQ workers) | ~13 | `*.Service`, `*.Servicev2` |
| **Jobs** (timed/loop workers) | 4 | `Job.CCF`, `Job.ForexEUR`, `Job.Operator.Score`, `Job.Resettlement` |
| **Console** (utility/load test) | 1 | `BetPlacement.Console` |
| **Shared library** | 1 | `Platform.Shared` |
| **Test** | 1 | `Platform.Shared.Test` |
| **Client/sample** | 1 | `Platform.WebSocket.Client` |

## Per-service sub-projects

Several services split into companion projects referenced by the host: **[Verified]** (seen
in references) / **[Likely]** (naming):

- `*.Service.Repository` — data access for that service (e.g. `Platform.Settlement.Service.Repository`).
- `*.Service.Telemetry` — OpenTelemetry instrumentation (e.g. `Platform.Provider.Service.Telemetry`).

> These sub-projects are **not** separate solution entries with their own `.csproj` at the
> 39 count in all cases — some are folders/namespaces inside the host project. This is an
> inconsistency tracked in [Unknowns.md](Unknowns.md) (exact split of `.Repository`/`.Telemetry`
> into projects vs folders was not exhaustively traced for all 13 services).

## Build & CI mapping

`.github/workflows/ci.yml` builds a **subset** of 26 projects as Docker images (matrix
`app.path` → `app.image`). **[Verified]** Projects present in the solution but **absent from
the CI matrix** (i.e. not deployed as their own image) include:
`Platform.AgentReports.API`, `Platform.AgentReports.Service`, `Platform.Cancellation.Service`,
`Platform.ESportOperator.Service`, `Platform.Oddin.API`, `Platform.Operator.Cashout.Service`,
`Platform.QTech.API`, `Platform.RedCat.API`, `Platform.RedCatBetting.API`,
`Platform.RedCatOperator.API`, `Platform.WebSocket.API` (note: `Websocket.API2` builds as
`platform-websocket-api`), `Platform.WebSocket.Client`, `Platform.BetPlacement.Console`,
`Platform.Shared`, `Platform.Shared.Test`. **[Verified]** by diffing the CI matrix against
the project list. Why some shipping integrations (QTech, RedCat, Oddin, AgentReports) are not
in CI is **[Unknown]** — see [Unknowns.md](Unknowns.md).
