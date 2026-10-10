## Scope
All paths verified present today (Test-Path, no renames). `git log --follow --format=%h` per file (924ca2e5 is the original in every case; later commits listed newest first):
- Messaging/Health: `IEncinaHealthCheck.cs` none; `ProviderHealthCheckOptions.cs` none; `HealthCheckResult.cs` b4116642; `EncinaHealthCheck.cs` b4116642, 73431b94, 7b586da4, 0e8b3b86; `OutboxHealthCheck.cs` 73431b94, 17252d45, 5496b46a; `InboxHealthCheck.cs` 73431b94, 5496b46a; `SagaHealthCheck.cs` 5496b46a; `SchedulingHealthCheck.cs` 73431b94, 5496b46a, 06c9e316; `DatabaseHealthCheck.cs` b4116642, 1a15abe2, 0e8b3b86.
- AspNetCore/Health: `EncinaHealthCheckAdapter.cs` and `CompositeEncinaHealthCheck.cs` b4116642; `HealthCheckBuilderExtensions.cs` nine later commits (c696eb9a, 17252d45, b81dac55, 1d339434, 8f731dab, 04d45256, 699e459f, 74f590c8, 1a6143d3).
Scope list:
- `src/Encina.Messaging/Health/`: `IEncinaHealthCheck.cs`, `HealthCheckResult.cs`, `EncinaHealthCheck.cs`, `OutboxHealthCheck.cs`, `InboxHealthCheck.cs`, `SagaHealthCheck.cs`, `SchedulingHealthCheck.cs`. The closing commit 924ca2e5 also added `DatabaseHealthCheck.cs` and `ProviderHealthCheckOptions.cs` (not in the issue comment; belong to the #113 direction) and touched `MessagingConfiguration.cs`, `MessagingServiceCollectionExtensions.cs`, `PublicAPI.Unshipped.txt`.
- `src/Encina.AspNetCore/Health/`: `EncinaHealthCheckAdapter.cs`, `CompositeEncinaHealthCheck.cs`, `HealthCheckBuilderExtensions.cs`.
- Tests today: the original `tests/Encina.Tests/Health/*` (7 files) were deleted in the test consolidation (65826302). Successors: `tests/Encina.UnitTests/AspNetCore/Health/{EncinaHealthCheckAdapter,CompositeEncinaHealthCheck,HealthCheckBuilderExtensions}Tests.cs`, `tests/Encina.UnitTests/Messaging/Health/{HealthCheckResult,SagaHealthCheck,SchedulingHealthCheck}Tests.cs`, `HealthChecksTests.cs`, `HealthCheckOptionsTests.cs`, and `tests/Encina.GuardTests/Messaging/Health/HealthChecksGuardTests.cs` (Outbox and Inbox checks are only in the combined HealthChecksTests/guard files; no per-class Outbox/Inbox test files remain).
- Delivered by commit 924ca2e5 (no PR), authored 2025-12-25T23:48Z, after closed_at 2025-12-25T10:31:28Z.
- Not scoped: nothing removed on purpose. Packages the body listed (core Encina, all database, transport, cache providers) were not touched by this issue.
- Open questions from the pre-draft for the code stage: the Degraded/Unhealthy mapping in the adapter (switch present, unknown status falls back to Unhealthy), the composite aggregation rule (documented in XML docs), and Saga/Scheduling stuck/overdue detection (SchedulingHealthCheck uses OverdueTolerance and thresholds).

## Destinations
- Decisions 1 and 2 (abstractions in Encina.Messaging; adapter, composite and builder extensions) are MISSING from `docs/guides/health-checks.md`: 0 matches for `Encina.Messaging`, `EncinaHealthCheckAdapter` or `CompositeEncinaHealthCheck`, no Healthy/Degraded/Unhealthy mapping and no composite worst-status rule; `IEncinaHealthCheck` appears once (`:188`, in a sample). The AspNetCore README (`:680-840`) and Messaging README (`:533-617`) do not state them either. Destination is `planned` (concept and reference page work in #1955). Decision 3 (messaging checks vs infrastructure) is stated by the guide (`:13`, `:19-20`): present. ADR-018 lists "Health Checks (`IEncinaHealthCheck`)" as cross-cutting function 4: present. AGENTS.md section 6 row 4 also names `IEncinaHealthCheck`: present.
- Regression tests: present (see Scope), but 52 tests claimed in the comment were consolidated; count today not verified.
- Handler-registration, database, transport and cache health checks from the body: not delivered here; follow-ups below. No destination needed (recorded as `current: no`).
- Pre-draft inaccuracies fixed: `closed_at` format, `linked_prs: [unknown]`, packages (it listed Encina and Encina.AspNetCore; the code is in Encina.Messaging and Encina.AspNetCore), and "Rules and lessons" were an inference, dropped; "Open questions" are kept here, not in the record.

## Successor and duplicate issues
Not a rejected/duplicate issue. Follow-ups named in the closing comment, verified with `gh issue view`:
- #113 "Automatic health checks per infrastructure provider": CLOSED, completed.
- #114 "Health checks for modules in Modular Monolith": CLOSED, completed.
- #115 "Health Checks integration with AspNetCore.HealthChecks.*": CLOSED, completed (created 2025-12-25T10:31:09Z, closed 2025-12-26T10:38:39Z; the timeline's 2026-02-17 cross-reference is a later mention, not the creation).
The body's "handler health status reporting" and "Kubernetes readiness/liveness" items have no explicit follow-up; the code stage should check whether they exist.

## Lessons for the pipeline
- A timeline `cross-referenced` event is dated when the reference was made, not when the referencing issue was created (#115 shows 2026-02-17 but was created 2025-12-25); take creation dates from `gh issue view --json createdAt`.
- Print the per-file `git log --follow` output instead of summarising it; most files had later commits (verifier correction 3).
- Grep the destination for each decision's own symbols before marking it `done`; a page that mentions the topic (messaging vs infrastructure) does not carry the adapter or composite rules (verifier correction 5).
- Take closing dates of follow-ups from `gh issue view --json closedAt`, never "today" (verifier correction 4).
- Issue duration computed from gh timestamps: 2025-12-24T13:23:18Z to 2025-12-25T10:31:28Z is 21h 08m 10s.
