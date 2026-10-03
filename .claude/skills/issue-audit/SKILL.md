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

**Local model: only where it is reliable.** `audit-next.ps1` pre-drafts the knowledge record with the free
local model before the archivist stage starts; `issue-archivist` verifies and completes that draft rather
than redoing the extraction from zero. The remediation stage no longer uses the local model: its drafts
contradicted their own findings (wrong packages, invented figures, meta-text; audit #18 failed verification on
them, #1565/#1571 could not repair them with regex checks), so since #1572 the `remediation-drafter` agent
writes them and `audit-draft-remediation.ps1` only prepares and checks (maintainer decision, 2026-10-02).

**Sharing the llama-server slot.** `qwen-predraft.ps1`'s queue loop and a worker's own local-model call
compete for the one llama-server slot. When a worker needs the model now, create
`artifacts/knowledge/predraft/PAUSE` (any content) under the main root; the queue loop waits, rechecking
every 30s, while that file exists, and resumes as soon as you remove it. `-Issue` (single-issue) mode ignores
PAUSE, because `audit-next.ps1` needs that draft immediately to start the archivist stage.

**Deduplication.** Before a remediation draft becomes an issue, `audit-verifier` checks that it does not
duplicate an open issue (`gh issue list --state open --search "<keywords>"`); a duplicate finding gets no new
draft, only a reference to the existing issue number. The remediation stage's own `gh issue list --search`
evidence check (`audit-draft-remediation.ps1 -Prepare`) is the first pass; `audit-verifier` re-checks it
independently afterward, never trusting that first pass's own search.

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
| remediation | `remediation-drafter` (between `audit-draft-remediation.ps1 -Prepare` and `-Finalize`) | `stages/remediation.md` (+ the drafts `artifacts/knowledge/remediation/<n>-*.md` in the main checkout) |
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

The **remediation** stage (#1572) is a script step, an agent spawn and a script step, once `stages/docs.md` is
committed:

1. Prepare (deterministic, no model):
   ```powershell
   pwsh -NoProfile -File tools/ai/audit/audit-draft-remediation.ps1 -Prepare
   ```
   It splits each of `code.md`, `tests.md` and `docs.md`'s `## Findings` section into individual findings (the
   numbered "N. **Blocker/Major/Minor** — ..." paragraphs the stage agents write; a section that is the `- none`
   marker alone, optional trailing period, any case, has no findings; prose after the marker is ignored with a note
   recorded as a pipeline lesson in the manifest, and any finding-shaped line after it (a numbered item, or a line
   starting with a severity word) makes the section fall through to the normal parser, so a real finding is never
   dropped), groups same-location
   findings, searches open issues for duplicates with deterministic evidence, applies `-DuplicateOf`
   overrides, and writes in the MAIN checkout's `artifacts/knowledge/remediation/` one
   `_input-<n>-<stage>-<id>.md` per finding plus `_manifest-<n>.json`: per finding its group, duplicate or merge
   decision, partially/possibly related candidates, the routed template (`bug_report.md`/`[BUG]` with milestone
   `v0.14.0 — Hardening`, `test_implementation.md`/`[TEST]`, `technical_debt.md`/`[DEBT]` for debt and for
   documentation drift) or, for a code finding, the kinds the drafter chooses from, the draft file to write, the
   `Reported by:` line and the exact `stages/remediation.md` line. A full Prepare first removes this audit's
   previous drafts, inputs and manifest; every `gh` call runs before that cleanup, through a retry helper
   (3 retries after 5, 15 and 45 s on a TLS, dial or connection failure, an HTTP 5xx, or a rate limit reported
   as HTTP 403 or 429; no retry on any other 4xx; a malformed JSON reply stops the run; #1548).
2. Spawn `remediation-drafter` **in the foreground**, naming `#<n>`, `wia-<n>` and the manifest path. It writes
   every draft and `stages/remediation.md`; its definition holds the drafting rules (facts verified with
   `file:line` in the `src/` and `tests/` of the audit worktree the manifest's `worktree` names, `wia-<n>`,
   never the main checkout, only the packages the finding names, only figures measured in `stages/tests.md`, no
   invented code, no pipeline meta-text).
3. Finalize (deterministic, no model):
   ```powershell
   pwsh -NoProfile -File tools/ai/audit/audit-draft-remediation.ps1 -Finalize
   ```
   It applies the sanitizers below to every regenerated draft, then checks each draft's header block
   (title prefix, labels, milestone, `kind:`), the template headers in order, leftover placeholders, missing
   drafts, stale drafts for duplicate or merged findings, and that `stages/remediation.md` carries the manifest's
   line for every finding, and that the current code/tests/docs findings (keys, severities and text) still match
   the manifest (a stage re-committed after Prepare makes it stale: run `-Prepare` again). It removes, with a
   note, any `<n>-*.md` that is not a manifest draft (an orphan or a second draft of one group would become an
   extra issue; nobody else can delete it). It prints every problem and exits 1
   when any remains: re-spawn `remediation-drafter` (naming `#<n>` and `wia-<n>`) and paste Finalize's whole
   output into its prompt — the drafter keeps every draft that output does not name and rewrites only the ones it
   names — then run `-Finalize` again.
4. Commit: `pwsh -NoProfile -File tools/ai/audit/audit-commit-stage.ps1 -Stage remediation` (it refuses unless
   `.authors.json` records `remediation-drafter` as the last writer of `stages/remediation.md` and `-Finalize`,
   which it runs again, is clean; the manifest's lessons must also appear in the stage file's Lessons section).

**An audit opened before #1572** (audit #18 is one) carries its own copy of `tools/ai/audit/pipeline.json`
in `wia-<n>`, whose remediation entry still names the local-model script; `audit-stage-guard.ps1`,
`enforce-path-ownership.ps1` and `audit-commit-stage.ps1` all read that copy, so `remediation-drafter` could
not write or commit the stage (and `-Prepare` refuses to run). Only after the #1572 PR is merged and the main
checkout is pulled (the scripts and hooks run from there), change that worktree copy's remediation entry to
`"agent": "remediation-drafter", "model": "sonnet"` with the Edit tool (the orchestrator may edit it; no commit
is needed, since `audit-commit-stage.ps1` stages only `artifacts/knowledge`). Then validate before continuing:

```powershell
(Get-Content -Raw .claude/worktrees/wia-<n>/tools/ai/audit/pipeline.json | ConvertFrom-Json).stages | Where-Object stage -eq 'remediation'
pwsh -NoProfile -File tools/ai/audit/audit-stage.ps1 -Next
```

The first must print `remediation-drafter`/`sonnet` (a parse error means the edit broke the JSON: the hooks then
deny every stage write and spawn until it is fixed); once the docs stage is committed, the second must print
the three remediation steps (before that it prints the earlier stage that is still due).

`-DryRun` (either mode) works only inside the sandbox `artifacts/knowledge/remediation/_dryrun-<n>/`: Prepare
writes its inputs, manifest and draft paths there (the stage-file preview is `_dryrun-<n>/remediation.md`),
Finalize reads that sandbox manifest, and neither ever deletes or overwrites a live draft, input, manifest or
`stages/remediation.md` (#1540). `-NoGh` skips every `gh` call (no duplicate is then found); the automated test
suite uses it together with a stubbed `gh`, so no real `gh` call happens in tests. `audit-verifier` checks each
draft against the open issues before you open any of them.

**Intra-audit deduplication (#1491).** Before drafting, the script groups the audit's OWN findings (from
`code.md`, `tests.md` and `docs.md` together) by their leading location anchor — `Get-FindingLeadingAnchor`/
`Group-FindingsByLocation` in `_remediation-checks.ps1`: same file and an overlapping or equal line (or line
range) is the same group; different lines of the same file are different groups; a finding with no `file:line`
anchor at all is never grouped. This is what audit #17 needed: docs finding 7 and code finding 4 both cited
`src/Encina.DomainModeling/AggregateBase.cs:20`, a stale XML doc comment, and got two separate drafted issues
for the one defect. One draft is written per group, from the group's highest-severity finding
(`Get-GroupPrimary`: Blocker > Major > Minor > Unknown; ties broken by stage order code/tests/docs); its
Description names every stage and finding id in the group with a deterministic `Reported by: <stage> <id>, ...`
line (`Add-ReportedByLine`, applied by `-Finalize`). Every OTHER member's own line in
`stages/remediation.md` reads `merged into <stage> <id> (same location)` instead of getting a draft of its own,
so the verifier still sees every finding accounted for. `-Only "<stage> <n>"` on a merged (non-primary)
finding prepares its group's one draft — the primary's — rather than a draft of the merged finding on its own.

A `technical_debt.md`-routed draft's `## Type` checkbox is never left to the drafter: `-Finalize` ticks it,
deterministically, from the finding's stage and (for a code-stage finding) the kind in the draft's `kind:` line
(`Get-DeterministicDebtType`/`Set-DebtType` in `_remediation-checks.ps1`) -- a docs-stage finding always ticks
"Documentation gap"; a tests-stage finding ticks "Missing tests", or "Refactoring needed" when its own text is
about duplicating/consolidating/refactoring existing tests; a code-stage "debt" finding ticks the template's own
exact "Code quality (warnings, analyzers)" label, or "Documentation gap" when its own text is about a stale
label/comment/string, and a code-stage "docs" finding
ticks "Documentation gap" too. This closes the exact instability audit #17 hit: regenerating every draft to fix
one detail used to re-roll every other draft's own Type tick as well (#1492).

Regenerating just one or two findings' drafts (a verifier `FAIL` naming only those) does not have to touch
every other draft: `-Prepare -Only "<stage> <n>"` (one finding per run, e.g. `-Only "code 3"`; PowerShell rejects a repeated parameter and `pwsh -File` does not split a list, #1645) prepares
only the named finding's group; every other finding keeps its draft, input and `stages/remediation.md` line
byte-identical, and the manifest marks it `"regenerate": false` with its existing line, so the drafter rewrites
only the named drafts (#1492 decision 3). It requires `stages/remediation.md` to already carry a line for every
OTHER currently-parsed finding (i.e. a full Prepare and drafter run happened at least once); otherwise it errors
rather than guessing.

Some real duplicates can never pass `Test-DuplicateEvidence`: a candidate that only MENTIONS the finding's file
and symbol as one item of a numbered list inside its own Description is exactly what #1393 excludes from
evidence (audit #18's docs finding 12 vs. #1177, which lists it as item 6 of a drift report). For that case,
`-DuplicateOf "<stage> <n>=<issue>"` (one value per run, e.g. `-DuplicateOf "docs 12=1177"`; several values are #1645) records the named finding
as a duplicate of the given issue by explicit, logged override -- once `audit-verifier` or the orchestrator has
confirmed it, never guessed by the script or the drafter. It format-validates each entry up front and (unless
`-NoGh`) verifies the target is a real OPEN issue via `gh issue view`, before touching any file; a key that does
not match a finding currently parsed from the stage artifacts is also an error. The overridden finding's own
line in `stages/remediation.md` reads exactly like an automatically detected duplicate's line, plus
"(manual override)"; when the overridden finding is the PRIMARY of a same-location group (#1491), the WHOLE
group is recorded as that duplicate -- every member's own line, never a "merged into ..." line for a
non-primary sibling. The typical pairing is `-Only "docs 12" -DuplicateOf "docs 12=1177"` (prepare and record
just that one finding), but a `-DuplicateOf` entry always prepares and records its own finding's group this
run even when its key is not separately repeated under `-Only`, and it works the same way in a full run too.
Every override is a lesson in the manifest, which the drafter copies under `stages/remediation.md`'s own
`## Lessons for the pipeline` section, so the verifier and the pipeline's lessons history both see it (#1534).
Audit #18's docs finding 12 case:

```powershell
pwsh -NoProfile -File tools/ai/audit/audit-draft-remediation.ps1 -Prepare -Only 'docs 12' -DuplicateOf 'docs 12=1177'
```

When two findings of DIFFERENT location groups describe one defect, `-MergeInto "<stage> <n>=<stage> <m>"`
(several overrides go in one comma-separated value, because PowerShell rejects a repeated parameter name; `-Prepare` only, #1632) merges the first finding's whole group into the second finding's group by
explicit, logged override, represented exactly like a same-location merge (#1491): one draft whose
`Reported by:` line names every member, the merged findings' lines read "merged into <stage> <m> (manual
override)", the manifest records `mergedInto` plus `mergeSource`, and each override is a lesson. Every entry is
validated before any file is touched: the format, both keys matching a parsed finding, source and target in
different groups, no cycle, a target that is not itself merged, and neither side in a `-DuplicateOf` group. An
override's group is always prepared, with or without `-Only`. Merges persist: the manifest lists them, and every
later `-Prepare` (full or `-Only`) re-applies them with a printed note, so an `-Only` run on the target or on a
merged source re-drafts the whole merged group and never un-merges it. A `-MergeInto` that gives a kept source
another target is an error; to change a merge, delete `_manifest-<n>.json` and run a full `-Prepare`.
`-Finalize` fails when a "merged into <x>" line names a finding that x's draft does not list in its `Reported
by:` line. The merged group's primary is its highest-severity member, so a Blocker merged into a Minor drafts
from the Blocker. Audit #19's case:

```powershell
pwsh -NoProfile -File tools/ai/audit/audit-draft-remediation.ps1 -Prepare -MergeInto 'docs 6=code 2,docs 5=code 3,docs 4=code 4,docs 2=code 8,docs 9=code 8,tests 5=code 7'
```

Duplicate-vs-new is deterministic: `tools/ai/audit/_remediation-checks.ps1`'s `Find-DuplicateAmongCandidates`
runs `Test-DuplicateEvidence` (the finding's own evidence -- a cited file AND a cited symbol -- found in a
candidate's real `gh issue view` title/body) against EVERY candidate the duplicate search returned. When one or
more candidates pass, the finding is a duplicate of the lowest-numbered passing candidate, so the same finding
against the same set of open issues always classifies the same way (#1424). When no candidate passes, every
candidate that covers part of the same defect -- a file anchor AND a specific symbol anchor of the finding in
its location text, or a symbol anchor that is a type declared under the audited worktree's `src/` (#1592) --
is listed in the manifest as "partially related" (the drafter cites it verbatim; `-Finalize` adds the line back
if it is missing, and refuses a manifest written under a different rule version), and the other search hits as
"possibly related" (awareness only, never cited). `Test-DuplicateEvidence` requires EVERY file anchor of the finding's own leading location
clause to match, not just one, so a candidate that covers only part of a multi-location finding gets a
"partially related" note instead of being accepted as the same defect (#1400). The candidate must be ABOUT the
finding's location and symbol, not merely mention them (#1393): both anchors are looked up only in its title and
its location sections, taken from the `.github/ISSUE_TEMPLATE` headers (Location, Current Behavior, Steps to
Reproduce, Actual Behavior, Code Sample, Stack Trace, Component Affected, Current Structure, Affected Files,
Packages Affected, Packages / Providers Affected, Current Coverage, Affected Packages, plus `**File(s)**`-style
bold fields written before the first heading or under Environment), never in its Description, Root Cause,
Proposed Fix, Additional Context or Related Issues; a file matches only by its full path, its path without the
root, or a brace pattern that expands to it, never by a bare file name (`README.md`, `OutboxStoreADO.cs` exists
once per provider) or a directory segment; and a folder (`src/`), a line reference or a token that
AGENTS.md/CLAUDE.md itself backticks (`EncinaError.Message`) is never symbol evidence. A candidate whose location
text matches at least one file anchor AND at least one specific symbol anchor of the finding, or names a
symbol anchor that is an interface, class, record, struct or enum declared under the audited worktree's `src/`
(`Get-DeclaredEncinaTypes`, scanned once per `-Prepare`), but not the full duplicate bar, is "partially related",
never a duplicate. A package or project name (`Encina`, `Encina.Kafka`) is never symbol evidence in either route.
A shared file or package alone, or a symbol that is not a declared type (a framework type such as
`IServiceCollection`, a member name, or a bare connection-setting name such as `Host`, which is also never a
duplicate's only symbol), is only "possibly related" (#1592).
`audit-draft-remediation.ps1 -Finalize` also strips an outer code fence from a draft (`Remove-OuterFence`),
fills a bug draft's `## Environment` section (`Set-BugEnvironment`), and reports as a problem any of the issue
template's own placeholder text still in the draft (`[e.g., ...]`, `#___`, an untouched `Test <n>: Description`
row, or any other instruction line derived straight from the routed template's own body) and any template
header missing or out of order. Finally, `Limit-RelatedIssues` sanitizes every `#n` reference anywhere in the
draft's WHOLE body -- not only a labelled Related Issues section (a `## Related Issues` header, a
`- **Related Issues**:` bold bullet, or a plain `Related Issues:` line), but any other section too (Description,
Current Behavior, Additional Context, ...) -- keeping only a reference that is the audited issue itself, appears
in the finding's own text, or is one of the manifest's already anchor-checked "partially related"
candidates -- never merely because it was offered as a search candidate,
which is not on its own evidence of a real relation. A removed reference inside ordinary prose drops just the
`#n` token (and a bare enclosing `(...)`/`(see ...)` wrapper), leaving the rest of the sentence readable, rather
than the whole line. Every removed reference is printed as a note (#1400, narrowed by #1424, widened by #1428,
made whole-body by #1492).

Every remediation draft is written to the MAIN checkout's `artifacts/knowledge/remediation/` (not the
`wia-<n>` audit worktree, which has no working copy of that path), and `audit-verifier` reads them from there
too. While the audit is open, `enforce-path-ownership.ps1` lets only `remediation-drafter` write those
`<n>-*.md` drafts: a correction goes back through a drafter re-spawn, never a hand edit.

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
- Re-committing a stage's artifact AFTER a PASS verdict (e.g. regenerating remediation drafts once more)
  makes that verdict stale by the same git-history check (#1555): `audit-stage-guard.ps1` then allows
  re-spawning `audit-verifier` on its own, and `audit-done.ps1` refuses to close the audit until it does.

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
  `issue-auditor`, `test-auditor`, `docs-reviewer`, `remediation-drafter`, `audit-verifier`).

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
