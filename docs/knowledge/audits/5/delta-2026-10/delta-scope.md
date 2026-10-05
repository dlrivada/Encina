# Delta scope of issue #5 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published audit result (docs/knowledge/audits/issue-5.md) only: the original audit has no published stage files.

## Knowledge record (docs/knowledge/issues/5.md)

```yaml
schema: 2
nav_exclude: true
issue: 5
title: "[DEBT] Stream load tests cause CLR crash on .NET 10"
closed: 2025-12-30
state_reason: completed
outcome: rejected-reasoned
type: debt
area: testing-quality
packages: []
prs: []
linked_prs: []
audit:
  checklist: 1
  date: 2026-09-24
  verdict: findings-tracked
  record: docs/knowledge/audits/issue-5.md
remediation:
  - "TBD (draft: artifacts/knowledge/remediation/5-stream-load-test-gap-and-unverified-citations.md)"
review: verified
knowledge:
  - kind: rejected-alternative
    statement: "Fixing the JIT crash directly (in LanguageExt, Encina's stream dispatch, or by minimizing repro for Microsoft) was rejected in favor of skipping the 8 crashing tests and waiting for an upstream .NET patch, because the maintainer classified it as a third-party runtime bug outside Encina's control."
    current: unknown
    sources:
      - "https://github.com/dlrivada/Encina/issues/5, 2025-12-30, quote: 'This is a third-party dependency issue (Microsoft .NET runtime bug). We cannot fix it ourselves - we must wait for the .NET 10 patch or upgrade to .NET 11 when available.'"
    destinations:
      - kind: none
        target: "n/a"
        status: done
  - kind: gotcha
    statement: "The root-cause citations used to justify closing the issue (dotnet/runtime issue #121736, PR #121771) could not be verified by CodeRabbit across three rounds of web search, and the closing comment did not address this before the issue was marked completed."
    current: yes
    sources:
      - "https://github.com/dlrivada/Encina/issues/5, 2025-12-30, quote: 'No pude encontrar publicamente: dotnet/runtime issue #121736, dotnet/runtime PR #121771'"
      - "https://github.com/dlrivada/Encina/issues/5, 2025-12-30, paraphrase: closing comment repeats the same numbers without correction"
    destinations:
      - kind: none
        target: "not yet delivered; tracked as a finding in the remediation draft below (unverified-claim class)"
        status: planned
  - kind: pending-work
    statement: "The workaround `DOTNET_JitObjectStackAllocationConditionalEscape=0` was documented as a temporary mitigation to re-evaluate once a .NET 10.0.x patch or NBomber update landed; no such re-evaluation has happened, and the affected test file was deleted instead of re-evaluated."
    current: yes
    sources:
      - "https://github.com/dlrivada/Encina/issues/5, 2025-12-22, paraphrase: commit c137e40d ROADMAP note 'Fixed in .NET 11, awaiting backport to .NET 10.0.x'"
    destinations:
      - kind: backlog
        target: "remediation draft artifacts/knowledge/remediation/5-stream-load-test-gap-and-unverified-citations.md"
        status: planned
  - kind: direction-change
    statement: "The Jan 2026 test-consolidation refactor (commit 65826302) deleted tests/Encina.Tests/LoadTests/StreamRequestLoadTests.cs (480 lines, the 8 skipped tests) with no migration and no justification file, unlike sibling LoadTests features which received a justification .md (Specification.md, Repository.md, ModuleIsolation.md)."
    current: yes
    sources:
      - "git show --stat 658263027956ba0614cc5e5daeb30cc3735975af, 2026-01-16, paraphrase: 480 deletions, 0 additions for that path"
    destinations:
      - kind: backlog
        target: "remediation draft artifacts/knowledge/remediation/5-stream-load-test-gap-and-unverified-citations.md"
        status: planned
```

## Where the knowledge lives (record)

- Workaround env var: [docs/testing/load-tests-known-issues.md](../../../../testing/load-tests-known-issues.md) — present, but never mentions the Stream case and lists unrelated projects only.
- CI exclusion rationale: `CLAUDE.md:1386` ("project history: #5, #496") — present but overgeneralized (see audit AUD-01, AUD-11).
- Stream/StreamDispatcher load-test justification: **missing** — no `.cs` or `.md` under `tests/Encina.LoadTests/` for this feature today.

## Audit result (docs/knowledge/audits/issue-5.md)

# Audit — Issue #5: "[DEBT] Stream load tests cause CLR crash on .NET 10"

Checklist version: 1 (SPEC-003 §5.2, amendments §15.3). Date: 2026-09-24. Audit unit: issue #5 (no merged PR; the closing commits are `e5732baa871889fac9dc0259439a56147ae19d81`, `69389b05a22665115ac4ff22002f7df5d61fca94`, `c137e40d2f82bad3ed32f79060328e4bcda93f7f`).

## Scope

| Path at time of issue | Status today | Evidence |
|---|---|---|
| `tests/Encina.Tests/LoadTests/StreamRequestLoadTests.cs` (8 tests skipped by `e5732baa`) | **Deleted**, no replacement, no justification file | `git log --follow` ends at `65826302` ("refactor: Consolidate tests and update documentation", 2026-01-16): 480 deletions, 0 additions for this path |
| `ROADMAP.md` (root-cause note added by `c137e40d`) | Entry removed entirely in a later ROADMAP rewrite | `Grep` for `121736`/`StreamRequestLoadTests`/`#5` in current `ROADMAP.md`: no matches |
| `src/Encina/Core/ServiceCollectionExtensions.cs`, `tests/Encina.Tests/StreamPipelineBehaviorTests.cs` (bundled in `e5732baa` but for a different concern — behavior-registration dedup, not #5's crash) | Out of scope for #5; not audited here | Commit message and diff show two unrelated changes bundled together |
| `src/Encina/Core/StreamDispatcher.cs` (the code under test, not modified by the issue's commits) | Exists unchanged in shape; still uses `Either.Map` inside `IAsyncEnumerable` | Read today: lines 59, 108 |
| `docs/testing/load-tests-known-issues.md` | Exists, documents the JIT/NBomber workaround generically, does not mention Stream | Read today |
| `CLAUDE.md` "Build Environment Known Issues" | Cites "(project history: #5, #496)" | `CLAUDE.md:1386` |

No code was removed by a recorded decision (unlike Oracle/SQLite, ADR-009/024); the deletion of the test file was incidental to an unrelated mass refactor, so it is audited as a **finding**, not filed under `code-removed`.

## Checklist

| ID | Outcome | Evidence |
|---|---|---|
| AUD-01 | **Finding (Class B, major)** | The issue's accepted decision was "skip the tests, wait for a .NET patch, keep the workaround documented." Today: the tests are gone (not skipped, deleted), the workaround is not wired into any CI script, and no later ADR/issue/comment records this drift. `git show --stat 65826302 -- tests/Encina.Tests/LoadTests/StreamRequestLoadTests.cs`; `Grep JitObjectStackAllocationConditionalEscape` over `.github/` and `tools/`: no matches. |
| AUD-02 (12 cross-cutting functions) | N/A | The unit is a test-only change (skip annotations); it creates no entity, store, pipeline behavior or external integration. |
| AUD-03 (coverage manifest) | N/A | No source file under `src/` was added or changed by this issue's commits; nothing to enter in a manifest for it. |
| AUD-04 (no reflection-only tests) | N/A | The deleted tests are gone; nothing to inspect today. |
| AUD-05 (test type per feature category) | **Finding (major)** | `tests/Encina.LoadTests/` has justification `.md` files for sibling features (`Specification.md`, `Repository.md`, `ModuleIsolation.md`, confirmed present) but **no `.cs` and no `.md`** for Stream/StreamDispatcher load testing. Per `CLAUDE.md`, "If a folder has neither `.cs` test files nor `.md` justification, the test coverage for that feature/provider has NOT been evaluated yet." Verified via `Glob("**/*Stream*", tests/Encina.LoadTests)`: only stale `bin/` artifacts. |
| AUD-06 (regression test for a bug) | N/A | Type is `debt`, not `bug`, and the fix here was a test skip, not a code fix; no regression test is expected under AUD-06's own terms. |
| AUD-07 (provider matrix) | N/A | The stream-dispatch pipeline is provider-agnostic core functionality, not a database/caching/lock/validation/cloud provider feature. |
| AUD-08 (EventIds) | N/A | No `[LoggerMessage]` or `LoggerMessage.Define` was touched by this issue's commits. |
| AUD-09 (PublicAPI) | N/A | No public API surface was added or removed by this issue's commits (the load-test file was internal test code). |
| AUD-10 (XML docs) | N/A | Same as AUD-09; nothing public to document. |
| AUD-11 (README/docs accuracy) — runs once per package/page in scope | **Finding (minor, ×2)** | (a) `CLAUDE.md:1386` conflates two unrelated causes under one citation: the LoadTests-excluded-from-CI-pipeline statement is a blanket policy (`*LoadTests*` project patterns excluded generically for being long-running, per `.github/workflows/templates/encina-test.yml` and `encina-full-ci.yml`), not something caused specifically by #5's JIT bug or #496's MSBuild crash — the sentence overgeneralizes a narrow, now-orphaned case into "the reason." (b) `docs/testing/load-tests-known-issues.md:44-48` ("Affected Load Test Projects") lists only `Encina.FluentValidation.LoadTests` and `Encina.GuardClauses.LoadTests`; the Stream case that originated the JIT-bug narrative for this repo is not mentioned anywhere on the page, so the paper trail is orphaned. |
| AUD-12 (fail-closed defaults) | N/A | Not a security/compliance/audit/personal-data unit. |
| AUD-13 (no message/identifier leaks) | N/A | No `EncinaError`, log message or telemetry tag was added by this issue's commits. |
| AUD-14 (`TimeProvider`) | N/A | No `DateTime.UtcNow`/`DateTimeOffset.UtcNow` was introduced by this issue's commits. |
| AUD-15 (secrets in options) | N/A | No options class involved. |
| AUD-16 (async DB calls) | N/A | Not a database-provider unit. |
| AUD-17 (DI `ValidateOnBuild`) | N/A | No `AddEncina*` extension was touched. |
| AUD-18 (no `[Obsolete]`/compatibility alias) | Pass | `Grep` for `[Obsolete]` in the (now-deleted) scope and in `StreamDispatcher.cs`: no match. |
| **Additional (not a numbered AUD item, but load-bearing to this issue's closure): claim verification** | **Finding (major)** | The issue was closed `COMPLETED` on a root-cause citation (`dotnet/runtime#121736`, PR `#121771`) that CodeRabbit's bot could not verify across three rounds of live web search on the issue thread itself, and asked the author twice to confirm the numbers; the closing comment repeats the same unverified numbers without addressing the flag. This is the "no unverified claim becomes a destination" failure mode this SPEC-003 audit exists to catch (INV-002 analog). |

## Coverage per flag

Not applicable: the audit unit's only in-scope file (`StreamRequestLoadTests.cs`) no longer exists, so no flag can be measured against it today; no other source file was changed by this issue's commits. `StreamDispatcher.cs` (the unmodified code under test) is covered under the `Encina` core package's own manifest and is out of scope for this issue's audit (it was not touched by #5's commits).

## Specialist passes

- **`adversarial-reviewer`** (foreground, Sonnet): ran against this scope with the same background above. Confirmed all findings independently (own `git log`/`git show`/`Grep` runs), found no false leads in the auditor's summary, and added: the issue's own body self-declared Priority "Medium — should be fixed before 1.0 release," which the eventual skip-and-forget resolution (later silently deleted) does not satisfy. Verdict: "Do not accept as resolved... real, current technical debt." Full report folded into the findings table above.
- **`docs-reviewer`**: **skipped** — the `block-worker-spawn` hook restricts this `issue-worker` role to `ci-diagnoser`, `mechanical-fixer`, `Explore`, `adversarial-reviewer`; `docs-reviewer` is not in that allowlist for this agent type. The orchestrator should run `docs-reviewer` on `docs/testing/load-tests-known-issues.md` directly; in the meantime the auditor performed the same accuracy check manually (AUD-11 row above) using the same facts that would have been handed to `docs-reviewer`.

## Remediation

- `artifacts/knowledge/remediation/5-stream-load-test-gap-and-unverified-citations.md` — one `[TEST]` issue draft grouping: (1) the missing Stream/StreamDispatcher load-test coverage/justification (AUD-05), (2) the unverified root-cause citation left standing in `ROADMAP.md` history / `CLAUDE.md` / `docs/testing/load-tests-known-issues.md` (AUD-01, AUD-11), (3) the misleading blanket citation in `CLAUDE.md:1386`.
- Deduplication check: `gh issue list --state open --search "Stream load test"`, `"StreamDispatcher"`, `"load test justification"` — no open issue tracks this gap (closest are #560, BenchmarkTests for Core Mediator, and #1324, a different package's DI/coverage gap; neither covers this). No duplicate found; one new draft.


