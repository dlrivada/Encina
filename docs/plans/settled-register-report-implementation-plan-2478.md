# Implementation Plan: Daily list of Settled and Trivial register entries for maintainer veto

> **Issue**: [#2478](https://github.com/dlrivada/Encina/issues/2478) - [FEATURE] Daily report and board list Settled register entries for maintainer veto
> **Status**: Draft plan (written against `origin/main` 4b0a600c7, 2026-11-25)
> **Related**: #2470 (decision register, the acceptance criterion split out of it)
> **Scope note**: repository tooling only. No `src/` code, no EventIds.

---

## Summary

**What it implements.** A PowerShell script `tools/ai/plans/settled-register.ps1` that, for one UTC day, lists every Settled and Trivial entry of the decision register recorded that day, from two sources: the Design Choices of the plans under `docs/plans/` that changed on `origin/main` that day or carry a `Verified:` date of that day, and the issue comments headed "Decision register" created that day. It writes `artifacts/decisions/settled-<date>.md` (the report section) and `artifacts/decisions/settled-<date>.json` (the board data), one row per entry: `Issue | Id | Choice | Class | Settled by | Verified | Link`. A documented veto step turns a vetoed entry into a Dilemma.

**What exists today.**

- `docs/engineering/prompts/implementation-plan-prompt.md` and `tools/ai/plans/check-plan.ps1` define the register in plans: `<details>` choices with `**Class:**`, `**Settled by:**`, `**Verified:** adversarial-reviewer <yyyy-MM-dd>`, or a register table `Id | Choice | Class | Settled by | Verified`; 24 fixtures under `tools/ai/plans/fixtures/` cover them.
- `.claude/skills/implementation-plan/SKILL.md` step 8 already says Settled entries "are listed one line each in the daily report for veto", and `.claude/skills/open-issue/SKILL.md` defines the issue-comment register headed "Decision register" with the columns `Id | Source sentence | Options | Class | Settled by | Verified` (written by the `decision-extractor` agent to `artifacts/decisions/<issue>.md`, a git-ignored per-worktree file, so it is not a shared source).
- No script collects the entries, and the daily report is a procedure of the orchestrator session, not a file of this repository: the safeguard exists only in wording. `tools/ai/board/README.md` documents how the board's data files are produced and uploaded by the orchestrator.

**Estimated scope.** 5 files, about 450 changed lines, one pull request.

**Affected packages.** None. Paths: `tools/ai/plans/`, `.claude/skills/implementation-plan/`, `tools/ai/board/README.md`, `.github/workflows/plan-conformance.yml`.

**Provider category.** None.

---

## Design Choices

<details>
<summary><strong>1. Source of the entries: parse plans and comments, or emit a machine-readable file from the extractor</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Parse the plan files and the "Decision register" comments directly** | No new store; both formats are already checked by `check-plan.ps1` and `check-issue-template` | Parsing Markdown prose and tables |
| **B) `decision-extractor` also writes `artifacts/decisions/<issue>.json`, and the report reads that** | Cheap to read | A second source of truth to keep in sync; the file is git-ignored and per worktree, so another checkout does not see it |

### Chosen Option: **A - Parse plans and comments (recommended, pending the maintainer)**

### Rationale

- This is the Dilemma of the issue body (first option wins while few entries exist, second once the register is large). A wins today; B wins when the register grows past what a daily parse handles or when comments stop being the register of plan-less issues.

**Class:** Dilemma

</details>

<details>
<summary><strong>2. Parser reuse: share with `check-plan.ps1` or keep a standalone parser</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Standalone small parser in `settled-register.ps1`, with a self-test that runs it over the same fixtures `check-plan.ps1` uses and asserts the entry counts** | No change to the CI-enforced checker; drift is caught by the fixture agreement | Two parsers |
| **B) Extract the register parsing of `check-plan.ps1` (`Get-ChoiceRegister`, `Get-LabelValue`) into a library both dot-source** | One parser | Refactors the checker that gates every plan PR; a regression blocks all plans |

### Chosen Option: **A - Standalone parser plus fixture agreement (recommended, pending the maintainer)**

### Rationale

- A wins while the report is new and the checker must stay stable; B wins when the maintainer wants a single parser and accepts the refactor risk.

**Class:** Dilemma

</details>

<details>
<summary><strong>3. Which date puts an entry into a day's list</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The `Verified:` date when the entry has one, otherwise the date the plan (or comment) was recorded; an entry can appear twice, first as `pending`, then as verified** | The maintainer can veto at both moments, including after the adversarial check | An entry appears twice |
| **B) Only the date the plan or comment was recorded** | Each entry once | A choice verified days later is never shown with its verification, which is what the issue asks the list to carry |

### Chosen Option: **A - Verified date, else recorded date (recommended, pending the maintainer)**

### Rationale

- A wins when the veto is meant to follow verification (plans merge with `Verified:` still absent, SKILL step 8); B wins when the maintainer wants each entry exactly once.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Language and location</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) PowerShell script in `tools/ai/plans/`** | Next to `check-plan.ps1` and its fixtures; the `plan-conformance` workflow already triggers on `tools/ai/plans/**` | None |
| **B) C# file-based app in `tools/ai/`** | Typed | A second toolchain for text parsing; the fixtures are read by PowerShell today |

### Chosen Option: **A - PowerShell in `tools/ai/plans/`**

### Rationale

- Follows the existing tooling next to the fixtures it reuses.

**Class:** Trivial
**Settled by:** AGENTS.md section 2; layout of `tools/ai/plans/` and the `paths` filter of `.github/workflows/plan-conformance.yml`

</details>

<details>
<summary><strong>5. Determinism, fail-closed collection and the veto step</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Output sorted by issue and id with no generation timestamp; the script runs `git fetch origin main` and exits 2 when the fetch or a REST read fails (never a partial list); the veto is a documented orchestrator step in the implementation-plan skill** | Re-running a day yields the same bytes; a missed source cannot read as "nothing to veto"; the veto path exists | A failed fetch stops the report |
| **B) Best-effort: skip a failed source and continue** | Always produces a file | A silent gap hides an entry the maintainer meant to review |

### Chosen Option: **A - Deterministic, fail closed, documented veto**

### Rationale

- Acceptance criteria of the issue: re-running the same day yields the same output; a vetoed entry becomes a Dilemma by a documented step. The safeguard fails only if a source is missed silently.

**Class:** Settled
**Settled by:** issue #2478 acceptance criteria ("Re-running the script for the same day yields the same output"; "A vetoed entry is turned into a Dilemma by the orchestrator (documented step)"); AGENTS.md section 3 ("gates fail closed")

</details>

---

## Implementation Phases

### Phase 1: Core API (collection and parsing)

<details>
<summary><strong>Tasks</strong></summary>

1. **`tools/ai/plans/settled-register.ps1`** (new). Usage: `pwsh tools/ai/plans/settled-register.ps1 [-Date yyyy-MM-dd] [-OutDir artifacts/decisions] [-Repo dlrivada/Encina] [-SelfTest]` (default date: yesterday UTC, because a report covers closed days only).
2. Plans: `git fetch origin main` (failure: exit 2); `git log origin/main --since/--until <UTC day> --name-only --format= -- docs/plans` lists the plans recorded that day; every plan on `origin/main` is also scanned for a `Verified: adversarial-reviewer <date>` equal to the day (choice 3). Entries come from the `<details>` choices (`Class:`, `Settled by:`, `Verified:`, choice title) and from register tables; only Settled and Trivial are kept; the plan's issue number comes from the file name.
3. Comments: `GET repos/<repo>/issues/comments?since=<day start>&per_page=100` through `Invoke-GhQuotaGh` of `tools/ai/gh-quota.ps1` (REST, rate-limit aware), paginated; a comment whose first non-empty line is "Decision register" (heading or plain, case-insensitive) created that day is parsed as the table `Id | Source sentence | Options | Class | Settled by | Verified`.
4. Row model: `Issue`, `Id`, `Choice`, `Class`, `SettledBy`, `Verified` (date or `pending`), `Source` (`plan` or `comment`), `Link` (the issue URL and, for a plan, the file on `main`). Text cells are cut at 160 characters, `|` and newlines are removed, nothing is rendered as HTML.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 1</strong></summary>

```
You are implementing Phase 1 of #2478.

CONTEXT:
- PowerShell tooling (AGENTS.md section 2). Plan format: docs/engineering/prompts/implementation-plan-prompt.md and tools/ai/plans/check-plan.ps1 (Get-ChoiceRegister, Get-LabelValue); fixtures in tools/ai/plans/fixtures/.
- REST helper: tools/ai/gh-quota.ps1 (Invoke-GhQuotaGh).

TASK:
Create tools/ai/plans/settled-register.ps1 with collection and parsing as the plan Phase 1 describes. No rendering yet.

KEY RULES:
- Exit 2 when git fetch or a REST read fails; never return a partial list.
- Never interpret cell text as Markdown structure.
- Edit with Edit/Write only.

REFERENCE FILES:
- tools/ai/plans/check-plan.ps1, tools/ai/plans/fixtures/pass-mixed-implementation-plan-1.md, tools/ai/gh-quota.ps1
```

</details>

### Phase 2: Rendering and the report surfaces

<details>
<summary><strong>Tasks</strong></summary>

1. Render `artifacts/decisions/settled-<date>.md`: heading, one table `Issue | Id | Choice | Class | Settled by | Verified | Link`, rows sorted by issue then id (natural order), a final line with the counts; with no entries the file says "No Settled or Trivial entry was recorded on <date>." No timestamp of generation.
2. Render `artifacts/decisions/settled-<date>.json` (same rows, sorted keys) for the board data, as `tools/ai/board/README.md` describes for the other board files; the orchestrator uploads it.
3. **`.claude/skills/implementation-plan/SKILL.md`** (modify, step 8): name the script as the source of the daily veto list, and write the veto procedure: on a maintainer reply the orchestrator changes the entry in the plan to `**Class:** Dilemma`, removes its `Settled by:` and `Verified:` lines, marks the Chosen Option "(recommended, pending the maintainer)", adds a Dilemma entry when work starts, and adds the `needs-decision` label to the issue.
4. **`tools/ai/board/README.md`** (modify): a short section "Settled register list" (file names, columns, how the orchestrator uploads it).

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 2</strong></summary>

```
You are implementing Phase 2 of #2478.

CONTEXT:
- Phase 1 is merged: the script returns the rows.

TASK:
Add rendering of the .md and .json files, and edit the implementation-plan skill (step 8) and tools/ai/board/README.md as the plan Phase 2 describes.

KEY RULES:
- Same day, same bytes: sort everything, no timestamps, UTF-8 without BOM, LF endings.
- The skill text is the documentation: concrete commands, no vague wording.

REFERENCE FILES:
- .claude/skills/implementation-plan/SKILL.md, tools/ai/board/README.md, .claude/agents/decision-extractor.md
```

</details>

### Phase 3: Testing, CI wiring and finalization

<details>
<summary><strong>Tasks</strong></summary>

1. `-SelfTest` of `settled-register.ps1` (no network, `git` and `gh` stubbed like `tools/ai/audit/audit-done-selftest.ps1`): the fixtures `pass-mixed`, `pass-settled`, `pass-register-table`, `fail-settled-unverified` give the expected Settled/Trivial counts (agreement with `check-plan.ps1`, choice 2); a Dilemma is never listed; a comment fixture `tools/ai/plans/fixtures/decision-register-comment.md` (new) parses to its rows; a `pending` and a verified row of one entry appear on their two days; the empty day text; hostile cells (`|`, newline, HTML) are neutralised; a stubbed fetch failure exits 2; two runs produce identical bytes. Numeric target: at least 14 assertions.
2. **`.github/workflows/plan-conformance.yml`** (a shared hot spot: **the orchestrator applies it**): one step `./tools/ai/plans/settled-register.ps1 -SelfTest` after the checker's self-test (the workflow already runs on `tools/ai/plans/**`).
3. No changelog fragment and no docs page: developer tooling, and the issue states that the skill text is the documentation.
4. Verification: `pwsh tools/ai/plans/settled-register.ps1 -SelfTest` exits 0; `pwsh tools/ai/plans/check-plan.ps1 -SelfTest` still exits 0.

</details>

<details>
<summary><strong>Prompt for AI Agents — Phase 3</strong></summary>

```
You are implementing Phase 3 of #2478.

CONTEXT:
- Phases 1-2 are merged.

TASK:
Write the -SelfTest and the comment fixture; report the exact plan-conformance.yml step for the orchestrator (do NOT edit .github/workflows/*).

KEY RULES:
- No network in the self-test; stub git and gh.
- Every public behavior (parse, filter, render, fail closed, determinism) has a passing and a failing case.

REFERENCE FILES:
- tools/ai/audit/audit-done-selftest.ps1, tools/ai/plans/check-plan.ps1 (-SelfTest), tools/ai/plans/fixtures/
```

</details>

---

## Research

### Relevant Standards & Specifications

| Standard | Requirement | Key Details |
|----------|-------------|-------------|
| Issue #2470 | Decision register: Trivial, Settled, Dilemma | Only Dilemmas reach the maintainer; Settled stay reopenable |
| AGENTS.md section 11 | Issues, plans, `needs-decision` | Veto turns an entry into a Dilemma |
| AGENTS.md section 3 | Gates fail closed | A missed source never reads as "nothing to veto" |
| ADR-054 D4 / ADR-062 | Telemetry tags and error categories | Not applicable: no telemetry, no `EncinaError` |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage |
|-----------|----------|-------|
| Plan checker and fixtures | `tools/ai/plans/check-plan.ps1`, `tools/ai/plans/fixtures/` | Format reference and test data |
| REST helper | `tools/ai/gh-quota.ps1` (`Invoke-GhQuotaGh`) | Comment stream, rate-limit aware |
| Skills | `.claude/skills/implementation-plan/SKILL.md`, `.claude/skills/open-issue/SKILL.md` | Veto step; comment register format |
| Extractor | `.claude/agents/decision-extractor.md` | Producer of the register table |
| Board data | `tools/ai/board/README.md` | How data files reach the board |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | PowerShell tooling: no `[LoggerMessage]`; no block claimed |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| New | 2 | `settled-register.ps1`, comment fixture |
| Modified | 2 | `implementation-plan/SKILL.md`, `tools/ai/board/README.md` |
| Workflow | 1 | `plan-conformance.yml` step, applied by the orchestrator |
| Coverage flags | 0 | Manifests cover `src/` only; numeric target of the self-test: at least 14 assertions |

One pull request: 5 files in total, under the limits.

---

## Combined AI Agent Prompts

<details>
<summary><strong>Full combined prompt for all phases</strong></summary>

```
PROJECT CONTEXT:
- Encina issue #2478: a daily list of Settled and Trivial decision-register entries for maintainer veto. PowerShell tooling in tools/ai/plans/.

IMPLEMENTATION OVERVIEW:
Phase 1 collection and parsing from plans and "Decision register" comments; Phase 2 .md and .json rendering plus the skill's veto step and the board README; Phase 3 self-test, fixture and the plan-conformance step (orchestrator applies).

KEY PATTERNS:
- Same day, same bytes; fail closed (exit 2) on a failed fetch or REST read.
- Source of entries, parser reuse and the date rule are Dilemmas (recommended: parse plans and comments; standalone parser with fixture agreement; Verified date else recorded date), pending the maintainer.

REFERENCE FILES:
- tools/ai/plans/check-plan.ps1, tools/ai/plans/fixtures/, tools/ai/gh-quota.ps1, .claude/skills/implementation-plan/SKILL.md
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Not applicable: tooling, no runtime reads |
| 2 | OpenTelemetry | ❌ | Not applicable: no .NET component |
| 3 | Structured Logging | ❌ | Not applicable: console output of a script |
| 4 | Health Checks | ❌ | Not applicable: not a service |
| 5 | Validation | ✅ | Phase 1: cell sanitising and strict date parameter |
| 6 | Resilience | ✅ | Phase 1: REST through the quota-aware helper; failure exits 2 |
| 7 | Distributed Locks | ❌ | Not applicable: single writer |
| 8 | Transactions | ❌ | Not applicable |
| 9 | Idempotency | ✅ | Phase 2: re-running a day yields the same bytes |
| 10 | Multi-Tenancy | ❌ | Not applicable: single repository |
| 11 | Module Isolation | ❌ | Not applicable |
| 12 | Audit Trail | ✅ | Phase 2: the list is the audit surface of the register |

---

## Next Steps

1. Review and approve this plan; three Dilemmas (source, parser reuse, date rule) are answered when work on #2478 starts.
2. The orchestrator applies the `plan-conformance.yml` self-test step.
