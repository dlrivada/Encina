<!--
title: [DEBT] Fix fictitious choreography API symbols in docs/messaging/sagas.md
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

The code block in the Choreography "Configuration" section of `docs/messaging/sagas.md` references API symbols that do not exist in the Encina .NET library. Specifically, `config.UseChoreography = true;` and `services.AddEncinaChoreography(options => { options.StuckSagaTimeout = ...; })` are non-existent. The options class `ChoreographyOptions` does not have a `StuckSagaTimeout` property; its actual properties include `AutoCompensateOnFailure`, `SagaTimeout`, `PersistState`, `MaxCompensationRetries`, and `CompensationRetryDelay`. This violates the SKILL.md §3 house rule 3 ("Every code example names real API").

## Location

- **File(s)**: `docs/messaging/sagas.md:291-300`
- **Package(s)**: Encina.Messaging

## Current Behavior

The documentation lists `UseChoreography` and `AddEncinaChoreography` as valid configuration entries, and lists `StuckSagaTimeout` as a valid option. Developers following these instructions will encounter compiler errors or missing member exceptions because these symbols are absent from the source code.

## Expected Behavior

The documentation should only reference existing API symbols. The code block must be updated to use the actual `ChoreographyOptions` properties (e.g., `SagaTimeout` instead of `StuckSagaTimeout`) and remove or replace non-existent methods like `AddEncinaChoreography` and properties like `UseChoreography`.

## Root Cause

The documentation was likely written based on a planned or outdated API design that was not implemented, or the API was renamed/refactored without updating the documentation.

## Proposed Fix

1. Verify the current public API for enabling choreography and setting its options in `Encina.Messaging`.
2. Update the code block in `docs/messaging/sagas.md` to reflect the actual API:
   - Remove or correct `config.UseChoreography`.
   - Remove or correct the call to `AddEncinaChoreography`.
   - Replace `options.StuckSagaTimeout` with the correct property name (e.g., `options.SagaTimeout`).
3. Ensure all other code examples in the file comply with SKILL.md §3 house rule 3.

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