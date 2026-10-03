<!--
title: [DEBT] Messaging transports count in docs/messaging/index.md lacks required citation
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

The documentation page `docs/messaging/index.md` contains a statement regarding the number of supported messaging transports that does not follow the citation conventions established in SKILL.md §3. Specifically, the count for facts not measured by dashboards requires a citing sentence with a date and command.

## Location

- **File(s)**: `docs/messaging/index.md` (lines 14-24, "Messaging Transports" table)
- **Package(s)**: Encina (Docs)

## Current Behavior

The "Messaging Transports" table in `docs/messaging/index.md` states that "Encina supports **10 messaging transports**". This statement lacks the required citation sentence (date + command) mandated by SKILL.md §3 house rule 2 for counts that no dashboard measures.

## Expected Behavior

The count of messaging transports in `docs/messaging/index.md` should include a citation sentence specifying the date and the command used to verify the count, in accordance with SKILL.md §3 house rule 2.

## Root Cause

The documentation was likely written before the citation convention in SKILL.md §3 was strictly enforced or was omitted in error. Although the count currently matches AGENTS.md §5 (an authoritative, versioned source), the page itself does not explicitly cite the source or method of verification as required by house rules.

## Proposed Fix

Add a citation sentence to the "Messaging Transports" section in `docs/messaging/index.md` that includes the current date and the specific command or source reference used to determine the count of 10 transports, satisfying the requirement for "counts that no dashboard measures."

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues
- #1177 - partially related (it covers only part of this finding)

- #16