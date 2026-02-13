# ID Generation Configuration Guide

How to choose, configure, and tune each ID generation strategy for your application.

---

## Strategy Selection

### Decision Matrix

| Criterion | Snowflake | ULID | UUIDv7 | ShardPrefixed |
|-----------|:---------:|:----:|:------:|:-------------:|
| **Size** | 8 bytes | 16 bytes | 16 bytes | Variable |
| **Database type** | `BIGINT` | `CHAR(26)` | `GUID`/`UUID` | `VARCHAR` |
| **Time-ordered** | Yes | Yes | Yes | Yes |
| **Shard-embedded** | Yes | No | No | Yes |
| **Human-readable** | No | Moderate | Low | High |
| **Throughput** | ~4M/sec | ~2M/sec | ~3M/sec | ~1M/sec |
| **Coordination-free** | Per machine | Yes | Yes | Per shard |
| **Index performance** | Excellent | Good | Good | Moderate |

### When to Use Each Strategy

#### Snowflake

- High-throughput OLTP with `BIGINT` primary keys
- Systems where shard routing from ID alone is required
- Environments with reliable NTP (clock drift must be managed)
- When 64-bit integer compatibility matters (legacy systems, Kafka keys)

#### ULID

- API response bodies and external-facing identifiers
- Systems where IDs are frequently displayed or logged
- Cross-system communication where string IDs are preferred
- When lexicographic sorting in string form is valuable

#### UUIDv7

- Existing systems already using `Guid`/`UUID` columns
- When `System.Guid` API compatibility is required
- Drop-in replacement for `Guid.NewGuid()` with time ordering
- When database supports native UUID type (PostgreSQL, SQL Server)

#### ShardPrefixed

- Development and debugging environments
- Systems where visual shard identification in IDs is valuable
- Multi-tenant applications where tenant/shard must be human-visible
- When shard extraction must work with string manipulation (no bit math)

---

## Snowflake Configuration

### Bit Allocation

Total must equal 63 bits (sign bit excluded):

```csharp
options.UseSnowflake(sf =>
{
    sf.TimestampBits = 41;  // ~69 years from epoch at ms precision
    sf.ShardBits = 10;      // 1024 unique machine/shard IDs
    sf.SequenceBits = 12;   // 4096 IDs per millisecond per machine
});
```

### Common Bit Allocation Profiles

| Profile | Timestamp | Shard | Sequence | Epoch Duration | Max Shards | IDs/ms |
|---------|-----------|-------|----------|---------------|------------|--------|
| **Default** | 41 | 10 | 12 | ~69 years | 1,024 | 4,096 |
| **Few Shards** | 41 | 5 | 17 | ~69 years | 32 | 131,072 |
| **Many Shards** | 41 | 16 | 6 | ~69 years | 65,536 | 64 |
| **Long Epoch** | 45 | 8 | 10 | ~1,114 years | 256 | 1,024 |
| **No Sharding** | 41 | 0 | 22 | ~69 years | 1 | 4,194,304 |

### Epoch Start

Choose an epoch close to your system's launch date:

```csharp
sf.EpochStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
```

A more recent epoch extends the timestamp range. With 41 bits:

- Epoch 2024-01-01 → overflows ~2093
- Epoch 2015-01-01 → overflows ~2084
- Unix epoch (1970) → already expired for 41-bit Snowflake

### Clock Drift Tolerance

```csharp
sf.ClockDriftToleranceMs = 5; // Default: 5ms
```

| Threshold | Behavior |
|-----------|----------|
| 0 | Any backward clock movement returns error immediately |
| 1-10 | Spin-waits briefly; suitable for NTP-synchronized hosts |
| 50-100 | Tolerates VM live migration or aggressive NTP adjustments |
| >100 | Not recommended; can hide systematic clock issues |

---

## ShardPrefixed Configuration

### Format Selection

```csharp
options.UseShardPrefixed(sp =>
{
    sp.Format = ShardPrefixedFormat.Ulid;         // Default
    sp.Delimiter = ':';                            // Default
});
```

| Format | Example Output | Sequence Size | Sort Order |
|--------|---------------|---------------|------------|
| `Ulid` | `shard-01:01ARZ3NDEKTSV4RRFFQ69G5FAV` | 26 chars | Lexicographic |
| `UuidV7` | `shard-01:019374c8-7b00-7000-8000-000000000001` | 36 chars | Lexicographic |
| `TimestampRandom` | `shard-01:1706745600000-a3f8` | ~20 chars | Numeric prefix |

### Delimiter

The delimiter separates the shard prefix from the sequence portion:

```csharp
sp.Delimiter = ':';   // shard-01:sequence
sp.Delimiter = '_';   // shard-01_sequence
sp.Delimiter = '/';   // shard-01/sequence
```

---

## ULID and UUIDv7 Configuration

These strategies require no configuration beyond enablement:

```csharp
options.UseUlid();      // Uses cryptographic randomness, system clock
options.UseUuidV7();    // Uses cryptographic randomness, system clock
```

Both accept an optional `TimeProvider` via DI for testability.

---

## Health Check Configuration

```csharp
services.AddHealthChecks()
    .AddCheck<IdGeneratorHealthCheck>(
        IdGeneratorHealthCheck.DefaultName,
        tags: ["ready"]);
```

### Health Check Options

```csharp
// Configure via DI
services.Configure<IdGeneratorHealthCheckOptions>(options =>
{
    options.ClockDriftThresholdMs = 100; // Default: 100ms
});
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ClockDriftThresholdMs` | `long` | `100` | Maximum tolerated clock drift before reporting Degraded |

---

## Multiple Strategies

Register and use multiple strategies simultaneously:

```csharp
services.AddEncinaIdGeneration(options =>
{
    options.UseSnowflake(sf => sf.MachineId = 1);
    options.UseUlid();
});

// Inject the specific generator you need
public class OrderService(
    IShardedIdGenerator<SnowflakeId> snowflake,  // For internal PKs
    IIdGenerator<UlidId> ulid)                     // For external API IDs
{
    public Order CreateOrder(string shardId)
    {
        var internalId = snowflake.Generate(shardId);
        var externalId = ulid.Generate();
        // ...
    }
}
```

---

## See Also

- [Feature Overview](../features/id-generation.md) — Architecture and API reference
- [Scaling Guide](./id-generation-scaling.md) — Machine ID allocation and cluster operations
- [ADR-011](../architecture/adr/011-id-generation-multi-strategy.md) — Why multi-strategy
