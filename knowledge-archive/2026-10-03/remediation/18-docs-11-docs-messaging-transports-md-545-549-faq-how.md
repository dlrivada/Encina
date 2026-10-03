<!--
title: [DEBT] docs/messaging/transports.md FAQ configures AddEncinaPolly with RetryCount and CircuitBreakerThreshold, which do not exist
labels: technical-debt
milestone:
kind: docs
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

The FAQ answer "How do I handle transport failures?" in `docs/messaging/transports.md` configures `AddEncinaPolly` with two options that `EncinaPollyOptions` does not have. The sample does not compile.

## Location

- **File(s)**: `docs/messaging/transports.md:545-549`; `src/Encina.Polly/ServiceCollectionExtensions.cs:185-212`
- **Package(s)**: Encina.Polly

## Current Behavior

Lines 545-549 document `services.AddEncinaPolly(options => { options.RetryCount = 3; options.CircuitBreakerThreshold = 5; });`. `EncinaPollyOptions` (`src/Encina.Polly/ServiceCollectionExtensions.cs:185-212`) has exactly two members, `EnableTelemetry` and `EnableLogging`, and its own XML documentation says "Reserved for future extensibility. Currently, all configuration is done via attributes." `RetryCount` and `CircuitBreakerThreshold` exist on neither this nor any other type of the package.

Retry and circuit-breaker settings are configured per request with attributes: `[Retry(MaxAttempts = ...)]` (`src/Encina.Polly/Attributes/RetryAttribute.cs:22`) and `[CircuitBreaker(FailureThreshold = ...)]` (`src/Encina.Polly/Attributes/CircuitBreakerAttribute.cs:37`).

## Expected Behavior

The FAQ shows `services.AddEncinaPolly();` and configures retry and circuit breaking with the `[Retry]` and `[CircuitBreaker]` attributes, as the package's own XML documentation does.

## Root Cause

The sample assumes an options-based configuration model that the package does not implement.

## Proposed Fix

Replace the options lambda at `docs/messaging/transports.md:545-549` with `services.AddEncinaPolly();` and an attribute example such as `[Retry(MaxAttempts = 3)]` and `[CircuitBreaker(FailureThreshold = 5)]` on the request type.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #18 (This issue)
