# Skill: Production Investigation

Diagnose live issues safely.

## Rules
- **Read-only first** — [.claude/playbooks/production-readonly.md](../playbooks/production-readonly.md).
  Don't place/settle/cancel bets or mutate config/wallet during diagnosis.
- Treat `docs/` `[Likely]/[Unknown]` as verify-before-rely.

## Method
1. Classify the symptom ([docs/knowledge/Troubleshooting.md](../../docs/knowledge/Troubleshooting.md)).
2. Correlate by OTel trace id (propagated through RabbitMQ headers) + `CorrelationId`; Serilog console-JSON.
3. Decide the tier: gateway/API · worker/messaging · SQL · Mongo (odds-engine-2-owned) · cache (per-pod,
   stale until eviction/TTL) · WebSocket (feeder = OE.Websocket.Service) · deploy/secrets (argocd/SSM).
4. Use `/investigate-production` + the relevant `/trace-*`.

## Output
Timeline + hypothesis + evidence + confidence + suggested next read/owner (platform-final vs sibling).
