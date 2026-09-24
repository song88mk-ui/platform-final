---
name: bug-investigation
description: Find the root cause of a platform-final bug with evidence (StatusCode decoding, data store and message tracing, trace ids), not guesses. Use when investigating a defect, an unexpected error, or a suspected regression.
---

# Skill: Bug Investigation

Find root cause with evidence, not guesses.

## Method
1. **Reproduce / read the error correctly:** body `StatusCode` (not HTTP code); bare 500 = unhandled
   exception (no global handler).
2. **Symptom → cause table:** [docs/knowledge/Troubleshooting.md](../../../docs/knowledge/Troubleshooting.md).
3. **Locate:** [docs/knowledge/FrequentlyModifiedAreas.md](../../../docs/knowledge/FrequentlyModifiedAreas.md)
   + the relevant `/trace-*`.
4. **Trace the data:** SQL vs Mongo vs cache vs message — confirm which store/flow is involved.
5. **Cross-repo?** stale catalogue/odds → odds-engine-2; real-time gaps → WS feeder; auth → Auth.
6. **Correlate:** OTel trace id (through RabbitMQ headers) + domain `CorrelationId`.
7. **Use git history** if a regression is suspected (`git log -p`, `git diff <tag>`).

## Output
Root-cause hypothesis → evidence (file:line, logs/traces) → confidence → suggested fix area (don't
auto-refactor unless asked). Use [.claude/playbooks/investigate-bug.md](../../playbooks/investigate-bug.md).
