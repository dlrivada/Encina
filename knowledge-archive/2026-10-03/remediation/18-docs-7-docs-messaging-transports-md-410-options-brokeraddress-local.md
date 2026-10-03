<!--
title: [DEBT] docs/messaging/transports.md MQTT sample sets options.BrokerAddress, which does not exist
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

The MQTT sample in `docs/messaging/transports.md` assigns `options.BrokerAddress`, a member that `EncinaMQTTOptions` does not declare. The sample does not compile.

## Location

- **File(s)**: `docs/messaging/transports.md:410`; `src/Encina.MQTT/EncinaMQTTOptions.cs:15`
- **Package(s)**: Encina.MQTT

## Current Behavior

Line 410 reads `options.BrokerAddress = "localhost";`. `src/Encina.MQTT/EncinaMQTTOptions.cs` exposes `Host` (`:15`), `Port`, `ClientId`, `TopicPrefix`, `Username`, `Password`, `QualityOfService`, `UseTls`, `CleanSession` and `KeepAliveSeconds`, never `BrokerAddress`.

## Expected Behavior

The sample sets `options.Host = "localhost";`.

## Root Cause

The sample was written without being checked against `EncinaMQTTOptions.cs`.

## Proposed Fix

Change `BrokerAddress` to `Host` at `docs/messaging/transports.md:410`.

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
