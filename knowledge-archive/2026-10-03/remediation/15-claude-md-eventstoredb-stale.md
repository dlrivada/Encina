<!-- issue
title: [DEBT] CLAUDE.md still lists Encina.EventStoreDB as "(future)" instead of deprecated per ADR-027
labels: technical-debt, area-documentation
milestone: v0.14.0 — Hardening
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

Found by the SPEC-003 audit of #15. ADR-027 ("Marten Is the Event-Sourcing Provider; EventStoreDB Is Deprecated", Accepted, recorded 2026-09-22) states `Encina.EventStoreDB` "receives no new capabilities, no cross-cutting integrations and no place in the 1.0 package list". `CLAUDE.md`'s "Event Sourcing Providers (1 primary)" table still lists it as a live, forward-looking option.

## Location

- **File(s)**: `CLAUDE.md:331`
- **Package(s)**: N/A (documentation only)

## Current Behavior

`CLAUDE.md:331` reads: `| **Encina.EventStoreDB** | EventStoreDB | Dedicated event store (future) |` — implying EventStoreDB is a planned/future provider.

## Expected Behavior

The table reflects ADR-027: `Encina.Marten` is the sole event-sourcing provider; `Encina.EventStoreDB` is deprecated (no new capabilities, excluded from 1.0), not "future". The row should say "(deprecated, see ADR-027)" or be removed from the "primary" table entirely with a footnote.

## Root Cause

ADR-027 was written after `CLAUDE.md`'s Event Sourcing Providers table, and the table was never updated to match. This is a smaller instance of the same documentation-destination gap the SPEC-003 audit is designed to catch: a decision reaches an ADR but not the other places a contributor actually reads (project history: similar to #1336's finding for issue #12).

## Proposed Fix

Update `CLAUDE.md:331` (and the section heading "Event Sourcing Providers (1 primary)" if needed) to state EventStoreDB is deprecated per ADR-027, linking the ADR the same way the Oracle/SQLite removal notes already do elsewhere in the same file.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

Found by the SPEC-003 audit of #15. Related: ADR-027, #1336 (same class of ADR-vs-CLAUDE.md drift for a different issue).
