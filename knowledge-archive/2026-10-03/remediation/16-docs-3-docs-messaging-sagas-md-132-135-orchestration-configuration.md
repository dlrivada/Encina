<!--
title: [DEBT] Fix incorrect AddEncinaDapperSqlServer API example in docs/messaging/sagas.md
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

The code example in the "Configuration" section of the Sagas documentation references a non-existent method `AddEncinaDapperSqlServer` with an incorrect parameter list. The documentation must be updated to reference the actual public API `AddEncinaDapper` defined in the `Encina.Dapper.SqlServer` package.

## Location

- **File(s)**: `docs/messaging/sagas.md:132-135`, `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs:43`
- **Package(s)**: Encina.Dapper.SqlServer

## Current Behavior

The documentation at `docs/messaging/sagas.md:132-135` instructs users to call `services.AddEncinaDapperSqlServer(connectionString, config => { config.UseSagas = true; })`. This method does not exist in the codebase. The actual method is `AddEncinaDapper(this IServiceCollection, Action<MessagingConfiguration> configure)` located in `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs:43`. The documented method incorrectly includes a `connectionString` parameter which is not part of the actual signature.

## Expected Behavior

The code example in `docs/messaging/sagas.md` must accurately reflect the real API. It should call `services.AddEncinaDapper(config => { config.UseSagas = true; })` without the `connectionString` parameter, aligning with the implementation in `src/Encina.Dapper.SqlServer/ServiceCollectionExtensions.cs:43`.

## Root Cause

The documentation was likely written based on an assumed or outdated API signature rather than the current implementation, leading to a mismatch between the documented method name/parameters and the actual code.

## Proposed Fix

Update the code block in `docs/messaging/sagas.md` lines 132-135 to replace `AddEncinaDapperSqlServer(connectionString, ...)` with `AddEncinaDapper(...)`. Ensure the lambda expression correctly targets the `MessagingConfiguration` object as defined in the actual extension method signature.

## Priority

- [x] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #16