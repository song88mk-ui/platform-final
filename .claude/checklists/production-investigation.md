# Checklist: Production Investigation

- [ ] **Read-only:** no state mutation (bets/wallet/config) during diagnosis.
- [ ] **Error read correctly:** body `StatusCode`, not HTTP code; bare 500 = unhandled exception.
- [ ] **Symptom classified** via [docs/knowledge/Troubleshooting.md](../../docs/knowledge/Troubleshooting.md).
- [ ] **Correlated** by OTel trace id (through RabbitMQ headers) + `CorrelationId`.
- [ ] **Tier identified:** gateway/API · worker/messaging · SQL · Mongo (OE2-owned) · cache (per-pod) ·
      WebSocket (feeder=OE.Websocket.Service) · deploy/secrets (argocd/SSM).
- [ ] **Cross-repo ownership** considered (catalogue/odds → odds-engine-2; auth → Auth; deploy → argocd).
- [ ] **Stale data?** per-pod cache miss-on-eviction or platform-web 30s tier considered.
- [ ] **Hypothesis + evidence + confidence + suggested next step** recorded; unverified = Unknown.
