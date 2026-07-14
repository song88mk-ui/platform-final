# /update-docs

Keep the `docs/` knowledge base accurate after a change.

## Steps
1. Identify which doc(s) the change affects (the [docs/README.md](../../docs/README.md) index maps
   topics → files). Common: a new endpoint → `PublicApiSurface.md` + `knowledge/PlatformApiEndpoints.md`;
   a new message → `Contracts.md` + `knowledge/WebSocketMessages.md`; a schema change → `Database.md`/
   `MongoDb.md`; a new pattern → `ImplementationPatterns.md`.
2. Preserve the **confidence tags** (`[Verified]/[Likely]/[Unknown]`, `[PW]`, `[Verified-sibling]`) and
   cite file:line evidence. Don't downgrade verified facts to prose.
3. If you resolved an Unknown, strike it through in `Unknowns.md` with the resolving evidence; if you
   found new debt/risk, add it to `TechnicalDebt.md` (TD-xx) / `ArchitectureRisks.md` (R-xx) with the
   4 fields (Evidence·Files·Confidence·Suggested investigation).
4. Update [docs/knowledge/FrequentlyModifiedAreas.md](../../docs/knowledge/FrequentlyModifiedAreas.md)
   if hotspots shifted materially.

## Rule
Docs are descriptive of the **current** code — never document aspirational/unbuilt behavior as fact.
