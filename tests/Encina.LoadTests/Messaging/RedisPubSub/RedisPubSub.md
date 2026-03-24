# Load Tests - Redis Pub/Sub Messaging

## Status: Not Implemented

## Justification

The Redis Pub/Sub messaging transport is a thin wrapper over `StackExchange.Redis`. Load testing this wrapper in isolation does not provide meaningful performance insights because throughput depends entirely on the Redis server and network.

### 1. I/O-Bound Pub/Sub Pattern

`RedisPubSubMessagePublisher` delegates directly to `ISubscriber.PublishAsync()` from `StackExchange.Redis`. The performance characteristics are:

- **Throughput**: Determined by Redis server throughput, network bandwidth, and `StackExchange.Redis` multiplexer performance
- **Latency**: Dominated by the network round-trip to Redis
- **Serialization overhead**: Standard `System.Text.Json` serialization of `RedisMessageWrapper`, which is negligible compared to I/O

Load testing would measure Redis and `StackExchange.Redis` performance, not Encina's code.

### 2. No Complex Logic to Stress

The publisher performs three operations: serialize message to JSON, determine the channel name, call `PublishAsync()`. There is no connection pooling, batching, retry logic, or backpressure mechanism in the Encina layer that would benefit from load testing.

### 3. Adequate Coverage from Other Test Types

- **Unit Tests**: Options validation, DI registration, publisher behavior with mocked `IConnectionMultiplexer`

### 4. Recommended Alternative

If pub/sub throughput testing becomes necessary:

1. Use NBomber to measure messages/second throughput against a real Redis instance
2. Focus on the serialization pipeline (message wrapping + JSON encoding) rather than the Redis round-trip
3. Compare with encryption-enabled paths to quantify the encryption overhead

## Related Files

- `src/Encina.Redis.PubSub/` - Source package
- `tests/Encina.UnitTests/Messaging/RedisPubSub/` - Unit tests

## Date: 2026-03-25
## Issue: #899
