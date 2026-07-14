# /trace-feature

Trace a whole feature across the platform (REST + messaging + datastores + siblings).

## When to use
You need the full picture of a capability (e.g. "bet placement", "cashout", "favorites", "livestream").

## Steps
1. **Find the surface:** gateway endpoint(s) in `Platform.API` + any worker(s) and message types.
2. **Sync path:** `/trace-request` for the REST leg(s).
3. **Async path:** identify the `On*` message(s) it publishes/consumes and which `*.Service` workers
   handle them (see [docs/DependencyGraph.md](../../docs/DependencyGraph.md) §3).
4. **Data:** which SQL context(s) (`BettingContext`, …) and/or Mongo collections.
5. **Cross-repo:** does it depend on odds-engine-2 (catalogue/odds), Auth (token), bet-genius/MTS
   (provider), Wallet, or platform-web (consumer)? See [docs/CrossRepositoryDependencies.md](../../docs/CrossRepositoryDependencies.md).
6. **Lifecycle/rules:** cross-check [docs/EntityLifecycle.md](../../docs/EntityLifecycle.md) and
   [docs/BusinessRules.md](../../docs/BusinessRules.md).

## Output
A diagram-style trace (browser→gateway→services→stores→siblings) with file:line evidence; flag
`[Likely]/[Unknown]` hops. Reference [docs/SequenceDiagrams.md](../../docs/SequenceDiagrams.md).
