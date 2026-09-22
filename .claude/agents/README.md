# Claude Code agent definitions

Subagents the main Claude Code session can spawn for this repository, each pinned to the cheapest model and effort that does the job. The model tiers are chosen for cost (`docs/engineering/ai-task-routing.md`): free local AI for bulk mechanical work, Haiku for polling, Sonnet for bounded execution and diagnosis, Opus for adversarial judgement, and the main session's model only for specifying, deciding and the final gate.

| Agent | Model / effort | Role | Writes? |
|---|---|---|---|
| `pr-watcher` | Haiku 4.5 / low | Watches a PR and reports each failed check, bot review and the merge as they happen | No |
| `ci-diagnoser` | Sonnet 5 / medium | Root-causes one failed job or test and proposes the minimal fix | No |
| `mechanical-fixer` | Sonnet 5 / low | Executes an already-decided change in a given worktree, verifies, commits | Yes (in its worktree) |
| `adversarial-reviewer` | Opus 5 / high | SDD Adversarial Reviewer: verified findings against spec, providers, cross-cutting rule, tests, API and claims | No |

Conventions shared by all four:

- Tooling per `CLAUDE.md`: PowerShell or direct CLI calls, never python or bash constructs.
- Agents never commit to `main` and never open or close issues or PRs on their own; those actions stay with the main session or the maintainer (`AI-DEVELOPMENT-MODEL.md` §4, INV-006 of SPEC-000).
- Event-driven watching is preferred over report-at-the-end: the watcher sends a message on every event, and for pure polling the main session uses a scripted monitor that costs no model tokens at all.
- Every spawn records its token usage in the completion notice; the main session adds it to the per-task ledger alongside the local AI's `artifacts/local-ai/ledger.csv`.

The equivalent definitions for the free local model (opencode) live in `.opencode/agents/`.
