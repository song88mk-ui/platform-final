# .claude/ — platform-final

Repo-specific commands, skills and task guides, plus `settings.json` (the standing-rules prompt hook).
Read [../CLAUDE.md](../CLAUDE.md) and [../AGENTS.md](../AGENTS.md) first.

## commands/
/trace-request · /trace-feature · /trace-bet · /trace-settlement · /trace-cashout · /trace-fixture ·
/trace-market · /trace-selection · /trace-translation · /trace-websocket · /trace-platform-api ·
/find-service · /find-repository · /find-pattern · /impact-analysis · /review-feature ·
/security-review · /performance-review · /investigate-production · /update-docs

## skills/
repository-analysis · feature-development · architecture-review · code-review ·
platform-api-investigation · websocket-investigation · mongodb-investigation · sql-investigation ·
messaging-investigation · translation-investigation · bug-investigation · production-investigation ·
performance-analysis · regression-analysis · jira-updates ·
blazor-development (redirects to platform-web)

## playbooks/
implement-feature · investigate-bug · trace-api · trace-websocket · production-readonly ·
performance-investigation · regression-investigation

## checklists/
before-coding · before-pull-request · architecture-review · regression-review ·
production-investigation · deployment-review · documentation-review

## templates/
new-api-endpoint · new-repository-method · new-message-contract · new-background-service ·
new-bet-provider

## prompts/
onboard-engineer · explain-architecture · estimate-impact · explain-data-flow

## Rules and synced files

Workflow and rules: [../CLAUDE.md](../CLAUDE.md) (standing rules, § Development workflow, § Engineering principles).

The `jira-updates` skill and [`.github/pull_request_template.md`](../.github/pull_request_template.md) are
generated from `song88-claude` by `sync_standing_rules.py`. Do not edit them here; change them in
`song88-claude`.
