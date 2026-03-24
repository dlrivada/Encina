# IntegrationTests - Redis Pub/Sub Messaging

## Status: Not Implemented

## Justification

The Redis Pub/Sub messaging transport (`RedisPubSubMessagePublisher`) requires a running Redis server for pub/sub round-trip testing. The publisher uses `StackExchange.Redis` to publish messages to Redis channels, and verifying end-to-end delivery requires an active Redis connection with both publisher and subscriber.

### 1. External Redis Server Required

Integration tests for the Redis Pub/Sub transport require:

- A running Redis server (or compatible alternative such as Valkey, Dragonfly, Garnet, KeyDB)
- Active connections for both publishing and subscribing
- Verification of message delivery across channels with subscriber confirmation

The current CI/CD pipeline does not include a Redis service in the core test profile. Adding one requires Docker Compose with the `caching` or `messaging` profile.

### 2. Thin Wrapper Over StackExchange.Redis

`RedisPubSubMessagePublisher` is a thin wrapper that serializes messages to JSON and publishes them via `ISubscriber.PublishAsync()`. The actual pub/sub reliability and message delivery guarantees are provided by Redis and the `StackExchange.Redis` library, not by Encina code.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Options validation (`EncinaRedisPubSubOptionsTests`), DI registration (`ServiceCollectionExtensionsTests`), publisher behavior with mocked `IConnectionMultiplexer` (`RedisPubSubMessagePublisherTests`)
- **Guard Tests**: Core messaging guard tests cover parameter validation
- **Contract Tests**: Core messaging contract tests verify `IMessageTransport` interface compliance

### 4. Recommended Alternative

When Docker infrastructure includes Redis in CI/CD:

1. Create a `RedisFixture` that starts Redis via Testcontainers
2. Test publish/subscribe round-trip: publish a message, verify it is received by a subscriber within a timeout
3. Test channel routing: verify messages reach the correct channel based on message type
4. Add as `[Trait("Category", "Integration")][Trait("Transport", "Redis")]`

## Related Files

- `src/Encina.Redis.PubSub/` - Source package
- `tests/Encina.UnitTests/Messaging/RedisPubSub/` - Unit tests

## Date: 2026-03-25
## Issue: #899
