Draft ONE remediation issue file for a finding from the SPEC-003 audit of closed GitHub issue #16 of
the Encina .NET library (docs stage, finding 9, severity Minor). Use
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
  the same problem (a same-problem duplicate must never reach this step): #1372: [DEBT] Package and provider counts differ between the GitHub description, docs/index.md, README.md and the code; #1177: [DEBT] Documentation drift found while writing #80/#81: README install, patterns-guide API, ADR-001/006, index figures, Oracle row, validation how-to; #1363: [DEBT] Process docs and templates out of sync: duplicate ADR-007, stale HOW-ENCINA-IS-BUILT facts, agent tier table, PR and spike templates; #1043: [FEATURE] Wire OTLP exporter as opt-in option in Encina.OpenTelemetry; #1133: [BUG] EF Core filtered indexes use SQL Server-only filters: unquoted identifiers and 'IsRecurring = 1' break PostgreSQL DDL; #1302: [BUG] Encina.ADO.MySQL ProcessingActivities script fails with "key too long" on MySQL/utf8mb4; #1385: [DEBT] Remove the stale dashboard data copies tracked under docs/*/data now that Pages is authoritative; #658: [FEATURE] Add Guards.TryValidateGreaterThanOrEqual<T> ROP guard clause; #760: [FEATURE] Add TenantId to InboxMessage for tenant-scoped idempotency; #586: [FEATURE] IDeadLetterStore for Elasticsearch/OpenSearch
- Priority: tick Low (from the finding's severity: Blocker -> High, Major -> Medium, Minor/Unknown -> Low).
- Effort Estimate: use your own judgement (Small/Medium/Large) from the finding's scope.
- This is a documentation gap: tick only the 'Documentation gap' box in the Type section (leave the other Type boxes unticked).
