---
name: implementation-plan
description: Produce the implementation plan for an Encina [FEATURE] issue with the maintainer's versioned prompt, save it under docs/plans and link it from the issue before any code is written. Use when starting work on a feature issue that has no plan yet, or when asked to plan a feature.
---

# Implementation plan for a feature

`AGENTS.md` §11, Issues, plans and changelog: a `[FEATURE]` issue of any size gets a plan before implementation starts.

## Inputs

- The issue: `gh issue view <n> --repo dlrivada/Encina --comments`.
- The prompt, versioned in the repository: `docs/engineering/prompts/implementation-plan-prompt.md`. Read it in full each time; it changes.
- The style reference: `docs/plans/dsr-implementation-plan-404.md`.
- Any SPEC or ADR the issue names, and SPEC-000 for the 1.0 scope.

## Rules that make this the required path (#1927)

- A `[FEATURE]` gets no worker brief until its plan exists, passes `check-plan`, and the maintainer has answered every Design Choice.
- The plan is generated with the prompt verbatim: only `{{ISSUE_URL}}`, `{{ISSUE_NUMBER}}` and `{{FEATURE_NAME}}` are replaced. No added, renamed or reordered sections.
- Only the maintainer answers Design Choices, never an agent and never the orchestrator. The agent recommends: until the maintainer decides, each choice reads `### Chosen Option: **X — name** (recommended, pending the maintainer)`.
- The only addition the plan allows is the mandatory last section `## Maintainer Decisions` (step 8). The plan PR cannot merge before that section exists. Review logs go to PR or issue comments, never into the plan (step 9).

## Procedure

1. Read the prompt and apply it to the issue exactly as written, with only the placeholders filled. Do not summarise or reorder its sections.
2. Ground every statement in the code: name the real files, types and registrations the feature touches. Read them; do not infer them from names.
3. Apply the project rules the prompt asks for:
   - the provider matrix of the feature's category (10 database providers, 8 caching, the 1.0 lock set, AWS and Azure for cloud),
   - the 12 cross-cutting functions of ADR-018, each marked integrate, defer with an issue, or not applicable with a reason,
   - the test types and coverage flags from `.github/coverage-manifest/<Package>.json`,
   - EventId ranges from `src/Encina/Diagnostics/EventIdRanges.cs` for any new logging.
4. Save the plan as `docs/plans/<feature>-implementation-plan-<issue>.md`.
5. Run the structural checker from the repository root and fix every gap it names, until it exits 0:

   ```powershell
   pwsh tools/ai/plans/check-plan.ps1 -Path docs/plans/<feature>-implementation-plan-<issue>.md
   ```

   While the maintainer has not decided yet, add `-Draft` to validate the structure: it skips only the decisions check (CI never uses it). Without `-Draft` the check fails with "Design Choices await the maintainer" until step 8 is done.

   It checks the section order, the Design Choices (at least 4, each with options, chosen option and rationale), the per-phase Tasks and Prompt blocks, the four Research tables, the combined prompt, the 12-function matrix and the file name. `-Changed` checks every plan changed against `origin/main`; `-SelfTest` tests the checker. The `plan-conformance` workflow runs the same script on the pull request.
6. Open deferred integrations as issues with the `open-issue` skill and reference them in the plan.
7. Ship the plan in its own PR (use the `pr-cycle` skill), or as the first commit of the feature PR when the maintainer prefers. Comment on the issue with the plan's link.
8. Present every Design Choice to the maintainer in the chat (the question, the options, your recommendation) and wait for the answers. Record the answers in the plan, in the mandatory `## Maintainer Decisions` section placed last, after `## Next Steps`: one dated entry per Design Choice, numbered like the choices (`1. (yyyy-MM-dd) ...` or `**D1** (yyyy-MM-dd): ...`; the heading variant "Decisions of the maintainer" is also accepted). Replace the "(recommended, pending the maintainer)" marker and update `Chosen Option` in every Design Choice, so the choices and the section agree; the checker fails any entry that is missing and any marker left behind. Commit both in the plan PR and run `check-plan` again. Also post a short issue comment that links to the section.
9. Put review logs (adversarial review, CodeRabbit resolution) in PR or issue comments, never in the plan.
10. After the `## Maintainer Decisions` section is merged or committed in the plan PR, remove the `needs-decision` label from the issue. Implementation starts after that. Write no worker brief for the feature before the plan, a passing `check-plan` and the `## Maintainer Decisions` section all exist.

## Delegation

The first draft of mechanical sections can go to the local model with the `local-ai-task` skill, such as the file inventory or the provider table. The design decisions, the cross-cutting evaluation and the final review stay with the main session.
