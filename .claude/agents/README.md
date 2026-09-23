# Claude Code agent definitions

Subagents the main Claude Code session can spawn for this repository, each pinned to the cheapest model and effort that does the job. The model tiers are chosen for cost, shown in the table below: free local AI for bulk mechanical work, Haiku for polling, Sonnet for bounded execution and diagnosis, Opus for adversarial judgement, and the main session's model only for specifying, deciding and the final gate (task-routing rationale: `docs/engineering/ai-task-routing.md`).

| Agent | Model / effort | Role | Writes? |
|---|---|---|---|
| `pr-watcher` | Haiku 4.5 / low | Watches a PR and reports each failed check, bot review and the merge as they happen. Superseded for routine watching by `tools/ai/watch-pr-events.ps1` run as a background monitor, which costs no model tokens; keep the agent for PRs whose events need judgement to triage | No |
| `ci-diagnoser` | Sonnet 5 / medium | Root-causes one failed job or test and proposes the minimal fix | No |
| `mechanical-fixer` | Sonnet 5 / low | Executes an already-decided change in a given worktree, verifies, commits | Yes (in its worktree) |
| `adversarial-reviewer` | Opus 5 / high | SDD Adversarial Reviewer: verified findings against spec, providers, cross-cutting rule, tests, API and claims | No |
| `issue-worker` | Sonnet 5 / medium (Opus only when the brief says why: unknown root cause or design-heavy task) | Implements one issue from the orchestrator's brief in a pre-created worktree, verifies, reports | Yes (in its worktree, never pushes) |
| `docs-writer` | Sonnet 5 / medium | Writes or restructures one documentation page or issue under the `encina-docs` skill: one Diátaxis quadrant per page, identifiers verified in `src/`, cited figures, links and lint checked | Yes (in its worktree, never pushes) |
| `docs-reviewer` | Sonnet 5 / medium | Read-only review of documentation pages against the `encina-docs` checklist: quadrant, real API, no hand-typed figures, ADR/SPEC links, provider coverage, links and lint | No |

Conventions shared by all agents:

- Tooling per `CLAUDE.md`: PowerShell or direct CLI calls, never python or bash constructs.
- Agents never commit to `main` and never open or close issues or PRs on their own; those actions stay with the main session or the maintainer (`AI-DEVELOPMENT-MODEL.md` §4, INV-006 of SPEC-000).
- Event-driven watching is preferred over report-at-the-end: the watcher sends a message on every event, and for pure polling the main session uses a scripted monitor that costs no model tokens at all.
- Every spawn records its token usage in the completion notice; the main session adds it to the per-task ledger alongside the local AI's `artifacts/local-ai/ledger.csv`.

When to spawn which, from the experience of the first sessions:

- `issue-worker` on Sonnet by default. The orchestrator passes `model: opus` only when its brief states the reason (the root cause is unknown, or the task is design-heavy), because a closed brief rarely needs Opus and Opus costs several times more.

- `mechanical-fixer` for any change that is already decided (formatting, exclusions, thread replies with given text, renames), so the main session does not spend its tokens executing it.
- `ci-diagnoser` when a failed job's cause is not visible in the first error lines.
- `adversarial-reviewer` for every PR that touches gates, CI workflows or `.github/scripts`, and for any PR that merged without a CodeRabbit review (for example when CodeRabbit was rate limited). An `issue-worker` whose change touches production code also runs it on its own diff before reporting and fixes the blockers and majors, so the PR opens without them; this does not replace the orchestrator's PR-level review when CodeRabbit is rate limited.

The equivalent definitions for the free local model (opencode) live in `.opencode/agents/`, for the roles that have one.

## Delegation (mandatory)

The main session orchestrates: it writes a closed brief per issue (`worker-brief` skill), runs one `issue-worker` or `docs-writer` per issue in its own worktree (two to four at a time), reviews each diff, pushes, opens the PR and runs `adversarial-reviewer`. It does not edit `src/` or `tests/` itself. Every step goes to the specialist that owns it, at every level and however small; doing a specialist's step yourself is not allowed. The maintainer confirmed this on 2026-09-24 (#1181) and rejected a proposal to make delegation proportional: the cost of a spawn is trivial next to the quality a specialist brings. This table is the single statement of who owns what; the agent definitions repeat the rows that concern them. When a spawn fails, the agent lists the steps that should have been delegated so the orchestrator dispatches them.

| Step | Specialist | Spawned by |
|---|---|---|
| Implement one issue from a brief | `issue-worker` | orchestrator |
| Root-cause a failing job or test not obvious from the first errors | `ci-diagnoser` | orchestrator, `issue-worker`, `mechanical-fixer` |
| Already-decided mechanical edits (renames, format, tables, thread replies), and every changelog fragment, `PublicAPI.*.txt` line and coverage-manifest entry an `issue-worker` needs | `mechanical-fixer` | orchestrator, `issue-worker`, `docs-writer` |
| Review a PR or a specification, every PR merged without CodeRabbit, and an `issue-worker`'s own diff when it touches production code | `adversarial-reviewer` | orchestrator, `issue-worker` |
| Write or restructure documentation: `docs/**` except `docs/plans/**`, root and package READMEs, `CONTRIBUTING.md` | `docs-writer` | orchestrator, `issue-worker` |
| Review documentation: a documentation PR, and a `docs-writer`'s pages before it reports | `docs-reviewer` | orchestrator, `docs-writer` |
| Read-only research across many files | `Explore` | anyone |
| Bulk drafts, classification, summaries, and the first draft of every follow-up issue file a worker writes (the worker checks and fixes the draft; when the local model is not running the worker writes the file and says so) | local model (`local-ai-task` skill) | anyone |
| Watching PRs and runs | token-free scripts (`tools/ai/watch-pr-events.ps1`) | orchestrator |

Shared hot spots stay with the orchestrator: workflows, and anything two open PRs would both edit. Changelog entries go to `changelog.d/` fragments.

The hooks below enforce part of this table: who may spawn whom, which paths each writing agent may edit, that the specialists an `issue-worker` or `docs-writer` diff requires were spawned before it stops, and that the orchestrator does not edit `src/` or `tests/`. What they do not see is listed under "Limits".

## Skills

Procedures the main session loads on demand, in `.claude/skills/<name>/SKILL.md`:

| Skill | Use |
|---|---|
| `pr-cycle` | Open a PR, request CodeRabbit, watch it event by event, unblock the merge, clean up |
| `open-issue` | Open an issue with the template prefix and the template's headers verbatim |
| `implementation-plan` | Plan a `[FEATURE]` with `docs/engineering/prompts/implementation-plan-prompt.md` before coding |
| `local-ai-task` | Delegate a bounded task to the local model with a brief, a ledger line and a review |
| `worker-brief` | Write a worker's brief: the fixed protocol part copied as is (worktree, absolute paths and worktree-anchored verification commands, Edit tool only, no publishing, changelog fragment, verification by kind of change, self-review, delegation, report with issue files, model choice), what differs for a `docs-writer`, plus the task's goal, scope, decisions and acceptance |
| `encina-docs` | House rules for documentation: Diátaxis quadrants (`diataxis.md` in the skill folder), where each kind of page lives, front matter, cited figures, verification and the review checklist |

## Hooks

Project hooks are wired in `.claude/settings.json`. They run for the main session and, per Claude Code, for every subagent's tool calls too. Each reads the hook input from stdin and exits 2 to block the call, with the reason shown to the session:

| Hook | Event | Blocks |
|---|---|---|
| `.claude/hooks/block-ai-attribution.ps1` | PreToolUse `Bash`, `PowerShell` | `git commit` and `gh pr create/edit/merge` whose message, body or message file carries AI attribution (co-author trailers naming an AI, "generated with" lines) |
| `.claude/hooks/check-issue-template.ps1` | PreToolUse `Bash`, `PowerShell` | `gh issue create` whose title lacks a template prefix, or whose body misses or reorders the template's `##` headers. It reads the templates at run time and allows calls whose title or body it cannot resolve, and issues on other repositories |
| `.claude/hooks/guard-orchestrator-writes.ps1` | PreToolUse `Write`, `Edit`, `MultiEdit`, `NotebookEdit`, `Bash`, `PowerShell` | in the main session only (the hook input has no `agent_id`): file-tool edits and resolvable shell writes under `src/` or `tests/` of the main checkout or any worktree, with "The orchestrator does not edit src/ or tests/; brief an issue-worker (worker-brief skill)". Subagent calls are allowed; specifications, plans, `.claude`, memory and scratch files stay open to the orchestrator |

The first two read only the arguments of the `git` / `gh` statement itself, through the quote-aware tokenizer in `.claude/hooks/_command-text.ps1`.

Agent-scoped hooks are wired in the `hooks:` frontmatter of the agents that need them, so they apply only while that agent runs (a `Stop` hook there runs as `SubagentStop`):

| Hook | Agents | Blocks |
|---|---|---|
| `.claude/hooks/block-worker-publish.ps1` | `issue-worker`, `docs-writer`, `docs-reviewer` | `git push` and any git subcommand off the local allowlist, git aliases, `gh pr`/`gh issue` writes, mutating `gh api` calls |
| `.claude/hooks/block-worker-spawn.ps1 -Agent <name>` | `issue-worker`, `docs-writer`, `mechanical-fixer` | spawning a subagent type outside the agent's allowlist: `issue-worker` → `ci-diagnoser`, `mechanical-fixer`, `Explore`, `adversarial-reviewer`, `docs-writer`; `docs-writer` → `mechanical-fixer`, `docs-reviewer`, `Explore`; `mechanical-fixer` → `ci-diagnoser`, `Explore`. The `Agent(...)` list in each `tools:` line documents the same set, but Claude Code ignores it inside a subagent, so the hook is the enforcement |
| `.claude/hooks/enforce-path-ownership.ps1 -Agent <name>` | `issue-worker`, `docs-writer` | Write/Edit calls on paths another specialist owns: an `issue-worker` on documentation (→ `docs-writer`; `docs/plans/**` excepted) or on `changelog.d/**`, `**/PublicAPI.*.txt`, `.github/coverage-manifest/**` (→ `mechanical-fixer`); a `docs-writer` on `src/`, `tests/`, `.github/` other than READMEs and `CONTRIBUTING.md` (→ `mechanical-fixer` or the report). The message names the specialist to spawn |
| `.claude/hooks/require-specialists.ps1 -Agent <name>` | `issue-worker`, `docs-writer` (`Stop`) | stopping before the specialists the worktree's diff requires were spawned (read from the agent's own transcript): `issue-worker` production code → `adversarial-reviewer`, documentation → `docs-writer`, changelog/PublicAPI/manifests → `mechanical-fixer`; `docs-writer` documentation → `docs-reviewer`. It answers `{"decision":"block"}` with the list and the reason, once: when the agent stops again (`stop_hook_active`) it lets it stop |
| `.claude/hooks/block-main-checkout-writes.ps1` | `issue-worker`, `mechanical-fixer`, `docs-writer` | Write/Edit/NotebookEdit paths and the shell writes it can resolve (`Set-Content`, `Add-Content`, `Out-File`, `Tee-Object`, `New-Item`, `Copy-Item`/`Move-Item` destination with PowerShell parameter binding, `[IO.File]` writes, copies and moves, `[IO.StreamWriter]::new`, `>`/`>>`, Bash `cp`/`mv`/`touch`/`tee`, and git commands that change the working tree) aimed at the main checkout instead of a worktree, following `cd`/`Set-Location`/`Push-Location`/`Pop-Location`, `~`, `$env:X` and `$HOME`; a target it cannot resolve from a main-checkout cwd gets a warning, not a block. And content writes to repo source files (`.cs`, `.csproj`, `.props`, `.targets`, `.sln`/`.slnx`, `.json`, `.yml`/`.yaml`, `.md`, `.txt`, `.sql`, `.ps1`/`.psm1`/`.psd1`, `.sh`, `.editorconfig`, `.xml`, `.config`, `.resx`, `.razor`, `.cshtml`), which go through the Edit tool: denied when the target resolves inside the project outside `artifacts/`, or when a variable target is written by a command that uses `-replace`/`.Replace(`/`[regex]::Replace` and names a repository source path (#1181) |
| `.claude/hooks/block-prohibited-commands.ps1` | every agent in this folder | the executables `CLAUDE.md` prohibits (`python`, `grep`, `sed`, `awk`, `head`, `tail`, `wc`, `xargs`, `curl`, `cut`, `paste`, `shuf`, `unzip`, `basename`, `xxd`, `dd`, `bash -c`/`sh -c`; in Bash also `find`, `cat`, `ls`, `sort`, `tee`), Bash `for`/`while`/`until`/`if`/`case`/`select`, `test`/`[`/`[[` conditions, `$( ... )` and backtick substitution and `<( )`/`>( )` process substitution; comments and PowerShell hashtable keys are not commands; the message names the PowerShell or tool equivalent (#1181) |

Every hook lets the call through if the hook itself fails (fail open). `pwsh -NoProfile -File <checkout>\.claude\hooks\tests\Test-Hooks.ps1` runs their regression suite; run it after changing a hook.

### Limits

What the hooks do not enforce, so the agent definitions still say it in words:

- Shell writes the analysis cannot resolve (a target held in a variable other than `$env:X`/`$HOME`, a subexpression, a program such as `dotnet` or a script writing files, deletions, `Rename-Item`) pass `block-main-checkout-writes` and `guard-orchestrator-writes`; only an unresolved target from a main-checkout cwd gets a warning.
- `enforce-path-ownership` sees the file tools only. A shell copy or move into `docs/` or `changelog.d/` passes it; the content-write rule of `block-main-checkout-writes` still blocks `Set-Content`-style writes to those `.md`/`.txt` files, and `require-specialists` still asks for the specialist at stop time.
- `require-specialists` checks that a specialist was spawned, not what it did, and asks once per stop: an agent that stops again after the reminder is let through. It reads the diff since the branch left `main`, so on a branch stacked on another branch it also counts that branch's files.
- Whether frontmatter hooks of an agent also run for the subagents it spawns is not documented; the per-agent hooks take the agent from the hook input's `agent_type` when present, so an inherited hook applies the right agent's rule (see the follow-up issue on nested-agent hooks).
- Claude Code allows three levels of subagents below the main session: a `mechanical-fixer` spawned by a `docs-writer` spawned by an `issue-worker` cannot spawn further.
