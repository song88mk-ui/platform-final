# Playbook: Production Read-Only Investigation

**Rule: observe, don't mutate.** No bet placement/settlement/cancel, no wallet/config writes, no
cache busting, no message publishing during diagnosis.

1. **Reproduce from artifacts only:** request, response (`StatusCode`), logs, traces, metrics.
2. **Correlate:** OTel trace id (propagated through RabbitMQ headers) + domain `CorrelationId`; Serilog
   console-JSON. Use OTel `Meter` counters for throughput/hotspots.
3. **Locate the tier** without changing it: gateway/API · worker/messaging · SQL (read with
   `AsNoTracking` mentally) · Mongo (OE2-owned read model) · cache (per-pod) · WebSocket (feeder) ·
   deploy/secrets (argocd/SSM).
4. **Classify** via [docs/knowledge/Troubleshooting.md](../../docs/knowledge/Troubleshooting.md).
5. **Cross-repo:** if the data originates upstream (odds-engine-2), hand off / read its repo read-only.
6. **Report:** timeline + hypothesis + evidence + confidence + suggested (non-mutating) next step.

Escalate any state change as an explicit, separately-approved action.
