<!--
title: [DEBT] ADR-019 lists EventStoreDB as a live option in event sourcing comparison
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

The ADR-019 document (`docs/architecture/adr/019-compliance-event-sourcing-marten.md`) contains a comparison table at line 38 that lists "Marten/EventStoreDB (specialized)" as the option for Event Sourcing. This line is factually stale because it presents EventStoreDB as a current, live specialized option, despite the decision being made earlier and the project history indicating that specific choices had been settled. The ADR is marked as Accepted and dated 2026-03-14, but the row itself was stale on the day it was written.

## Location

- **File(s)**: `docs/architecture/adr/019-compliance-event-sourcing-marten.md`
- **Package(s)**: N/A (Documentation)

## Current Behavior

The table in ADR-019 at line 38 displays the following row:
`| **Event Sourcing** | Could store events in any DB table | Marten/EventStoreDB (specialized) |`
It lists EventStoreDB alongside Marten as a valid specialized option without any deprecation note or indication that it is no longer a live candidate.

## Expected Behavior

The comparison table in ADR-019 should accurately reflect the status of EventStoreDB at the time of the decision. If EventStoreDB was not the selected option or was excluded from the final decision scope, the table should either remove it from the "specialized" options list or add a clarifying note indicating it is not the selected implementation path, ensuring the documentation does not mislead readers about the current architectural choices.

## Root Cause

The ADR was drafted with a comparison table that included EventStoreDB as a potential option. However, the specific row became stale prior to or at the time of writing because the project context (referenced in issue #17) had already moved past the consideration of EventStoreDB as a live alternative, or the decision criteria had narrowed in a way that made the inclusion of EventStoreDB in that specific comparison cell inaccurate. ADRs are generally not revised after acceptance, but this specific data point was incorrect at the time of authoring.

## Proposed Fix

Review the history and context of the decision regarding Event Sourcing providers. Update line 38 of `docs/architecture/adr/019-compliance-event-sourcing-marten.md` to remove "EventStoreDB" from the specialized options cell if it was not a valid candidate at the time of the ADR's creation, or add a footnote clarifying why it is listed or if it was excluded from the final selection. Ensure the table accurately reflects the options considered and rejected/accepted at the time of the decision.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues
- #1177 - possibly related (the local model proposed it as a duplicate; the evidence check rejected it)

#17, #1202