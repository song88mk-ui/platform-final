# /find-service

Locate the project/service responsible for a capability.

## Steps
1. Start from [docs/RepositoryMap.md](../../docs/RepositoryMap.md) ("where do I look for…") and
   [docs/ProjectOverview.md](../../docs/ProjectOverview.md) (per-project purpose + message types).
2. Classify: **API** (`*.API`), **worker** (`*.Service`/`*.Servicev2`), **job** (`Job.*`).
3. For async behavior, map the `On*` message to its consumer via
   [docs/DependencyGraph.md](../../docs/DependencyGraph.md) §3.
4. **Confirm the live variant** if duplicated (`*.Service` vs `*.Servicev2`, `WebSocket.API` vs
   `Websocket.API2`) — check argocd. (TD-07)

## Output
The owning project + entry file (Controller or `Worker.cs`) + its repository, with a one-line role.
