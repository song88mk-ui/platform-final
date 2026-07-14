# /investigate-production

Diagnose a production issue (read-only first).

## Steps
1. **Read-only mindset:** follow [.claude/playbooks/production-readonly.md](../playbooks/production-readonly.md);
   don't mutate state during diagnosis.
2. **Symptom → cause:** start at [docs/knowledge/Troubleshooting.md](../../docs/knowledge/Troubleshooting.md)
   (API/auth, stale-data, real-time, settlement/cashout, infra tables).
3. **Read errors right:** check the body `StatusCode`, not HTTP code; a bare 500 = unhandled exception
   (no global handler).
4. **Locate:** map the area via [docs/knowledge/FrequentlyModifiedAreas.md](../../docs/knowledge/FrequentlyModifiedAreas.md);
   `/trace-request` or `/trace-bet`/`/trace-settlement` as relevant.
5. **Cross-repo?** stale catalogue/odds → odds-engine-2; no real-time → `OE.Websocket.Service` feeder;
   auth → Auth/operator token; deploy/secrets → argocd/SSM.
6. **Telemetry:** correlate via OTel trace id (propagated through RabbitMQ) + `CorrelationId`.

## Output
Hypothesis → evidence (logs/traces/code) → confidence → suggested next read. Mark unverified Unknown.
