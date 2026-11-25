# Implementation Plan: Bot registry (`tools/ai/bots.json`) and automatic `BOT-UNAVAILABLE` detection with fallback

> **Issue**: [#1448](https://github.com/dlrivada/Encina/issues/1448) - [FEATURE] Bot registry (tools/ai/bots.json) and automatic BOT-UNAVAILABLE detection with fallback
> **Status**: Draft plan (written against `origin/main` 3bccacc1c, 2026-11-25)
> **Piece**: 2 of the "Fallback system for external review bots" epic ([#1453](https://github.com/dlrivada/Encina/issues/1453)). Piece 1 (`pr-reviewer`, `.claude/agents/pr-reviewer.md`) is already on `main`.
> **Scope note**: repository tooling only. No `src/` code, no .NET package, no EventIds.

---

## Summary

**What it implements.** A registry `tools/ai/bots.json` (same conventions as `tools/ai/sites.json`) that maps each external review bot to its failure signals and its internal fallback agent; a shared PowerShell library that evaluates those signals; changes to the two PR watchers so they emit one `BOT-UNAVAILABLE <bot_id> <pr>` line per detected failure; a new row in the `pr-cycle` skill that launches the registered fallback; and a daily bot-health check that the `site-health` skill / `site-steward` agent runs.

**Today (verified on `origin/main`).**

- `.claude/skills/pr-cycle/SKILL.md` section 2 already has a row "CodeRabbit Review rate limited (or no review posted after the normal wait)" that tells the orchestrator to spawn `pr-reviewer` by hand. This plan makes the detection automatic and registry-driven; it replaces that row.
- `tools/ai/watch-pr-events.ps1` (per PR) already reads issue comments and reviews over REST and echoes `ISSUE-COMMENT <user> <160 chars>`; `tools/ai/watch-open-prs.ps1` (all open PRs) emits `CHECK-FAIL`, `NEW-THREAD`, `MERGED|CLOSED`. Both dot-source `tools/ai/gh-quota.ps1` and are REST-only (#2568).
- `tools/ai/bots.json` does not exist. `tools/ai/sites.json` and `dashboard-freshness.cs --check-registry` are the model for a registry plus its validation.

**Estimated scope.** 6 new files, 6 modified files and the `ci.yml` step the orchestrator applies, about 700 changed lines, in two pull requests (see the split under Estimated File Count).

**Affected packages.** None of the Encina packages. Paths: `tools/ai/`, `.claude/skills/`, `.claude/agents/`, `docs/engineering/`.

**Provider category.** None (provider-independent tooling).

**Maintainer decision already recorded on the issue.** Comment of 2026-10-17 ("Settled by elimination"): registry-driven like `sites.json`, watchers emit `BOT-UNAVAILABLE <bot_id> <pr>`, `pr-cycle` launches the registered fallback, a bot that is down globally goes to the maintainer.

---

## Design Choices

<details>
<summary><strong>1. Registry-driven fallback instead of hard-coded logic in `pr-cycle`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `tools/ai/bots.json` registry** | A new bot is one entry; same pattern as `sites.json` | One more file to validate |
| **B) Hard-code the fallback in `pr-cycle`** | No new file | Every new bot needs a skill edit |
| **C) Scheduled GitHub Actions poll** | Independent of the local session | Minutes of latency; spends runner minutes; cannot launch a local agent |

### Chosen Option: **A - Registry**

### Rationale

- The maintainer comment on #1448 (2026-10-17) records this choice; C cannot start a local agent at all.

**Class:** Settled
**Settled by:** maintainer comment on issue #1448 (2026-10-17, "Settled by elimination")

</details>

<details>
<summary><strong>2. Detection logic lives in one shared dot-sourced library</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `tools/ai/bot-signals.ps1`, dot-sourced by both watchers** | One implementation, one self-test | New file |
| **B) Copy the matching into each watcher** | No new file | Two copies drift |
| **C) A C# file-based app called by the watchers** | Typed code | A process start per poll |

### Chosen Option: **A - Shared library**

### Rationale

- Both watchers already dot-source `tools/ai/gh-quota.ps1` the same way, and AGENTS.md section 2 allows PowerShell for tooling.

**Class:** Trivial
**Settled by:** convention of `tools/ai/watch-open-prs.ps1` and `tools/ai/watch-pr-events.ps1` (both dot-source `gh-quota.ps1`); AGENTS.md section 2
</details>

<details>
<summary><strong>3. Who may trigger a signal: only comments and reviews authored by the bot's own login</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Registry field `login` (`coderabbitai[bot]`); text signals match only that author's comments** | A human quoting "Review limit reached" cannot spend fallback tokens | One more field |
| **B) Match the text from any author** | Simpler | Spoofable by any commenter, including external contributors |

### Chosen Option: **A - Author-bound signals**

### Rationale

- Signal text is untrusted input and a match launches a paid agent; the registry binds the text to the one author that can legitimately say it. The watcher also never echoes the comment text, only the bot id and the PR number.

**Class:** Settled
**Settled by:** AGENTS.md section 3 ("Compliance and security gates fail closed"; untrusted input never decides on its own); `.claude/skills/pr-cycle/SKILL.md` (a spawn costs paid tokens)
</details>

<details>
<summary><strong>4. Clock of the time-based signal `no-review-<N>m`</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Time since the monitor first observed the head SHA** | Close to the push time when the monitor is armed right after the push; never fires on an old commit | A monitor re-armed late detects late |
| **B) Committer date of the head commit** | Survives a monitor restart | A commit made hours before the push fires at once and spends a fallback spawn |
| **C) Time of the `synchronize` event from the issue timeline** | Exact push time | Extra REST pages per poll; the timeline API has no plain push event |

### Chosen Option: **A - Observation time (recommended, pending the maintainer)**

The clock of `no-review-<N>m` restarts also at the time of the latest `@coderabbitai review` trigger comment on the current head SHA (CodeRabbit first reviews are not automatic and need that comment), so a silence that precedes the trigger never counts. The scenarios of this Dilemma are therefore: push then immediate trigger (clock from the trigger), push with no trigger yet (clock from the first observation), monitor re-armed late (clock from the first observation, or from the committer date under B).

### Rationale

- A wins when the orchestrator arms the monitor right after `gh pr create` (the pr-cycle rule); B wins when monitors are restarted often (the 30-minute re-arm). The cost of a late detection is minutes; the cost of a false one is a paid review.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Idempotency of the fallback: where "already launched" is remembered</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) A marker in the PR review the fallback posts (`<!-- bot-fallback:<bot>:<headSha7> -->`), read over REST before emitting** | Durable, visible to everyone, survives worktree removal and monitor restarts | One extra REST read per PR and sha |
| **B) A state file `artifacts/bots/fired.json` in the checkout** | No API call | Lost with the worktree; two sessions do not see each other |
| **C) In-memory set of the monitor only** | Simplest | A re-armed monitor fires again |

### Chosen Option: **A - Marker on the PR (recommended, pending the maintainer)**

### Rationale

- A wins when several sessions or re-armed monitors watch one PR; B wins when the REST quota is the scarce resource. A fired signal is keyed by bot, PR and head SHA, so a new push (which CodeRabbit reviews again) is a new expectation.

**Class:** Dilemma

</details>

<details>
<summary><strong>6. How the fallback is launched: the orchestrator reads the event; the agent never posts</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `pr-cycle` row: on `BOT-UNAVAILABLE <bot> <pr>` the orchestrator spawns the registry's `fallback` agent in the foreground and posts its artifact** | Matches the roles: agents read, the orchestrator publishes | Needs a model session |
| **B) The watcher script spawns the agent itself** | No model in the loop | A script cannot use the Agent tool; agents may not post (`.claude/agents/pr-reviewer.md`) |

### Chosen Option: **A - Orchestrator launches and posts**

### Rationale

- `pr-reviewer` writes only `artifacts/pr-review/<pr>.md`; CLAUDE.md "Orchestration and delegation" says workers never open or edit PRs.

**Class:** Settled
**Settled by:** `.claude/agents/pr-reviewer.md` (tools: Write limited to its own artifact); CLAUDE.md "Orchestration and delegation"
</details>

<details>
<summary><strong>7. Daily bot-health check: what "globally down" means</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Activity-based: the bot reviewed none of the last N (default 10) closed PRs** | Uses only GitHub REST like the watchers; one rule for every bot | Detects after PRs were already affected |
| **B) Vendor status-page JSON** | Detects an outage before a PR hits it | One parser per vendor; the registry cannot describe it; some bots (Copilot) have no useful page |

### Chosen Option: **A - Activity-based (recommended, pending the maintainer)**

### Rationale

- A wins for a registry with several bots and a REST-only rule (#2568); B wins when one vendor outage must be announced before the next PR.

**Class:** Dilemma

</details>

---

## Implementation Phases

### Phase 1: Registry and schema

<details>
<summary><strong>Tasks</strong></summary>

1. **`tools/ai/bots.json`** (new): `{ "schema": 1, "healthWindowPrs": 10, "bots": [ { "id", "name", "role", "phase", "login", "failureSignals": [...], "fallback" } ] }`. Entries: `coderabbit` (login `coderabbitai[bot]`, signals `Review limit reached`, `rate limited`, `no-review-5m`, fallback `pr-reviewer`) and `copilot` (login `copilot-pull-request-reviewer[bot]`, signal `no-review-10m`, fallback `pr-reviewer`). Signal grammar: a string `no-review-<N>m` is time-based (N minutes without a review of the bot on the current head SHA); any other string is a case-insensitive literal searched in the bot's own comments created after the head commit time (a comment older than the current head never matches). No `//` comments (JSON): the schema is documented in the docs page of Phase 5.
2. **`tools/ai/bot-signals.ps1`** (new, library): `Read-BotRegistry -Path` (validates and returns the entries; throws on a missing file, schema other than 1, empty `id`, duplicate `id`, a `fallback` with no `.claude/agents/<name>.md`, a signal that is empty or `no-review-0m`), `Test-BotTextSignal -Bot -Comments -HeadCommitUtc` (author-bound and time-bound match: only a comment by the bot's `login` created after the head commit's committer time counts, so an old "Review limit reached" comment cannot re-fire the fallback on every later push), `Test-BotNoReview -Bot -HeadSha -FirstSeenUtc -NowUtc -Reviews -Comments`, `Get-BotUnavailableLines`.
3. **`tools/ai/bots-selftest.ps1`** (new): the self-test (Phase 4).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of #1448 (bot registry), tooling only.

CONTEXT:
- Pattern to copy: tools/ai/sites.json + .github/scripts/dashboard-freshness.cs --check-registry; library style of tools/ai/gh-quota.ps1.
- Registry is data; every failure path of Read-BotRegistry throws with a message naming the field.

TASK:
Create tools/ai/bots.json and tools/ai/bot-signals.ps1 (functions listed in the plan Phase 1). No watcher change yet.

KEY RULES:
- PowerShell only (AGENTS.md section 2). Edit with the Edit/Write tools.
- Text signals match only comments authored by the bot's `login` and created after the head commit time; never echo comment text.
- A malformed registry must not be silently treated as "no bots": the caller gets an exception.

REFERENCE FILES:
- tools/ai/sites.json, tools/ai/gh-quota.ps1, .claude/agents/pr-reviewer.md
```

</details>

### Phase 2: Watcher changes

<details>
<summary><strong>Tasks</strong></summary>

1. **`tools/ai/watch-pr-events.ps1`** (modify): dot-source `bot-signals.ps1`; load the registry once (an invalid registry prints one `WARN bots.json: <reason>` and detection stays off for the run, the other events continue); for each poll evaluate the registry's bots over the comments and reviews the script already reads and over the head SHA it already knows; emit `BOT-UNAVAILABLE <bot_id> <pr>` once per bot and head SHA, after the idempotency check of the chosen option of choice 5.
2. **`tools/ai/watch-open-prs.ps1`** (modify): the same for every open PR, reusing the per-PR head SHA it already reads; the first sight of a SHA starts the `no-review-<N>m` timer (choice 4). Issue comments and reviews of a PR are read only for a PR whose `updated_at` changed (the quota rule already in the script) and only while a bot of the registry has not yet reviewed the current SHA.
3. Both header comments gain the new `BOT-UNAVAILABLE <bot_id> <pr>` line in their event list.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of #1448.

CONTEXT:
- tools/ai/bot-signals.ps1 exists (Phase 1). Both watchers are REST-only (#2568, docs/engineering/github-api-quota.md).

TASK:
Wire detection into tools/ai/watch-pr-events.ps1 and tools/ai/watch-open-prs.ps1 as the plan Phase 2 says. Output format is exactly: BOT-UNAVAILABLE <bot_id> <pr_number>.

KEY RULES:
- No GraphQL, no new `gh pr` subcommand. A rate-limit failure keeps the existing backoff path.
- The line carries only the registry id and the PR number (both from trusted sources), never text from a comment.
- Keep the existing events byte-for-byte unchanged.

REFERENCE FILES:
- tools/ai/watch-pr-events.ps1, tools/ai/watch-open-prs.ps1, tools/ai/gh-quota.ps1
```

</details>

### Phase 3: `pr-cycle` fallback and daily health check

<details>
<summary><strong>Tasks</strong></summary>

1. **`.claude/skills/pr-cycle/SKILL.md`** (modify): replace the manual "CodeRabbit Review rate limited" row by `BOT-UNAVAILABLE <bot> <pr>`: read `fallback` of the bot in `tools/ai/bots.json`, spawn that agent in the foreground with the PR number, post `artifacts/pr-review/<n>.md` as one review that starts with the marker `<!-- bot-fallback:<bot>:<headSha7> -->`, record in the PR that the bot was skipped. The existing sentence about `adversarial-reviewer` for gates and workflows stays.
2. **`tools/ai/check-bot-health.ps1`** (new): for each bot, count how many of the last `healthWindowPrs` closed PRs have a review or comment by its `login` (REST, paginated); prints one line per bot (`OK`, `SILENT <n>/<N>`) and writes `bots-health.md` into `-OutDir`; exit 0 always (information), exit 2 for a bad registry.
3. **`.claude/skills/site-health/SKILL.md`** and **`.claude/agents/site-steward.md`** (modify): a new section "5. Bot health" (the skill has sections 1 When, 2 Freshness, 3 Snapshots, 4 Route, 5 Report; the new section goes before Report, which becomes 6) running the script with `-OutDir artifacts/site-health/bots` (inside the steward's write allow-list); a bot `SILENT` for the whole window is drafted as a note for the maintainer, never acted on by the steward.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of #1448.

CONTEXT:
- Phases 1-2 are merged. site-steward is Haiku, read-only except artifacts/site-health/**.

TASK:
Edit the pr-cycle row, create tools/ai/check-bot-health.ps1, add the bot-health step to the site-health skill and the site-steward agent.

KEY RULES:
- The orchestrator posts; no agent posts.
- The health script writes only under -OutDir and never opens an issue.

REFERENCE FILES:
- .claude/skills/pr-cycle/SKILL.md, .claude/skills/site-health/SKILL.md, .claude/agents/site-steward.md
```

</details>

### Phase 4: Cross-cutting integration and testing

<details>
<summary><strong>Tasks</strong></summary>

1. **`tools/ai/bots-selftest.ps1`**: stubs `gh` (function override, no network, like `tools/ai/gh-quota-selftest.ps1`) and asserts: valid registry loads; each rejection of Phase 1 task 2 throws; a text signal from the bot's login fires; the same text from another login does not; the same text from the bot's login created before the head commit time does not fire (the stale "Review limit reached" case after a new push); `no-review-5m` fires at 5 minutes, not at 4, and not when a bot review of the current SHA exists; a new SHA restarts the timer; one `BOT-UNAVAILABLE` per bot and SHA; the marker found on the PR suppresses the line; the line never contains comment text.
2. **CI wiring** (`.github/workflows/ci.yml`, a shared hot spot): one step `pwsh -NoProfile -File tools/ai/bots-selftest.ps1` next to the other tool self-tests of the `knowledge-records` job. **The orchestrator applies this edit**; the PR that carries the self-test lists it as a post-merge step.
3. Run the live smoke: `pwsh tools/ai/watch-open-prs.ps1 -Once` against the real repository prints no error.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 4</strong></summary>

```
You are implementing Phase 4 of #1448.

CONTEXT:
- Phases 1-3 are merged; bot-signals.ps1 is the unit under test.

TASK:
Write tools/ai/bots-selftest.ps1 with the cases listed in the plan and make it exit 1 on the first failed assertion. Do NOT edit .github/workflows/*; report the exact step for the orchestrator.

KEY RULES:
- No network in the self-test; stub gh.
- Every exported function of bot-signals.ps1 has at least one passing and one failing case.

REFERENCE FILES:
- tools/ai/gh-quota-selftest.ps1, tools/ai/audit/audit-done-selftest.ps1
```

</details>

### Phase 5: Documentation and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. **`docs/engineering/bot-fallback.md`** (new, written by `docs-writer`): the schema field by field, the signal grammar, the event line, the idempotency marker, the daily health check, how to add a bot. No hand-typed coverage figures.
2. **`.claude/agents/README.md`** (modify): one row noting that `pr-reviewer` is launched from `BOT-UNAVAILABLE`.
3. **`changelog.d/1448-bot-registry-fallback.added.md`** (new, by `mechanical-fixer`): one bullet. Developer tooling only; the fragment is optional if the maintainer treats tooling as not user-visible (the issue ticks it).
4. Verification: `pwsh tools/ai/bots-selftest.ps1` exits 0; `pwsh tools/ai/plans/check-plan.ps1` is not involved; no `dotnet build` is needed because no .NET project changes.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 5</strong></summary>

```
You are implementing Phase 5 of #1448 (documentation).

CONTEXT:
- All code phases are merged; the behavior is what the files above do.

TASK:
Write docs/engineering/bot-fallback.md following .claude/skills/encina-docs/SKILL.md (one Diataxis quadrant: reference), update .claude/agents/README.md, add the changelog fragment.

KEY RULES:
- English only; never name a private client application.
- Cite only files that exist on main.

REFERENCE FILES:
- tools/ai/bots.json, tools/ai/bot-signals.ps1, docs/engineering/github-api-quota.md
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| AGENTS.md section 2 | Tooling in PowerShell or C# file-based apps | All new code is `.ps1` |
| AGENTS.md section 3 (fail closed) | Missing context denies | Invalid registry warns and never reads as "no bots" |
| ADR-054 decision 4 | Tenant and telemetry tags | Not applicable: no telemetry, no tenant data |
| ADR-062 `ErrorCategory` | Category of every new error code | None: no `EncinaError` code is added |
| #2568 | REST-only polling | Detection reads comments and reviews over REST |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| Watchers | `tools/ai/watch-pr-events.ps1`, `tools/ai/watch-open-prs.ps1` | Emit the new event |
| Quota helpers | `tools/ai/gh-quota.ps1` (`Invoke-GhQuotaGh`, `Get-GhCheckBuckets`) | REST calls and backoff |
| Registry model | `tools/ai/sites.json`, `.github/scripts/dashboard-freshness.cs` | Schema and validation style |
| Fallback agent | `.claude/agents/pr-reviewer.md` | Registry `fallback` target |
| Skills | `.claude/skills/pr-cycle/SKILL.md`, `.claude/skills/site-health/SKILL.md`, `.claude/agents/site-steward.md` | Launch and daily check |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | PowerShell tooling: no `[LoggerMessage]`, no `EventIdRanges.cs` change, no block claimed in the batch registry |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| PR 1 (registry, library, watchers, self-test) | 5 | `bots.json`, `bot-signals.ps1`, two watchers, `bots-selftest.ps1` (new: 3, modified: 2); `ci.yml` step applied by the orchestrator, one more file |
| PR 2 (pr-cycle, health, docs) | 7 | `pr-cycle` skill, `check-bot-health.ps1`, `site-health` skill, `site-steward.md`, `docs/engineering/bot-fallback.md`, `.claude/agents/README.md`, changelog fragment |
| Coverage flags | 0 | The manifests of `.github/coverage-manifest/` cover `src/`; the numeric target is the self-test: at least 1 passing and 1 failing assertion per exported function (about 24 assertions), enforced by the CI step |

PR 1 stands alone (the watchers emit a line nobody consumes yet); PR 2 needs PR 1.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina, issue #1448: registry tools/ai/bots.json, BOT-UNAVAILABLE events from the two PR watchers, pr-cycle launches the fallback agent, daily bot-health check. Tooling only (PowerShell), no src/ change.

IMPLEMENTATION OVERVIEW:
Phase 1 registry + tools/ai/bot-signals.ps1; Phase 2 watchers; Phase 3 pr-cycle row + check-bot-health.ps1 + site-health/site-steward step; Phase 4 self-test (CI step applied by the orchestrator); Phase 5 docs and fragment.

KEY PATTERNS:
- Author-bound text signals; observation-time clock; marker on the PR for idempotency (recommended, pending the maintainer).
- REST only; the event line carries ids and numbers only.

REFERENCE FILES:
- tools/ai/sites.json, tools/ai/gh-quota.ps1, tools/ai/watch-pr-events.ps1, tools/ai/watch-open-prs.ps1, .claude/skills/pr-cycle/SKILL.md, .claude/agents/pr-reviewer.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: a poll result is not reused beyond the existing `updated_at` guard |
| 2 | OpenTelemetry | ❌ | Not applicable: PowerShell scripts with no .NET instrumentation |
| 3 | Structured Logging | ❌ | Not applicable: no .NET code; output lines are the watcher protocol |
| 4 | Health Checks | ✅ | Phase 3: `check-bot-health.ps1` is the bot health check |
| 5 | Validation | ✅ | Phase 1: `Read-BotRegistry` validates the registry and fails loudly |
| 6 | Resilience | ✅ | Phase 2: REST failures use the existing backoff (`Invoke-GhQuotaGh`); a failed read keeps the previous state |
| 7 | Distributed Locks | ❌ | Not applicable: several monitors are deduplicated by the PR marker, not by a lock |
| 8 | Transactions | ❌ | Not applicable: no multi-step state change |
| 9 | Idempotency | ✅ | Phase 2 and 3: one line per bot and head SHA; the marker on the PR (recommended option) survives restarts |
| 10 | Multi-Tenancy | ❌ | Not applicable: single repository, no tenant data |
| 11 | Module Isolation | ❌ | Not applicable: not a modular component |
| 12 | Audit Trail | ✅ | Phase 3: the posted fallback review with its marker is the record that a bot was skipped |

---

## Next Steps

1. Review and approve this plan; the three Dilemmas (clock, idempotency store, health rule) are answered when work on #1448 starts.
2. Open PR 1, then PR 2; the orchestrator applies the `ci.yml` self-test step.
