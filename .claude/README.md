# .claude/ — Repository-Specific Claude Assets (platform-final)

Generated in Phase 11 from the [`docs/`](../docs/README.md) knowledge base — **derived from this
repository**, not generic templates. Start with [../CLAUDE.md](../CLAUDE.md) and [../AGENTS.md](../AGENTS.md).

> Scope: subject = **platform-final** (backend). UI work belongs to the sibling **platform-web**.
> Siblings are read-only references; `/Reference` is never modified. (User-confirmed assumptions
> A-1/A-2 — see [../docs/KnowledgeValidation.md](../docs/KnowledgeValidation.md).)

## commands/ (slash commands)
- **Trace:** `/trace-request`, `/trace-feature`, `/trace-bet`, `/trace-settlement`, `/trace-cashout`,
  `/trace-fixture`, `/trace-market`, `/trace-selection`, `/trace-translation`, `/trace-websocket`, `/trace-platform-api`
- **Find:** `/find-service`, `/find-repository`, `/find-pattern`
- **Review/analyze:** `/impact-analysis`, `/review-feature`, `/security-review`, `/performance-review`,
  `/investigate-production`, `/compare-reference`, `/update-docs`

## skills/
repository-analysis · feature-development · architecture-review · code-review · platform-api-investigation ·
websocket-investigation · mongodb-investigation · sql-investigation · messaging-investigation ·
translation-investigation · bug-investigation · production-investigation · performance-analysis ·
regression-analysis · blazor-development (→ redirects to platform-web)

## playbooks/
implement-feature · investigate-bug · trace-api · trace-websocket · production-readonly ·
performance-investigation · regression-investigation

## checklists/
before-coding · before-pull-request · architecture-review · regression-review ·
production-investigation · deployment-review · documentation-review

## templates/
new-api-endpoint · new-repository-method · new-message-contract · new-background-service · new-bet-provider

## prompts/
onboard-engineer · explain-architecture · estimate-impact · explain-data-flow

## Mandatory workflow & principles (Phase 12)
Every task follows the **Development workflow** + **Engineering principles** in
[../CLAUDE.md](../CLAUDE.md): search→docs→trace→explain→assess risk→**ask if unclear**→**approval before any
code change**→smallest change→validate→update docs→summarize. **Production is read-only by
default.** Never guess (label verified/assumption/unknown). External repos: identify dependencies,
don't invent behavior.

## The non-negotiables (encoded throughout)
1. `Platform.Shared` change = platform-wide rebuild (R-1).
2. Cross-repo contracts are convention-only — verify against siblings (R-2).
3. Auth = per-controller ActionFilters; no global fallback (R-6).
4. No Mongo writes here; parameterize SQL; no committed/printed secrets; builds on .NET 10 (net10.0).
5. Wait for approval before any code change; production is read-only by default (Phase 12).
