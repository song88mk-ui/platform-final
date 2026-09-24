---
name: performance-analysis
description: Find and reason about platform-final hot paths (multi-thread workers, Mongo catalogue reads, per-pod caches, large repository methods, RabbitMQ throughput). Use when investigating slowness or resource usage, before suggesting any refactor.
---

# Skill: Performance Analysis

Find and reason about platform-final hot paths. Use `/performance-review` for a structured pass.

## Where time goes (known)
- **Multi-thread `BlockingCollection` workers** (CashOut 100, Placement.Servicev2 100, Operator.Servicev2 36,
  Operator.Cashout 36, Client.TopMarket 3) — R-10.
- **Mongo catalogue reads** (projection-first, secondary read pref); index reliance owned by OE2 (U-28).
- **Per-pod `IMemCache`** read-through + platform-web's 30s tier; coarse hour-TTLs.
- **Large repo methods** (`SportsbookRepository`, `BettingRepository`).
- **RabbitMQ** prefetch ~1000; manual-ack throughput; shared SPOF (R-8).

## Method
1. Use existing OTel `Meter` counters (e.g. placement counters) + Serilog timings to locate hotspots.
2. Trace the hot path (`/trace-bet`, `/trace-request`); identify N+1 reads, missing projections,
   cache misses, lock/thread contention.
3. Report observations with evidence + confidence; **suggest investigation, not refactors** (unless asked).

Cross-ref [docs/Hosting.md](../../../docs/Hosting.md), [docs/Caching.md](../../../docs/Caching.md),
[docs/PersistencePatterns.md](../../../docs/PersistencePatterns.md).
