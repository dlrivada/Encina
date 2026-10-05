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
`audit-done.ps1` will close the audit (see step 6) — apply it now, or say explicitly why not.

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
`progress.csv`. It refuses when an audit is already open — close it first (step 6) — creates
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

## 5. Open remediation

```powershell
pwsh -NoProfile -File tools/ai/audit/open-remediation.ps1 -Issue <n>
```

Opens every `artifacts/knowledge/remediation/<n>-*.md` draft as a real issue (title/labels/milestone from its
header block; unknown labels are dropped; bugs default to the Hardening milestone) and records each in
`artifacts/knowledge/remediation/opened.csv`. This is the one script in the pipeline that opens issues. Run it
once `audit-verifier` has PASSed and checked each draft for duplicates, and **before** `audit-done.ps1`
(#1735): the published audit result names the opened issues, and `audit-done.ps1` refuses while a draft has no
row in `opened.csv`. Record the opened issue numbers on the board (`audits/<n>.opened`).

## 6. Close the audit

```powershell
pwsh -NoProfile -File tools/ai/audit/audit-done.ps1
```

Refuses when any stage artifact is missing or uncommitted, the verification verdict is not PASS, any lesson
still says `Applied: TODO`, the worktree's `knowledge-records --check` fails on `artifacts/knowledge/issues`, or a
remediation draft is not opened yet (step 5). Otherwise it first **publishes the audit to the repository** (#1735):
in a temporary worktree it creates the branch `knowledge/audit-<n>` from `origin/main`, puts the record in
`docs/knowledge/issues/<n>.md` (replacing a fix PR's schema 1 record; its `audit.verdict` becomes
`findings-tracked` when remediation issues were opened, else `conforms`, and `audit.record` becomes
`docs/knowledge/audits/issue-<n>.md`), the audit result in `docs/knowledge/audits/issue-<n>.md` and the stage
files in `docs/knowledge/audits/<n>/stages/`, validates the whole `docs/knowledge` tree with
`knowledge-records.cs --check`, commits `docs(knowledge): SPEC-003 audit of #<n>`, pushes (a force-push to that
script-owned branch, so a retry replaces an earlier attempt) and opens a pull request whose body is `Refs #1345`
(never `Fixes`), or reuses the one already open. When the pipeline wrote no audit result (it does not for the six
stages), the script generates a short one: the verdict line and pass count of the verification stage, one line per
stage with a link to its stage file, the remediation issues (from `opened.csv`) and the duplicates the
remediation stage noted. Remediation drafts are not published. If publishing fails, nothing else happens (the
`wia-<n>` worktree, the `audit/<n>` branch and `current-audit.json` stay) and the script can be run again; when the
publication is already on `origin/main` (the pull request was merged) a retry counts as published and closes the
audit. After publishing it deletes the local `knowledge/audit-<n>` branch. `-NoPublish` prepares the branch and
prints the push and `gh pr create` commands without running them, and leaves the audit open. Merge the knowledge
pull request like any other (`pr-cycle`).

Only after the pull request exists it copies the records, audits, remediation drafts, stage artifacts and the
ledger into the main `artifacts/knowledge/` (still git-ignored, the working area), appends `progress.csv`,
appends every `role:<agent>` lesson to that agent's memory file, removes the `wia-<n>` worktree and its
`audit/<n>` branch, and deletes `current-audit.json`.

Update the board: `audits/<n>.status=closed`, its `outcome` (from the knowledge record), `opened=[...]`
remediation issue numbers from step 5, and `meta/board.pipeline="v2"`.


## 7. Delta mode: re-check audits #1-#29 for the 2026-10-05 rules (#1763)

Audits #1 to #29 ran before two rules were decided (audit #30 on applies them in the normal pipeline):

- **Rule (a), documentation** (`docs-reviewer`, audit mode checklist): pages are visual and scannable (Mermaid, UML or C4 diagrams, charts, tables, code and terminal snippets where they help; no walls of text; professional, no emojis); `csharp` samples are correct against `src/`; figures are cited, never hand-typed; the page is placed as the `encina-docs` skill says; a feature in scope has adequate docs and appears in the tutorials and learning paths. Each gap is a finding.
- **Rule (b), obligations** (`test-auditor`, step 7): every file in the audit scope has per-flag targets in `.github/coverage-manifest/{Package}.json`; a missing set of targets, an unjustified target or one clearly below what is demanding and realistic for that file is a finding, and a 0 is acceptable only with a justification. Until the manifest schema gains a justification field (#1762), the finding proposes the target and its justification.

`audit-verifier` checks both rules like any other claim; `remediation-drafter` routes rule-(a) findings to `technical_debt.md` (Documentation gap) and rule-(b) findings to `test_implementation.md`.

The delta pipeline is `tools/ai/audit/pipeline-delta.json`: **docs** (`docs-reviewer`, rule (a) only) -> **tests** (`test-auditor`, rule (b) only) -> **remediation** (`remediation-drafter`) -> **verification** (`audit-verifier`). There is no archivist or code stage: the delta reuses the scope the original audit recorded. Start one with:

```powershell
pwsh -NoProfile -File tools/ai/audit/audit-next.ps1 -Delta rules-2026-10
```

(`-Issue <n>` picks a specific audited issue that has no delta yet; without it the script takes the first one.)

- **Which issue.** The set covers exactly the audits done before the rules: the candidates are the distinct issues of `artifacts/knowledge/progress.csv`, in order, whose published record does not date its audit on or after the set's cut-off (`delta.cutOff` in `pipeline-delta.json`, 2026-10-05; audits #30 and later already apply the rules and never enter the queue). An issue with no published record `docs/knowledge/issues/<n>.md` on `origin/main` is skipped with a warning (#4; the pilot-format #11-#15 and #20 until #1765 lands). Progress is kept in `artifacts/knowledge/delta-progress-rules-2026-10.csv` (git-ignored), so `progress.csv` and the original audit are never touched.
- **What it does.** It creates `wia-<n>` on `audit/<n>` from `origin/main` as in step 1 (there is no pre-draft), writes the reused scope to `artifacts/knowledge/delta-scope.md` in that worktree (the front matter of `docs/knowledge/issues/<n>.md` plus the scope lists of the published `docs/knowledge/audits/<n>/stages/archivist.md` and `code.md`; when `docs/knowledge/audits/<n>/stages/` is not published, it is built from the record and the published result `docs/knowledge/audits/issue-<n>.md`, and says which source was used; `knowledge-records.cs` accepts an `audits/<n>/` folder that holds only `delta-*` folders), records `mode: delta` and `set: rules-2026-10` in `current-audit.json`, and prints the first stage.
- **Stage prompts.** Spawn each agent in the foreground, naming `#<n>` and `wia-<n>` as always, and say `delta: rules-2026-10, check only rule (a)` for the docs stage, `delta: rules-2026-10, check only rule (b)` for the tests stage, and `delta: rules-2026-10, verify only rules (a) and (b)` for the verifier. `audit-stage-guard.ps1` reads `pipeline-delta.json` for order and agent, denies the full pipeline's other agents (`issue-archivist`, `issue-auditor`) and a prompt without the marker; `enforce-path-ownership.ps1` applies the same ownership to the delta stage files. Commit stages, run `audit-lessons.ps1` and `audit-commit-stage.ps1 -Lessons` as in steps 2 to 4; the remediation stage (`-Prepare`, spawn, `-Finalize`) reads only the stages the delta pipeline has.
- **Close.** Open the remediation first (step 5, `-Consolidate` below), then `audit-done.ps1` works as in step 6 (it refuses while a draft has no `opened.csv` row) but publishes to `docs/knowledge/audits/<n>/delta-2026-10/` (the delta stage files, `lessons.md` and `delta-scope.md`) on the branch `knowledge/audit-<n>-delta-2026-10` through the same pull request mechanism (`Refs #1345`); it does not replace the original record or audit result, and appends `<n>,done` to the delta progress file. The remediation step is `open-remediation.ps1 -Issue <n> -Consolidate` (add `-Set <set>` for a set other than `rules-2026-10`): the delta opens exactly ONE `[DEBT] Delta re-audit (<set>) of #<n>: <k> findings (docs and coverage obligations)` issue that carries every remediation draft as a checkbox and a per-finding subsection under the `technical_debt.md` headers, and `opened.csv` gets one row per draft pointing at it, so `audit-done.ps1` and the published audit result work unchanged (a `[BUG]` draft is still its own issue). Why: maintainer decision 2026-10-05, one issue per audit and the fixes done in batches; full audits keep one issue per draft.
- **Self-test of the consolidation.** `pwsh -NoProfile -File tools/ai/audit/open-remediation-selftest.ps1` (gh stubbed) asserts the one issue, its headers, checkboxes and `opened.csv` rows, the separate bug and the idempotent re-run.
- **Self-test.** `pwsh -NoProfile -File tools/ai/audit/audit-delta-selftest.ps1` runs `audit-next.ps1 -Delta` against a fixture with stubbed `git push` and `gh` and asserts the scope reuse, the stage order and the publish layout.

## Rules

- One audit open at a time; `audit-next.ps1` refuses a second one, and a stray `wia-*` worktree without
  `current-audit.json` blocks starting a new one until you clean it up.
- Every stage spawn is in the foreground, names the issue number and worktree explicitly, and is never
  batched with another issue's audit.
- You never write a stage's own artifact; you write `stages/lessons.md` (the one file that is yours) and the
  board.
- Analysis-only stages never touch `src/`, `tests/` or `docs/`; only `open-remediation.ps1` (a script you run,
  not an agent) opens anything on GitHub.
