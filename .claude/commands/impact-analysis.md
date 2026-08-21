# /impact-analysis

Assess the blast radius of a proposed change **before** making it.

## Always check
1. **Is it in `Platform.Shared`?** If yes → **every image rebuilds** (CI always builds all 26 matrix images) and behavior
   may shift platform-wide. Treat message models / entities / enums as public API. (R-1)
2. **Does it touch a cross-repo contract?** Message field, routing key, Mongo/SQL column, or SignalR
   method name → **up-to-4-repo silent break** (odds-engine-2, platform-web, oddsfeed, backoffice).
   Diff against the sibling definitions; there is no compiler/contract test. (R-2,
   [docs/Contracts.md](../../docs/Contracts.md))
3. **Is it a `StatusCode` value?** Consumers map these to retry/maintenance/redirect behavior.
4. **Shared SQL schema?** `Sportsbook` DB is also written by `backoffice-final` (R-12) — coordinate.
5. **A repository file?** Use [docs/knowledge/FrequentlyModifiedAreas.md](../../docs/knowledge/FrequentlyModifiedAreas.md)
   "change-coupled clusters" — e.g. a gateway endpoint usually drags `IPlatformRepository` + a
   downstream repo + platform-web's DTO.

## Steps
- Identify the change site → list compile-time dependents (everything if Shared) → list runtime
  consumers (HTTP callers, message consumers, WebSocket clients) → list sibling repos affected →
  rate likelihood×impact. Cross-ref [docs/ArchitectureRisks.md](../../docs/ArchitectureRisks.md).

## Output
A blast-radius report: files/projects rebuilt, runtime consumers, sibling repos, and a risk rating.
