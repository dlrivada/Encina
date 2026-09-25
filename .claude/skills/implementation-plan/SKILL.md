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

## Procedure

1. Read the prompt and apply it to the issue exactly as written. Do not summarise or reorder its sections.
2. Ground every statement in the code: name the real files, types and registrations the feature touches. Read them; do not infer them from names.
3. Apply the project rules the prompt asks for:
   - the provider matrix of the feature's category (10 database providers, 8 caching, the 1.0 lock set, AWS and Azure for cloud),
   - the 12 cross-cutting functions of ADR-018, each marked integrate, defer with an issue, or not applicable with a reason,
   - the test types and coverage flags from `.github/coverage-manifest/<Package>.json`,
   - EventId ranges from `src/Encina/Diagnostics/EventIdRanges.cs` for any new logging.
4. Save the plan as `docs/plans/<feature>-implementation-plan-<issue>.md`.
5. Open deferred integrations as issues with the `open-issue` skill and reference them in the plan.
6. Ship the plan in its own PR (use the `pr-cycle` skill), or as the first commit of the feature PR when the maintainer prefers. Comment on the issue with the plan's link.
7. Present the plan's decisions and open questions to the maintainer. Implementation starts after they approve.

## Delegation

The first draft of mechanical sections can go to the local model with the `local-ai-task` skill, such as the file inventory or the provider table. The design decisions, the cross-cutting evaluation and the final review stay with the main session.
