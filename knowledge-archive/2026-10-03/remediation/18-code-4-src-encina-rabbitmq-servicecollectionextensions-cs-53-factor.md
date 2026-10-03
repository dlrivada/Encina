<!--
title: [DEBT] RabbitMQ and MQTT singleton factories block synchronously on the async connect at first resolution
labels: technical-debt
milestone:
kind: debt
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [x] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [ ] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

The `TryAddSingleton` factory delegates of `Encina.RabbitMQ` and `Encina.MQTT` block synchronously on an asynchronous connect with `.GetAwaiter().GetResult()`. The delegate runs at first resolution of the singleton, typically at host startup, so the blocking call fires on every application start. This is the same sync-over-async anti-pattern that AGENTS.md section 3 forbids for database calls (Sonar S6966); it is not a database call here, but the risk is the same class of defect: thread-pool stall and, under a captured `SynchronizationContext`, potential deadlock. Severity is low because ASP.NET Core's default host has no synchronization context and the path runs once per process.

## Location

- **File(s)**: `src/Encina.RabbitMQ/ServiceCollectionExtensions.cs:53` (`factory.CreateConnectionAsync().GetAwaiter().GetResult()`) and `:59` (`connection.CreateChannelAsync().GetAwaiter().GetResult()`); `src/Encina.MQTT/ServiceCollectionExtensions.cs:67` (`client.ConnectAsync(connectOptions).GetAwaiter().GetResult()`)
- **Package(s)**: Encina.RabbitMQ, Encina.MQTT

## Current Behavior

RabbitMQ registers `IConnection` (`:42-54`) and `IChannel` (`:56-60`) as singletons whose factories create the connection and the channel by blocking on `CreateConnectionAsync()` and `CreateChannelAsync()`. MQTT registers `IMqttClient` (`:43-70`) as a singleton whose factory blocks on `ConnectAsync(connectOptions)`. The comment at `src/Encina.MQTT/ServiceCollectionExtensions.cs:66` says "Connect synchronously during registration", but the call actually runs when the singleton is first resolved, not during registration.

## Expected Behavior

Opening the broker connection does not block a thread on an asynchronous call. The connect is awaited, for example during host startup, and the registration is safe to resolve from any context.

## Root Cause

The connection objects are exposed through synchronous `IServiceProvider` factory delegates, which cannot await, so the async connect was wrapped in `GetAwaiter().GetResult()`.

## Proposed Fix

Move the asynchronous connect out of the synchronous factory delegate: establish the connection in an awaited startup step (for example an `IHostedService.StartAsync`, or an async connection provider the publisher awaits on first use) and make the registration resolve the already-connected object. Choose a design that also gives tests a seam to substitute the connection, since the delegates currently construct `ConnectionFactory` and `MqttClientFactory` inline.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [ ] Small (< 1 hour)
- [x] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #18 (This issue)
