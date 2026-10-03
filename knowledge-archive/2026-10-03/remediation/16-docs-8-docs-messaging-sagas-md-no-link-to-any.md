<!--
title: [DEBT] Add internal cross-links to sagas documentation
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

The `docs/messaging/sagas.md` documentation lacks links to adjacent Encina quadrants. Specifically, the "Further Reading" section links only to external sites and omits references to internal resources such as the `Encina.Messaging` package README, reference pages for `SagaOrchestrator`/`ISagaStore`, or the ADR noted as missing in finding 5. This fails the checklist's "Neighbours" requirement.

## Location

- **File(s)**: `docs/messaging/sagas.md:553-557`
- **Package(s)**: Encina.Messaging

## Current Behavior

The "Further Reading" section at lines 553-557 contains links only to three external sites: microservices.io, learn.microsoft.com, and martinfowler.com. There are no links to the `Encina.Messaging` package README, to a reference page for `SagaOrchestrator`/`ISagaStore`, or to the ADR.

## Expected Behavior

The "Further Reading" section should include links to adjacent Encina quadrants, including the `Encina.Messaging` package README, reference pages for `SagaOrchestrator`/`ISagaStore`, and the ADR, satisfying the "Neighbours" row of the documentation checklist.

## Root Cause

The documentation was likely written focusing on external educational resources without accounting for the internal documentation structure and cross-referencing requirements of the Encina library.

## Proposed Fix

Update the "Further Reading" section in `docs/messaging/sagas.md` to include relative or absolute links to the `Encina.Messaging` package README, internal reference pages for key types like `SagaOrchestrator` and `ISagaStore`, and the relevant ADR, ensuring compliance with the "Neighbours" documentation checklist item.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

#16