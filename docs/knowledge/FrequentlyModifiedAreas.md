# Frequently Modified Areas

> Phase 9. Where change concentrates — from **git history** (3,580 commits, 2021-02 → 2026-06).
> Use this to anticipate review load, test focus, and merge-conflict risk. All **[Verified]** (git).

## Most-changed projects (commit-touch count)
| Rank | Project | Touches | Why it churns |
|-----:|---------|--------:|---------------|
| 1 | **Platform.Shared** | 1857 | the shared kernel — contracts/entities/enums/utilities; **every change rebuilds all images** |
| 2 | **Platform.API** | 1514 | the public gateway — new endpoints/headers/flows |
| 3 | **Platform.Sportsbook.API** | 1105 | catalogue reads + curation; Mongo query shaping |
| 4 | **Platform.Betting.API** | 942 | bet placement/config/limits/cashout |
| 5 | Platform.Operator.API | 336 | operator/back-office surface |
| 6 | Platform.AgentReports.API | 311 | reporting queries |
| 7 | Platform.Settlement.Service | 280 | settlement grading |
| 8 | Platform.Operator.Servicev2 | 267 | operator-channel provider routing |
| 9 | Platform.ClientPlatform.API | 255 | client reads |
| 10 | Platform.WebSocket.API | 222 | real-time hub |
| … | Client.API (187), Placement.Servicev2 (185), QTech.API (160), Oddin.API (135), CashOut.Service (135), RedCat.API (133) | | |

## Most-changed individual files (the real hotspots)
| Touches | File |
|--------:|------|
| **724** | `Platform.Sportsbook.API/Repository/SportsbookRepository.cs` |
| 423 | `Platform.Betting.API/Repository/BettingRepository.cs` |
| 395 | `Platform.API/Repository/PlatformRepository.cs` |
| 266 | `Platform.API/Controllers/IntegrationController.cs` |
| 181 | `Platform.Operator.Servicev2/Repository/BettingRepository.cs` |
| 180 | `Platform.Settlement.Service/Repository/BettingRepository.cs` |
| 170 | `Platform.API/Repository/IPlatformRepository.cs` |
| 161 | `Platform.Sportsbook.API/Controllers/SportsbookController.cs` |
| 109 | `Platform.Placement.Servicev2/Repository/BettingRepository.cs` |
| 82 | `Platform.API/Controllers/InternalController.cs` |
| 78 | `Platform.Betting.API/Controllers/BetController.cs` |
| 76 | `Platform.AgentReports.API/Repository/ReportsRepository.cs` |
| 69 | `Platform.Client.API/ActionFilters/APIKeyFilter.cs` |
| 65 | `Platform.Shared/Models/TWResponseDTO.cs` |

**Takeaways:**
- The **big repository classes are the churn centres** — `SportsbookRepository` (724) and
  `BettingRepository` (423) dwarf everything else. They concentrate business logic (see
  [AntiPatterns.md](../AntiPatterns.md) AP-10) → biggest review/test/conflict surface.
- `TWResponseDTO.cs` (the Fixture/Market/Selection DTO aggregate) churns with every catalogue shape
  change → a cross-cutting contract file.
- `BettingRepository.cs` exists (separately) in **5+ projects** (Betting.API, Settlement, Placement,
  CashOut, Operator.Servicev2) — the same name, different bodies; changes often ripple across them.

## Change-coupled clusters (what tends to change together) `[Likely]`
| If you change… | Expect to touch… |
|----------------|------------------|
| `Platform.Shared` contract/enum/entity | **all** consumers rebuild; verify message/DTO/Mongo alignment ([Contracts.md](../Contracts.md)) |
| A gateway endpoint (`IntegrationController`) | `IPlatformRepository`+`PlatformRepository` + the downstream API repo + platform-web's consumer DTO |
| Catalogue/odds shape (`SportsbookRepository`, `TWResponseDTO`) | Sportsbook projections + platform-web fixture models + WebSocket message models |
| Bet flow | `Betting.API` + `Placement/Settlement/CashOut` `BettingRepository`s + audit tables |
| A `StatusCode` value | consumers' retry/error mapping (platform-web) |

## Long-lived churn vs. dead code `[Verified]`
- History shows superseded predecessors still in the log: `Platform.Placement.Service` (106),
  `Platform.Operator.Service` (99), `Platform.Job.BetAudit.Tableau` (80) — the **v1→v2 rewrites**
  ([AntiPatterns.md](../AntiPatterns.md) AP-8). Confirm which variant is live before editing.
- The only uncommitted working-tree change throughout this documentation effort is
  `Platform.Sportsbook.API/Repository/SportsbookRepository.cs` — fittingly, the #1 hotspot.

## Practical guidance
- **Highest blast radius:** `Platform.Shared` (rebuilds everything) + the cross-repo contracts
  (no compiler check) — change with extra review/contract verification.
- **Highest local complexity:** `SportsbookRepository.cs` / `BettingRepository.cs` — large, frequently
  edited, business-logic-dense; prime candidates for careful diffing and (absent) tests.
- **Merge-conflict hotspots:** the same files above; coordinate concurrent edits.
- See [TechnicalDebt.md](../TechnicalDebt.md) for the debt concentrated in these areas.
