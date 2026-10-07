# Delta scope of issue #24 (set rules-2026-10)

Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.

**Scope source:** the published record and the published stage files of the original audit (docs/knowledge/audits/24/stages/).

## Knowledge record (docs/knowledge/issues/24.md)

```yaml
schema: 1
nav_exclude: true
issue: 24
title: "[FEATURE] Health Check Abstractions - IHealthCheck integration"
closed: 2025-12-24
state_reason: completed
outcome: duplicate
duplicate_of: 35
type: feature
area: observability
review: verified
packages:
  - Encina.Messaging
  - Encina.AspNetCore
prs:
linked_prs:
knowledge:
audit:
  checklist: 1
  date: 2026-10-03
  verdict: not-audited
  record: "docs/knowledge/audits/issue-24.md"
remediation:
```

## From the original archivist.md (docs/knowledge/audits/24/stages/archivist.md)

Issue #24 shipped no code. It was closed 2025-12-24T11:52Z with the comment "Reverted - issue created in error". Its only timeline reference is commit 2b50a1ec (docs restructuring: `.claude/CLAUDE.md`, `ROADMAP.md`, `docs/history/2025-12.md`), verified with `git show --stat`; it lists #24 as pending work and changes no `src/` file. No linked PRs.

The real surface is the duplicate #35's work (commit 924ca2e5), mapped to today (all verified present):
- `src/Encina.Messaging/Health/` : `IEncinaHealthCheck.cs`, `EncinaHealthCheck.cs`, `HealthCheckResult.cs`, `OutboxHealthCheck.cs`, `InboxHealthCheck.cs`, `SagaHealthCheck.cs`, `SchedulingHealthCheck.cs` (also `DatabaseHealthCheck.cs`, `ProviderHealthCheckOptions.cs` from the commit, not re-checked).
- `src/Encina.AspNetCore/Health/` : `EncinaHealthCheckAdapter.cs`, `CompositeEncinaHealthCheck.cs`, `HealthCheckBuilderExtensions.cs`.
- Tests: the original `tests/Encina.Tests/Health/` no longer exists (the folder is gone); `tests/Encina.UnitTests/AspNetCore/Health/CompositeEncinaHealthCheckTests.cs` and `EncinaHealthCheckAdapterTests.cs` exist; I did not locate Messaging-side tests (OutboxHealthCheckTests etc.) by file name, so the test stage should search for them.
The code stage should audit this under #35, not #24.

## From the original code.md (docs/knowledge/audits/24/stages/code.md)

Issue #24 shipped no code: closed in error as a duplicate of #35. I read `artifacts/knowledge/stages/archivist.md` and `artifacts/knowledge/issues/24.md`. I confirmed the empty diff: the only commit referencing #24 is 2b50a1ec (`git show --stat`: `.claude/CLAUDE.md`, `ROADMAP.md`, `docs/history/2025-12.md`, no `src/` file), and there are no linked PRs. The proposed types exist today under `src/Encina.Messaging/Health/` (`IEncinaHealthCheck.cs` present). The code that delivered the feature belongs to #35 and is audited under #35; walking into it here would duplicate that audit. No scope correction.


