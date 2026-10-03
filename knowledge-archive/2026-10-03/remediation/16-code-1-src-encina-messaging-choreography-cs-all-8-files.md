<!--
title: [DEBT] Choreography feature has zero implementation and DI registration but is documented as production-ready
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

The Choreography feature in `Encina.Messaging` exists only as API surface (interfaces, options class, and error-code constants) with zero concrete implementation and zero DI registration. However, `docs/messaging/sagas.md` presents a complete usage example for `config.UseChoreography` and `services.AddEncinaChoreography` and recommends Choreography in decision-table rows, falsely presenting it as a supported, production-ready path. This creates a critical documentation gap where the documentation does not match the actual state of the codebase.

## Location

- **File(s)**: `src/Encina.Messaging/Choreography/*.cs` (all 8 files), `src/Encina.Messaging/MessagingServiceCollectionExtensions.cs`, `src/Encina.Messaging/MessagingConfiguration.cs`, `docs/messaging/sagas.md`
- **Package(s)**: Encina.Messaging

## Current Behavior

The `Choreography` namespace contains only interfaces (`IChoreographyEventBus`, `IChoreographyStateStore`), an options class, and constants tracked in `PublicAPI.Unshipped.txt`. There are no concrete implementations and no DI registration methods (e.g., `AddEncinaChoreography`). `MessagingConfiguration.cs` lacks a `UseChoreography` property. Despite this, `docs/messaging/sagas.md:289-408` provides code examples using `config.UseChoreography = true;` and `services.AddEncinaChoreography(...)`, and lines 484 and 499 recommend Choreography as a valid architectural choice.

## Expected Behavior

The documentation in `docs/messaging/sagas.md` must accurately reflect that the Choreography feature is not yet implemented or registered. The usage examples and decision-table recommendations should be removed, marked as "Planned," or clearly labeled as non-functional until the implementation and DI registration are complete.

## Root Cause

The feature was designed as an API surface and documented as if it were complete, but the implementation and dependency injection integration were never finished or never included in the repository, leading to a mismatch between documentation and code.

## Proposed Fix

Update `docs/messaging/sagas.md` to remove the misleading usage examples and recommendations for Choreography. Clearly state that the feature is currently unavailable or in a pre-release/unimplemented state. Alternatively, if the feature is planned for immediate implementation, mark it clearly as "Not Yet Available" in the documentation.

## Priority

- [x] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

#16