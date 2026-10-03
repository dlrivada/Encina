<!--
title: [DEBT] docs/messaging/transports.md Redis Pub/Sub pattern-subscribe sample matches no overload of SubscribeAsync
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

The Redis Pub/Sub sample in `docs/messaging/transports.md` subscribes to a pattern with a call that compiles against neither subscribe method of `IRedisPubSubMessagePublisher`.

## Location

- **File(s)**: `docs/messaging/transports.md:388-392`; `src/Encina.Redis.PubSub/IRedisPubSubMessagePublisher.cs:32,46`
- **Package(s)**: Encina.Redis.PubSub

## Current Behavior

Line 389 reads `await subscriber.SubscribeAsync("cache-*", async (channel, message) => {...});`. The real `SubscribeAsync` (`IRedisPubSubMessagePublisher.cs:32`) is `SubscribeAsync<TMessage>(Func<TMessage, ValueTask> handler, string? channel = null, CancellationToken cancellationToken = default)`: a single-channel subscription whose first parameter is the handler, not a pattern string, and whose handler takes only the message, not `(channel, message)`.

Pattern subscription with a `(channel, message)` handler is a different method, `SubscribePatternAsync<TMessage>(string pattern, Func<string, TMessage, ValueTask> handler, CancellationToken cancellationToken = default)` (`:46`). The documented call matches neither overload.

## Expected Behavior

The sample subscribes to the pattern with `SubscribePatternAsync`, passing the pattern first and a handler that receives the channel and the message.

## Root Cause

The sample merged the single-channel and the pattern subscription into one call that the interface does not declare.

## Proposed Fix

Replace the call at `docs/messaging/transports.md:389` with `await subscriber.SubscribePatternAsync<CacheInvalidated>("cache-*", async (channel, message) => { ... });`, and keep the handler body that invalidates the cache.

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
