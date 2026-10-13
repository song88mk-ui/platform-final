---
name: jira-updates
description: Rules for ANY interaction with Jira (song88.atlassian.net) via the Atlassian MCP tools, and for outward-facing git actions (git push, pull request creation). MUST be followed whenever creating, editing, commenting on, linking, or transitioning a Jira issue, when a task might touch Jira as a side effect, and before any git commit, git push or PR creation in any repository (every commit must be preceded by a rebuild and a test run).
---

# Jira Updates - Approval Required

Jira is a shared, team-visible system. Every write is immediately visible to the whole team under the user's own name. These rules are absolute.

## Rules

1. **Never write to Jira unless the user explicitly asks for that write in this conversation.**
   - "Explicitly asks" means the user requests the Jira change itself (e.g. "update the ticket", "add a comment to GM-123").
   - Providing a ticket number, connecting the MCP server, or discussing a ticket is NOT permission to write to it.
   - Read operations (get issue, search, list transitions) are always fine.

2. **Before any write: show the exact content first, then wait for approval.**
   - Present verbatim what will be saved (summary, description, comment body, field values) in the chat.
   - Ask for approval and stop the turn. Only perform the write after the user approves in a later message.
   - If the user edits the proposal, show the revised version again unless they say to just send it.

3. **NEVER update issue status.** Do not call transition tools, set resolutions, or otherwise move an issue through its workflow, even if asked implicitly ("mark it done"). If the user explicitly requests a transition, remind them of this rule and ask them to do it themselves in Jira (or to explicitly override this rule for that one action).

4. **Content style:** no em dash character ("—") in anything written to Jira or Confluence, use "-" or "," instead; strictly no emojis.

5. Scope: applies to all Atlassian write tools (`createJiraIssue`, `editJiraIssue`, `addOrEditJiraIssueComment`, `transitionJiraIssue`, `createConfluenceContent`, `updateConfluenceContent`, and any `executeWrite`/`executeDestructive` operation such as worklogs or issue links).

## Suggested flow

1. Read the issue (allowed without asking).
2. Draft the change; show it verbatim: "This is exactly what I would save to GM-XXX: ...".
3. Ask: "Shall I save this?" and end the turn.
4. On approval, perform ONLY the approved write, then report the result (issue key, comment id).

# Git Pushes and Pull Requests - Approval Required

Pushing branches and opening pull requests are outward-facing, team-visible actions, exactly like Jira writes. These rules apply in every repository (platform-web, infrastructure, odds-engine-2, platform-final, and all other org repos).

## Rules

1. **Never `git push` or create a pull request unless the user explicitly asks for that action in this conversation.**
   - "Proceed with the fix" / "implement it" / "apply the change" authorizes local changes only: file edits and, at most, a local commit.
   - Providing a ticket number (e.g. "ticket: GM-784") is naming/reference material, NOT permission to push, open a PR, or write to the ticket.
   - Read operations (fetch, pull, log, diff, status) are always fine.

2. **After implementing, stop at the local commit.** Report what was changed and ask before pushing or opening a PR, then end the turn.

3. **Do exactly the approved scope and nothing more.** "Push the branch" does not imply "open a PR"; "open a PR" does not imply merging, editing reviewers, or commenting on tickets.

(Added after GM-784, 2026-09-01: "proceed with the fix, ticket: GM-784" was wrongly treated as permission to push and open a PR.)

# Rebuild and Test Before Committing - Mandatory

A commit is a checkpoint that claims the tree is good. Never create one until the change has been
built and the relevant tests have been run. This applies in every repository, not just platform-web.

## Rules

1. **Before every `git commit`: rebuild the affected project(s), then run the relevant test suite.**
   Run both, in that order, and read the output. A build that succeeds is not evidence the tests pass.
2. **Commit only on green.** If the build fails or a test fails, fix it first, or stop and report it.
   Never commit a red tree to "save progress" unless the user asks for exactly that.
3. **If validation cannot be run** (no test project for the area, missing dependency, environment
   unavailable), say so explicitly in the commit report, name what was not covered and why, and
   recommend the tests that should run before deployment. Never let silence imply it was verified.
4. **Report the result** with the commit: build status, test status (counts), and anything skipped.
5. When a change touches more than one repository, this applies to each of them separately.

## Commands per repository

| Repo | Build | Test |
| --- | --- | --- |
| `platform-web` | `dotnet build Platform/Platform.Child.Web` | `dotnet test Platform/Platform.Test -c Release` (use `-c Release`: a Debug build triggers the wwwroot pnpm `PreBuild` target) |
| `odds-engine-2` | `dotnet build src/OddsEngine/OddsEngine.sln` | `dotnet test src/OddsEngine/OddsEngine.sln`, or the single matching `*.Tests` project |
| `platform-final` | `dotnet build Platform/Platform.sln` | `dotnet test Platform/Platform.sln`, or the single matching `*.Test` project (singular) |
| `bet-genius` | `dotnet build BetGenius.sln` | `dotnet test BetGenius.sln` |
| `backoffice-final` | `dotnet build` | `dotnet test` |

For any repo not listed, find the solution or test project first and say which command was run.

(Added 2026-09-21 after an OE.Job.Cleanup change was committed on build evidence alone.)
