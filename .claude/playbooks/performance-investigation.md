# Playbook: Performance Investigation

1. **Frame the symptom:** latency? throughput? CPU/memory? which endpoint/flow/worker?
2. **Use existing telemetry:** OTel `Meter` counters (e.g. placement) + Serilog timings + spans
   (trace context flows through RabbitMQ). Identify the slow hop.
3. **Trace the hot path** (`/trace-bet`, `/trace-request`, `/trace-fixture`): look for N+1 reads,
   missing Mongo projections, cache misses, lock/thread contention.
4. **Check the known suspects:** 100-thread workers (R-10); secondary-vs-primary Mongo reads;
   per-pod cache + coarse TTLs; large repo methods; RabbitMQ prefetch/SPOF (R-8).
5. **Datastore:** confirm `AsNoTracking`, projection-first Mongo, proc usage, `DbContextPool` (RedCat).
6. **Report** observed hotspots with evidence + confidence; **suggest investigation, not refactors**
   (unless asked). Ref [docs/Hosting.md](../../docs/Hosting.md), [docs/Caching.md](../../docs/Caching.md).
