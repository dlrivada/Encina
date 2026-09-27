# Implementation Plan: `pr-reviewer` agent — CodeRabbit-style review of published PRs

> **Issue**: [#1447](https://github.com/dlrivada/Encina/issues/1447)
> **Epic**: [#1453](https://github.com/dlrivada/Encina/issues/1453) — Fallback system for external review bots (Phase 1: Review fallback)
> **Type**: Feature (developer-experience / tooling — Claude Code agent, no .NET code)
> **Complexity**: Low-medium (1 new agent file, 4 hook edits, ~10 new `Test-Hooks.ps1` cases, 3 documentation tables, 1 skill step, 1 lessons file)
> **Estimated Scope**: ~350-450 lines of Markdown/PowerShell across 8-10 files; no `src/`, no `tests/*.cs`, no provider matrix, no EventIds

---

## Summary

Adds `pr-reviewer`, a read-only Claude Code subagent that reviews an already-published pull request the way CodeRabbit does — against the PR diff (`gh pr diff`), its linked issue's acceptance criteria (`Fixes #N`), and the repository's own rules (`AGENTS.md`, `CLAUDE.md`, the `path_instructions` of `.coderabbit.yaml`) — and writes its findings to `artifacts/pr-review/<pr>.md` for the orchestrator to post as a PR review. It exists because CodeRabbit has been rate-limited since 2026-09-26 ("Review limit reached"), and the orchestrator's current stop-gap, `adversarial-reviewer`, was designed for a different job: pre-push self-review of an `issue-worker`'s own diff against its closed brief, not a published PR against a linked issue and the repository's public rule set. This is Phase 1 of epic #1453; #1448 (automatic BOT-UNAVAILABLE detection) and #1452 (the one-week comparison trial) build on it later.

- **Standard covered**: none (project-internal tooling), but the output shape deliberately mirrors CodeRabbit's own review structure (summary/walkthrough, per-file findings with severity, a linked-issue check, pre-merge-style checks) so the two are easy to compare during the #1452 trial.
- **Affected paths**: `.claude/agents/pr-reviewer.md` (new), `.claude/agents/lessons/pr-reviewer.md` (new), `.claude/agents/README.md`, `.claude/hooks/block-worker-spawn.ps1`, `.claude/hooks/block-worker-publish.ps1`, `.claude/hooks/enforce-path-ownership.ps1`, `.claude/hooks/no-background-specialists.ps1`, `.claude/hooks/tests/Test-Hooks.ps1`, `.claude/skills/pr-cycle/SKILL.md`, `docs/engineering/AI-DEVELOPMENT-MODEL.md`, `docs/engineering/ai-task-routing.md`, `artifacts/pr-review/**` (its own runtime output, git-ignored).
- **Provider category**: none — this is a Claude Code agent/hook feature, not a database, caching, transport, lock or validation feature (AGENTS.md §5 does not apply).
- **Cross-cutting functions (AGENTS.md §6)**: none apply — no entities, stores, pipeline behaviors, background services or external integrations are created; see the matrix in §f below for the one-sentence reason against each of the 12 functions (the same reasoning the issue and epic bodies already gave, verified against the actual change).

---

## Design Choices

<details>
<summary><strong>1. Role split from <code>adversarial-reviewer</code> — a distinct agent, not a mode flag</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) New `pr-reviewer` agent, `adversarial-reviewer` unchanged** | Each agent keeps one clear job (pre-push brief review vs. post-publish PR review); no risk of `adversarial-reviewer`'s existing behaviour regressing for the workers and orchestrator that already depend on it | One more `.md` file, one more row everywhere agents are enumerated |
| **B) Add a `mode: pr` flag to `adversarial-reviewer`** | No new file | `adversarial-reviewer` is spawned by `issue-worker` too (self-review, `require-specialists` hook); a mode flag would need every caller, hook and doc to disambiguate which mode ran, and the two jobs read different inputs (a brief's acceptance criteria vs. a linked issue's, `.coderabbit.yaml` vs. nothing) — the issue explicitly rejects this (Alternatives Considered, Alternative 1) |
| **C) Reuse CodeRabbit's own GitHub Action locally** | Zero new agent code | CodeRabbit is the exact service that is unavailable; running its own action does not remove the dependency (issue Alternative 3, same problem one layer down) |

### Chosen Option: **A — new `pr-reviewer` agent**

### Rationale

- `adversarial-reviewer`'s prompt, inputs (a PR number *or a branch and base*, `SPEC-NNN`) and known-failure-pattern list are all shaped around reviewing a diff against a brief or a spec before it is pushed; retrofitting a "review a published PR against a linked issue and `.coderabbit.yaml`" mode onto it risks the existing self-review path (`issue-worker` Method step 2, the `require-specialists` hook) picking up the wrong prompt section.
- The maintainer's own issue body states the constraint directly: "read-only and strictly scoped to prevent conflicts with `adversarial-reviewer` (pre-push) and other hooks."
- A dedicated agent gets its own hook wiring (spawn allowlist, path ownership, publish block) instead of a conditional inside `adversarial-reviewer`'s, which keeps every hook's regex simple and keeps `Test-Hooks.ps1` cases attributable to one agent name.

</details>

<details>
<summary><strong>2. Output artifact — one Markdown file per PR under <code>artifacts/pr-review/</code>, never posted by the agent itself</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `artifacts/pr-review/<pr>.md`, agent never publishes** | Matches the existing pattern (`site-steward` → `artifacts/site-health/**`, the SPEC-003 stage agents → `artifacts/knowledge/stages/**`); `block-worker-publish` already exists to enforce "never publishes" mechanically; the orchestrator controls exactly when and how the review reaches GitHub | The orchestrator has one more manual step (post the file) instead of a fully automatic loop — acceptable for Phase 1 (#1448 automates detection later, not posting) |
| **B) Agent posts `gh pr review --comment --body-file` itself** | One fewer orchestrator step | Every writing/reviewing agent in this repository is barred from publishing (`block-worker-publish`, `AI-DEVELOPMENT-MODEL.md` §4, INV-006 of SPEC-000); giving `pr-reviewer` a publish exception breaks that invariant for no strong reason, since the orchestrator already watches PR events and can post in the same turn it reads the report |
| **C) Return the review only as the agent's final message, no file** | Nothing to clean up | Read-only agents already write a findings artifact (`adversarial-reviewer` is the one exception, but it reports fewer than 10 findings typically inline); a PR-level review can be long, and a file survives session boundaries the way `SubagentHandback` text does not once the orchestrator's context rolls over |

### Chosen Option: **A — `artifacts/pr-review/<pr>.md`, read-only agent, orchestrator publishes**

### Rationale

- Reuses the exact enforcement the repository already has for "this agent writes only its own report": `enforce-path-ownership.ps1` gets one more `elseif ($Agent -eq 'pr-reviewer')` branch (Phase 2), the same shape as its existing `site-steward` branch.
- `block-worker-publish.ps1` is wired unconditionally by agent name in frontmatter (`issue-worker`, `mechanical-fixer`, `docs-writer`, `docs-reviewer`, `site-steward`); adding `pr-reviewer` to that same hook's frontmatter wiring costs one line and needs no new logic in the hook itself, since it already reads `agent_type` from the payload only to word its message.
- File sections, fixed by the issue body and repeated here verbatim so the agent's own prompt and the orchestrator's posting step agree on the contract:
  (a) a short summary and walkthrough of the diff;
  (b) findings with `file:line`, severity (`blocker`/`major`/`minor`/`nit`), a suggested fix and a verification note;
  (c) a linked-issue check: each acceptance criterion marked `met` / `not met` / `not verifiable`;
  (d) a security section: secrets, personal data in logs or messages, injection, fail-closed gates, the specific AGENTS.md §3 rules (`TimeProvider`, `[JsonIgnore]`, async DB calls, no swallowed errors, fail-closed gates, `EncinaError.Message` never logged);
  (e) lessons for the pipeline — patterns worth adding to `.claude/agents/lessons/pr-reviewer.md` or worth an issue of their own.
- The orchestrator posts the whole file as one PR review comment (`gh pr review --comment --body-file artifacts/pr-review/<pr>.md`) and adds inline comments only for findings that carry an exact `file:line` the GitHub review API can anchor to — the same distinction CodeRabbit itself draws between its summary comment and its inline threads.

</details>

<details>
<summary><strong>3. Model routing — Sonnet by default, Opus only when the orchestrator names the reason in the spawn</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Sonnet default; orchestrator passes `model: opus` with a stated reason** | Matches every other reviewing/implementing agent in the roster (`issue-worker`, `adversarial-reviewer`); keeps the common case cheap | The orchestrator must remember to escalate for the rare heavy case — same trade-off already accepted for `adversarial-reviewer` |
| **B) Always Sonnet** | Simplest | A genuinely security- or personal-data-touching PR, or an unusually large diff, deserves the deeper reasoning `adversarial-reviewer`'s Opus escalation already gets for the same reasons; refusing it here would make `pr-reviewer` strictly weaker than its sibling for the highest-risk PRs |
| **C) Always Opus** | Maximum thoroughness on every PR | Directly contradicts `CLAUDE.md` "Model routing" ("Opus only when the brief states why") and the cost discipline the whole delegation model (#1181) is built on; most merged PRs are small and do not need it |

### Chosen Option: **A — Sonnet default, Opus by exception**

### Rationale

- `.claude/agents/README.md`'s own model-tier table already states the rule generally: "Opus only when a brief or a review request states why." `pr-reviewer` inherits it verbatim, worded the same way `adversarial-reviewer`'s file does: *"The orchestrator passes `model: opus` in the spawn only for a pull request that touches security or personal data, or an unusually large diff, and says which in the prompt."*
- `effort: high` regardless of model tier (matches `adversarial-reviewer`): a review task benefits from higher reasoning effort even on Sonnet; the model swap, not the effort level, is the cost lever.

</details>

<details>
<summary><strong>4. Hook wiring — extend the existing per-agent hook pattern, no new hook script</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add `pr-reviewer` as one more branch/entry in the four existing hooks** (`block-worker-spawn`, `block-worker-publish`, `enforce-path-ownership`, `no-background-specialists`) | Zero new hook files; every hook's own regression-test file (`Test-Hooks.ps1`) already has the harness to add cases the same shape as `site-steward`'s; the four hooks already generalise over "agent name" via the `-Agent` frontmatter parameter and `agent_type` payload field | The four hook files grow by a handful of lines each, and each is a shared hot spot other in-flight agent work could also be touching |
| **B) A dedicated `pr-reviewer`-only hook script** | Isolated diff | Duplicates logic `enforce-path-ownership.ps1` and `block-worker-spawn.ps1` already generalise correctly; the project's own hook README explicitly frames per-agent rules as branches inside shared hooks, not one hook per agent (only `audit-stage-guard.ps1` is dedicated, and that is because its rule — which SPEC-003 stage runs next — has no analogue elsewhere) |

### Chosen Option: **A — extend the four existing hooks**

### Rationale

- `block-worker-spawn.ps1`: add `'pr-reviewer' = @('Explore')` to the `$allowlists` hashtable (empty-allowlist agents like `adversarial-reviewer` get "blocked: cannot spawn any subagent"; `pr-reviewer` needs exactly `Explore` per the issue's constraint, so it is not empty — same shape as `mechanical-fixer`'s `@('ci-diagnoser', 'Explore')` but with only `Explore`, since `pr-reviewer` never diagnoses CI failures, it reviews).
- `block-worker-publish.ps1`: add `pr-reviewer` to the agents whose frontmatter wires this hook (no code change inside the hook itself — it already words its message from `agent_type`).
- `enforce-path-ownership.ps1`: add an `elseif ($Agent -eq 'pr-reviewer')` branch immediately after the existing `site-steward` branch, denying anything outside `^artifacts/pr-review/` the same way `site-steward` is denied anything outside `^artifacts/site-health/`. No new `Get-PathCategory` category is needed — like `site-steward`, this is an agent-name branch, not a path-category rule, because no other agent needs to reason about `artifacts/pr-review/` paths.
- `no-background-specialists.ps1`: add `-Agent pr-reviewer` to its frontmatter wiring (the hook's own logic is already agent-name-agnostic; it only needs the frontmatter matcher present so the rule applies while `pr-reviewer` runs).
- `require-specialists.ps1` needs **no change**: it only gates `issue-worker` and `docs-writer` stopping without a specialist; `pr-reviewer` is itself a specialist a `Stop` for it is never checked against.
- `audit-stage-guard.ps1` needs **no change**: it only governs the five SPEC-003 audit-stage agents.

</details>

<details>
<summary><strong>5. <code>pr-cycle</code> skill integration — one manual "CodeRabbit unavailable" branch, not automatic detection</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Add a manual step to `pr-cycle` §2's reaction table; leave automatic detection to #1448** | Matches the epic's own phase split (#1447 builds the agent, #1448 builds the automatic BOT-UNAVAILABLE detector); ships the reviewer now without waiting on the detector | The orchestrator still decides "CodeRabbit is unavailable" by reading the same signal it already reads today (a rate-limit comment, or no CodeRabbit review after the usual wait) — no regression, since that is exactly what happened manually on 2026-09-26 |
| **B) Block on #1448 landing first** | One combined, fully automatic change | Directly reopens the problem the issue exists to solve: CodeRabbit has been failing since 2026-09-26 and #1448 is a separate, unscheduled issue; the epic explicitly orders #1447 before #1448 |

### Chosen Option: **A — manual step now, wired for #1448 to replace later**

### Rationale

- `pr-cycle` §2 already has a row for "CodeRabbit 'Review rate limited' → run `adversarial-reviewer` on the PR instead." This plan replaces that row's *action* with "spawn `pr-reviewer`", keeps the same *trigger* (a CodeRabbit rate-limit comment, or no review after the normal wait), and keeps `adversarial-reviewer` available for its own unrelated trigger ("a PR that touches gates, CI workflows or `.github/scripts`"), so the change is additive rather than a removal.
- Posting step, spelled out in `pr-cycle` so a future reader does not have to infer it from the agent definition: `gh pr review --repo dlrivada/Encina <n> --comment --body-file artifacts/pr-review/<n>.md`, then inline comments (`gh api .../pulls/<n>/comments`) only for findings with an exact `file:line`.

</details>

<details>
<summary><strong>6. Documentation updates — delegate to <code>docs-writer</code>, table rows only</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `docs-writer` adds one row to each of two existing tables** (`docs/engineering/AI-DEVELOPMENT-MODEL.md`'s agent list if present, `docs/engineering/ai-task-routing.md`'s "Claude subagent tiers" table at line 68-75) and keeps them verbatim-in-sync with `.claude/agents/README.md` as the file's own header promises | Small, targeted diff; follows the existing "kept verbatim in sync" contract the file states about itself | None significant |
| **B) A new standalone page under `docs/`** | More discoverable | The issue does not ask for one, and the existing pattern for a new agent (`site-steward`, added the same way in #1382) is a table row, not a new page |

### Chosen Option: **A — table rows via `docs-writer`**

### Rationale

- `ai-task-routing.md` line 66 states the table is "kept verbatim in sync with `.claude/agents/README.md`"; `docs-writer` is the specialist for documentation edits (`enforce-path-ownership.ps1` already denies an `issue-worker`/`mechanical-fixer` write to this path), so it drafts and commits the matching row once `.claude/agents/README.md`'s own row text is final.
- No `AI-DEVELOPMENT-MODEL.md` table currently enumerates individual agents by name (its §4 talks about the model in prose, e.g. the "Self-review before hand-off" paragraph at line 802); the implementer should grep for the exact roster location before assuming a table exists there, and if none does, add one short paragraph next to the existing self-review paragraph instead of inventing a table — flagged as a research step in Phase 5, not decided here, since it depends on the file's exact current shape at implementation time.

</details>

---

## Implementation Phases

### Phase 1: Agent definition

> **Goal**: Create `.claude/agents/pr-reviewer.md` and its lessons file.

<details>
<summary><strong>Tasks</strong></summary>

1. **`.claude/agents/pr-reviewer.md`** (new), frontmatter modelled on `.claude/agents/site-steward.md` (same three hook matchers: `Bash|PowerShell`, `Write|Edit|MultiEdit|NotebookEdit`, `Agent`) with `pr-reviewer`'s own tool list:
   ```yaml
   ---
   name: pr-reviewer
   description: CodeRabbit-style review of a published pull request against AGENTS.md, CLAUDE.md, the path_instructions of .coderabbit.yaml and its linked issue's acceptance criteria. Fallback for when CodeRabbit is unavailable or rate-limited. Read-only on the repository except its own artifacts/pr-review/<pr>.md.
   model: sonnet
   effort: high
   tools: Agent(Explore), Bash, PowerShell, Read, Grep, Glob, Write, Edit
   maxTurns: 60
   color: orange
   hooks:
     PreToolUse:
       - matcher: "Bash|PowerShell"
         hooks:
           - type: command
             command: 'pwsh -NoProfile -File "$CLAUDE_PROJECT_DIR/.claude/hooks/block-worker-publish.ps1"'
           - type: command
             command: 'pwsh -NoProfile -File "$CLAUDE_PROJECT_DIR/.claude/hooks/block-prohibited-commands.ps1"'
           - type: command
             command: 'pwsh -NoProfile -File "$CLAUDE_PROJECT_DIR/.claude/hooks/no-background-specialists.ps1" -Agent pr-reviewer'
       - matcher: "Write|Edit|MultiEdit|NotebookEdit"
         hooks:
           - type: command
             command: 'pwsh -NoProfile -File "$CLAUDE_PROJECT_DIR/.claude/hooks/enforce-path-ownership.ps1" -Agent pr-reviewer'
       - matcher: "Agent"
         hooks:
           - type: command
             command: 'pwsh -NoProfile -File "$CLAUDE_PROJECT_DIR/.claude/hooks/block-worker-spawn.ps1" -Agent pr-reviewer'
           - type: command
             command: 'pwsh -NoProfile -File "$CLAUDE_PROJECT_DIR/.claude/hooks/no-background-specialists.ps1" -Agent pr-reviewer'
   ---
   ```
   `Write, Edit` are present only to let it create `artifacts/pr-review/<pr>.md`; `enforce-path-ownership.ps1` (Phase 2) denies every other target, so the tool grant is safe by construction rather than by the agent's own discipline.

2. **Body**, written to match the house style of `adversarial-reviewer.md`/`site-steward.md` (role statement, model-routing note, inputs, tooling rules, numbered method, "never work around a hook" paragraph verbatim from `AGENTS.md`/other agent files, output contract):
   - **Role**: reviews a *published* PR, distinct from `adversarial-reviewer`'s pre-push brief review (state the distinction explicitly, per Design Choice 1, so a future reader is not tempted to merge the two).
   - **Model**: Sonnet by default; the orchestrator passes `model: opus` only for a PR touching security or personal data, or an unusually large diff, and says which in the prompt (Design Choice 3).
   - **Inputs**: a PR number; reads `gh pr view <n> --json title,body,baseRefName` for the linked issue (`Fixes #N` / `Closes #N` in the body), `gh pr diff <n>` for the diff, `AGENTS.md`, `CLAUDE.md`, `.coderabbit.yaml` (`path_instructions`, `tone_instructions`).
   - **Tooling rules**: PowerShell/CLI only, per `AGENTS.md` §2 (no python, no bash constructs, no `grep`/`sed`/`head`/`tail`).
   - **Method**:
     1. Read the PR (`gh pr view`, `gh pr diff`), the linked issue and its acceptance criteria, `AGENTS.md`, `CLAUDE.md`, `.coderabbit.yaml`.
     2. For each changed file, match its path against `.coderabbit.yaml`'s `path_instructions` (`src/**/*.cs`, `tests/**/*.cs`, `**/*.md`) and apply those instructions plus the matching `AGENTS.md` rules (§3 code rules, §5 provider coherence, §6 cross-cutting check, §7 EventIds, §9 testing obligations as applicable).
     3. Check every acceptance criterion of the linked issue against the diff: `met` (name the file/line evidence), `not met`, or `not verifiable` (state what evidence is missing).
     4. Write the security section: `EncinaError.Message` leaking into logs/traces/health checks, options classes with secrets missing `[JsonIgnore]`/`ToString()` override, synchronous DB calls, swallowed `Left` results in background infrastructure, fail-open gates — the same "known failure patterns" list `adversarial-reviewer.md` already carries, reused verbatim since the underlying rules are the same AGENTS.md §3 rules.
     5. Spawn `Explore` (the only agent it may spawn) for read-only cross-file research when a claim needs checking beyond the diff itself (e.g. "is this the only caller of this method").
     6. Write `artifacts/pr-review/<pr>.md` with the five sections from Design Choice 2 (summary/walkthrough, findings, linked-issue check, security, lessons).
     7. Report to the orchestrator: the file path, a one-line verdict (merge / merge after fixes / do not merge, same three-way verdict `adversarial-reviewer` already uses so the orchestrator's downstream handling is identical), and any pattern worth adding to its own lessons file.
   - **Never work around a hook** paragraph, copied verbatim from `adversarial-reviewer.md`/other agent files (the project's standard wording, not reworded per agent).

3. **`.claude/agents/lessons/pr-reviewer.md`** (new): the one-line placeholder used by every other lessons file today (`# Lessons for pr-reviewer` + `No lessons yet.`), read by the agent at the start of every run (its own prompt says so) and appended to by hand by the orchestrator after a run surfaces a durable pattern — no automated pipeline script is added for this (unlike the SPEC-003 stage agents' `tools/ai/audit/audit-lessons.ps1`), since `pr-reviewer` is not part of that fixed six-stage pipeline; automating it is a candidate follow-up, not part of this issue's acceptance criteria.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of the pr-reviewer agent (Issue #1447).

CONTEXT:
- Encina's Claude Code agents live in .claude/agents/*.md, one Markdown file per agent: YAML frontmatter
  (name, description, model, effort, tools, maxTurns, color, hooks) then a body written in second person
  ("You are...", "You never...").
- pr-reviewer reviews a PUBLISHED pull request (gh pr diff) against its linked issue's acceptance criteria
  and the repository's rules (AGENTS.md, CLAUDE.md, .coderabbit.yaml path_instructions). It is distinct from
  adversarial-reviewer, which reviews a pre-push diff against a closed brief.
- It is read-only on the repository except artifacts/pr-review/<pr>.md, may spawn only Explore, and never
  publishes to GitHub (the orchestrator posts its report as a PR review).

TASK:
Create .claude/agents/pr-reviewer.md and .claude/agents/lessons/pr-reviewer.md.

KEY RULES:
- Frontmatter hooks: three matchers, same shape as .claude/agents/site-steward.md (Bash|PowerShell,
  Write|Edit|MultiEdit|NotebookEdit, Agent), each running block-worker-publish.ps1,
  block-prohibited-commands.ps1, no-background-specialists.ps1 -Agent pr-reviewer (on Bash|PowerShell),
  enforce-path-ownership.ps1 -Agent pr-reviewer (on the write matcher), block-worker-spawn.ps1 -Agent
  pr-reviewer and no-background-specialists.ps1 -Agent pr-reviewer (on Agent). Do not invent a new hook.
- tools: Agent(Explore), Bash, PowerShell, Read, Grep, Glob, Write, Edit — Write/Edit exist only to produce
  its own artifact; enforce-path-ownership.ps1 (added in Phase 2) is what actually restricts the target, not
  agent self-discipline.
- model: sonnet, effort: high (matches adversarial-reviewer's default tier; Opus is an orchestrator-time
  override, never the agent's own default).
- Body sections, in this order: role and distinction from adversarial-reviewer; model-routing note; inputs
  (gh pr view/diff, the linked issue, AGENTS.md, CLAUDE.md, .coderabbit.yaml); tooling rules (AGENTS.md §2,
  PowerShell/CLI only); the seven-step method from the plan's Phase 1 Tasks (read inputs, match
  path_instructions per file, check acceptance criteria, write the security section using the same known
  failure patterns adversarial-reviewer.md lists, spawn Explore only when needed, write the artifact, report
  verdict); the "never work around a hook" paragraph, copied verbatim from another agent file in this
  repository (do not reword it).
- Output contract for artifacts/pr-review/<pr>.md: five sections — (a) summary/walkthrough, (b) findings
  with file:line, severity (blocker/major/minor/nit), suggested fix, verification note, (c) linked-issue
  acceptance-criteria check (met/not met/not verifiable), (d) security section, (e) lessons for the pipeline.
  State this contract in the agent's own body so it is self-contained without re-reading this plan.
- lessons/pr-reviewer.md: exactly the placeholder shape of the other five files in .claude/agents/lessons/
  ("# Lessons for pr-reviewer" heading, "No lessons yet." body).

REFERENCE FILES:
- .claude/agents/adversarial-reviewer.md (closest sibling: read-only reviewer, verdict format, known-failure-
  patterns list, "never work around a hook" wording)
- .claude/agents/site-steward.md (closest sibling for hook-wiring shape: three matchers, own artifacts/
  subfolder, publish-blocked)
- .claude/agents/README.md (roster table row format, model-tier rationale)
- .claude/agents/lessons/issue-auditor.md (placeholder lessons file shape)
- .coderabbit.yaml (path_instructions and tone_instructions this agent must apply per file)
```

</details>

---

### Phase 2: Hook wiring

> **Goal**: Enforce the agent's constraints mechanically (read-only, spawn allowlist, no publishing, no background specialists), the same way every other agent's constraints are enforced.

<details>
<summary><strong>Tasks</strong></summary>

1. **`.claude/hooks/block-worker-spawn.ps1`**: add `'pr-reviewer' = @('Explore')` to the `$allowlists` hashtable (near `site-steward`'s single-entry `@('ci-diagnoser')`); update the header comment's per-caller list to add the `pr-reviewer` row.
2. **`.claude/hooks/block-worker-publish.ps1`**: no code change (it already words its message from `agent_type`/`-Agent`); document in its header comment that `pr-reviewer`'s frontmatter now wires it too, alongside `issue-worker`, `mechanical-fixer`, `docs-writer`, `docs-reviewer`, `site-steward`.
3. **`.claude/hooks/enforce-path-ownership.ps1`**: add an `elseif ($Agent -eq 'pr-reviewer')` branch right after the existing `site-steward` branch (around line 293-298), denying any relative path not matching `^artifacts/pr-review/` with a message in the same shape as `site-steward`'s ("pr-reviewer writes only under artifacts/pr-review/** ... it is read-only on the rest of the repository"); update the header comment's per-agent list.
4. **`.claude/hooks/no-background-specialists.ps1`**: no code change; its header comment already generalises "every agent that may itself delegate or run shell commands" — add `pr-reviewer` to the named list in the comment for accuracy, since it is now wired in that agent's own frontmatter (Phase 1).
5. **`.claude/agents/README.md`**: add `pr-reviewer` to the roster table (model/effort, role, "Writes?" = "Yes (only `artifacts/pr-review/**`, never pushes/comments/opens)" — the same phrasing `site-steward`'s row already uses), to the Delegation table ("Fallback CodeRabbit-style review of a published PR | `pr-reviewer` | orchestrator"), and update the three hook-table rows (`block-worker-spawn.ps1`, `enforce-path-ownership.ps1`, the frontmatter-hooks table for `block-worker-publish.ps1` and `no-background-specialists.ps1`) to name it alongside `site-steward`.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of the pr-reviewer agent (Issue #1447).

CONTEXT:
- Phase 1 created .claude/agents/pr-reviewer.md with frontmatter that wires block-worker-publish.ps1,
  block-prohibited-commands.ps1, no-background-specialists.ps1 -Agent pr-reviewer,
  enforce-path-ownership.ps1 -Agent pr-reviewer and block-worker-spawn.ps1 -Agent pr-reviewer.
- Four shared hooks generalise their rule over an agent name via a -Agent frontmatter parameter and the
  payload's agent_type field: block-worker-spawn.ps1 (a hashtable of allowlists), enforce-path-ownership.ps1
  (a chain of elseif branches per agent), block-worker-publish.ps1 and no-background-specialists.ps1 (already
  fully agent-name-agnostic; they only need the frontmatter matcher present).

TASK:
Wire pr-reviewer into the four hooks exactly as site-steward is wired, adjusted to its own rule
(spawn allowlist = Explore only; write allowlist = artifacts/pr-review/** only), and update
.claude/agents/README.md's roster, delegation and hooks tables.

KEY RULES:
- block-worker-spawn.ps1: add 'pr-reviewer' = @('Explore') to $allowlists (near site-steward's
  'site-steward' = @('ci-diagnoser')). Update the header comment's per-caller list.
- enforce-path-ownership.ps1: add an elseif ($Agent -eq 'pr-reviewer') branch after the existing
  site-steward branch (same file, look for "elseif ($Agent -eq 'site-steward')"), same shape: if
  ($relative -notmatch '^artifacts/pr-review/') { write the Console.Error message; return $false }. Update
  the header comment.
- block-worker-publish.ps1 and no-background-specialists.ps1: no logic change; only update each file's own
  header comment to list pr-reviewer among the agents whose frontmatter wires it.
- .claude/agents/README.md: new roster row (model Sonnet 5 / high, Opus only when the orchestrator's spawn
  states why for security/personal-data/large-diff PRs; role one sentence; Writes? = "Yes (only
  artifacts/pr-review/**, never pushes/comments/opens)"); new Delegation-table row ("Fallback CodeRabbit-
  style review of a published PR" | pr-reviewer | orchestrator); update the block-worker-spawn.ps1,
  enforce-path-ownership.ps1 and frontmatter-hooks-table rows to mention pr-reviewer the same way they
  already mention site-steward.
- Do not touch require-specialists.ps1 or audit-stage-guard.ps1 — neither governs pr-reviewer.

REFERENCE FILES:
- .claude/hooks/block-worker-spawn.ps1 (the $allowlists hashtable and its header comment)
- .claude/hooks/enforce-path-ownership.ps1 (the site-steward elseif branch, around "elseif ($Agent -eq
  'site-steward')")
- .claude/hooks/block-worker-publish.ps1, .claude/hooks/no-background-specialists.ps1 (header comments only)
- .claude/agents/README.md (roster table, Delegation table, Hooks table, frontmatter-hooks table)
```

</details>

---

### Phase 3: `Test-Hooks.ps1` regression cases

> **Goal**: Prove the Phase 2 wiring mechanically, the same way every other agent's wiring is proven.

<details>
<summary><strong>Tasks</strong></summary>

Add cases to `.claude/hooks/tests/Test-Hooks.ps1`, following the exact shape of the existing `site-steward` cases (lines 267-271, 288, 667-673):

1. **Spawn allowlist** (near the `block-worker-spawn` test block):
   - `pr-reviewer` spawning `Explore` → `0` (allowed).
   - `pr-reviewer` spawning `mechanical-fixer` → `2` (blocked).
   - `pr-reviewer` spawning `general-purpose` → `2` (blocked).
   - `pr-reviewer` spawning nothing (`$null` `subagent_type`) → `2` (blocked).
   - `orchestrator` spawning `pr-reviewer` → `0` (allowed).
2. **Path ownership** (near the `enforce-path-ownership` test block, using the same `$wt` worktree fixture the `site-steward` cases use):
   - `pr-reviewer` `Write` to `$wt\artifacts\pr-review\1447.md` → `0`.
   - `pr-reviewer` `Edit` to `$wt\src\Encina\X.cs` → `2` (a repo source file is denied).
   - `pr-reviewer` `Edit` to `$wt\docs\en\guide.md` → `2` (documentation is denied).
   - `pr-reviewer` `Write` to `$wt\artifacts\site-health\report.md` → `2` (another agent's `artifacts/` subfolder is denied).
3. **Publish block**: reuse the existing parametrised `block-worker-publish` test block by adding `pr-reviewer` to whichever loop already iterates `issue-worker`, `mechanical-fixer`, `docs-writer`, `docs-reviewer`, `site-steward` for the "never runs `gh pr create`/`gh issue create`/`git push`" cases — do not hand-write new cases if the block is already parametrised over agent name; only add the new value to the existing list.
4. **No-background-specialists**: same treatment — add `pr-reviewer` to the existing parametrised list for "a specialist may not run a background Agent/Bash call" if the block already iterates agent names; otherwise add the minimal pair of cases (`pr-reviewer` spawning `Explore` with `run_in_background: true` → `2`; a Bash call with `run_in_background: true` → `2`).

Run `pwsh -NoProfile -File .claude/hooks/tests/Test-Hooks.ps1` from the worktree (not the main checkout, per the known heuristic-trip issue #1368) and confirm 0 failures.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of the pr-reviewer agent (Issue #1447).

CONTEXT:
- Phase 2 wired pr-reviewer into block-worker-spawn.ps1 (allowlist: Explore only) and
  enforce-path-ownership.ps1 (write allowlist: artifacts/pr-review/** only).
- .claude/hooks/tests/Test-Hooks.ps1 already has site-steward cases in exactly the shape needed here: lines
  267-271 (spawn allowlist), line 288 (orchestrator may spawn site-steward), lines 667-673 (path ownership,
  using a $wt worktree fixture already defined earlier in the file).

TASK:
Add pr-reviewer cases to Test-Hooks.ps1 for its spawn allowlist and its path-ownership rule, following the
exact array-row shape the site-steward cases already use (read them first). Also add pr-reviewer to any
already-parametrised block-worker-publish.ps1 / no-background-specialists.ps1 test loops that iterate agent
names, rather than duplicating whole new blocks.

KEY RULES:
- Spawn cases (5): pr-reviewer -> Explore (0, allowed), pr-reviewer -> mechanical-fixer (2, blocked),
  pr-reviewer -> general-purpose (2, blocked), pr-reviewer -> $null subagent_type (2, blocked), orchestrator
  -> pr-reviewer (0, allowed).
- Path-ownership cases (at least 4): Write to $wt\artifacts\pr-review\1447.md (0), Edit to
  $wt\src\Encina\X.cs (2), Edit to $wt\docs\en\guide.md (2), Write to $wt\artifacts\site-health\report.md
  (2, another agent's subfolder).
- Every new row needs a short descriptive label as the last array element (see the site-steward rows for the
  wording pattern: "pr-reviewer: X is allowed" / "pr-reviewer: Y is denied").
- Run pwsh -NoProfile -File .claude/hooks/tests/Test-Hooks.ps1 from the WORKTREE (not the main checkout:
  #1368 means the suite is currently blocked for the main checkout by its own text heuristic) and confirm
  every case passes, new and old.

REFERENCE FILES:
- .claude/hooks/tests/Test-Hooks.ps1 (lines 267-271, 288, 667-673 for the exact row shape to copy)
```

</details>

---

### Phase 4: `pr-cycle` skill integration

> **Goal**: Give the orchestrator the manual trigger and posting steps.

<details>
<summary><strong>Tasks</strong></summary>

1. In `.claude/skills/pr-cycle/SKILL.md` §2's reaction table, replace the row:
   > `CodeRabbit "Review rate limited"` → `Do not wait for it. Run adversarial-reviewer on the PR instead, and record in the PR that the external review was skipped.`

   with:
   > `CodeRabbit "Review rate limited" (or no review posted after the normal wait)` → `Do not wait for it. Spawn pr-reviewer with the PR number in the foreground. Post its artifacts/pr-review/<n>.md as one PR review (gh pr review --repo dlrivada/Encina <n> --comment --body-file artifacts/pr-review/<n>.md) and add inline comments only for findings with an exact file:line. Record in the PR that CodeRabbit was skipped. A PR that also touches gates, CI workflows or .github/scripts still gets an adversarial-reviewer pass too (existing rule, unchanged).`

2. Keep the existing sentence in §2 ("A PR that touches gates, CI workflows or `.github/scripts`, or that merges without a CodeRabbit review, gets an `adversarial-reviewer` pass before or right after the merge.") — it is a second, independent trigger for `adversarial-reviewer` and stays true regardless of this change (a PR reviewed by `pr-reviewer` because CodeRabbit was down *is* "a PR that merges without a CodeRabbit review", so both agents may legitimately run on the same PR; that is not a conflict, since they check different things).
3. Cross-reference: `.claude/skills/pr-cycle/SKILL.md` should mention this replaces the manual step first improvised on PR #1408 (per epic #1453's "Related Issues": "#1408 - A PR reviewed by adversarial-reviewer because CodeRabbit was limited"), so a future reader understands why the row changed.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of the pr-reviewer agent (Issue #1447).

CONTEXT:
- .claude/skills/pr-cycle/SKILL.md drives every Encina PR from open to merge. Its section 2 ("Watch,
  event-driven") has a reaction table; one row currently sends CodeRabbit rate-limit events to
  adversarial-reviewer as a stop-gap (this was the manual step taken on PR #1408).
- pr-reviewer (Phases 1-3) now exists as the purpose-built replacement for that row.

TASK:
Update the CodeRabbit-rate-limited row in pr-cycle/SKILL.md's reaction table to trigger pr-reviewer instead,
and spell out the exact posting commands so a future reader does not have to infer them.

KEY RULES:
- New row action, verbatim shape: spawn pr-reviewer in the foreground with the PR number; post its
  artifacts/pr-review/<n>.md with `gh pr review --repo dlrivada/Encina <n> --comment --body-file
  artifacts/pr-review/<n>.md`; add inline comments only for findings with an exact file:line; record in the
  PR that CodeRabbit was skipped.
- Do NOT remove or weaken the separate, still-true sentence that a PR touching gates/CI/.github/scripts, or
  merging without a CodeRabbit review, still gets an adversarial-reviewer pass. Both agents can legitimately
  run on the same PR because they check different things (pre-push brief vs. published-PR/issue rules).
- Note in the skill (one sentence) that this replaces the manual step first improvised on PR #1408.
- This is a skill file (`.claude/skills/**/*.md`), not documentation under docs/ or a README: it is edited
  directly (mechanical-fixer or the orchestrator itself), not through docs-writer.

REFERENCE FILES:
- .claude/skills/pr-cycle/SKILL.md (section 2, the reaction table)
```

</details>

---

### Phase 5: Documentation — agent tables

> **Goal**: Keep `docs/engineering/ai-task-routing.md` and `docs/engineering/AI-DEVELOPMENT-MODEL.md` in sync with the new agent, via `docs-writer`.

<details>
<summary><strong>Tasks</strong></summary>

1. `docs/engineering/ai-task-routing.md`, "Claude subagent tiers" table (currently lines 68-75): add a `pr-reviewer` row between `adversarial-reviewer` and `issue-worker` (or wherever alphabetical/logical order the implementer finds the table already follows), text kept "verbatim in sync" with the new `.claude/agents/README.md` row per the file's own stated contract (line 66).
2. `docs/engineering/AI-DEVELOPMENT-MODEL.md`: research first whether an agent-enumerating table exists (the plan's Design Choice 6 flags that none was found at planning time, only prose mentions such as the "Self-review before hand-off" paragraph around line 802); if none exists, add one sentence next to that paragraph naming `pr-reviewer` as the fallback for a rate-limited CodeRabbit, with a pointer to `.claude/agents/pr-reviewer.md`. Do not invent a new table structure only for this row.
3. Both edits go through `docs-writer`, spawned in the foreground on the worktree with: the exact row text agreed for `.claude/agents/README.md` (Phase 2) and this plan's Design Choice 6 rationale, so `docs-writer` need not re-derive it.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of the pr-reviewer agent (Issue #1447) — documentation only.

CONTEXT:
- .claude/agents/README.md now has a pr-reviewer row (Phase 2): model Sonnet 5 / high (Opus only when the
  orchestrator's spawn states why), role = CodeRabbit-style review of a published PR against AGENTS.md,
  CLAUDE.md, .coderabbit.yaml path_instructions and its linked issue's acceptance criteria, Writes? = "Yes
  (only artifacts/pr-review/**, never pushes/comments/opens)".
- docs/engineering/ai-task-routing.md states at line 66 that its "Claude subagent tiers" table (lines 68-75)
  is "kept verbatim in sync with .claude/agents/README.md".

TASK:
Add the matching pr-reviewer row to ai-task-routing.md's Claude subagent tiers table. Check
AI-DEVELOPMENT-MODEL.md for an existing agent-enumerating table; if one exists, add a row there too in the
same shape; if none exists (only prose mentions of specific agents), add one sentence near the existing
"Self-review before hand-off" discussion naming pr-reviewer as the CodeRabbit fallback, pointing to
.claude/agents/pr-reviewer.md, without inventing a new table.

KEY RULES:
- Row text must genuinely match .claude/agents/README.md's row, not merely resemble it — copy the finished
  text from that file once Phase 2 lands, do not re-derive it independently.
- This is documentation under docs/: normal docs-writer rules apply (Diátaxis quadrant awareness is not
  relevant to a table-row edit, but linting and link-checking still run per the encina-docs skill).
- Do not touch .claude/agents/README.md itself (already done in Phase 2, and docs-writer's own path
  ownership denies .claude/** anyway).

REFERENCE FILES:
- docs/engineering/ai-task-routing.md (lines 55-77 for the exact table and its "kept verbatim in sync"
  contract)
- docs/engineering/AI-DEVELOPMENT-MODEL.md (around line 802, the existing self-review paragraph, as the
  fallback location if no table exists)
- .claude/agents/README.md (the finished pr-reviewer row, source of truth for the copy)
```

</details>

---

### Phase 6: Acceptance evidence — trial comparison against CodeRabbit

> **Goal**: Produce the evidence the issue's own acceptance criteria and the epic's Phase 3 trial (#1452) will need: run `pr-reviewer` against two already-merged, CodeRabbit-reviewed PRs and compare.

<details>
<summary><strong>Tasks</strong></summary>

1. Run `pr-reviewer` against **#1397** and **#1399** (both merged; `gh pr diff <n>` and `gh api repos/dlrivada/Encina/issues/<n>/comments` still work on a closed PR, so no special-casing is needed for "already merged").
2. Its own output goes to `artifacts/pr-review/1397.md` and `artifacts/pr-review/1399.md` as normal — these are reproducible, git-ignored working files, not the deliverable.
3. The deliverable is a comparison, written into `docs/knowledge/issues/1447.md` — the per-issue knowledge record the `issue-worker` closing #1447 writes anyway per `pr-cycle` §1 ("Record step", SPEC-003 REQ-031, DEC-005) — with one short table per compared PR: finding-by-finding, whether `pr-reviewer` found the same issue CodeRabbit did (or found nothing where CodeRabbit found nothing, confirmed by the `cr-1397.md`/`cr-1399.md`-style comment fetch this planning leg already used as a research step), missed a real CodeRabbit finding, or raised something CodeRabbit did not (a true addition, not necessarily a false positive — note which).
4. This comparison is evidence for #1452 (the one-week trial), not the trial itself; do not attempt to decide primary-vs-fallback status here — that decision is explicitly out of scope for #1447 (epic's own phase split).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 6</strong></summary>

```
You are implementing Phase 6 of the pr-reviewer agent (Issue #1447) — acceptance evidence.

CONTEXT:
- Phases 1-5 built and wired the pr-reviewer agent. This phase proves it works by running it against two
  real, already-merged, CodeRabbit-reviewed PRs and comparing outputs.
- #1397 and #1399 are both merged PRs whose CodeRabbit review is readable via
  `gh api repos/dlrivada/Encina/issues/<n>/comments` (CodeRabbit posts as an issue comment on the PR, not a
  PR "review" object — gh api repos/dlrivada/Encina/pulls/<n>/reviews returns an empty array for these).

TASK:
Spawn pr-reviewer on #1397 and on #1399 (foreground, one at a time). Compare each output against the
corresponding CodeRabbit comment. Write the comparison into docs/knowledge/issues/1447.md (the knowledge
record this issue's closing PR needs anyway per pr-cycle skill section 1's "Record step").

KEY RULES:
- pr-reviewer's own artifacts/pr-review/1397.md and artifacts/pr-review/1399.md are working files, not the
  deliverable; do not hand-edit them.
- The comparison table per PR: finding | pr-reviewer said | CodeRabbit said | agreement (same finding /
  pr-reviewer missed it / pr-reviewer found something CodeRabbit didn't). Note explicitly when both found
  nothing actionable (CodeRabbit's own words on #1397: "No actionable comments were generated").
- Do not decide whether pr-reviewer should become primary or stay a fallback — that is #1452's job, out of
  scope here. State only what was observed.
- docs/knowledge/issues/1447.md is written by the issue-worker per SPEC-003 DEC-005 (it holds facts and
  audit outcomes of its own diff) — do not delegate this file to docs-writer.

REFERENCE FILES:
- .claude/skills/pr-cycle/SKILL.md (section 1, "Record step")
- Any existing docs/knowledge/issues/*.md file for the expected shape of a knowledge record
```

</details>

---

## Research

### Standards and specifications

| Reference | Relevance |
|---|---|
| SPEC-000 (INV-006) | Agents never publish on their own; the orchestrator commits and pushes — enforced here via `block-worker-publish.ps1` |
| SPEC-003 (closed-issue audit) | Not directly used by `pr-reviewer` (it reviews open PRs, not closed issues), but Phase 6's knowledge-record obligation follows its DEC-005 |
| `.coderabbit.yaml` | The per-path rule source `pr-reviewer` must read and apply (`path_instructions` for `src/**/*.cs`, `tests/**/*.cs`, `**/*.md`; `tone_instructions`) |
| `AGENTS.md` §3 (Code rules), §6 (Cross-cutting check), §9 (Testing obligations) | The rule set `pr-reviewer` checks a diff against, alongside the linked issue |

### Existing Encina infrastructure to leverage

| Component | Location | Usage in this feature |
|---|---|---|
| `adversarial-reviewer` agent | `.claude/agents/adversarial-reviewer.md` | Closest sibling: verdict format, known-failure-patterns list, hook wiring style, "never work around a hook" wording — reused, not modified |
| `site-steward` agent | `.claude/agents/site-steward.md` | Closest sibling for the "own `artifacts/` subfolder, publish-blocked, read-only elsewhere" hook shape |
| `block-worker-spawn.ps1` | `.claude/hooks/block-worker-spawn.ps1` | Extended with a `pr-reviewer` → `@('Explore')` allowlist entry |
| `enforce-path-ownership.ps1` | `.claude/hooks/enforce-path-ownership.ps1` | Extended with a `pr-reviewer` branch restricting writes to `artifacts/pr-review/` |
| `block-worker-publish.ps1`, `no-background-specialists.ps1` | `.claude/hooks/*.ps1` | Already agent-name-agnostic; only need frontmatter wiring, no code change |
| `Test-Hooks.ps1` `site-steward` cases | `.claude/hooks/tests/Test-Hooks.ps1` lines 267-271, 288, 667-673 | Exact shape to copy for the new `pr-reviewer` cases |
| `pr-cycle` skill | `.claude/skills/pr-cycle/SKILL.md` §1 ("Record step"), §2 (reaction table) | The manual trigger row this plan edits, and the knowledge-record obligation Phase 6 fulfils |
| CodeRabbit's own comment shape | `gh api repos/dlrivada/Encina/issues/1397/comments` (fetched during planning) | The output shape `pr-reviewer`'s artifact deliberately echoes: walkthrough table, priority/merge-risk line, pre-merge-style checks (title/description/linked-issue/out-of-scope), so a side-by-side comparison in Phase 6 is easy |

### Event ID allocation

Not applicable — this is a Claude Code agent/hook feature with no `.NET` logging; nothing is added to `src/Encina/Diagnostics/EventIdRanges.cs`.

### Estimated file count by category

| Category | Files | Notes |
|---|---|---|
| New agent definitions | 2 | `.claude/agents/pr-reviewer.md`, `.claude/agents/lessons/pr-reviewer.md` |
| Hook edits | 4 | `block-worker-spawn.ps1`, `block-worker-publish.ps1` (comment only), `enforce-path-ownership.ps1`, `no-background-specialists.ps1` (comment only) |
| Hook regression tests | 1 | `Test-Hooks.ps1` (~10 new cases) |
| Roster/skill docs | 2 | `.claude/agents/README.md`, `.claude/skills/pr-cycle/SKILL.md` |
| Documentation | 1-2 | `docs/engineering/ai-task-routing.md`, possibly `docs/engineering/AI-DEVELOPMENT-MODEL.md` |
| Evidence / knowledge record | 1 | `docs/knowledge/issues/1447.md` (written when the implementation PR closes the issue, per DEC-005) |
| Runtime artifacts (git-ignored) | up to 4 | `artifacts/pr-review/{1397,1399,<own-pr>}.md` |

---

## Combined AI Agent Prompts

<details>
<summary><strong>Combined prompt — all phases</strong></summary>

```
PROJECT CONTEXT:
Encina is a pre-1.0 .NET 10 library. Its Claude Code orchestration lives entirely under .claude/ (agents,
hooks, skills) and is governed by AGENTS.md/CLAUDE.md. You are implementing Issue #1447: a new read-only
subagent, pr-reviewer, that reviews an already-PUBLISHED pull request (gh pr diff) against its linked
issue's acceptance criteria (Fixes #N) and the repository's rules (AGENTS.md, CLAUDE.md, the
path_instructions of .coderabbit.yaml), as a fallback for when CodeRabbit is rate-limited (it has been since
2026-09-26). It is distinct from the existing adversarial-reviewer, which reviews a PRE-PUSH diff against a
closed worker brief — do not merge the two roles or files.

IMPLEMENTATION OVERVIEW (6 phases):
1. Agent definition: .claude/agents/pr-reviewer.md (frontmatter modelled on site-steward.md's three-matcher
   hook shape; body modelled on adversarial-reviewer.md's role/method/verdict/known-failure-patterns shape)
   plus .claude/agents/lessons/pr-reviewer.md (placeholder, same shape as the other five lessons files).
2. Hook wiring: block-worker-spawn.ps1 gets a pr-reviewer -> @('Explore') allowlist entry;
   enforce-path-ownership.ps1 gets a pr-reviewer branch restricting writes to artifacts/pr-review/**;
   block-worker-publish.ps1 and no-background-specialists.ps1 need only their header comments updated (their
   logic already generalises by agent name, and Phase 1's frontmatter is what actually wires them in).
   .claude/agents/README.md gets a new roster row, a new Delegation-table row, and updated hook-table rows.
3. Test-Hooks.ps1: ~10 new cases in the exact shape of the existing site-steward cases (lines 267-271, 288,
   667-673) proving the spawn allowlist and path-ownership rule; run the suite from the WORKTREE (not the
   main checkout, #1368) and confirm 0 failures.
4. pr-cycle skill: replace the "CodeRabbit rate limited -> run adversarial-reviewer" row with "-> spawn
   pr-reviewer, then post its artifacts/pr-review/<n>.md as a PR review with inline comments for exact-line
   findings"; keep the separate, still-true "gates/CI/.github/scripts PR -> adversarial-reviewer" rule
   unchanged, since both agents can legitimately run on the same PR.
5. Documentation: docs-writer adds a matching row to docs/engineering/ai-task-routing.md's "Claude subagent
   tiers" table (stated to be "kept verbatim in sync with .claude/agents/README.md") and, if
   AI-DEVELOPMENT-MODEL.md has no agent-enumerating table, one sentence near its existing "Self-review before
   hand-off" paragraph instead of inventing a table.
6. Acceptance evidence: run pr-reviewer on merged PRs #1397 and #1399, compare its findings against the
   CodeRabbit comment already on each PR (fetched via gh api repos/dlrivada/Encina/issues/<n>/comments, since
   CodeRabbit posts as an issue comment, not a PR "review" object), and write the comparison into
   docs/knowledge/issues/1447.md (the knowledge record the closing PR needs anyway, per SPEC-003 DEC-005).

KEY PATTERNS:
- Every hook this feature touches already generalises its rule over an agent name (a -Agent frontmatter
  parameter, or the payload's agent_type field): extend the existing hashtable/branch, never write a new
  hook script for one more agent (that is what audit-stage-guard.ps1's header comment explains is reserved
  for a rule with no analogue elsewhere, which this is not).
- A read-only, single-purpose agent in this repository owns exactly one artifacts/<name>/** subtree, may
  spawn nothing that could touch GitHub or the repository outside it, and never publishes; pr-reviewer is a
  straightforward instance of that existing shape (site-steward, the five SPEC-003 stage agents), not a new
  shape.
- "Never work around a hook": if any hook blocks a step of this plan, stop and report its exact message
  instead of rephrasing the command, splitting it, or routing around it (AGENTS.md, CLAUDE.md, every agent
  file's own Protocol section).

REFERENCE FILES:
- .claude/agents/adversarial-reviewer.md, .claude/agents/site-steward.md, .claude/agents/README.md,
  .claude/agents/lessons/issue-auditor.md
- .claude/hooks/block-worker-spawn.ps1, .claude/hooks/enforce-path-ownership.ps1,
  .claude/hooks/block-worker-publish.ps1, .claude/hooks/no-background-specialists.ps1,
  .claude/hooks/tests/Test-Hooks.ps1 (lines 267-271, 288, 667-673)
- .claude/skills/pr-cycle/SKILL.md
- docs/engineering/ai-task-routing.md (lines 55-77), docs/engineering/AI-DEVELOPMENT-MODEL.md (around line
  802)
- .coderabbit.yaml
```

</details>

---

## Cross-Cutting Integration Matrix

Per `AGENTS.md` §6: every function is evaluated even though this is a tooling feature with no entities, stores, pipeline behaviors, background services or external runtime integrations.

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ N/A | No data reads at runtime beyond one-shot `gh`/file reads per review; nothing to cache |
| 2 | OpenTelemetry | ❌ N/A | Agent runs in the Claude Code CLI, not a .NET process; no `ActivitySource`/`Meter` |
| 3 | Structured Logging | ❌ N/A | No `.NET` `[LoggerMessage]` infrastructure involved; the agent's own report *is* its output |
| 4 | Health Checks | ❌ N/A | Not a runtime dependency any `IEncinaHealthCheck` could probe |
| 5 | Validation | ❌ N/A | No `IValidationProvider` pipeline; input validation is the hook layer (Phase 2), not `AGENTS.md` §5-category validation |
| 6 | Resilience | ❌ N/A | No external system calls beyond `gh`/`git`, which already have their own retry/rate-limit behaviour outside this feature's scope |
| 7 | Distributed Locks | ❌ N/A | Single-agent, single-run tool invocation; no shared mutable state across concurrent runs (two `pr-reviewer` runs on different PRs do not contend) |
| 8 | Transactions | ❌ N/A | No multi-step atomic operation; writing one Markdown file is not transactional in the ROP sense |
| 9 | Idempotency | ❌ N/A | Re-running `pr-reviewer` on the same PR simply overwrites `artifacts/pr-review/<pr>.md`; no duplicate-processing risk to guard against |
| 10 | Multi-Tenancy | ❌ N/A | No tenant-scoped data; this is repository tooling |
| 11 | Module Isolation | ❌ N/A | No `ModuleId`/`IModuleContext`; not an Encina runtime module |
| 12 | Audit Trail | ❌ N/A | `artifacts/pr-review/<pr>.md` itself is the durable record of what was reviewed and found; no `IAuditStore` applies to CLI tooling |

---

## Open considerations for the orchestrator (not decided by this plan)

- **Concurrent edits to shared hook files.** `.claude/hooks/block-worker-spawn.ps1` and `enforce-path-ownership.ps1` are exactly the kind of shared hot spot two epic #1453 child issues (#1447 here, and later #1449's `issue-enricher`) could both need to touch around the same time. This plan only adds `pr-reviewer`'s own entries; the orchestrator should sequence #1447 and #1449's hook edits rather than run them in parallel worktrees, to avoid a merge conflict in files neither issue "owns" alone.
- **Whether `AI-DEVELOPMENT-MODEL.md` needs a new table.** Design Choice 6 and Phase 5 flag that no agent-enumerating table was found in that file during planning; the implementer confirms this at implementation time (the file may have changed) rather than this plan asserting a table's absence as fact today.
- **Automating the lessons file.** This plan deliberately does not build a `pr-reviewer`-specific equivalent of `tools/ai/audit/audit-lessons.ps1`, since `pr-reviewer` is not part of the fixed SPEC-003 pipeline; if the orchestrator finds itself updating `.claude/agents/lessons/pr-reviewer.md` by hand often enough to want automation, that is a follow-up issue, not part of #1447's acceptance criteria.
