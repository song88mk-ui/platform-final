# Prompt: Estimate Impact of a Change

"What breaks / rebuilds / needs verification if I change X?"

## Answer using the blast-radius lens (`/impact-analysis`)
1. **Compile-time:** is X in `Platform.Shared`? → **all 37 dependents rebuild** (CI builds all 26 matrix images on every push to main/PR; no affected-files detection).
   Otherwise only the project + its consumers.
2. **Contract?** message field / routing key / Mongo or SQL column / SignalR method / `StatusCode` →
   list the **sibling repos** affected (odds-engine-2, platform-web, oddsfeed, backoffice) — no
   compiler/contract test catches drift (R-2).
3. **Runtime consumers:** HTTP callers (gateway/back-office/oddsfeed), message consumers, WS clients.
4. **Shared SQL schema:** `Sportsbook` DB co-written by backoffice (R-12) — coordinate.
5. **Hotspot coupling:** use [docs/knowledge/FrequentlyModifiedAreas.md](../../docs/knowledge/FrequentlyModifiedAreas.md)
   change-coupled clusters.

## Output
Files/projects rebuilt · runtime consumers · sibling repos to update · risk rating (L×I) · suggested
verification steps. Reference [docs/ArchitectureRisks.md](../../docs/ArchitectureRisks.md).
