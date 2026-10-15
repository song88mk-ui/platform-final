# AGENTS.md — platform-final

The headless .NET sportsbook backend: a microservice, RabbitMQ-driven solution in
`Platform/Platform.sln`. Read [CLAUDE.md](CLAUDE.md) first: it holds the standing rules every agent
follows and the full operating manual. This file is the short version for agents.

## Mission boundaries
- Subject = platform-final (backend services, SQL contexts, the `Websocket.API2` hub). It has no UI:
  UI work belongs to `platform-web`.
- Only modify files inside this repo; siblings (`odds-engine-2`, `platform-web`, `Auth`,
  `backoffice-final`, `bet-genius`, `argocd`, `infrastructure`) are read-only references.
- odds-engine-2 owns the MongoDB catalogue; platform-final only reads it.

## Hard rules
- A `Platform.Shared` change rebuilds every image; treat its message models, entities and enums as
  public API (CLAUDE.md § Critical rules, rule 1).
- Cross-repo contracts are convention-only: check odds-engine-2 and platform-web before renaming a
  field, routing key, column or SignalR method (CLAUDE.md § Critical rules, rule 2).
- Every new controller needs a `[ServiceFilter(typeof(<Filter>))]`; there is no global auth fallback
  (CLAUDE.md § Critical rules, rule 3).
- Never write MongoDB from platform-final (CLAUDE.md § Critical rules, rule 5).
- Parameterize every `FromSqlRaw`; money moves only through stored procs (CLAUDE.md § Critical
  rules, rule 6).
- Mask a secret that must appear in a log line with `StringExtensions.Mask()` (CLAUDE.md § Critical
  rules, rule 7).
- `DeleteCache(key)` removes one exact key; use `DeleteCacheByPrefix` or the full key (CLAUDE.md §
  Critical rules, rule 8).
- A consumer-side mirror of an OE2 message needs `[JsonPropertyName]` on every property and a test
  that deserializes an OE2-shaped payload; `System.Text.Json` silently drops names that do not match
  (CLAUDE.md § Critical rules, rule 12).
- `Platform.Shared` and the three Mongo services stay on `net8.0`; everything else is `net10.0`
  (CLAUDE.md § Scope & build reminders).

## See also
[CLAUDE.md](CLAUDE.md) · [.claude/README.md](.claude/README.md) · [docs/README.md](docs/README.md)
