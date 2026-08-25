# Solution Structure

> Confidence: **High [Verified]** — derived directly from `Platform/Platform.sln` and every `.csproj`.

## Solution file

- Single solution: [`Platform/Platform.sln`](../Platform/Platform.sln).
- **40 projects.** **[Verified]** (`find Platform -name "*.csproj" | wc -l` = 40).
  Note: `Platform.Cancellation.Service/` is **not** a project — it is an orphan directory
  containing only `bin`/`obj` leftovers (no `.csproj`, no solution entry). **[Verified]**
- **Flat structure: the solution defines no solution folders, no nested-project grouping,
  and no "Solution Items".** **[Verified]** — `Platform.sln` contains no
  `NestedProjects` section and no folder-type `Project(...)` entries. All 40 projects sit at
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
    └── <40 project folders> (plus the bin/obj-only orphan dir Platform.Cancellation.Service)
```

## Target frameworks

| Target | Count | Projects |
|--------|------:|----------|
| `net10.0` | 36 | All APIs, services, jobs, console, tests except the four below |
| `net8.0` | 4 | `Platform.Shared`, `Platform.Sportsbook.API`, `Platform.QTech.API`, `Platform.CashOutStake.Servicev2` |

**[Verified]** via `grep TargetFramework Platform/*/*.csproj`. Unified by the .NET 10
upgrade (GM-744); the three MongoDB-reading services stay on `net8.0` because
MongoDB.Driver 2.11.6's LINQ partial evaluator throws `NotSupportedException` on .NET 10
when rendering filters that use `Contains` over an array (reproduced 2026-08-24), and
`Platform.Shared` must stay consumable by them. Previously a heterogeneous mix of
`netcoreapp3.1` (EOL), `netstandard2.1` (`Platform.Shared`), `net7.0` (`Websocket.API2`)
and `net8.0` (`Shared.Test`) — see [TechnicalDebt.md](TechnicalDebt.md) TD-02 (resolved).

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
| **Test** | 2 | `Platform.Shared.Test`, `Platform.Sportsbook.API.Test` |
| **Client/sample** | 1 | `Platform.WebSocket.Client` |

## Per-service sub-projects

Several services split into companion projects referenced by the host: **[Verified]** (seen
in references) / **[Likely]** (naming):

- `*.Service.Repository` — data access for that service (e.g. `Platform.Settlement.Service.Repository`).
- `*.Service.Telemetry` — OpenTelemetry instrumentation (e.g. `Platform.Provider.Service.Telemetry`).

> These sub-projects are **not** separate solution entries with their own `.csproj` at the
> 40 count in all cases — some are folders/namespaces inside the host project. This is an
> inconsistency tracked in [Unknowns.md](Unknowns.md) (exact split of `.Repository`/`.Telemetry`
> into projects vs folders was not exhaustively traced for all 13 services).

## Build & CI mapping

`.github/workflows/ci.yml` builds a **subset** of 26 projects as Docker images (matrix
`app.path` → `app.image`); `Platform.Operator.Cashout.Service` **is** in the matrix.
**[Verified]** A PR-only `test` job runs `Platform.Shared.Test` and
`Platform.Sportsbook.API.Test` on the .NET SDK 10.0.x. **[Verified]** Projects present in
the solution but **absent from the build matrix** (i.e. not deployed as their own image):
`Platform.AgentReports.API`, `Platform.AgentReports.Service`,
`Platform.ESportOperator.Service`, `Platform.Oddin.API`,
`Platform.QTech.API`, `Platform.RedCat.API`, `Platform.RedCatBetting.API`,
`Platform.RedCatOperator.API`, `Platform.WebSocket.API` (note: `Websocket.API2` builds as
`platform-websocket-api`), `Platform.WebSocket.Client`, `Platform.BetPlacement.Console`,
`Platform.Shared`, `Platform.Shared.Test`, `Platform.Sportsbook.API.Test` (the two test
projects run in the test job instead). **[Verified]** by diffing the CI matrix against
the project list. Why some shipping integrations (QTech, RedCat, Oddin, AgentReports) are not
in CI is **[Unknown]** — see [Unknowns.md](Unknowns.md). (Because CI never builds them, six of
these — Oddin.API, QTech.API, RedCat.API, RedCatBetting.API, RedCatOperator.API,
WebSocket.API — had carried an unnoticed compile break since commit `95add342` (stale
`using Platform.Shared.Telemetry;`, missing `Infra.Extensions.Telemetry` reference); fixed
in GM-744, 2026-08-20.)
