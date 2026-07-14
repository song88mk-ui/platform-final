# Checklist: Regression Review

- [ ] **Compared to `/Reference`** for the affected method(s) — behavior, not formatting.
- [ ] **Git history** reviewed for the file (esp. hotspots `SportsbookRepository.cs`,
      `BettingRepository.cs`); suspect commit identified.
- [ ] **Status codes / branching** unchanged unless intended (`StatusCode` is a consumer contract).
- [ ] **SQL/Mongo queries** unchanged unless intended (projections, filters, read preference).
- [ ] **Message/DTO/SignalR shapes** unchanged, or sibling repos verified (R-2).
- [ ] **Correct variant** compared (live `*.Servicev2` / `Websocket.API2`, not the superseded one).
- [ ] **Confidence stated** per finding; suspected regressions flagged for verification (not auto-fixed).
