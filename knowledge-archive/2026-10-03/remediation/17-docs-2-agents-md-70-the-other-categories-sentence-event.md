<!--
title: [DEBT] AGENTS.md incorrectly lists EventStoreDB as a future provider contradicting ADR-027
labels: technical-debt
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

The `AGENTS.md` file, specifically the "Other categories" sentence, incorrectly states that EventStoreDB is a "future" provider for Event Sourcing. This statement directly contradicts ADR-027 and the decision made in issue #17 to deprecate `Encina.EventStoreDB` and keep only `Encina.Marten`. Since `AGENTS.md` is the binding rules file read by every session and subagent, this inaccuracy poses a significant risk by presenting deprecated technology as a planned future capability.

## Location

- **File(s)**: `AGENTS.md:70`
- **Package(s)**: Encina

## Current Behavior

Line 70 of `AGENTS.md` contains the text: "Event sourcing (Marten primary, EventStoreDB future; applies to aggregate repositories, projections, snapshots, crypto-shredding)." This implies EventStoreDB is a future/planned provider, which is factually incorrect given ADR-027.

## Expected Behavior

The sentence in `AGENTS.md` should reflect the current architectural decision by removing EventStoreDB from the list of providers or explicitly marking it as deprecated, ensuring it aligns with ADR-027 and the decision to retain only `Encina.Marten` for Event Sourcing.

## Root Cause

The documentation in `AGENTS.md` was not updated to reflect the decision made in issue #17 and codified in ADR-027 to deprecate `Encina.EventStoreDB`.

## Proposed Fix

Update line 70 in `AGENTS.md` to remove the reference to EventStoreDB as a "future" provider. The text should be revised to accurately state that Marten is the sole provider for Event Sourcing, in compliance with ADR-027.

## Priority

- [x] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues
- #1347 - possibly related (the local model proposed it as a duplicate; the evidence check rejected it)

- #17
- #1347