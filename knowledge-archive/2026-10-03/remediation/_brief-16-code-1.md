Draft ONE remediation issue file for a finding from the SPEC-003 audit of closed GitHub issue #16 of
the Encina .NET library (code stage, finding 1, severity Blocker). Use
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
  the same problem (a same-problem duplicate must never reach this step): #1418: [BUG] Encina.ADO.SqlServer, Encina.ADO.MySQL and the three Dapper packages read wall-clock time and call ADO.NET synchronously; #1329: [DEBT] FunctionalShardedRepository (ADO x3, Dapper x3, EF Core, MongoDB) logs via raw ILogger extension methods instead of Log.cs; #1355: [DEBT] crap-gate.cs diff parsing and exemption matching have known edge-case gaps; #1331: [DEBT] Document the FakeLogger<T> testing pattern and tidy Quartz log-assertion/guard-test duplication; #1340: [DEBT] Six near-identical TransactionPipelineBehavior guard test files duplicate one shared class; #1419: [REFACTOR] Make IReferenceTableStoreFactory.CreateForShard asynchronous across all 10 providers; #1385: [DEBT] Remove the stale dashboard data copies tracked under docs/*/data now that Pages is authoritative; #1301: [DEBT] About 30 health checks put raw exception messages into their unhealthy results; #1171: [DEBT] PublicAPI analyzers not enforced in Messaging and EF Core; stale API files and coverage manifests; #1202: [DEBT] Compliance READMEs and ADR-019 describe types and provider coverage that do not exist; state the PostgreSQL/Marten requirement
- Priority: tick High (from the finding's severity: Blocker -> High, Major -> Medium, Minor/Unknown -> Low).
- Effort Estimate: use your own judgement (Small/Medium/Large) from the finding's scope.
- This is a documentation gap: tick only the 'Documentation gap' box in the Type section (leave the other Type boxes unticked).
