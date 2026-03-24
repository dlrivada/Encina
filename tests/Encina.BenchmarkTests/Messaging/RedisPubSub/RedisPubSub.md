# Benchmark Tests - Redis Pub/Sub Messaging

## Status: Not Implemented

## Justification

The Redis Pub/Sub messaging transport is a thin wrapper over `StackExchange.Redis` with standard `System.Text.Json` serialization. The performance-sensitive code paths are in external libraries, not in Encina's wrapper code.

### 1. JSON Serialization Is Standard System.Text.Json

`RedisPubSubMessagePublisher` serializes messages using `System.Text.Json.JsonSerializer`. Benchmarking this would measure `System.Text.Json` performance for a simple `RedisMessageWrapper` DTO, which is well-documented by the .NET team and not specific to Encina.

### 2. No Custom Performance-Critical Logic

The publisher performs three operations per message:

1. Serialize message to JSON (`System.Text.Json`)
2. Determine channel name (string concatenation)
3. Call `ISubscriber.PublishAsync()` (`StackExchange.Redis`)

None of these operations involve Encina-specific algorithms or data structures that would benefit from benchmarking.

### 3. Encryption Benchmarks Already Cover the Sensitive Path

The existing encryption benchmarks in `Encina.BenchmarkTests` already measure the more performance-sensitive code path: message encryption/decryption. When encryption is enabled for Redis Pub/Sub messages, the encryption overhead dominates the serialization cost and is already benchmarked.

### 4. Adequate Coverage from Other Test Types

- **Unit Tests**: Options validation, DI registration, publisher behavior with mocked dependencies

### 5. Recommended Alternative

If benchmark data is needed:

1. Add a `MessageSerializationBenchmarks` class comparing serialization performance across all messaging transports (Redis, RabbitMQ, Kafka, etc.)
2. Compare with and without encryption to quantify the overhead
3. This should be part of a broader messaging benchmarks initiative, not specific to Redis Pub/Sub

## Related Files

- `src/Encina.Redis.PubSub/` - Source package
- `tests/Encina.UnitTests/Messaging/RedisPubSub/` - Unit tests
- `tests/Encina.BenchmarkTests/Encina.Benchmarks/` - Existing benchmarks (encryption)

## Date: 2026-03-25
## Issue: #899
