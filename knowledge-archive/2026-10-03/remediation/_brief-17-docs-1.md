Draft ONE remediation issue file for a finding from the SPEC-003 audit of closed GitHub issue #17 of
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
- Related Issues: include #17 and any of these candidate open issues that are related but are NOT
  the same problem (a same-problem duplicate must never reach this step): #1347: [DEBT] CLAUDE.md still lists Encina.EventStoreDB as "(future)" instead of deprecated per ADR-027; #585: [FEATURE] IDeadLetterStore for EventStoreDB; #591: [FEATURE] IRoutingSlipStore for Marten and EventStoreDB (Event Sourcing); #593: [FEATURE] IChoreographyStateStore: Marten and EventStoreDB Event Sourcing Implementations; #576: [FEATURE] IAuditLogStore for EventStoreDB; #323: [FEATURE] Advanced Snapshot Strategies (TimeInterval, BusinessBoundary); #331: [FEATURE] EventQL-Style Preconditions for Fine-Grained Consistency; #327: [FEATURE] Event Archival and Stream Compaction; #1372: [DEBT] Package and provider counts differ between the GitHub description, docs/index.md, README.md and the code; #893: [EPIC] v0.23.0 — Release Preparation
- Priority: tick High (from the finding's severity: Blocker -> High, Major -> Medium, Minor/Unknown -> Low).
- Effort Estimate: use your own judgement (Small/Medium/Large) from the finding's scope.
- This is a documentation gap: tick only the 'Documentation gap' box in the Type section (leave the other Type boxes unticked).
