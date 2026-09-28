# SPEC-004 — Bounded-WIP Stage Pipeline for Issue Delivery

| | |
|---|---|
| **Status** | **APPROVED (2026-09-28)**. **DEC-001 … DEC-003 are DECIDED (maintainer, 2026-09-28)**: no commit-count staleness threshold (every stage re-checks its input against current `main` on entry instead, §4); the WIP limit is fixed at 4-5 fronts, not a pilot-tunable parameter (§5); the pipeline is maintainer/orchestrator-directed, not a fully autonomous scheduled dispatcher (§8). §12 records the three decisions taken. |
| **Author** | Specifier (Claude), from the maintainer's brief of 2026-09-28 and issue #1554 |
| **Date** | 2026-09-28 |
| **Refines** | [AI-DEVELOPMENT-MODEL.md](../engineering/AI-DEVELOPMENT-MODEL.md) §9 (Historian), §10 (Auditor), §22 (audit passes), and the `worker-brief` / `pr-cycle` skills that the orchestrator runs by hand today |
| **Evidence** | Issue #1554 ("[SPIKE] Bounded-WIP stage pipeline for issue delivery (SPEC-004)"); #1552 (scored priority list); #1551 (worker-brief CRAP rule); #1540 (a `-DryRun` deleted an open audit's real stage drafts); #1534 (a duplicate finding could not be recorded against a multi-item issue); #1386 (a Pages deploy could revert another dashboard's fresh data or be cancelled mid-flight); [SPEC-003](SPEC-003-closed-issue-knowledge-migration-and-quality-audit.md) (the audit pipeline this specification generalises) |
| **Supersedes** | — |

> This specification implements nothing. It states what must be true when issues move through a bounded-WIP stage pipeline instead of being driven step by step from the orchestrator's own context. Requirements state *what*; the design choices that arise while building the dispatcher go through the ADR process ([AI-DEVELOPMENT-MODEL.md](../engineering/AI-DEVELOPMENT-MODEL.md) §7).
>
> **Identifiers.** Unprefixed REQ, AC, DEC, INV identifiers are this specification's. Identifiers of other specifications carry their prefix (SPEC-003 REQ-020).

---

## 1. Problem

Today the orchestrator drives every step of an issue's life by hand: it writes the brief, spawns the worker, reviews the diff, opens the pull request, spawns the reviewer, and requests auto-merge, holding the state of every open front in its own conversational context. [SPEC-003](SPEC-003-closed-issue-knowledge-migration-and-quality-audit.md)'s audit pipeline already proves an alternative for one recurring task: fixed stages, each with a single owner and a single output document, and a verifier that can fail a stage and send it back to an earlier one (SPEC-003 §15.0, §15.4). The same shape generalises to the whole issue lifecycle, not only to closed-issue audits.

Three problems this specification addresses:

- **No structured handoff between stages of an issue's life.** A brief, a review verdict and a merge decision exist only as conversation turns; nothing marks an issue's current stage, what it is waiting on, or whether the document it is about to use is still valid against today's `main`.
- **Unbounded work in progress.** Nothing bounds how many issues are open in parallel, so intake can pile up inventory faster than later, slower stages (maintainer decisions, CI, review) can absorb it, which is exactly the failure mode the maintainer named on 2026-09-28.
- **No visibility and no conflict check.** Two fronts can pick up issues that touch the same files with no check beforehand (the collision pattern behind #1466 and #1523, cited in issue #1554), and the maintainer's control board has no place to see, at a glance, what stage every open front is in.

## 2. Scope and definitions

### 2.1 In scope

- The design of the stage model (§3), the stage document format (§4), the bounded-WIP and intake rules (§5, §6), the urgent lane (§7), the dispatcher (§8), and the pilot (§10) for delivering GitHub issues of `dlrivada/Encina` from intake to close-out.
- Visibility of the pipeline on the maintainer's private control board (§9).

### 2.2 Out of scope

- **Option C of issue #1554**: a fully autonomous, scheduled cloud dispatcher that runs paid agent stages without an active maintainer session. DECIDED out of scope (DEC-003, §12): cost control and debuggability (issue #1554, Option C cons) outweigh the lower latency it would offer; this is not an open question the pilot revisits.
- Changes to the SPEC-003 audit pipeline itself. This specification reuses SPEC-003's proven shape (single-owner stages, one document per stage, a verifier that can fail a stage) but does not alter SPEC-003's stages, checklist or execution model.
- The knowledge-record obligation of SPEC-003 REQ-031 for issues delivered through this pipeline; that obligation is unchanged and independent of this specification.

### 2.3 Definitions

| Term | Meaning |
|---|---|
| Front | One issue being delivered, together with the pull requests it spawns (the original PR, its derived PRs and small follow-ups); one unit of bounded WIP (§5). |
| Stage | One step of a front's life with a single owner and one input and one output document (§3). |
| Stage document | The file at `artifacts/flow/<issue>/<stage>.md` that records a stage's status, verdict and provenance (§4). |
| Stale document | A stage document whose input no longer holds against the current `main`; there is no commit-count threshold — the consuming stage discovers staleness by re-checking the document on entry (§4, DEC-001). |
| WIP limit | The maximum number of fronts open at once: 4-5, fixed by DEC-002 (§5, §12), not a pilot-tunable parameter. |
| Urgent lane | Work that bypasses the bounded-WIP queue because it blocks other fronts or the pipeline itself (§7). |
| Dispatcher | The free PowerShell script that computes which stages are ready to run, respecting WIP limits and file conflicts (§8). |

## 3. Stages

Every front moves through the following stages, in order, with the option to return to an earlier stage on a failed verdict (§3.1). Each stage has exactly one owner, one input document and one output document.

| # | Stage | Owner | Input | Output |
|---|---|---|---|---|
| 1 | Intake | Local model (`local-ai-ask.cs`) | The issue's body and comments (`gh issue view`) | `artifacts/flow/<issue>/intake.md`: a summary of what the issue asks, restated in the issue's own terms |
| 2 | Verification | A Claude agent, read-only | `intake.md`, today's `src/` and `docs/` | `artifacts/flow/<issue>/verification.md`: whether the issue's premise still holds against today's code, with file:line evidence |
| 3 | Decisions | Architect (Claude) with the maintainer, batched | `verification.md` | `artifacts/flow/<issue>/decisions.md`: the Class C choices the issue needs, decided or still open |
| 4 | Spec or plan | Specifier / `docs-writer` (spec) or the `implementation-plan` skill (plan) | `decisions.md` | `artifacts/flow/<issue>/spec-or-plan.md` (or a link to the SPEC/plan file it produced under `docs/`) |
| 5 | Implementation | `issue-worker` or `docs-writer`, in its own worktree | `spec-or-plan.md` | The worktree's diff, committed; `artifacts/flow/<issue>/implementation.md` records the commit and worktree |
| 6 | Review | `pr-reviewer` / `adversarial-reviewer` | `implementation.md` and the diff | `artifacts/flow/<issue>/review.md`: verdict PASS or FAIL with `returns-to` (§3.1) |
| 7 | PR and merge | Orchestrator session (pushes, opens the PR, requests merge) | `review.md` (verdict PASS) | `artifacts/flow/<issue>/pr-and-merge.md`: the PR number and merge SHA |
| 8 | Close-out | Depends on the issue: the knowledge record (SPEC-003 REQ-031, where the issue's PR closes it) or nothing, when the issue is a SPIKE whose PR references it without closing it | `pr-and-merge.md` | `artifacts/flow/<issue>/close-out.md` |

### 3.1 Returns to an earlier stage

Any stage's verifier (the next stage's owner, or a dedicated review stage) can fail its input and send the front back: the failing stage's document records `returns-to: <stage>` with a one-line reason, and the returned-to stage re-runs from its own input, which may itself now be stale (§4). This is the same shape SPEC-003 §15.4 already uses between its audit sub-stages (`adversarial-reviewer`, `docs-reviewer`, test review feeding back into the record).

## 4. Stage documents

**REQ-001** Every stage of every front produces exactly one file at `artifacts/flow/<issue>/<stage>.md`.

**REQ-002** Every stage document starts with a header block:

```text
stage: <one of the eight stages of §3>
status: ready | in-progress | done | rejected
main-sha: <the commit of main the stage was produced against>
input: <path to the input document, or "none" for intake>
verdict: <the stage's verdict, free text; "n/a" when the stage has none>
returns-to: <stage name, or "none">
returns-to-reason: <one sentence, or "n/a">
```

**REQ-003** There is no commit-count staleness threshold (DEC-001, §12). Every stage, on entry, re-checks its input document against the current `main` — the same way SPEC-003's `audit-verifier` re-checks every claim of the archivist, code and test stage artifacts it consumes, rather than trusting them by age. When the input no longer holds, the stage does not proceed on it: it returns the work to the stage that produced the document, recording `returns-to` and `returns-to-reason` (§3.1) instead of `status: done`.

**REQ-004** `artifacts/flow/` is git-ignored, consistent with the other `artifacts/` working documents of AGENTS.md §9 and SPEC-003 §9 (Constraints); stage documents are working state, not durable knowledge. Durable knowledge from a front still goes to its usual destination (an ADR, a SPEC amendment, a doc page, a knowledge record) exactly as it would without this pipeline.

## 5. Bounded WIP

**REQ-005** No more than 4-5 fronts (§2.3) are open at once. A derived PR or a small follow-up that a front's implementation or review stage spawns counts inside the front that created it, not as a new front against the limit.

**REQ-006** A new front is not opened while the WIP limit is at capacity; the dispatcher (§8) reports the queue instead of starting intake on more issues than the limit allows to be *in flight past intake* (§6 bounds intake specifically).

## 6. Intake order and just-in-time intake

**REQ-007** Intake follows the scored priority list of #1552 (`tools/ai/priority/`, ranked by `score-issues.ps1 -All`; the ranking step is documented in `.claude/skills/open-issue/SKILL.md` §4).

**REQ-008** Intake takes the first item of the priority list that shares no files with the fronts currently open (a file-overlap check against each open front's known scope, §8). An item skipped for file overlap is logged in the dispatcher's output with the reason (which open front it collides with).

**REQ-009** Intake runs just-in-time: about twice the WIP limit ahead of the fronts actually in progress (so roughly 8-10 issues carry an `intake.md` at once when the WIP limit is 4-5), never intake run over the whole backlog at once. Running intake far ahead of the WIP limit produces the inventory pile-up this specification exists to avoid (§1).

## 7. Urgent lane

**REQ-010** An urgent lane exists outside the bounded-WIP queue for: a broken CI check on `main`, an audit blocker (a defect that stops the SPEC-003 pipeline from progressing), or a defect that blocks other open fronts. The examples the maintainer named on 2026-09-28 are #1534 (a duplicate finding could not be recorded against a multi-item issue, blocking that audit), #1540 (a `-DryRun` run deleted an open audit's real stage drafts, corrupting live state), and #1386 (a Pages deploy could revert another dashboard's fresh data or be cancelled mid-flight, an infra defect blocking other dashboards' visibility).

**REQ-011** An urgent-lane item does not count against the WIP limit of §5 and is dispatched by the orchestrator session as soon as it is found, ahead of the queued fronts.

## 8. Dispatcher

**REQ-012** A free PowerShell script (no model) computes, from the stage documents under `artifacts/flow/`, which stages are ready to run: a stage is ready when its input document exists with `status: done` and is not stale (§4), and starting it would not exceed the WIP limit (§5) or create a file conflict with another in-progress front (§6, §8.1).

**REQ-013** Local-model stages (intake, §3 stage 1) are run directly by the dispatcher. Paid-agent stages (verification, decisions, spec/plan, implementation, review) are reported as ready by the dispatcher and dispatched by the orchestrator session (Option B of issue #1554); the dispatcher itself never spawns a paid agent. This is the pipeline's decided degree of automation (DEC-003, §12): not fully automatic. The maintainer and the orchestrator choose which fronts open (§6); the orchestrator dispatches every paid-agent stage; each front moves through its stages the way a SPEC-003 audit moves from the archivist onward, each stage producing exactly the document the next stage needs.

**REQ-014** Every dispatcher script that can write into a front's live stage documents accepts a dry-run mode that never writes outside its own dry-run output location; a dry-run write into a live stage folder is the defect class #1540 recorded, and the pipeline's dispatcher must not repeat it.

### 8.1 File-conflict check

**REQ-015** Before an issue enters intake (§6) or a front's implementation stage starts, the dispatcher checks the files the issue is expected to touch (from its body, linked PRs of related issues, or the package/area the priority score attributes to it) against the files already claimed by open fronts, and refuses to start the front when they overlap, logging the collision (REQ-008).

## 9. Visibility

**REQ-016** A "Flow" tab exists on the maintainer's private control board, with one column per stage of §3 and a WIP counter per column. The control board itself is private infrastructure; this specification refers to it only as "the maintainer's control board", with no URL (consistent with how the maintainer's private tooling is referenced elsewhere in the documentation).

## 10. Pilot and success measures

**REQ-017** Before this pipeline replaces hand orchestration for all work, a pilot runs it on the first 4-5 items of the #1552 priority list (a full WIP-limit's worth of fronts).

**REQ-018** The pilot measures, per issue, against the baseline week of 2026-09-22: lead time (intake to merge), rework loops (count of `returns-to` events), tokens spent (from `artifacts/agent-usage/ledger.csv` and `artifacts/local-ai/ledger.csv`), and maintainer interruptions (decisions or unblocks the maintainer had to make outside the batched decisions stage).

**REQ-019** The pilot's measurements (REQ-018) are reported to the maintainer, who decides whether the pipeline continues past the pilot set or needs revision. The staleness handling (§4, DEC-001), the WIP limit (§5, DEC-002) and the degree of automation (§8, DEC-003) are already decided and are not among the pilot's open questions; if the pilot's measurements suggest one of them should change, that is a new amendment to this specification, not a default outcome of running the pilot.

## 11. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Stage documents go stale as `main` advances past their `main-sha` | REQ-003, DEC-001: a SHA stamp per document, and every stage re-checks its input against current `main` on entry instead of trusting a commit-count threshold |
| Intake outruns the stages that consume it, piling up inventory | REQ-005, REQ-009: WIP limits and just-in-time intake |
| A returned-to stage cascades rework through several fronts | REQ-005: a small WIP limit bounds how much rework can be in flight at once |
| Two fronts collide on the same files | REQ-008, REQ-015: a conflict check at intake and before implementation |
| A local-model stage's error propagates downstream (for example an invented verification line, as seen in PR bodies on 2026-09-28) | Each stage's owner treats its input document as unverified until its own stage checks it; a Claude-owned stage (verification, review) re-derives facts from source rather than trusting the local model's prose |
| Stage tooling touches live state outside its own stage (#1540: a dry run deleted real drafts) | REQ-014: dry-run modes never write into a live stage folder |
| The paid-agent stages still need an active maintainer session to dispatch | Accepted (DEC-003, §12): Option C is out of scope; the session-bound dispatch of REQ-013 is a deliberate trade-off for cost control, not an open question |
| New tooling (the dispatcher) becomes one more thing to maintain | The pilot (§10) runs before this replaces hand orchestration, so the tooling cost is measured against a small, bounded trial first |

## 12. Decisions for the maintainer

Class C decisions ([AI-DEVELOPMENT-MODEL.md](../engineering/AI-DEVELOPMENT-MODEL.md) §14). DEC-001 … DEC-003 were DECIDED by the maintainer on 2026-09-28, ahead of the rest of this draft's review; they are recorded here in the same format SPEC-003 uses for its own decided items (SPEC-003 §12).

| ID | Decision | Options | Recommendation | Consequences | Status |
|---|---|---|---|---|---|
| **DEC-001** | How a stage document's staleness is detected and handled (REQ-003) | (a) stamp a `main` SHA and treat the document stale once it is more than a fixed number of commits behind; (b) no commit-count threshold: every stage re-checks its input document against the current `main` on entry, the way SPEC-003's `audit-verifier` re-checks every claim of the stages before it, and returns the work to the stage that produced the document when it no longer holds | (b) | Under (a) the threshold number is arbitrary and needs recalibrating as the repository's commit cadence changes; under (b) every stage carries its own small, bounded re-check instead of trusting an artifact because it is recent enough | DECIDED (b): maintainer, 2026-09-28 |
| **DEC-002** | The WIP limit (REQ-005) | (a) fix a single number; (b) a band of 4-5 fronts open at once plus the PRs they spawn, with derived PRs and small follow-ups counted inside the front that created them | (b) | (b) is not treated as a pilot-tunable parameter (REQ-019); the maintainer keeps day-to-day judgement within the band instead of a rigid single number | DECIDED (b): maintainer, 2026-09-28 |
| **DEC-003** | Degree of automation (REQ-012, REQ-013) | (a) a fully autonomous scheduled dispatcher (Option C of issue #1554) that opens and runs fronts without an active session; (b) the maintainer and the orchestrator choose which fronts open, the orchestrator dispatches every paid-agent stage, each front moves through its stages the way a SPEC-003 audit moves from the archivist onward — each stage producing the document the next needs — and the dispatcher script may run local-model stages | (b) | Under (a) cost control and debuggability are harder (issue #1554, Option C cons); under (b) the pipeline needs an active maintainer/orchestrator session, which is accepted rather than left as an open question (§2.2) | DECIDED (b): maintainer, 2026-09-28 |

## 13. Related documents

- [SPEC-003 — Closed-Issue Knowledge Migration and Quality Audit](SPEC-003-closed-issue-knowledge-migration-and-quality-audit.md) (the stage model this specification generalises)
- Issue [#1552](https://github.com/dlrivada/Encina/issues/1552) — scored priority list of open pre-1.0 issues
- Issue [#1551](https://github.com/dlrivada/Encina/issues/1551) — worker-brief CRAP rule
- Issue [#1540](https://github.com/dlrivada/Encina/issues/1540) — a dry run deleted an open audit's real stage drafts
- Issue [#1554](https://github.com/dlrivada/Encina/issues/1554) — this specification's originating spike
- [AI-DEVELOPMENT-MODEL.md](../engineering/AI-DEVELOPMENT-MODEL.md)
- [ai-task-routing.md](../engineering/ai-task-routing.md)

## 14. Change log

| Date | Change |
|---|---|
| 2026-09-28 | DRAFT created from issue #1554 and the maintainer's brief of 2026-09-28: stages, stage documents, bounded WIP, intake order, urgent lane, dispatcher, visibility, pilot and risks stated as REQ items; DEC-001 … DEC-003 opened for review. |
| 2026-09-28 | DEC-001 … DEC-003 DECIDED (maintainer, 2026-09-28), after review of PR #1559: no commit-count staleness threshold — every stage re-checks its input against current `main` on entry and returns the work to the producing stage when it no longer holds (DEC-001, REQ-003); the WIP limit is a fixed band of 4-5 fronts, not a pilot-tunable parameter (DEC-002, REQ-005); the pipeline is maintainer/orchestrator-directed — the maintainer and orchestrator choose which fronts open, the orchestrator dispatches every paid-agent stage, and each front moves through its stages the way a SPEC-003 audit moves from the archivist onward — with a fully autonomous scheduled dispatcher (Option C of issue #1554) staying out of scope with no open question (DEC-003, REQ-012, REQ-013). REQ-019 reworded so the pilot no longer treats these as tunable. |
| 2026-09-28 | Approved by the maintainer. |
