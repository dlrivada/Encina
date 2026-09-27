<!--
title: [DEBT] Sagas documentation describes non-existent choreography API symbols
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

The choreography configuration example in the sagas documentation references API symbols that do not exist in the codebase. Specifically, the code block suggests using `config.UseChoreography = true;` and `services.AddEncinaChoreography(...)`, and passes a `StuckSagaTimeout` option to the latter. None of these symbols are present in the Encina source code.

## Location

- **File(s)**: `docs/messaging/sagas.md:291-300`
- **Package(s)**: [Encina.Messaging]

## Current Behavior

The documentation at lines 291-300 of `docs/messaging/sagas.md` instructs users to configure choreography using:
1. `config.UseChoreography = true;`
2. `services.AddEncinaChoreography(options => { options.StuckSagaTimeout = ...; })`

However:
- `Grep "UseChoreography"` across `src/` returns zero matches. `MessagingConfiguration.cs` only contains `UseSagas` (line 106).
- `Grep "AddEncinaChoreography"` across `src/` returns zero matches.
- `ChoreographyOptions.cs` (`src/Encina.Messaging/Choreography/ChoreographyOptions.cs:15-42`) does not contain a `StuckSagaTimeout` property. Its actual properties are `AutoCompensateOnFailure`, `SagaTimeout`, `PersistState`, `MaxCompensationRetries`, and `CompensationRetryDelay`.

## Expected Behavior

The documentation must provide a valid code example that uses actual API symbols available in the Encina codebase. The example should correctly reference the existing `ChoreographyOptions` properties and the actual method/property names for enabling choreography in the messaging configuration.

## Root Cause

The documentation example was written based on an assumed or outdated API surface rather than verifying the current implementation. The specific properties and methods cited do not exist in `src/Encina.Messaging`.

## Proposed Fix

Update the code block in `docs/messaging/sagas.md:291-300` to reflect the actual API. This involves:
1. Removing or replacing `config.UseChoreography = true;` with the correct property or method if one exists, or clarifying if choreography is enabled differently.
2. Replacing `services.AddEncinaChoreography(...)` with the actual registration method if it exists, or correcting the method name.
3. Updating the `options` lambda to use valid `ChoreographyOptions` properties, such as `SagaTimeout` instead of `StuckSagaTimeout`, or removing the invalid property entirely if it is not relevant to the configuration being demonstrated.

## Priority

- [x] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

Link any related issues here.
- #16
