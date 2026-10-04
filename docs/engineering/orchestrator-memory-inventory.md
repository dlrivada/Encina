# Orchestrator memory inventory

This is a reference page (Diátaxis: reference) for a maintainer or agent who needs to know which topics the orchestrator keeps in private working memory and where each one stands, so that nothing durable lives only there. It lists topic names only, never content. The tracking issue is [#1736](https://github.com/dlrivada/Encina/issues/1736); the process this supports is described in [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md) §17 (knowledge debt).

## The rule

Every durable method lesson goes to the repository in the same pull request that teaches it (maintainer, 2026-09-24). Private memory is a pointer, never the only copy.

## Classes

| Class | Meaning |
| --- | --- |
| Private by design | Stays out of the repository because it does not belong in a public repository. |
| Migrated by #1736 | The durable content now lives in the file named in the table. |
| Already versioned | The repository already held the rule before #1736. |
| Not yet migrated | Still only in private memory; the issue that will carry it is named where one exists. |

## Why some topics stay private

A cost plan with budget numbers, and the private details of the first application built on Encina, do not belong in a public repository. They are working notes of the maintainer, not engineering knowledge of the library. The rules derived from them are written generically in the repository instead.

## Inventory

| Topic | Class | Where it lives now / issue |
| --- | --- | --- |
| ai-budget-hypothesis (cost plan) | Private by design | Private memory only |
| reference-application notes | Private by design | Private memory only (the first application's private details) |
| resume points and handoffs (handoff-2026-09-25, handoff-2026-09-23-night, handoff-2026-09-22, handoff-2026-09-24-queue) | Private by design | Private memory only; working state of past sessions |
| arm-auto-merge-myself | Migrated by #1736 | [`.claude/skills/pr-cycle/SKILL.md`](../../.claude/skills/pr-cycle/SKILL.md) section 3 |
| brief-crap-local-check | Migrated by #1736 | [`.claude/skills/worker-brief/SKILL.md`](../../.claude/skills/worker-brief/SKILL.md) (fixed part) |
| brief-read-issue-comments | Migrated by #1736 | [`.claude/skills/worker-brief/SKILL.md`](../../.claude/skills/worker-brief/SKILL.md) (section 1 and fixed part) |
| hooks-live-from-main-checkout | Migrated by #1736 | [`.claude/agents/README.md`](../../.claude/agents/README.md), "Where hooks load from" |
| powershell-gh-gotchas | Migrated by #1736 | [`powershell-gh-gotchas.md`](powershell-gh-gotchas.md) |
| no-weekly-budget-gating | Migrated by #1736 | [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md), standing rules 1 and 2 |
| retrospective-2026-10-03 | Migrated by #1736 | [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md), standing rule 1 (the rejected and approved improvements) |
| pre10-best-solution-always | Migrated by #1736 | [`AGENTS.md`](../../AGENTS.md) §1 and [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md), standing rule 3 |
| local-ai-only-where-reliable | Migrated by #1736 | [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md), standing rule 4 |
| workflow-agents-cannot-spawn | Migrated by #1736 | [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md), standing rule 5 |
| watch-dependabot-prs | Migrated by #1736 | [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md), standing rule 6 |
| pr-watching-realtime | Migrated by #1736 | [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md), standing rule 7; scripting details in [`powershell-gh-gotchas.md`](powershell-gh-gotchas.md) |
| method-applies-to-all-work | Not yet migrated | To be classified |
| orchestrator-mode | Already versioned (the delegation model; some operating details are not yet migrated) | [`CLAUDE.md`](../../CLAUDE.md) ("Orchestration and delegation") and [`.claude/agents/README.md`](../../.claude/agents/README.md) |
| issue-format | Already versioned | [`.github/ISSUE_TEMPLATE`](../../.github/ISSUE_TEMPLATE), [`AGENTS.md`](../../AGENTS.md) §11, [`prompts/implementation-plan-prompt.md`](prompts/implementation-plan-prompt.md) |
| spec-002-decisions and the other maintainer decisions | Not yet migrated | [#1737](https://github.com/dlrivada/Encina/issues/1737) (decisions log) |
| control-board-artifact | Not yet migrated | To be classified |
| board-assessments | Not yet migrated | To be classified |
| knowledge-migration-spec003 | Not yet migrated | To be classified |
| local-ai-llama-server | Not yet migrated | To be classified |
| local-ai-off-temperature | Not yet migrated | To be classified |
| session-start-llama | Not yet migrated | To be classified |
| docs-strategy-diataxis | Not yet migrated | To be classified |
| ai-task-routing (the private note) | Not yet migrated | To be classified; the versioned page is [`ai-task-routing.md`](ai-task-routing.md) |
| handoff-2026-09-21 | Not yet migrated | To be classified |
| brief-1368-1380 (historical) | Not yet migrated | To be classified |

The list covers the topics classified for #1736; other private notes exist and are classified the same way as they are reviewed. Topics marked "To be classified" get their own issue once someone has decided whether they are private by design, already versioned or to be migrated; no issue exists for them yet.
