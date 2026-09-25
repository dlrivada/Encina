@AGENTS.md

# CLAUDE.md - Claude Code specifics

The engineering rules are in `AGENTS.md` (imported above) and bind every session and subagent. This file adds only what is specific to Claude Code. The frozen reasoning behind the rules is in `docs/engineering/ENGINEERING-HANDBOOK.md`.

## Active Plans

Before starting work, read the plan of your area and continue from where the last session stopped.

| Plan | File | Status |
| --- | --- | --- |
| Test Consolidation | `docs/plans/test-consolidation-plan.md` | 🟡 In Progress |
| Performance Measurement Infrastructure | `docs/plans/performance-infrastructure-plan.md` | 🟢 Phase 4 implemented (ADR-025) |
| Encina 1.0 — Phase 0 baseline | `docs/engineering/PHASE0-BASELINE.md` | 🟢 Diagnostic done 2026-09-21; next: #1088 (build) |
| SPEC-000 — 1.0 Baseline and Release Scope | `docs/specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md` | 🟢 APPROVED 2026-09-21 — the 1.0 boundary; six decisions recorded |
| AI development model (SDD, agents, routing) | `docs/engineering/AI-DEVELOPMENT-MODEL.md` | 🟢 Reference |

## Orchestration and delegation

- The main session orchestrates: one closed brief per issue (`worker-brief` skill), one `issue-worker` or `docs-writer` per issue in its own git worktree, then review, push, PR and merge (`pr-cycle` skill). Shared hot spots such as `.github/workflows/*` stay with the orchestrator.
- Workers never push, open or edit PRs, or open or comment on issues; they write follow-up issue files and the orchestrator opens them (`open-issue` skill).
- Every step goes to the specialist that owns it (#1181): `mechanical-fixer` for decided edits, changelog fragments, PublicAPI lines and coverage manifests; `docs-writer` for pages under `docs/`, READMEs and CONTRIBUTING; `ci-diagnoser` for failures; `adversarial-reviewer` for self-review and PR review. Spawn specialists in the foreground and name the worktree's absolute path.
- Agent definitions, the delegation table and the hooks that enforce these rules (prohibited commands, main-checkout writes, path ownership, AI attribution, issue templates) are described in `.claude/agents/README.md`; hooks live in `.claude/hooks/` with their tests in `.claude/hooks/tests/Test-Hooks.ps1`.

## Model routing

- Workers and reviewers run on Sonnet; Opus only when the brief states why (unknown root cause, design-heavy task, security or personal data). Haiku for polling and already-decided edits.
- Drafts, classification, summaries and first drafts of issue files go to the free local model through `tools/ai/local-ai-ask.cs` (`local-ai-task` skill; routing in `docs/engineering/ai-task-routing.md`). Record token usage in `artifacts/local-ai/ledger.csv` and `artifacts/agent-usage/ledger.csv`.

## Closed-issue audit (SPEC-003)

The per-issue knowledge migration and quality audit follows SPEC-003. Its pipeline is the `issue-audit` skill (`.claude/skills/issue-audit/`) and the scripts under `tools/ai/audit/`, both from #1345; they may land after this file, so check that they exist before relying on them.

## CodeRabbit

- Every PR gets a CodeRabbit review (`@coderabbitai review`); resolve or answer each comment. Configuration is `.coderabbit.yaml` (Spanish summaries, per-path instructions).
- CodeRabbit enriches issues only when they are created or edited; for an existing issue comment `@coderabbitai please analyze this issue and suggest related issues/PRs`, and read the enrichment before starting.
- `@coderabbitai plan` (or the `plan-me` label) generates an implementation plan that can seed a session; `@coderabbitai resolve` marks all comments resolved; `@coderabbitai help` lists commands.
- Use conventional commit messages and reference the issue in the PR (`Fixes #N`) so CodeRabbit validates the PR against it. When CodeRabbit is rate limited, `adversarial-reviewer` replaces its review.
- Reasoning and the full feature list: handbook "CodeRabbit Integration".
