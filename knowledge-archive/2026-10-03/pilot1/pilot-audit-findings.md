# Pilot report — closed-issue knowledge migration and quality audit

Sample: 20 closed issues, stratified 8 early (#1,#2,#3,#7,#9,#13,#19,#21 — Dec 2025 batch) / 6 mid (#949,#962,#1023,#1027,#1042,#1050 — Apr 2026) / 6 recent (#1155,#1160,#1163,#1181,#1262,#1273 — Sept 2026), mixing bug/debt/feature/refactor/infra/test types and core/database/testing/caching/event-sourcing/observability/ci-cd/compliance/security/mongodb/agent-system areas. Records: `artifacts/pilot/records/<n>.md` (20 files, YAML front matter + short prose summary each).

## Selection rationale

Numeric spread was chosen to sample three eras with visibly different closure discipline: single/two/three-digit issues (pre "Fixes #N" convention, closed Dec 2025 in one intense batch), four-digit mid-range issues (Apr 2026, each with a real linked PR), and four-digit recent issues (Sept 2026, immediately before this pilot, richest metadata). Type/area mix was read off `gh issue list --json labels` before selection to avoid an all-bug or all-database sample.

## Outcome distribution (manual read of the 20 records)

| Outcome | Count | Issues |
|---|---:|---|
| delivered | 16 | #1,#2,#3,#7,#9,#949,#1023,#1027,#1042,#1050,#1155,#1160,#1163,#1181,#1262,#1273 |
| rejected | 2 | #13, #21 |
| superseded | 1 | #19 |
| partial | 1 | #962 |
| duplicate | 0 | — |

## Destination counts (manual tally across the 20 records' `destinations[]`)

| Kind | Present | Missing/None |
|---|---:|---:|
| claude-md | 6 | — |
| docs | 5 | — |
| regression-test | 6 | 1 partial |
| adr | 3 | — |
| hook / agent / skill (agent-system meta-issue #1181) | 1 each | — |
| coverage-manifest | 1 | — |
| none (no destination expected or found) | — | 8 records had at least one "none" entry |

Reading: destinations concentrate in CLAUDE.md rules, regression tests and docs — the three cheapest, most git-visible outputs. ADRs are rarer (3/20) and only appear for architecturally significant fixes (event-sourcing registration rule, retention erasure port, EventId enforcement's own referenced ADR-021). No issue in this sample produced a benchmark or a reviewer-checklist destination — those two taxonomy entries got zero hits in 20 issues, which either means they are rare by nature or that the taxonomy has entries this issue population doesn't exercise; 20 issues is too small a sample to tell.

## Findings by severity

- **major: 1** — #21, closed with "Reverted - issue created in error" and no recoverable rationale at all (worse than the other early issues, which at least have a commit SHA or a substantive comment).
- **minor: 12** — spread across #1 (no PR/comment), #3 (EventId governance retrofitted 4 months late), #7 (same commit silently closed two issues), #9 (unfollowed skip-justification convention, later moot), #13 (sound rejection never promoted to a citable rule), #19 (9-month-stale issue before supersession), #1027 (undocumented FOLDERS/FILTERS sync risk), #1042 (no way to confirm the fix worked from the repo alone), #1155 (fail-open pattern not generalized into a rule), #1163 (no architecture test prevents recurrence), #1181 (schema fit but flagged for methodology note), #1273 (third registration-completeness bug in a week).
- **0 findings**: #2, #949, #962, #1023, #1050, #1160, #1262 — either the fix was proportionate and fully traceable, or (for #2) the affected code no longer exists.

## Findings by package/area

Security/compliance and DI-registration correctness dominate the actionable findings (#1155, #1160/#1161, #1163, #1273 — 4 of 12 minor findings), which is why two of the three drafted remediation issues target exactly those two clusters. Mutation-testing tooling produced one finding (#1027). The remaining findings are process/traceability observations about the early-issue batch, not code defects.

## Useless or missing destinations/taxonomy entries

- `benchmark` and `reviewer-checklist` kinds: zero hits in 20 issues — cannot tell yet if they're rare or if the taxonomy needs adjustment.
- The taxonomy has no "meta/process" kind for issues whose destination is the agent/hook/skill system itself (#1181); `hook`/`agent`/`skill` covered it adequately, so no schema change needed, but worth confirming again once more agent-system issues are sampled.
- The `outcome` enum's `rejected` and `duplicate` values need a real "created in error, no rationale" case (#21) distinguished from a reasoned rejection with a full explanation (#13) — both currently map to `rejected`, losing the traceability-quality distinction that matters most to this audit's own purpose. **Amendment**: split `rejected` into `rejected-reasoned` and `rejected-unexplained`, or add a `rationale_quality` field (`full` / `partial` / `none`).

## Time / tokens per issue

- Local model (llama-server, one batched call for all 20 issues' type/area/packages/outcome/summary draft): `pilot-triage-20,20170,914,20.1,45.5,artifacts/local-ai/out/pilot-triage-20.md` (ledger line, `artifacts/local-ai/ledger.csv`). Batching all 20 into one call rather than 20 separate calls was the single biggest cost lever found in this pilot — it cut what would have been ~20 round trips (each with repeated system-prompt overhead) to one.
- The local draft needed correction on every outcome field (it defaulted everything to "delivered", missed #13/#19/#21/#962's true outcomes, and invented three non-existent packages — Encina.Wolverine, Encina.NServiceBus, Encina.MassTransit — for issue #3). It was still useful as a first-pass type/area/summary skeleton, consistent with local-ai-task's own warning that it "invents APIs and misreads code."
- No nested Explore/adversarial-reviewer/mechanical-fixer/docs-writer agent spawns were used: this is an analysis-only pilot with no code changes, so the self-review and specialist-delegation gates that apply to code-touching work do not apply here. All per-issue research was done directly via `gh issue view --json`, `gh api .../timeline`, `gh pr view --json files`, and `gh issue view --json closedByPullRequestsReferences` (the precise linkage field — the earlier `gh pr list --search "#N in:body"` approach was tried first and abandoned as too noisy for low-digit issue numbers, a concrete methodology lesson).
- Rough session cost for this pilot: on the order of 40-50 tool calls total (gh queries, one local-ai call, 20 Write + a few Edit/Read calls) to fully research and record 20 issues plus draft 3 remediation issues and this report — call it **2-2.5 tool calls per issue** once the linkage-discovery technique (closedByPullRequestsReferences + timeline) was found.

## Estimated effort to scale to ~365 issues

At this pilot's depth (issue body + comments + timeline + linked PR's file list + a short prose audit, no per-issue subagent spawn, no live code re-verification of most claims), extrapolating linearly: ~18x this pilot's tool-call volume, i.e. roughly 700-900 tool calls and a proportionally larger set of ~1-1.5K-token record files (≈400-500K tokens of record output alone). That does not fit one session; it needs either (a) several pilot-sized sessions (15-20 sessions of ~20 issues each) run serially or in parallel worktrees, or (b) a scripted first pass (a C# script or PowerShell script driving `gh api` calls to precompute closedByPullRequestsReferences + PR file lists + comments for all ~365 issues into one dataset in a handful of API calls) followed by Claude sessions that only draft records from the pre-fetched dataset, which would cut the gh-query overhead to near zero and let sessions focus purely on the audit judgment calls. Recommended: build (b) before scaling past this pilot.

Depth is also uneven across eras: early issues (#1-#31-ish) need timeline/commit archaeology because they predate the "Fixes #N" convention, which costs more gh calls per issue than a modern issue with a clean `closedByPullRequestsReferences` link. A full-scale run should budget roughly 2x the per-issue cost for the pre-2026 batch versus the 2026 batch.

## Concrete amendments to the audit checklist and record format

1. **Split `rejected` outcome** into reasoned vs. unexplained (see taxonomy note above) — the single biggest quality signal this pilot surfaced (#13 vs #21) would otherwise be invisible in aggregate reporting.
2. **Pre-fetch `closedByPullRequestsReferences` first, `gh pr list --search` never** for issue-to-PR linkage — the full-text search approach returns false positives for any issue number that is also a small integer appearing elsewhere in PR bodies (dates, other issue numbers, etc.); this cost real turns in this pilot before being abandoned.
3. **Cap the audit's 12-cross-cutting-function and per-flag-coverage checks to "verified" vs "plausible from file list" vs "not checked"** rather than asserting pass/fail — most of this pilot's audit fields for the 12 functions could only honestly be marked "not independently re-verified" without reading full diffs, which a 20-issue pilot at this budget cannot afford to do for every function on every issue. Scaling to 365 issues makes this worse, not better, unless the checklist is explicit that most cells will be "not checked" and that is an accepted limitation, not a gap in the pilot's execution.
4. **Add a `linked_prs[]` field to the record schema** (currently folded into `provenance`) so the report can programmatically compute "% of issues with a real PR link" per era — this pilot found that number by hand (0/8 early issues had a clean PR link at first try, 6/6 mid, 6/6 recent) and it is one of the most useful era-level metrics produced.
5. **Add a "meta/process" note to `destinations[].kind`'s documentation** (not a new enum value) clarifying that agent-system issues like #1181 map to `hook`/`agent`/`skill`, so future auditors don't hesitate on how to classify them.

## Remediation issues drafted (not opened)

- `artifacts/pilot/remediation/di-registration-completeness-architecture-test.md` — architecture test preventing the DI-registration-gap bug class seen in #1163/#1273/#1260 (3 instances in one week).
- `artifacts/pilot/remediation/fail-closed-by-default-rule.md` — name "fail closed by default" as a CLAUDE.md rule citing #1155/#1160/#1161 (3 fail-open instances in one week).
- `artifacts/pilot/remediation/mutation-folders-filters-sync-check.md` — small CI check for the FOLDERS/FILTERS array-sync risk CLAUDE.md already flags as a manual-discipline rule (from #1027).
