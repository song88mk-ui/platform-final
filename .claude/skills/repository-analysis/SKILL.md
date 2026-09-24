---
name: repository-analysis
description: Understand any part of platform-final quickly and accurately using docs/, file:line tracing and the repo's mental model. Use when orienting in an unfamiliar area or producing an evidence-backed summary of it.
---

# Skill: Repository Analysis

Understand any part of platform-final quickly and accurately.

## Approach
1. **Orient** with [docs/README.md](../../../docs/README.md) → [docs/Architecture.md](../../../docs/Architecture.md)
   → [docs/RepositoryMap.md](../../../docs/RepositoryMap.md).
2. **Locate** the project/file ([docs/ProjectOverview.md](../../../docs/ProjectOverview.md),
   [docs/knowledge/FrequentlyModifiedAreas.md](../../../docs/knowledge/FrequentlyModifiedAreas.md)).
3. **Trace, don't infer** — read the implementation; cite file:line; mark unverified **Unknown**.
4. **Respect boundaries:** subject = platform-final; siblings read-only.
5. **Use sub-agents** for breadth (sweeping many files), keeping conclusions not dumps.

## Mental model to apply
Microservice + RabbitMQ `e.central`; one shared kernel `Platform.Shared` (high blast radius);
three stores/three owners (Mongo read model = odds-engine-2; SQL = platform-final/backoffice);
repository+`BaseResponseDTO` pattern; API-key ActionFilter auth.

## Output
A structured, evidence-backed summary with confidence tags — matching the `docs/` style.
