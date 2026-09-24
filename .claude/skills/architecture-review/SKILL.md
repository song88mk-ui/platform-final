---
name: architecture-review
description: Evaluate a design or change against platform-final's architecture and risk register (store ownership, Platform.Shared and cross-repo contract coupling, sync vs async communication). Use when reviewing a proposed design or an architectural change before it is built.
---

# Skill: Architecture Review

Evaluate a design/change against platform-final's architecture and risk register.

## Lens
1. **Layering/ownership:** does it respect the three-store/three-owner split? (No Mongo writes; money
   via Wallet procs; curation through the right path.) [docs/DomainModel.md](../../../docs/DomainModel.md).
2. **Coupling:** does it add a `Platform.Shared` change (R-1) or a cross-repo contract change (R-2)?
   Prefer keeping contracts stable; verify against odds-engine-2/platform-web.
3. **Communication:** sync (gateway HTTP) vs async (RabbitMQ `On*`) — is the choice consistent with
   existing flows? Avoid new runtime cycles.
4. **Cross-cutting:** auth filter present; OTel/Serilog; cache invalidation wired; no global-handler reliance.
5. **Risk:** map to [docs/ArchitectureRisks.md](../../../docs/ArchitectureRisks.md) R-1…R-16; rate L×I.

## Output
Conformance + risks + suggested investigations (no refactoring proposals unless asked). Use the
[.claude/checklists/architecture-review.md](../../checklists/architecture-review.md).
