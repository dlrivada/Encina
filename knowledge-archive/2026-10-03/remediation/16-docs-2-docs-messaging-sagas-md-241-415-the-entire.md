<!--
title: [DEBT] Docs: "Choreography Sagas" section documents a non-functional feature with zero implementation
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

The "Choreography Sagas" section in `docs/messaging/sagas.md` (lines 241-415) presents Choreography as a complete, working, production-ready pattern with a full worked example including events, reactions, compensation, and an event bus. However, the code stage audit confirms that zero concrete implementation and zero DI registration exist for Choreography anywhere in the repository; it consists only of interfaces, an options class, and error-code constants. This constitutes a Diátaxis/accuracy failure because the documentation describes a feature with no working path, independent of any specific API-name errors. Additionally, the Comparison Table (`sagas.md:419-432`) and both "Recommended: Choreography" examples (`sagas.md:482-501`) incorrectly recommend this non-functional pattern for real-world scenarios.

## Location

- **File(s)**: `docs/messaging/sagas.md` (lines 241-415, 419-432, 482-501)
- **Package(s)**: Encina.Documentation

## Current Behavior

The documentation presents Choreography Sagas as a usable, production-ready pattern. It provides a complete worked example that a reader expects to be functional, including specific implementations for `IChoreographyEventBus` and `IChoreographyStateStore` and clear instructions for wiring these components. In reality, no such implementations or registration surfaces exist in the codebase, making it impossible for a user to run the documented example.

## Expected Behavior

The documentation must accurately reflect the current state of the library. Since Choreography is currently interface-only with no concrete implementations or DI registrations, the documentation should either:
1. Clearly mark the feature as experimental, incomplete, or not yet available for production use.
2. Remove the "working example" aspects that imply functionality that does not exist.
3. Avoid recommending Choreography for real-world scenarios until it is fully implemented and registered.

## Root Cause

The documentation was written or maintained without synchronization to the actual implementation status of the Choreography feature. The docs assumed a level of completeness (concrete implementations, DI wiring) that has not been delivered by the codebase, leading to a gap between the promised functionality and the available API surface.

## Proposed Fix

1. Review the "Choreography Sagas" section in `docs/messaging/sagas.md` (lines 241-415).
2. Add clear warnings or status badges indicating that the feature is not yet production-ready or fully implemented.
3. Modify or remove the "worked example" code blocks that imply functionality for `IChoreographyEventBus` and `IChoreographyStateStore` if no concrete types are available for users to instantiate.
4. Update the Comparison Table (lines 419-432) and the "Recommended: Choreography" examples (lines 482-501) to reflect that this pattern is not currently usable in production or is limited to experimental use.
5. Ensure alignment with the SKILL.md §3 rule 3 regarding the "Real API" checklist item.

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