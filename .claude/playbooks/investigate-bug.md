# Playbook: Investigate a Bug

1. **Capture the symptom** precisely (request, response body `StatusCode`, timestamp, client/operator).
2. **Classify** via [docs/knowledge/Troubleshooting.md](../../docs/knowledge/Troubleshooting.md).
3. **Locate** the area ([docs/knowledge/FrequentlyModifiedAreas.md](../../docs/knowledge/FrequentlyModifiedAreas.md))
   and pick a tracer (`/trace-request`, `/trace-bet`, `/trace-settlement`, `/trace-cashout`, `/trace-websocket`).
4. **Follow the data** through one store/flow at a time (SQL · Mongo read model · cache · message).
5. **Correlate** with OTel trace id (through RabbitMQ headers) + `CorrelationId`; read Serilog JSON.
6. **Cross-repo check:** is the fault upstream (odds-engine-2 catalogue/odds, Auth, WS feeder)? Don't
   assume — confirm in the sibling (read-only).
7. **Regression?** `git log -p -- <path>` the suspect file; `git log -S<symbol>` to date a change.
8. **Conclude:** root cause + evidence (file:line, logs) + confidence; identify the fix *area* (don't
   refactor unless asked). Propose a verification.
