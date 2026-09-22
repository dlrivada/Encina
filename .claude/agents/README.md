# Claude Code agent definitions

Subagents the main Claude Code session can spawn for this repository, each pinned to the cheapest model and effort that does the job. The model tiers are chosen for cost (`docs/engineering/ai-task-routing.md`): free local AI for bulk mechanical work, Haiku for polling, Sonnet for bounded execution and diagnosis, Opus for adversarial judgement, and the main session's model only for specifying, deciding and the final gate.

| Agent | Model / effort | Role | Writes? |
|---|---|---|---|
| `pr-watcher` | Haiku 4.5 / low | Watches a PR and reports each failed check, bot review and the merge as they happen. Superseded for routine watching by `tools/ai/watch-pr-events.ps1` run as a background monitor, which costs no model tokens; keep the agent for PRs whose events need judgement to triage | No |
| `ci-diagnoser` | Sonnet 5 / medium | Root-causes one failed job or test and proposes the minimal fix | No |
| `mechanical-fixer` | Sonnet 5 / low | Executes an already-decided change in a given worktree, verifies, commits | Yes (in its worktree) |
| `adversarial-reviewer` | Opus 5 / high | SDD Adversarial Reviewer: verified findings against spec, providers, cross-cutting rule, tests, API and claims | No |
| `issue-worker` | Sonnet 5 / medium (Opus when the root cause is unknown) | Implements one issue from the orchestrator's brief in a pre-created worktree, verifies, reports | Yes (in its worktree, never pushes) |

Conventions shared by all agents:

- Tooling per `CLAUDE.md`: PowerShell or direct CLI calls, never python or bash constructs.
- Agents never commit to `main` and never open or close issues or PRs on their own; those actions stay with the main session or the maintainer (`AI-DEVELOPMENT-MODEL.md` §4, INV-006 of SPEC-000).
- Event-driven watching is preferred over report-at-the-end: the watcher sends a message on every event, and for pure polling the main session uses a scripted monitor that costs no model tokens at all.
- Every spawn records its token usage in the completion notice; the main session adds it to the per-task ledger alongside the local AI's `artifacts/local-ai/ledger.csv`.

When to spawn which, from the experience of the first sessions:

- `mechanical-fixer` for any change that is already decided (formatting, exclusions, thread replies with given text, renames), so the main session does not spend its tokens executing it.
- `ci-diagnoser` when a failed job's cause is not visible in the first error lines.
- `adversarial-reviewer` for every PR that touches gates, CI workflows or `.github/scripts`, and for any PR that merged without a CodeRabbit review (for example when CodeRabbit was rate limited).

The equivalent definitions for the free local model (opencode) live in `.opencode/agents/`.

## Delegation (mandatory)

The main session orchestrates: it writes a closed brief per issue, runs one `issue-worker` per issue in its own worktree (two to four at a time), reviews each diff, pushes, opens the PR and runs `adversarial-reviewer`. Every step goes to the specialist that owns it, at every level; a worker that cannot spawn agents lists the steps that should have been delegated so the orchestrator dispatches them.

| Step | Specialist |
|---|---|
| Implement one issue from a brief | `issue-worker` |
| Root-cause a failing job or test not obvious from the first errors | `ci-diagnoser` |
| Already-decided mechanical edits (docs, tables, renames, format, thread replies) | `mechanical-fixer` |
| Review a PR or a specification, and every PR merged without CodeRabbit | `adversarial-reviewer` |
| Bulk drafts, classification, summaries | local model (`local-ai-task` skill) |
| Watching PRs and runs | token-free scripts (`tools/ai/watch-pr-events.ps1`) |

Shared hot spots stay with the orchestrator: workflows, and anything two open PRs would both edit. Changelog entries go to `changelog.d/` fragments.

## Skills

Procedures the main session loads on demand, in `.claude/skills/<name>/SKILL.md`:

| Skill | Use |
|---|---|
| `pr-cycle` | Open a PR, request CodeRabbit, watch it event by event, unblock the merge, clean up |
| `open-issue` | Open an issue with the template prefix and the template's headers verbatim |
| `implementation-plan` | Plan a `[FEATURE]` with `docs/engineering/prompts/implementation-plan-prompt.md` before coding |
| `local-ai-task` | Delegate a bounded task to the local model with a brief, a ledger line and a review |

## Hooks

Wired in `.claude/settings.json` as `PreToolUse` hooks on the `Bash` and `PowerShell` tools. Each reads the tool call from stdin and exits 2 to block it, with the reason shown to the session:

| Hook | Blocks |
|---|---|
| `.claude/hooks/block-ai-attribution.ps1` | `git commit` and `gh pr create/edit/merge` whose message, body or message file carries AI attribution (co-author trailers naming an AI, "generated with" lines) |
| `.claude/hooks/check-issue-template.ps1` | `gh issue create` whose title lacks a template prefix, or whose body misses or reorders the template's `##` headers. It reads the templates at run time and allows calls whose title or body it cannot resolve, and issues on other repositories |

Both hooks read only the arguments of the `git` / `gh` statement itself, through the quote-aware tokenizer in `.claude/hooks/_command-text.ps1`, and let the call through if the hook itself fails. `pwsh -NoProfile -File .claude/hooks/tests/Test-Hooks.ps1` runs their regression suite; run it after changing a hook.
