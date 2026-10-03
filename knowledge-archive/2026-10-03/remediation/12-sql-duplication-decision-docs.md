<!-- issue
title: [DEBT] Document the accepted SQL-dialect duplication decision (ADR + CLAUDE.md)
labels: technical-debt, documentation
milestone:
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [x] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

Found by the SPEC-003 audit of #12.

Issue #12 centralized messaging-pattern behavior (`InboxPipelineBehavior`, `OutboxPostProcessor`,
the `IInboxMessageFactory`/`IOutboxMessageFactory`/`ISagaStateFactory`/`IScheduledMessageFactory`
factory interfaces, `SagaOrchestrator`, `SchedulerOrchestrator`) into `Encina.Messaging`, and
accepted **intentional** SQL-dialect duplication in the per-provider Store classes
(`OutboxStore*`, `InboxStore*`, `SagaStore*`, `ScheduledMessageStore*`), excluding them from
SonarCloud's copy-paste-detection (CPD) via `sonar.cpd.exclusions`.

That decision — and its rationale (provider independence, clarity, performance, maintainability)
— is documented only in the issue's own comment thread and in a comment inside
`.github/workflows/sonarcloud.yml`. It is not recorded in an ADR under `docs/architecture/adr/`,
and `CLAUDE.md`'s "Provider Coherence" section (under "Satellite Packages Philosophy") does not
state the rule. A contributor reading `CLAUDE.md` today cannot learn why Store SQL is allowed to
duplicate across providers while everything else must be centralized, or why
`.github/workflows/sonarcloud.yml`'s `sonar.cpd.exclusions` lists the paths it does.

## Location (File(s)/Package(s))

- **File(s)**: `docs/architecture/adr/` (new ADR), `CLAUDE.md` (Provider Coherence subsection)
- **Package(s)**: N/A (documentation only; the decision spans `Encina.Messaging` and all
  Dapper/ADO/EntityFrameworkCore/MongoDB provider packages)

## Current Behavior

The rationale for excluding Store implementations from SonarCloud CPD, and the boundary between
"must be centralized" (behavior) and "may duplicate" (SQL), exists only in issue #12's closed
comment thread and a workflow-file comment. `CLAUDE.md` and `docs/architecture/adr/` are silent
on it.

## Expected Behavior

1. A new ADR in `docs/architecture/adr/` records: what was centralized into `Encina.Messaging`
   and why; the decision to accept per-provider SQL-dialect duplication in Store classes; and the
   SonarCloud CPD exclusion that operationalizes that decision.
2. `CLAUDE.md`'s "Provider Coherence" subsection gains one bullet stating the rule — Store SQL
   implementations may duplicate structure across providers when SQL dialects differ; this is
   intentional and excluded from static-analysis duplication metrics; only the surrounding
   non-SQL-specific behavior (pipeline behaviors, orchestrators, factories) must be centralized in
   `Encina.Messaging` — linking to the new ADR.

## Root Cause

Issue #12 was resolved before this project's per-issue documentation-destination discipline
(SPEC-003) was in place: the decision was recorded where it was made (issue comments, a workflow
comment) but never propagated to the durable references (ADR, `CLAUDE.md`) a later contributor
would actually consult.

## Proposed Fix

1. Add `docs/architecture/adr/0NN-accept-sql-dialect-duplication-in-messaging-stores.md` (next
   free ADR number) capturing the decision, alternatives considered (abstract base classes, SQL
   templating — both already listed in issue #12's own body) and the four-point rationale from the
   issue's resolving comment (independence, clarity, performance, maintainability).
2. Add one bullet to `CLAUDE.md`'s "Provider Coherence" subsection stating the rule above and
   linking the new ADR, following the existing style of the Oracle/SQLite removal notes in the
   Multi-Provider Implementation Rule section.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

#12 (SPEC-003 audit source)
