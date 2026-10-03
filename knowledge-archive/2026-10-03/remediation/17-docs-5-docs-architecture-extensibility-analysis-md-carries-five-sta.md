<!--
title: [DEBT] extensibility-analysis.md still presents Encina.EventStoreDB as live/future despite ADR-027 deprecation
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

The documentation file `docs/architecture/extensibility-analysis.md` contains five stale references that treat `Encina.EventStoreDB` as a live, viable, or future capability. These references contradict ADR-027, which deprecates `Encina.EventStoreDB` in favor of Marten. The document is dated 2025-12-14, prior to the deprecation decision, and has not been updated since. It lacks a "snapshot, not maintained" banner (unlike `ENGINEERING-HANDBOOK.md`) and is not excluded from the navigation (`nav_exclude`), leading to the propagation of outdated architectural guidance.

## Location

- **File(s)**: `docs/architecture/extensibility-analysis.md`
    - Line 207: "✅ Event publishing (RabbitMQ, Kafka, EventStoreDB)"
    - Line 748: "### 2.10 Event Sourcing (EventStoreDB, Marten)"
    - Line 781: "**Satellite Package Opportunity:** `Encina.EventStoreDB`"
    - Line 1009: "| `Encina.EventStoreDB` | Event sourcing helpers | 🟢 Low |"
    - Line 1023: "Event sourcing with EventStoreDB"
- **Package(s)**: N/A (Documentation)

## Current Behavior

The document currently lists `Encina.EventStoreDB` alongside RabbitMQ and Kafka as a viable event publishing target (Line 207). It pairs EventStoreDB with Marten as co-equal, currently supported event-sourcing options in section headers (Line 748). It actively recommends building a new `Encina.EventStoreDB` satellite package (Line 781) and lists it as a planned, low-effort package in the roadmap table (Line 1009). Finally, it proposes an "Event sourcing with EventStoreDB" sample for the pre-1.0 samples repository (Line 1023).

## Expected Behavior

The document should reflect the decision in ADR-027 that `Encina.EventStoreDB` is deprecated and receives no new capabilities or place in the 1.0 package list. References to `Encina.EventStoreDB` as a recommended or future capability must be removed or corrected to indicate deprecation. The document should be clearly marked as a historical snapshot or frozen if not intended to be maintained, or updated to align with the current architectural decisions favoring Marten.

## Root Cause

The document was created on 2025-12-14, nine days before the deprecation commit for #17 (2025-12-23). It was never revisited after ADR-027 was issued, and it lacks the metadata (banner or nav exclusion) to indicate that it is historical content, causing it to be perceived as current guidance.

## Proposed Fix

Update `docs/architecture/extensibility-analysis.md` to remove or annotate the five identified references to `Encina.EventStoreDB` as deprecated. Specifically:
1. Remove EventStoreDB from the "Good For" list on line 207.
2. Update the section header on line 748 to reflect that EventStoreDB is deprecated.
3. Remove the "Satellite Package Opportunity" recommendation on line 781.
4. Remove the `Encina.EventStoreDB` row from the Satellite Package Roadmap table on line 1009.
5. Remove the "Event sourcing with EventStoreDB" sample proposal on line 1023.
Additionally, add a "snapshot, not maintained" banner to the document if it is to remain historical, or add `nav_exclude` to hide it from the main documentation navigation.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #17
- #1347