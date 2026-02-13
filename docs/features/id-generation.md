# Distributed ID Generation in Encina

Multi-strategy distributed ID generation for sharded and non-sharded entities, with full provider support and observability.

## Table of Contents

1. [Overview](#overview)
2. [The Problem](#the-problem)
3. [The Solution](#the-solution)
4. [Architecture](#architecture)
5. [Quick Start](#quick-start)
6. [API Reference](#api-reference)
7. [Provider Support](#provider-support)
8. [Observability](#observability)
9. [Testing](#testing)
10. [FAQ](#faq)

---

## Overview

Encina.IdGeneration provides four distributed ID strategies that produce globally unique, time-ordered identifiers without coordination between nodes. Each strategy offers different trade-offs in size, format, human-readability, and shard embedding.

### Why Distributed ID Generation?

| Benefit | Description |
|---------|-------------|
| **No Coordination** | Generate unique IDs without a central authority or database round-trip |
| **Time Ordering** | All four strategies embed timestamps for natural chronological sorting |
| **Shard Awareness** | Snowflake and ShardPrefixed strategies embed shard IDs for reverse routing |
| **Type Safety** | Strongly-typed `readonly record struct` values prevent ID type confusion |
| **Provider Agnostic** | Same ID types work across ADO.NET, Dapper, EF Core, and MongoDB |

---

## The Problem

### Challenge 1: Database Auto-Increment in Sharded Systems

Database-generated sequences (IDENTITY, SERIAL) produce collisions across shards. You cannot merge data from multiple shards without ID conflicts.

### Challenge 2: UUID v4 Fragmentation

Random UUIDs cause B-tree page splits and poor cache locality. Insert performance degrades as tables grow.

### Challenge 3: Shard Routing Without Extra Lookups

Given an entity ID, determining which shard holds the data requires either a directory lookup or information embedded in the ID itself.

---

## The Solution

Encina provides four strategies, each addressing these challenges differently:

### 1. Snowflake (64-bit, Shard-Embedded)

Best for high-throughput systems where a compact `long` primary key is needed and shard information must be embedded.

```text
┌─ sign (1 bit, always 0)
│ ┌─────────────── timestamp (41 bits: ~69 years from epoch)
│ │                              ┌──── shard/machine (10 bits: 0-1023)
│ │                              │          ┌── sequence (12 bits: 0-4095/ms)
0 TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT SSSSSSSSSS CCCCCCCCCCCC
```

### 2. ULID (128-bit, Crockford Base32)

Best for APIs and external-facing IDs where human readability and lexicographic sorting matter.

```text
 01ARZ3NDEKTSV4RRFFQ69G5FAV
 |----------||------------|
  Timestamp      Random
  (48 bits)    (80 bits)
```

### 3. UUIDv7 (128-bit, RFC 9562)

Best as a drop-in replacement for `Guid.NewGuid()` that preserves time ordering for B-tree index performance.

### 4. ShardPrefixed (Variable-length String)

Best for debugging and human inspection where shard identity must be immediately visible.

```text
shard-01:01ARZ3NDEKTSV4RRFFQ69G5FAV
|------| |------------------------|
 Shard        Sequence (ULID)
```

---

## Architecture

### Interface Hierarchy

```text
IIdGenerator (marker)
├── IIdGenerator<TId>
│   ├── Generate() → Either<EncinaError, TId>
│   └── StrategyName → string
│
└── IShardedIdGenerator<TId> : IIdGenerator<TId>
    ├── Generate(string shardId) → Either<EncinaError, TId>
    └── ExtractShardId(TId id) → Either<EncinaError, string>
```

### Type Mapping by Strategy

| Strategy | Interface | ID Type | Storage Type |
|----------|----------|---------|-------------|
| Snowflake | `IShardedIdGenerator<SnowflakeId>` | `readonly record struct` | `long` / `BIGINT` |
| ULID | `IIdGenerator<UlidId>` | `readonly record struct` | `string(26)` / `CHAR(26)` |
| UUIDv7 | `IIdGenerator<UuidV7Id>` | `readonly record struct` | `Guid` / `UNIQUEIDENTIFIER` |
| ShardPrefixed | `IShardedIdGenerator<ShardPrefixedId>` | `readonly record struct` | `string` / `NVARCHAR` |

### Error Code Taxonomy

| Code | Constant | Trigger |
|------|----------|---------|
| `encina.idgen.clock_drift_detected` | `ClockDriftDetected` | System clock moved backward beyond tolerance |
| `encina.idgen.sequence_exhausted` | `SequenceExhausted` | 4096 IDs/ms exceeded (Snowflake) |
| `encina.idgen.invalid_shard_id` | `InvalidShardId` | Shard ID null, empty, or out of range |
| `encina.idgen.id_parse_failure` | `IdParseFailure` | String cannot be parsed into ID type |

---

## Quick Start

### Step 1: Register Services

```csharp
services.AddEncinaIdGeneration(options =>
{
    options.UseSnowflake(sf =>
    {
        sf.MachineId = 1;
        sf.EpochStart = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    });
    options.UseUlid();
    options.UseUuidV7();
    options.UseShardPrefixed(sp => sp.Format = ShardPrefixedFormat.Ulid);
});
```

### Step 2: Inject and Generate

```csharp
public class OrderService(IShardedIdGenerator<SnowflakeId> generator)
{
    public Either<EncinaError, Order> CreateOrder(string shardId)
    {
        return generator.Generate(shardId)
            .Map(id => new Order { Id = id });
    }
}
```

### Step 3: Extract Shard for Routing

```csharp
public string GetShardForOrder(SnowflakeId orderId)
{
    return generator.ExtractShardId(orderId)
        .Match(Right: shard => shard, Left: _ => "unknown");
}
```

---

## API Reference

### Interfaces

| Interface | Method | Returns | Description |
|-----------|--------|---------|-------------|
| `IIdGenerator<TId>` | `Generate()` | `Either<EncinaError, TId>` | Generate a new ID |
| `IShardedIdGenerator<TId>` | `Generate(string shardId)` | `Either<EncinaError, TId>` | Generate with embedded shard |
| `IShardedIdGenerator<TId>` | `ExtractShardId(TId)` | `Either<EncinaError, string>` | Extract shard from ID |

### ID Types

| Type | Size | Key Properties | Key Methods |
|------|------|---------------|-------------|
| `SnowflakeId` | 8 bytes | `Value` (long) | `Parse`, `TryParse`, `TryParseEither` |
| `UlidId` | 16 bytes | — | `NewUlid`, `GetTimestamp`, `ToGuid`, `Parse` |
| `UuidV7Id` | 16 bytes | `Value` (Guid) | `NewUuidV7`, `GetTimestamp`, `Parse` |
| `ShardPrefixedId` | Variable | `ShardId`, `Sequence` | `Parse`, `TryParse`, `TryParseEither` |

### Configuration Classes

| Class | Key Properties |
|-------|---------------|
| `IdGenerationOptions` | `UseSnowflake()`, `UseUlid()`, `UseUuidV7()`, `UseShardPrefixed()` |
| `SnowflakeOptions` | `MachineId`, `EpochStart`, `TimestampBits`, `ShardBits`, `SequenceBits`, `ClockDriftToleranceMs` |
| `ShardPrefixedOptions` | `Format` (Ulid/UuidV7/TimestampRandom), `Delimiter` |

---

## Provider Support

### Type Mapping Matrix

| ID Type | ADO.NET | Dapper | EF Core | MongoDB |
|---------|---------|--------|---------|---------|
| `SnowflakeId` | `AddSnowflakeId()` | `SnowflakeIdTypeHandler` | `SnowflakeIdValueConverter` | `SnowflakeIdBsonSerializer` |
| `UlidId` | `AddUlidId()` | `UlidIdTypeHandler` | `UlidIdValueConverter` | `UlidIdBsonSerializer` |
| `UuidV7Id` | `AddUuidV7Id()` | `UuidV7IdTypeHandler` | `UuidV7IdValueConverter` | `UuidV7IdBsonSerializer` |
| `ShardPrefixedId` | `AddShardPrefixedId()` | `ShardPrefixedIdTypeHandler` | `ShardPrefixedIdValueConverter` | `ShardPrefixedIdBsonSerializer` |

### Database Column Types

| ID Type | SQLite | SQL Server | PostgreSQL | MySQL |
|---------|--------|------------|------------|-------|
| `SnowflakeId` | `INTEGER` | `BIGINT` | `BIGINT` | `BIGINT` |
| `UlidId` | `TEXT` | `CHAR(26)` | `CHAR(26)` | `CHAR(26)` |
| `UuidV7Id` | `TEXT` | `UNIQUEIDENTIFIER` | `UUID` | `CHAR(36)` |
| `ShardPrefixedId` | `TEXT` | `NVARCHAR(255)` | `VARCHAR(255)` | `VARCHAR(255)` |

---

## Observability

### Metrics (OpenTelemetry)

| Instrument | Type | Tags | Description |
|-----------|------|------|-------------|
| `encina.idgen.generated` | Counter | `strategy`, `shard_id` | Total IDs generated |
| `encina.idgen.collisions` | Counter | `strategy` | Collision detections |
| `encina.idgen.duration_ms` | Histogram | `strategy` | Generation latency |
| `encina.idgen.sequence_exhausted` | Counter | — | Sequence overflow events |

### Tracing (ActivitySource: `Encina.IdGeneration`)

| Activity | Tags | Description |
|----------|------|-------------|
| `IdGeneration` | `strategy`, `shard_id` | ID generation span |
| `ShardExtraction` | `strategy`, `id` | Shard extraction span |

### Health Check

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<IdGeneratorHealthCheck>(
        IdGeneratorHealthCheck.DefaultName,
        tags: ["ready"]);
```

Monitors:

- Clock drift against configurable threshold (default: 100ms)
- Snowflake sequence state validation

---

## Testing

### Test Coverage

| Test Type | Count | Description |
|-----------|-------|-------------|
| Unit Tests | 199 | Core logic, parsing, error paths |
| Guard Tests | 4 | Null argument validation |
| Contract Tests | 43 | Interface contract compliance |
| Property Tests | 19 | FsCheck invariant verification |
| Integration Tests | 14 files | All 13 providers (SQLite runs, others skip gracefully) |
| Load Tests | 7 scenarios | NBomber collision detection under concurrency |
| Benchmarks | 20 | BenchmarkDotNet throughput and allocation measurement |

### Running Tests

```bash
# Unit tests only
dotnet test tests/Encina.UnitTests --filter "FullyQualifiedName~IdGeneration"

# Integration tests (requires Docker)
dotnet test tests/Encina.IntegrationTests --filter "FullyQualifiedName~IdGeneration"

# Benchmarks
dotnet run --project tests/Encina.BenchmarkTests/Encina.IdGeneration.Benchmarks -c Release -- --list flat
```

---

## FAQ

### Q: Which strategy should I use?

Use **Snowflake** when you need compact `long` PKs with shard embedding. Use **UUIDv7** as a drop-in `Guid` replacement. Use **ULID** for external-facing IDs. Use **ShardPrefixed** when human readability matters. See [Configuration Guide](../guides/id-generation-configuration.md).

### Q: Can I use multiple strategies simultaneously?

Yes. Register multiple strategies in `AddEncinaIdGeneration()` and inject the specific `IIdGenerator<TId>` or `IShardedIdGenerator<TId>` you need.

### Q: How do I handle Snowflake clock drift?

The generator spins briefly when drift is within `ClockDriftToleranceMs` (default: 5ms). Beyond that, it returns `ClockDriftDetected` error. Use NTP synchronization on all nodes. See [Scaling Guide](../guides/id-generation-scaling.md).

### Q: What happens when the Snowflake sequence overflows?

The generator returns `SequenceExhausted` error. With default 12 sequence bits, this means >4096 IDs in a single millisecond from one machine. Consider spreading generation across multiple machine IDs.

---

## See Also

- [Configuration Guide](../guides/id-generation-configuration.md) — When to use each strategy
- [Scaling Guide](../guides/id-generation-scaling.md) — Machine ID allocation and cluster operations
- [ADR-011](../architecture/adr/011-id-generation-multi-strategy.md) — Architecture decision rationale
- [Database Sharding](./database-sharding.md) — Integration with `IShardRouter`
- [Issue #638](https://github.com/dlrivada/Encina/issues/638) — Feature implementation
