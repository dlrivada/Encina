Draft ONE remediation issue file for a finding from the SPEC-003 audit of closed GitHub issue #17 of
the Encina .NET library (docs stage, finding 4, severity Blocker). Use
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
  the same problem (a same-problem duplicate must never reach this step): #1299: [DEBT] docs/INVENTORY.md is Spanish and full of hand-typed, unverified figures; #1362: [INFRA] SPEC-000 REQ-001/AC-001: committed package manifest and CI drift check (approved, never implemented); #1226: [SPIKE] ADR on the Verifactu boundary: the regulation-neutral primitives Encina ships (sequence, chained log, outbox pacing) and why they do not make its publisher a component producer; #1225: [SPIKE] Consistency between EF Core or Dapper application data and Marten compliance aggregates on one PostgreSQL; #1245: [SPIKE] EHDS readiness: logging component and EEHRxF export as extension points; #1452: [SPIKE] One-week comparison trial: pr-reviewer versus CodeRabbit on the same PRs; #1266: [SPIKE] Long-lived, queue-driven specialist agents with mechanically enforced ownership; #1327: [TEST] Stream/StreamDispatcher load-test justification, and unverified .NET 10 JIT bug citations left in CLAUDE.md and docs; #1347: [DEBT] CLAUDE.md still lists Encina.EventStoreDB as "(future)" instead of deprecated per ADR-027; #585: [FEATURE] IDeadLetterStore for EventStoreDB
- Priority: tick High (from the finding's severity: Blocker -> High, Major -> Medium, Minor/Unknown -> Low).
- Effort Estimate: use your own judgement (Small/Medium/Large) from the finding's scope.
- This is a documentation gap: tick only the 'Documentation gap' box in the Type section (leave the other Type boxes unticked).

Your previous reply still contained the template's own placeholder text, unchanged, on these lines:
- - **Package(s)**: [e.g., Encina.ADO.Oracle]

Replace every one of them with real content drawn from the finding; never leave a bracketed example, '#___',
an 'Example.Package' row or a literal 'Test N: Description' row untouched.
