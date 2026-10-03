<!--
title: [DEBT] docs/messaging/transports.md documents NATS extension methods AddEncinaNatsCore and AddEncinaNatsJetStream that do not exist
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

Reported by: code 2, docs 1.

`docs/messaging/transports.md:293` (`services.AddEncinaNatsCore(options => ...)`) and `:305` (`services.AddEncinaNatsJetStream(options => ...)`) document two extension methods that do not exist. A developer who follows either NATS sample verbatim gets a compile error (`'IServiceCollection' does not contain a definition for 'AddEncinaNatsCore'`, and the same for `AddEncinaNatsJetStream`).

## Location

- **File(s)**: `docs/messaging/transports.md:293,305`; `src/Encina.NATS/ServiceCollectionExtensions.cs:13-72`
- **Package(s)**: Encina.NATS

## Current Behavior

`src/Encina.NATS/ServiceCollectionExtensions.cs:21` exposes a single extension method, `public static IServiceCollection AddEncinaNATS(this IServiceCollection services, Action<EncinaNATSOptions>? configure = null)`. JetStream mode is selected through the `UseJetStream` boolean of `EncinaNATSOptions` (read at `ServiceCollectionExtensions.cs:34` and `:51`), not through a second method. The doc sample for NATS Core and the one for NATS JetStream both call methods that are not declared anywhere in the package.

## Expected Behavior

Both NATS samples on the page call `AddEncinaNATS`: the Core sample with the default options (`UseJetStream` is false by default), and the JetStream sample with `options.UseJetStream = true` together with the `Url`, `StreamName` and `ConsumerName` members that `EncinaNATSOptions` declares.

## Root Cause

The samples were written against an API shape (one registration method per mode) that the package never had; they were not checked against `ServiceCollectionExtensions.cs`.

## Proposed Fix

Rewrite the two code samples in the NATS section of `docs/messaging/transports.md` to call `AddEncinaNATS`, and state that JetStream mode is selected with `UseJetStream = true`. Review the sentence "NATS offers two modes" so it describes one registration method with an option, not two methods.

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
