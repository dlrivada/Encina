Draft ONE remediation issue file for a finding from the SPEC-003 audit of closed GitHub issue #16 of
the Encina .NET library (docs stage, finding 1, severity Blocker). Use
ONLY the input finding text; never invent facts. Output EXACTLY the header comment block below followed by the
template body below it, keeping every '## ' header of the template body verbatim and in the same order, and
ticking a checkbox only from the options the template body itself lists:

<!--
title: [DEBT] <specific title drawn from the finding>
labels: technical-debt
milestone: 
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [ ] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

A clear description of the technical debt item.

## Location

- **File(s)**: `src/Encina.*/...`
- **Package(s)**: [e.g., Encina.ADO.Oracle]

## Current Behavior

What the code currently does (or doesn't do).

## Expected Behavior

What the code should do after addressing this debt.

## Root Cause

If known, explain why this debt exists.

## Proposed Fix

High-level description of how to fix this.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

Link any related issues here.

Guidance:
- Put the finding's file:line evidence in the Location (or Steps to Reproduce) section.
- Related Issues: include #16 and any of these candidate open issues that are related but are NOT
  the same problem (a same-problem duplicate must never reach this step): #1419: [REFACTOR] Make IReferenceTableStoreFactory.CreateForShard asynchronous across all 10 providers; #1329: [DEBT] FunctionalShardedRepository (ADO x3, Dapper x3, EF Core, MongoDB) logs via raw ILogger extension methods instead of Log.cs; #1270: [DEBT] Remove dead soft-delete and read-audit code; #1171: [DEBT] PublicAPI analyzers not enforced in Messaging and EF Core; stale API files and coverage manifests; #1416: [DEBT] docs/features/consent-management.md Error Handling example calls a non-existent store.RecordConsentAsync; #1343: [DEBT] Error-message leak static scan misses multi-line calls and ex.Message; leaks in SagaRunner and CDC cache invalidation; #1454: [BUG] EncinaError.Message still reaches OpenTelemetry activity tags/status in DataSubjectRights, DPIA and Retention; #1224: [DEBT] Remove the legacy AddApplicationMessaging alias
- Priority: tick High (from the finding's severity: Blocker -> High, Major -> Medium, Minor/Unknown -> Low).
- Effort Estimate: use your own judgement (Small/Medium/Large) from the finding's scope.
- This is a documentation gap: tick only the 'Documentation gap' box in the Type section (leave the other Type boxes unticked).
