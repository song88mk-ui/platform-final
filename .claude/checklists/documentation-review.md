# Checklist: Documentation Review

- [ ] **Affected docs identified** via the [docs/README.md](../../docs/README.md) index.
- [ ] **Facts are evidence-backed** (file:line) and carry confidence tags
      (`[Verified]/[Likely]/[Unknown]`, `[PW]`, `[Verified-sibling]`).
- [ ] **Descriptive, not aspirational:** documents the current code, not planned/unbuilt behavior.
- [ ] **No secret values** reproduced (key names only).
- [ ] **Unknowns** updated ([docs/Unknowns.md](../../docs/Unknowns.md)) — resolved ones struck through
      with evidence; new ones added with suggested investigation.
- [ ] **Debt/risk** updated ([docs/TechnicalDebt.md](../../docs/TechnicalDebt.md) TD-xx /
      [docs/ArchitectureRisks.md](../../docs/ArchitectureRisks.md) R-xx) with the 4 fields.
- [ ] **Cross-references resolve** and stay consistent (enum values, routes, routing keys).
- [ ] **Subject correct:** platform-final backend; UI facts tagged `[PW]` (platform-web).
