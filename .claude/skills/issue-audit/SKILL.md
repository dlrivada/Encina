---
name: issue-audit
description: Run one SPEC-003 audit of a closed Encina issue end to end - queue discipline, the fixed six-stage pipeline with single-owner agents, the verifier's FAIL loop, lessons flowing back into role memory, and the Audit board. Use when starting, continuing or closing an audit of a closed issue, or when asked to run the SPEC-003 audit pipeline.
---

# SPEC-003 issue audit (#1345)

You (the orchestrator, the main session) run this procedure. It replaces the unversioned coordinator brief
`artifacts/issue-audit-brief.md`: the pipeline is now enforced by scripts (`tools/ai/audit/`) and hooks
(`audit-stage-guard.ps1`, `enforce-path-ownership.ps1`, `no-background-specialists.ps1`), not by memory. You
never run the stages yourself — you spawn the stage agent, wait for it in the foreground, commit its
artifact, and move to the next one the hooks allow.

## Philosophy (carried over from the maintainer's brief)

A closed issue goes through the same lifecycle as new work, with the first half already done: the design and
the code exist. What is missing is the rest of the specialist pipeline that checks work before it becomes a
permanent part of Encina — reviewers, testers, an independent QA gate. You are the coordinator of that
pipeline for one issue at a time; the stage agents do the reviewing.

**No shortcuts.** Every closed issue gets the full pipeline: one issue, one worktree, six stages, no token
budget. Batches and cheap paths lose precision — they caused a false "implemented" claim in the first pass
over #26 and let a coordinator claim a stage was done when it was not. `audit-stage-guard.ps1` refuses to
spawn a stage agent out of order or for the wrong issue; `enforce-path-ownership.ps1` refuses to let anyone
but the assigned agent write a stage's artifact (#1345's fabrication gap).

**Feed the system.** Every stage ends with "## Lessons for the pipeline". You resolve every lesson before
`audit-done.ps1` will close the audit (see step 8) — apply it now, or say explicitly why not.

**Siblings and successor states (the #20 and #26 lessons).** `issue-auditor` audits the copies of a pattern
the issue's own fix did not reach, not only the files the issue's PRs touched. `issue-archivist` re-verifies
the state of every successor/duplicate issue with `gh issue view <m> --json state`, live, every time — an
OPEN successor means the work is pending, not "implemented".

**Local model: mandatory, not optional.** `audit-next.ps1` pre-drafts the knowledge record with the free
local model before the archivist stage starts; `issue-archivist` verifies and completes that draft rather
than redoing the extraction from zero. `audit-draft-remediation.ps1` (the remediation stage) also runs on the
local model. An audit that used no local-model tokens should be rare and explainable.

**Sharing the llama-server slot.** `qwen-predraft.ps1`'s queue loop and a worker's own local-model call
compete for the one llama-server slot. When a worker needs the model now, create
`artifacts/knowledge/predraft/PAUSE` (any content) under the main root; the queue loop waits, rechecking
every 30s, while that file exists, and resumes as soon as you remove it. `-Issue` (single-issue) mode ignores
PAUSE, because `audit-next.ps1` needs that draft immediately to start the archivist stage.

**Deduplication.** Before a remediation draft becomes an issue, `audit-verifier` checks that it does not
duplicate an open issue (`gh issue list --state open --search "<keywords>"`); a duplicate finding gets no new
draft, only a reference to the existing issue number. The remediation stage's own `gh issue list --search`
classification call is the first pass; `audit-verifier` re-checks it independently afterward, never trusting
that first pass's own search.

Credit where it is due: the single-owner-role, mandatory-handoff and independent-QA discipline this pipeline
enforces adapts the ideas of [unclebob/swarm-forge](https://github.com/unclebob/swarm-forge) to Claude Code,
PowerShell and C#.

## The pipeline

`tools/ai/audit/pipeline.json` is the single source of truth for stage order, the assigned agent and the
artifact file name; never hard-code it. As shipped:

| Stage | Agent | Artifact |
|---|---|---|
| archivist | `issue-archivist` | `stages/archivist.md` (+ the knowledge record `artifacts/knowledge/issues/<n>.md`) |
| code | `issue-auditor` | `stages/code.md` |
| tests | `test-auditor` | `stages/tests.md` |
| docs | `docs-reviewer` (audit mode) | `stages/docs.md` |
| remediation | the local model, via `audit-draft-remediation.ps1` | `stages/remediation.md` |
| verification | `audit-verifier` | `stages/verification.md` |

## 1. Start the audit

```powershell
pwsh -NoProfile -File tools/ai/audit/audit-next.ps1
```

With no `-Issue`, it takes the next entry of `artifacts/knowledge/audit-queue.txt` not already in
`progress.csv`. It refuses when an audit is already open — close it first (step 8) — creates
`.claude/worktrees/wia-<n>` on branch `audit/<n>` from `origin/main`, writes
`artifacts/knowledge/current-audit.json`, ensures the local-model pre-draft exists, and prints the next stage
to run.

**Audit board.** Create/update `audits/<n>` with `status=open` and `stage=archivist` using the `ArtifactData`
tool against the board at <https://claude.ai/artifact/TCuXwkn8D8FxWdDZWut9Te> (collections `audits/<n>`,
`gates/<id>`, `meta/board`).

## 2. Run each stage

For every stage `audit-stage-guard.ps1` is willing to let through:

1. Spawn the stage's agent **in the foreground**, naming the issue number and the `wia-<n>` worktree
   explicitly in the prompt (the guard denies a spawn that omits either, or that also names a different
   `wia-<m>` — the batching failure this pipeline closes). Give it nothing else: the agent's own definition
   states its Inputs.
2. Wait for its report. Do not edit its artifact yourself — `enforce-path-ownership.ps1` denies you anyway;
   if something is wrong, that is a finding for `audit-verifier`, not a fix you make mid-pipeline.
3. Commit the stage:
   ```powershell
   pwsh -NoProfile -File tools/ai/audit/audit-commit-stage.ps1 -Stage <stage>
   ```
   This refuses if the artifact is missing, or if `artifacts/knowledge/stages/.authors.json` does not record
   the assigned agent as the last writer (the fabrication-gap check: only a Write/Edit call from that exact
   agent, allowed by `enforce-path-ownership.ps1`, updates that sidecar).
4. Run `audit-stage.ps1 -Next` (or re-read `audit-next.ps1`'s last line) to see the next stage.

The **docs** stage is `docs-reviewer` in audit mode: name the `wia-<n>` worktree and the word "audit" in its
prompt so `audit-stage-guard.ps1` recognises it as this pipeline's stage, not an ordinary documentation
self-review. It has no code/README to review only when the issue delivered none — say so in its own
`stages/docs.md`, do not skip the stage.

The **remediation** stage is not an agent spawn; run the script directly once `stages/docs.md` exists:

```powershell
pwsh -NoProfile -File tools/ai/audit/audit-draft-remediation.ps1
pwsh -NoProfile -File tools/ai/audit/audit-commit-stage.ps1 -Stage remediation
```

It splits each of `code.md`, `tests.md` and `docs.md`'s `## Findings` section into individual findings — the
numbered "N. **Blocker/Major/Minor** — ..." paragraphs the stage agents already write — and for each surviving
finding: (a) a short local-model call classifies it as bug/test/debt/docs and checks it against a handful of
open-issue candidates found with `gh issue list --search` for duplicates; (b) a duplicate gets no draft, only a
"duplicate of #m" line in `stages/remediation.md`; (c) a non-duplicate is routed to the matching issue template
(`bug_report.md`/`[BUG]` for a code defect with milestone `v0.14.0 — Hardening`, `test_implementation.md`/`[TEST]`
for missing tests or a coverage gap, `technical_debt.md`/`[DEBT]` for messy/incomplete code, `technical_debt.md`/
`[DEBT]` with the "Documentation gap" type ticked for a documentation drift) and drafted into
`artifacts/knowledge/remediation/<n>-<stage>-<id>-<slug>.md` with the chosen template's real headers and
checkboxes embedded verbatim. `-DryRun` performs every step except the two local-model calls (writes the
per-finding input files and briefs under `artifacts/knowledge/remediation/_dryrun-<n>/` and previews the
routing with a deterministic fallback kind instead of the model's classification), and `-NoGh` additionally
skips the `gh issue list` duplicate search — this is what the automated test suite exercises, so the real
model and `gh` are never called in tests. `audit-verifier` checks each draft against the open issues before
you open any of them.

A model-named "duplicate-of #m" is honored only when `tools/ai/audit/_remediation-checks.ps1`'s
`Test-DuplicateEvidence` finds the finding's own evidence (a cited file and a cited symbol) in `#m`'s real
`gh issue view` title/body, not just a plausible-sounding candidate; a rejected claim is drafted as new with a
"possibly related" note instead of being dropped (#1388). `audit-draft-remediation.ps1` also strips an outer
code fence from the model's reply (`Remove-OuterFence`), and, when the stripped draft still has the issue
template's own placeholder text (`[e.g., ...]`, `#___`, an untouched `Test <n>: Description` row), re-asks the
model once, naming the offending lines; a draft that still has placeholders after that re-ask is kept (for
inspection), its finding's line in `stages/remediation.md` is marked `PLACEHOLDERS LEFT: <file>`, and the whole
run exits 1 at the end, naming every such draft.

Update the board's `audits/<n>.stage` after each stage commits.

## 3. The verifier and its FAIL loop

`audit-verifier` re-checks every claim in every prior stage's artifact against its source — it never fixes
anything. Its `stages/verification.md` starts with `Verdict: PASS` or `Verdict: FAIL`
(`pipeline.json.verdictLine`).

- **PASS**: continue to step 4.
- **FAIL**: its `## Corrections` section names, for each problem, the stage to re-run. `audit-stage-guard.ps1`
  allows re-running any earlier stage out of the normal fixed order exactly when the last verdict was FAIL
  (checked by reading `stages/verification.md`'s first line). Re-spawn the named stage's agent, re-commit it,
  then re-spawn `audit-verifier` for a fresh verdict. Update the board: `verdict=FAIL` and add a `gates/<id>`
  entry describing the correction; clear it (or add a `verdict=PASS` entry) once the re-run passes.

## 4. Lessons

```powershell
pwsh -NoProfile -File tools/ai/audit/audit-lessons.ps1
```

Collects every stage's "## Lessons for the pipeline" bullets into `stages/lessons.md`, each followed by
`Applied: TODO`. You resolve every `TODO` yourself, by hand-editing `stages/lessons.md` (you are the
orchestrator; `enforce-path-ownership.ps1`'s stage-ownership check only restricts `stages/<pipeline-stage>.md`
files that a `pipeline.json` stage names, and `lessons.md` is not one of them, so your Write/Edit call is not
blocked). For each lesson, replace `TODO` with one of:

- the commit SHA or file you changed to apply it now;
- `not applied: <reason>`, when it is out of scope or wrong;
- `role:<agent> <one-line note>` when the lesson belongs in that agent's own memory rather than a one-off
  fix — `audit-done.ps1` appends it, with today's date and this issue number, to
  `.claude/agents/lessons/<agent>.md` when the audit closes. Use the exact agent name (`issue-archivist`,
  `issue-auditor`, `test-auditor`, `audit-verifier`, `docs-reviewer`).

Commit `stages/lessons.md` on the audit branch with:

```powershell
pwsh -NoProfile -File tools/ai/audit/audit-commit-stage.ps1 -Lessons
```

It is not a `pipeline.json` stage, so the plain `-Stage` mode does not apply to it, and a bare `git commit`
is blocked by `enforce-path-ownership.ps1` for every caller inside an open audit's worktree; `-Lessons` is
the one authorized way to commit it. It runs the same lessons-resolved check as `audit-done.ps1` and refuses
if any lesson still has `Applied: TODO`.

## 5. Close the audit

```powershell
pwsh -NoProfile -File tools/ai/audit/audit-done.ps1
```

Refuses when any stage artifact is missing or uncommitted, the verification verdict is not PASS, any lesson
still says `Applied: TODO`, or the worktree's `knowledge-records --check` fails on `artifacts/knowledge/issues`.
Otherwise it copies the records, audits, remediation drafts, stage artifacts and the ledger into the main
`artifacts/knowledge/`, appends `progress.csv`, appends every `role:<agent>` lesson to that agent's memory
file, removes the `wia-<n>` worktree and its `audit/<n>` branch, and deletes `current-audit.json`.

Update the board: `audits/<n>.status=closed`, its `outcome` (from the knowledge record), `opened=[...]`
remediation issue numbers once step 6 runs, and `meta/board.pipeline="v2"`.

## 6. Open remediation

```powershell
pwsh -NoProfile -File tools/ai/audit/open-remediation.ps1 -Issue <n>
```

Opens every `artifacts/knowledge/remediation/<n>-*.md` draft as a real issue (title/labels/milestone from its
header block; unknown labels are dropped; bugs default to the Hardening milestone). This is the one script in
the pipeline that publishes — run it only after `audit-done.ps1` has closed the audit and `audit-verifier`
has checked each draft for duplicates. Record the opened issue numbers on the board (`audits/<n>.opened`).

## Rules

- One audit open at a time; `audit-next.ps1` refuses a second one, and a stray `wia-*` worktree without
  `current-audit.json` blocks starting a new one until you clean it up.
- Every stage spawn is in the foreground, names the issue number and worktree explicitly, and is never
  batched with another issue's audit.
- You never write a stage's own artifact; you write `stages/lessons.md` (the one file that is yours) and the
  board.
- Analysis-only stages never touch `src/`, `tests/` or `docs/`; only `open-remediation.ps1` (a script you run,
  not an agent) opens anything on GitHub.
