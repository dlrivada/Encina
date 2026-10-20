# Delta scope of issue #6 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published audit result (docs/knowledge/audits/issue-6.md) only: the original audit has no published stage files.

## Knowledge record (docs/knowledge/issues/6.md)

```yaml
schema: 2
nav_exclude: true
issue: 6
title: "Refactor Quartz logging tests to work with LoggerMessage delegates"
closed: 2025-12-23
state_reason: completed
outcome: delivered
type: debt
area: testing-quality
packages: [Encina.Quartz]
prs: []
linked_prs: []
knowledge:
  - kind: gotcha
    statement: >-
      NSubstitute.Received() cannot intercept [LoggerMessage] source-generator delegate calls
      because they are static extension methods; Microsoft.Extensions.Diagnostics.Testing's
      FakeLogger<T> (asserting on FakeLogger<T>.Collector.GetSnapshot()) is the correct
      replacement and is now the established pattern across the codebase (14+ test files as
      of 2026-09-25, including Hangfire, SignalR, Refit, Retention, AzureFunctions and
      RedisPubSub tests).
    current: yes
    sources:
      - "commit b7f1057a, 2025-12-23, quote: 'fix: refactor Quartz logging tests to use FakeLogger'"
      - "tests/Encina.UnitTests/Quartz/QuartzRequestJobTests.cs, 2026-09-25, paraphrase: FakeLogger<T> still in use, asserting on Collector.GetSnapshot()"
    destinations:
      - kind: rule
        target: "CLAUDE.md, Testing Standards > Assertion and helper libraries"
        status: planned
audit:
  checklist: 1
  date: 2026-09-25
  verdict: conforms-with-na
  record: "docs/knowledge/audits/issue-6.md"
remediation:
  - "issue-6-document-fakelogger-pattern (draft, not yet opened)"
review: draft
```

## Audit result (docs/knowledge/audits/issue-6.md)

---
issue: 6
checklist: 1
date: 2026-09-25
verdict: conforms-with-na
---

# Audit — issue #6

**Scope.** Issue #6's own diff (commit b7f1057a, 2025-12-23) touched only test files: `tests/Encina.Quartz.Tests/QuartzRequestJobTests.cs`, `QuartzNotificationJobTests.cs`, their `.csproj`, and `Directory.Packages.props`. These now live at `tests/Encina.UnitTests/Quartz/QuartzRequestJobTests.cs` and `QuartzNotificationJobTests.cs` (moved by the 2026 test-consolidation, traced with `git log --follow`). The issue's concern is narrow: whether the tests correctly verify `[LoggerMessage]` output. The production code they test (`src/Encina.Quartz/QuartzRequestJob.cs`, `QuartzNotificationJob.cs`, `Log.cs`, `JobFailure.cs`, `EncinaJobFailureData.cs`) was created by an earlier issue (`8fa9c402 feat: add job scheduling tests and rename database providers`, pre-dating #6) and is read here only as context to judge test correctness, not as the audited unit. Package-wide checks unrelated to this issue's concern (AUD-07, AUD-09 through AUD-12, AUD-14 through AUD-18) are marked not applicable to #6 and belong to the issue(s) that created `Encina.Quartz`.

| ID | Outcome | Evidence |
|---|---|---|
| AUD-01 | pass | The code still does exactly what #6 decided: `QuartzRequestJobTests.cs` and `QuartzNotificationJobTests.cs` still use `FakeLogger<T>` and `Collector.GetSnapshot()` today (2026-09-25), no drift, no later ADR/issue reversing it. |
| AUD-02 | n/a | #6 is a test-only fix, not a new entity/store/behavior/service; the 12 cross-cutting functions were evaluated by the issue that created `QuartzRequestJob`/`QuartzNotificationJob`, not by #6. |
| AUD-03 | pass | `.github/coverage-manifest/Encina.Quartz.json` has entries for `QuartzRequestJob.cs` and `QuartzNotificationJob.cs` (targets: unit 60, guard 15, property 26 for the package; no contract/integration flags declared). Measured directly from Cobertura XML (`dotnet test --collect "XPlat Code Coverage"`, this worktree, 2026-09-25): unit-flag line coverage on both files is 100% (82/82 and 66/66 lines), guard-flag 26.8%/33.3%, property-flag 82.9%/72.7% — all above the package's per-flag targets. |
| AUD-04 | pass | Every assertion in `QuartzRequestJobTests.cs`/`QuartzNotificationJobTests.cs` reads `_logger.Collector.GetSnapshot()`, a real sink populated by the actual `[LoggerMessage]`-generated calls; none is reflection-only or type-only. Verified by `adversarial-reviewer` (2026-09-25) and by reading the files directly. |
| AUD-05 | pass | Unit (68 tests), guard (9 tests, `tests/Encina.UnitTests/Quartz/Guards/*` + `tests/Encina.GuardTests/Quartz/QuartzGuardTests.cs`) and property (18 tests, `tests/Encina.PropertyTests/Web/Quartz/*`) all exist and pass (`dotnet test --filter FullyQualifiedName~Quartz`, 2026-09-25: 68/68, 9/9, 18/18, 0 skipped). Integration tests also exist (`tests/Encina.IntegrationTests/Web/Quartz/QuartzJobIntegrationTests.cs`) though not required by the manifest (no `integration` flag declared for this non-database package). |
| AUD-06 | n/a | Issue #6's `type` is `debt` (failing/skipped tests due to a test-framework limitation), not `bug`; there is no production defect to regression-test. The 7 originally-skipped tests themselves are the regression coverage for the original skip. |
| AUD-07 | n/a | `Encina.Quartz` is a scheduling adapter, not one of the 10 database/8 caching/5 lock/3 validation provider categories; not provider-dependent in the sense of AUD-07. |
| AUD-08 | pass | `src/Encina.Quartz/Log.cs` EventIds 4050–4061 are packed sequentially inside the registered range `EventIdRanges.Quartz = (4050, 4099)` (`src/Encina/Diagnostics/EventIdRanges.cs:191`). `Encina.Quartz` is listed in `EncinaEventIdAllocationTests`'s `AssemblyRanges` map (not independently re-verified by re-running the architecture test in this narrow audit; the range registration itself was read directly). |
| AUD-09 | n/a to #6 | `PublicAPI.*.txt` completeness for `Encina.Quartz` was set by the issue that created the package; #6 added no public API. |
| AUD-10 | n/a to #6 | XML documentation on `QuartzRequestJob`/`QuartzNotificationJob` predates #6 and was read as excellent (full `<remarks>`, `<exception>` tags) but is not this issue's concern. |
| AUD-11 | n/a to #6 | The `Encina.Quartz` README describes the whole package and predates #6; not re-reviewed here as it is not this issue's concern. |
| AUD-12 | n/a | #6 is not a security/compliance/audit/personal-data unit itself; the fail-closed behavior of the jobs it tests (`Left` always throws) was read and confirmed correct as context, but was decided by the issue that created the jobs. |
| AUD-13 | pass | Read `JobFailure.cs:60-76` and `Log.cs`: `EncinaError.Message` never reaches a log line or exception message — only the error code and `Transient`/`Permanent` classification are recorded. Tests assert this directly with PII-shaped payloads (`QuartzRequestJobTests.cs:78-99`, `QuartzNotificationJobTests.cs:136-164`), added for #1173 after #6 closed — a strict improvement over #6's own scope. |
| AUD-14 | n/a to #6 | No `DateTime.UtcNow`/`DateTimeOffset.UtcNow` in the files read; the jobs have no time-dependent behavior to begin with. Not #6's concern. |
| AUD-15 | n/a | `Encina.Quartz` has no options class with a password/connection-string/token/key in the files touched by #6 or read as context (`EncinaQuartzOptions.cs` was not part of #6's scope). |
| AUD-16 | n/a | No database calls in the audited unit. |
| AUD-17 | n/a to #6 | `ServiceCollectionExtensions.AddEncinaQuartz` registration completeness was not #6's concern (#6 touched no registration code); read briefly as context, not deeply audited here. |
| AUD-18 | pass | No `[Obsolete]`, compatibility alias or migration helper in any file read. |

## Specialist passes

- **adversarial-reviewer** (foreground, 2026-09-25): reviewed all files in scope against the issue's goal and CLAUDE.md's #1181-promoted rules (registration completeness, errors never swallowed, fail-closed, no `EncinaError.Message` leaks). Verdict: **merge**, no blocker or major finding. Two minor findings (see below).
- **docs-reviewer**: skipped — issue #6 shipped no documentation change and its own scope (test refactor) has no README/docs surface to review.
- Second test-focused `adversarial-reviewer`: not run separately; the single pass above already covered test quality (reflection-only tests, sleeps, shared state, coverage per flag) because the scope is small (2 production files + test files).

## Findings

1. **Minor.** `tests/Encina.UnitTests/Quartz/QuartzRequestJobTests.cs:92-99,137-141,156-160,176-180` locate the target log entry by `Message.Contains(...)` substring, while the equivalent notification-job test (`QuartzNotificationJobTests.cs:157`) pins the exact `EventId` (4059). No functional risk today (no message collision), but the two files are inconsistent in rigor.
2. **Minor.** Guard-clause tests for `QuartzRequestJob`/`QuartzNotificationJob` constructors and `Execute(null)` are duplicated almost verbatim between `tests/Encina.UnitTests/Quartz/Guards/*GuardsTests.cs` (counts toward the `unit` flag) and `tests/Encina.GuardTests/Quartz/QuartzGuardTests.cs` (the flag the manifest expects for `guard`). No stated reason for the split; maintenance duplication only.
3. **Knowledge gap (not a code finding).** The `FakeLogger<T>` pattern that fixed #6 is now used in 14+ test files across the codebase but is not documented anywhere in `CLAUDE.md`'s Testing Standards, unlike the Shouldly/FluentAssertions rule that sits right next to where it would go.

## Remediation

Findings 1 and 2 were judged too small (pure test-hygiene, no behavior or coverage risk) to justify a P0–P3 defect issue on their own; per SPEC-003 §5.4 they are folded into one `[DEBT]` draft together with finding 3 rather than each becoming a separate issue:

- `artifacts/issues/quartz-test-hygiene-and-fakelogger-documentation.md` (drafted, not yet opened) — covers findings 1, 2 and 3.

No duplicate existing issue was found (`gh issue list --state open --search "FakeLogger"` and `"LoggerMessage testing pattern"`, 2026-09-25: no hits).


