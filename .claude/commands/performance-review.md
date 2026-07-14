# /performance-review

Performance review tailored to platform-final's hot paths.

## Focus areas
1. **Hot-path consumers (R-10):** the 100-thread `BlockingCollection` workers (`CashOut.Service`,
   `Placement.Servicev2`, `Operator.Servicev2`, `Operator.Cashout.Service`, `Client.TopMarket`) —
   thread/queue sizing, ordering, back-pressure.
2. **Mongo reads:** projection-first (`.Project<T>()`), **secondary read preference** for high-volume
   catalogue; missing index reliance (indexes owned by odds-engine-2 — U-28).
3. **Caching:** read-through `IMemCache` (per-pod); hot keys (`cc:`, `mts:auth`, catalogue) + the
   per-pod/30s-platform-web layering; coarse hour-TTLs.
4. **SQL:** `AsNoTracking` on reads; stored procs for reporting/wallet; `DbContextPool` (RedCat);
   `Job.Resettlement` 30-min command timeout; large repo methods (`SportsbookRepository`, 724 changes).
5. **Messaging:** `PrefetchCount` (~1000); manual-ack throughput; RabbitMQ as shared SPOF (R-8).
6. **Telemetry:** use OTel `Meter` counters already present (e.g. placement counters) to locate hotspots.

## Output
Observed hotspots with evidence (code + metrics if available), confidence, and suggested investigation
(no refactoring unless asked). Cross-ref [docs/Caching.md](../../docs/Caching.md), [docs/Hosting.md](../../docs/Hosting.md).
