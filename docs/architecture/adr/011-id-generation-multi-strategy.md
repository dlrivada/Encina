# ADR-011: Multi-Strategy Distributed ID Generation

**Status:** Accepted
**Date:** 2026-02-13
**Deciders:** David Lozano Rivada
**Technical Story:** [#638 - Distributed ID Generation for Sharded Entities](https://github.com/dlrivada/Encina/issues/638)

## Context

Encina's database sharding infrastructure (ADR-010) routes entities to shards based on shard keys. When entities are created in a sharded system, their identifiers must be:

1. **Globally unique** across all shards without central coordination
2. **Time-ordered** for B-tree index performance and natural chronological sorting
3. Optionally **shard-aware** so that an entity's shard can be determined from its ID alone (reverse routing)
4. Compatible with the 13 database providers that Encina supports

### Industry Context

Existing solutions take different approaches:

| System | Strategy | Limitations |
|--------|----------|-------------|
| Twitter Snowflake | 64-bit with datacenter + worker bits | Fixed bit layout, single algorithm |
| MongoDB ObjectId | 96-bit with timestamp + random + counter | Tied to MongoDB, non-standard format |
| Vitess (vindexes) | Hash-based with configurable algorithms | Focused on MySQL, tightly coupled to Vitess |
| ShardingSphere | Key generation strategies (Snowflake, UUID) | Java-only, limited to two strategies |
| UUID v4 (random) | 128-bit random | No time ordering, B-tree fragmentation |
| Database sequences | Auto-increment per node | Require coordination to avoid collisions |

No existing solution provides:

- Multiple strategies with a unified interface
- Shard embedding with extraction for reverse routing
- Type-safe ID wrappers with Railway Oriented Programming
- Cross-provider support (ADO.NET, Dapper, EF Core, MongoDB)

## Decision

Implement a **multi-strategy ID generation package** (`Encina.IdGeneration`) offering four algorithms behind two interfaces (`IIdGenerator<TId>` and `IShardedIdGenerator<TId>`), each represented by a strongly-typed `readonly record struct`.

### Key Principles

1. **Choice over mandate**: Different applications have different ID requirements. A financial system optimizing for compact BIGINT keys has different needs than an API-first product using string identifiers. Forcing a single strategy would compromise one use case for another.

2. **Shard-awareness as an opt-in capability**: Only Snowflake and ShardPrefixed implement `IShardedIdGenerator<TId>` with `ExtractShardId()`. ULID and UUIDv7 implement the simpler `IIdGenerator<TId>`. Users choose whether shard embedding is worth the trade-offs.

3. **Railway Oriented Programming**: All generation methods return `Either<EncinaError, TId>`, making failure modes (clock drift, sequence exhaustion, invalid shard IDs) explicit and composable.

4. **Zero-allocation ID types**: All four ID types are `readonly record struct`, avoiding heap allocations in hot paths.

### The Four Strategies

#### Snowflake (64-bit, `long`)

Configurable bit layout (default 41+10+12=63) with embedded timestamp, shard/machine ID, and per-millisecond sequence. Thread-safe via lock-based sequencing with clock drift tolerance.

**Trade-offs**: Compact and fast, but requires machine ID coordination and NTP synchronization.

#### ULID (128-bit, Crockford Base32)

48-bit timestamp + 80-bit cryptographic randomness. No coordination needed. Lexicographically sortable in string form.

**Trade-offs**: Larger than Snowflake, no shard embedding, but simpler operations.

#### UUIDv7 (128-bit, RFC 9562 Guid)

Time-ordered UUID compatible with `System.Guid`. Drop-in replacement for UUID v4 with B-tree locality.

**Trade-offs**: Standard format, but 16-byte storage and no shard embedding.

#### ShardPrefixed (variable-length string)

Format: `{shardId}{delimiter}{sequence}` where the sequence portion can be ULID, UUIDv7, or TimestampRandom. Shard extraction is trivial string splitting.

**Trade-offs**: Human-readable and shard-aware, but larger storage footprint and string-based comparison.

### Provider Integration

Each ID type has dedicated type mapping for all 13 database providers:

| Layer | Mechanism | Files per database |
|-------|-----------|-------------------|
| ADO.NET | `DbParameter` extension methods | 1 per database (4 total) |
| Dapper | `SqlMapper.TypeHandler<T>` | 4 per database (16 total) |
| EF Core | `ValueConverter<TId, TStore>` | Shared converters (4 total) |
| MongoDB | `IBsonSerializer<T>` | Shared serializers (4 total) |

## Consequences

### Positive

- **Flexibility**: Applications choose the strategy that best fits their requirements without framework lock-in
- **Type safety**: `SnowflakeId` cannot accidentally be used where `UlidId` is expected
- **Explicit errors**: `Either<EncinaError, TId>` prevents silent failures from clock drift or sequence exhaustion
- **Integration with sharding**: `IShardedIdGenerator<TId>` composes naturally with `IShardRouter` for shard-aware entity creation
- **Provider coherence**: All 13 database providers support all four ID types with zero custom SQL

### Negative

- **Complexity**: Four strategies require documentation, testing, and maintenance. Users must understand trade-offs
- **Machine ID coordination**: Snowflake requires external coordination for unique `MachineId` values
- **Clock dependency**: Snowflake and (to a lesser extent) ULID/UUIDv7 depend on system clock accuracy
- **Testing burden**: 4 ID types x 13 providers = 52 type mapping combinations to validate

### Neutral

- Package is opt-in; applications that don't need distributed IDs pay no cost
- ID types are defined in `Encina.IdGeneration` package, keeping the core `Encina` package unchanged except for the interface abstractions

## Alternatives Considered

### 1. Single Algorithm (Snowflake Only)

Snowflake covers the most demanding use case (compact, shard-embedded, high throughput). Other ID formats could be generated by wrapping Snowflake values.

**Rejected because**: Forces BIGINT storage even when GUID or string is more natural. Does not address the "existing UUID columns" migration scenario. Wrapping Snowflake in a GUID format loses the size advantage.

### 2. Wrapper-Only Approach (Delegate to External Libraries)

Provide strongly-typed wrappers around `NUlid`, `UUIDNext`, and similar NuGet packages without custom generation logic.

**Rejected because**: External libraries don't provide shard embedding, Railway Oriented error handling, or consistent `TimeProvider` integration. The abstraction boundary would leak third-party types into the public API.

### 3. Single Interface (No IShardedIdGenerator)

Use only `IIdGenerator<TId>` with an optional shard parameter (nullable string).

**Rejected because**: This conflates two different capabilities. A ULID generator that receives a shard parameter would have to ignore it or throw. The separate `IShardedIdGenerator<TId>` interface makes the capability explicit at the type level.

### 4. String-Only IDs (No Typed Wrappers)

Generate raw `long`, `string`, or `Guid` values without wrapping types.

**Rejected because**: Primitive obsession leads to bugs. A `long` order ID can be accidentally used as a `long` customer ID. Strongly-typed wrappers provide compile-time safety and meaningful API signatures.

## Related Decisions

- [ADR-001: Railway Oriented Programming](./001-railway-oriented-programming.md) — Error handling pattern used by all generators
- [ADR-010: Database Sharding](./010-database-sharding.md) — Sharding infrastructure that ID generation integrates with

## References

- [Twitter Snowflake (original)](https://blog.twitter.com/engineering/en_us/a/2010/announcing-snowflake)
- [RFC 9562 - UUID Version 7](https://www.rfc-editor.org/rfc/rfc9562)
- [ULID Specification](https://github.com/ulid/spec)
- [MongoDB ObjectId](https://www.mongodb.com/docs/manual/reference/bson-types/#objectid)
