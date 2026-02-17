# Encina.IdGeneration

Distributed ID generation strategies for sharded and non-sharded entities in the Encina framework.

**Generate globally unique, time-ordered identifiers** — four strategies for different throughput, size, and human-readability trade-offs.

## Features

- **Snowflake IDs**: 64-bit time-ordered IDs with configurable bit allocation and shard embedding
- **ULID**: 128-bit Crockford Base32 identifiers with millisecond timestamps and cryptographic randomness
- **UUIDv7**: RFC 9562 time-ordered UUIDs compatible with `System.Guid`
- **ShardPrefixed IDs**: Human-readable `{shard}:{sequence}` format with pluggable sequence generators
- **Shard-Aware Generation**: Embed and extract shard IDs for reverse routing
- **Railway Oriented Programming**: All operations return `Either<EncinaError, TId>`
- **Full Observability**: Metrics, tracing, and structured logging out of the box
- **13 Database Providers**: Type handlers, value converters, and serializers for ADO.NET, Dapper, EF Core, MongoDB

## Installation

```bash
dotnet add package Encina.IdGeneration
```

## Quick Start

### 1. Register Services

```csharp
services.AddEncinaIdGeneration(options =>
{
    options.UseSnowflake(sf =>
    {
        sf.MachineId = 1;
        sf.EpochStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    });
    options.UseUlid();
    options.UseUuidV7();
    options.UseShardPrefixed(sp =>
    {
        sp.Format = ShardPrefixedFormat.Ulid;
        sp.Delimiter = ':';
    });
});
```

### 2. Generate IDs

```csharp
// Inject the generator
public class OrderService(IShardedIdGenerator<SnowflakeId> snowflake)
{
    public Either<EncinaError, Order> CreateOrder(string shardId)
    {
        return snowflake.Generate(shardId)
            .Map(id => new Order { Id = id, CreatedAtUtc = DateTime.UtcNow });
    }
}
```

### 3. Extract Shard Information

```csharp
// Reverse routing: extract shard from an existing ID
var shardResult = snowflake.ExtractShardId(existingOrderId);
// shardResult: Right("42") or Left(EncinaError)
```

## Strategy Comparison

| Strategy | Size | Format | Time-Ordered | Shard-Aware | Best For |
|----------|------|--------|:------------:|:-----------:|----------|
| **Snowflake** | 64-bit | `long` | Yes | Yes | High-throughput, database PKs |
| **ULID** | 128-bit | 26-char Base32 | Yes | No | APIs, external IDs |
| **UUIDv7** | 128-bit | Standard GUID | Yes | No | Drop-in UUID replacement |
| **ShardPrefixed** | Variable | `shard:sequence` | Yes | Yes | Human-readable, debugging |

## Snowflake Bit Layout

Default: 41 timestamp + 10 shard + 12 sequence = 63 bits (sign bit always 0)

```
┌─ sign (1 bit, always 0)
│ ┌─────────────── timestamp (41 bits: ~69 years from epoch)
│ │                              ┌──── shard/machine (10 bits: 0-1023)
│ │                              │          ┌── sequence (12 bits: 0-4095/ms)
│ │                              │          │
0 TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT SSSSSSSSSS CCCCCCCCCCCC
```

Configurable via `SnowflakeOptions`:

```csharp
options.UseSnowflake(sf =>
{
    sf.TimestampBits = 41;  // ~69 years
    sf.ShardBits = 10;      // 1024 shards
    sf.SequenceBits = 12;   // 4096 IDs/ms/shard
    sf.ClockDriftToleranceMs = 5;
});
```

## ShardPrefixed Formats

```csharp
// ULID format (default): shard-01:01ARZ3NDEKTSV4RRFFQ69G5FAV
options.UseShardPrefixed(sp => sp.Format = ShardPrefixedFormat.Ulid);

// UUIDv7 format: shard-01:019374c8-7b00-7000-8000-000000000001
options.UseShardPrefixed(sp => sp.Format = ShardPrefixedFormat.UuidV7);

// TimestampRandom format: shard-01:1706745600000-a3f8
options.UseShardPrefixed(sp => sp.Format = ShardPrefixedFormat.TimestampRandom);
```

## Database Provider Integration

### ADO.NET

```csharp
command.Parameters.AddSnowflakeId("@Id", snowflakeId);
command.Parameters.AddUlidId("@Id", ulidId);
command.Parameters.AddUuidV7Id("@Id", uuidV7Id);
command.Parameters.AddShardPrefixedId("@Id", shardPrefixedId);
```

### Dapper

```csharp
// Register type handlers at startup
SqlMapper.AddTypeHandler(new SnowflakeIdTypeHandler());
SqlMapper.AddTypeHandler(new UlidIdTypeHandler());
SqlMapper.AddTypeHandler(new UuidV7IdTypeHandler());
SqlMapper.AddTypeHandler(new ShardPrefixedIdTypeHandler());
```

### EF Core

```csharp
// In OnModelCreating
modelBuilder.Entity<Order>(entity =>
{
    entity.Property(e => e.Id).HasSnowflakeIdConversion();
});
```

### MongoDB

```csharp
// Register BSON serializers at startup
BsonSerializer.RegisterSerializer(new SnowflakeIdBsonSerializer());
```

## Observability

### Metrics

| Instrument | Type | Description |
|-----------|------|-------------|
| `encina.idgen.generated` | Counter | Total IDs generated, tagged by strategy and shard |
| `encina.idgen.collisions` | Counter | Collision count by strategy |
| `encina.idgen.duration_ms` | Histogram | Generation latency by strategy |
| `encina.idgen.sequence_exhausted` | Counter | Sequence overflow events |

### Health Check

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<IdGeneratorHealthCheck>(IdGeneratorHealthCheck.DefaultName);
```

## Error Handling

All generators use Railway Oriented Programming:

```csharp
var result = generator.Generate(shardId);

result.Match(
    Right: id => Console.WriteLine($"Generated: {id}"),
    Left: error => error.Code switch
    {
        IdGenerationErrorCodes.ClockDriftDetected => HandleClockDrift(error),
        IdGenerationErrorCodes.SequenceExhausted => HandleOverflow(error),
        IdGenerationErrorCodes.InvalidShardId => HandleBadShard(error),
        _ => HandleUnknown(error)
    }
);
```

## Documentation

- [Feature Overview](../../docs/features/id-generation.md)
- [Configuration Guide](../../docs/guides/id-generation-configuration.md)
- [Scaling Guide](../../docs/guides/id-generation-scaling.md)
- [ADR-011: Multi-Strategy ID Generation](../../docs/architecture/adr/011-id-generation-multi-strategy.md)

## Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

MIT License - see [LICENSE](../../LICENSE) for details.
