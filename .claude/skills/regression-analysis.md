# Skill: Regression Analysis

Determine whether/why behavior changed.

## Method
1. **Baseline:** compare against `/Reference` (`/compare-reference`) — read-only; focus on behavior
   (status codes, branching, SQL/Mongo queries, message/DTO shapes), not formatting.
2. **Git history:** the area's churn + recent commits ([docs/knowledge/FrequentlyModifiedAreas.md](../../docs/knowledge/FrequentlyModifiedAreas.md));
   `git log -p` the suspect file (hotspots: `SportsbookRepository.cs`, `BettingRepository.cs`).
3. **Contract drift:** a regression may originate in a sibling (odds-engine-2/platform-web) due to
   convention-only contracts (R-2) — verify field/key alignment.
4. **Duplicates:** confirm you're comparing the **live** variant (`*.Service` vs `*.Servicev2`, etc.).

## Output
What changed · where · when (commit) · why · confidence · suggested verification. Don't auto-fix.
Use [.claude/playbooks/regression-investigation.md](../playbooks/regression-investigation.md) +
[.claude/checklists/regression-review.md](../checklists/regression-review.md).
