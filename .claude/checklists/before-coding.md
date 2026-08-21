# Checklist: Before Coding

- [ ] **Right repo?** Backend → platform-final; UI → platform-web. Not editing a sibling or `/Reference`.
- [ ] **Traced the existing implementation** (`/trace-*`) — not inferring.
- [ ] **Found the pattern** to follow ([docs/ImplementationPatterns.md](../../docs/ImplementationPatterns.md),
      [docs/BestPractices.md](../../docs/BestPractices.md)) — no new framework where one exists.
- [ ] **Blast radius known** (`/impact-analysis`): is it `Platform.Shared`? a cross-repo contract? a
      `StatusCode`? the shared `Sportsbook` SQL DB?
- [ ] **Live variant confirmed** (not a superseded `*.Service`/`WebSocket.API` duplicate).
- [ ] **Data path chosen** (EF/proc/Mongo-read/HTTP) consistent with the area; no Mongo writes.
- [ ] **Auth:** new controller will get a `[ServiceFilter]`.
- [ ] **Config/secrets:** values via config/SSM, no hardcoding, no new committed secrets.
- [ ] **Compatibility:** change stays .NET Core 3.1-compatible.
- [ ] **Unknowns checked** ([docs/Unknowns.md](../../docs/Unknowns.md)) for anything you're assuming.
