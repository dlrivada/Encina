# ADR-010: Database Sharding

## Status

**Accepted** — February 2026

## Context

As applications built on Encina grow beyond a single database node, vertical scaling reaches its limits. Key pain points that motivate sharding include:

| Problem | Description |
|---------|-------------|
| **Data volume** | Single-node storage capacity or query latency degraded beyond acceptable thresholds |
| **Geographic distribution** | Data residency regulations (GDPR, CCPA) require data to stay within specific regions |
| **Tenant isolation** | Large multi-tenant systems need hard isolation between tenants for security or performance SLAs |
| **Write throughput** | Write-heavy workloads saturate a single primary's I/O capacity |

### Industry Context

Sharding is a well-established pattern, but implementations vary significantly:

| Approach | Pros | Cons |
|----------|------|------|
| **Proxy-based** (Vitess, Citus, mongos) | Transparent to application | Vendor lock-in, limited query flexibility |
| **ORM-level** (Django, Hibernate) | Deep integration | ORM-coupled, hard to customize |
| **Application-level** (Encina approach) | Full control, provider-agnostic | Application must manage routing explicitly |

### Design Requirements

1. **Sub-microsecond routing** — Routing must not become a bottleneck (<1μs for hash/range lookups)
2. **Multiple routing strategies** — Different workloads need hash, range, directory, or geographic routing
3. **Provider coherence** — Same abstractions across ADO.NET, Dapper, EF Core, and MongoDB
4. **Opt-in adoption** — Zero impact on non-sharded applications
5. **Observable** — Full OpenTelemetry tracing and metrics for production operations
6. **Rebalancing support** — Plan data migration when adding or removing shards

## Decision

### Core Abstraction Chain

```text
Entity  →  ShardKeyExtractor  →  IShardRouter  →  ShardTopology  →  Factory  →  Connection/Context
                                       │
                           ┌────────────┼────────────┐──────────────┐
                     HashShardRouter  RangeShardRouter  DirectoryShardRouter  GeoShardRouter
```

The sharding architecture is a pipeline:

1. **Shard key extraction** — `ShardKeyExtractor` reads the shard key from an entity via `IShardable.GetShardKey()` or `[ShardKey]` attribute (reflection-cached)
2. **Routing** — `IShardRouter.GetShardId(shardKey)` maps the key to a shard identifier
3. **Topology lookup** — `ShardTopology` resolves the shard identifier to connection metadata
4. **Connection creation** — Provider-specific factories create connections to the resolved shard

All steps return `Either<EncinaError, T>` following the project's Railway Oriented Programming pattern (ADR-001).

### Four Routing Strategies

| Strategy | Algorithm | Key Characteristic | Best For |
|----------|-----------|-------------------|----------|
| **Hash** | xxHash64 + consistent hashing with virtual nodes | Uniform distribution, ~1/N data movement on rebalance | General-purpose, multi-tenant |
| **Range** | Sorted boundaries + binary search | Contiguous key ranges per shard | Time-series, alphabetical partitioning |
| **Directory** | Explicit key-to-shard lookup via `IShardDirectoryStore` | Full control over placement | VIP tenants, compliance requirements |
| **Geo** | Region resolver + fallback chains | Location-aware routing | Data residency, latency optimization |

#### Hash Router Details

- **Algorithm**: xxHash64 (`System.IO.Hashing.XxHash64`) — chosen for speed (3.7 GB/s), excellent distribution, and zero external dependencies
- **Virtual nodes**: 150 per shard (configurable via `HashShardRouterOptions.VirtualNodesPerShard`). Each shard gets N virtual positions on the ring using the key format `"{shardId}#vn{i}"`
- **Ring structure**: `SortedDictionary<ulong, string>` with cached `ulong[]` for binary search
- **Rebalancing**: Implements `IShardRebalancer` to calculate `AffectedKeyRange` entries when topology changes — approximately 1/N of keys are affected when adding the N-th shard

#### Range Router Details

- **Comparison**: Lexicographic ordinal (configurable `StringComparer`)
- **Unbounded ranges**: `ShardRange.EndKey = null` means extends to +infinity
- **Validation**: Overlapping ranges detected at construction time (error code: `encina.sharding.overlapping_ranges`)

#### Directory Router Details

- **Storage**: `IShardDirectoryStore` interface for pluggable backends
- **Built-in**: `InMemoryShardDirectoryStore` (ConcurrentDictionary, thread-safe)
- **Fallback**: Optional `DefaultShardId` for unmapped keys

#### Geo Router Details

- **Region mapping**: `GeoRegion` records map region codes to shard IDs
- **Fallback chains**: Follow `FallbackRegionCode` links with cycle detection
- **Configuration**: `GeoShardRouterOptions` with `RequireExactMatch` and `DefaultRegion`

### Provider Factory Architecture

| Provider | Factory Interface | Creates |
|----------|-------------------|---------|
| **ADO.NET** | `IShardedConnectionFactory` / `IShardedConnectionFactory<TConnection>` | `IDbConnection` |
| **Dapper** | Reuses ADO's `IShardedConnectionFactory` (non-generic) | `IDbConnection` |
| **EF Core** | `IShardedDbContextFactory<TContext>` | `DbContext` subclass |
| **MongoDB** | `IShardedMongoCollectionFactory` | `IMongoCollection<TEntity>` |

**Key design decision**: Dapper does **not** register its own connection factory. It reuses ADO.NET's `IShardedConnectionFactory`, which means:

- Zero additional factory classes for Dapper providers
- ADO registration is a prerequisite for Dapper sharding
- This mirrors Dapper's nature as a thin extension over ADO.NET

### Provider Registration Matrix

| Category | DI Extension | Providers | Count |
|----------|-------------|-----------|-------|
| Core | `AddEncinaSharding<TEntity>()` | All (registers topology + router) | — |
| ADO.NET | `AddEncinaADOSharding<TEntity, TId>()` | SQLite, SqlServer, PostgreSQL, MySQL | 4 |
| Dapper | `AddEncinaDapperSharding<TEntity, TId>()` | SQLite, SqlServer, PostgreSQL, MySQL | 4 |
| EF Core | `AddEncinaEFCoreSharding{Provider}<TContext, TEntity, TId>()` | SQLite, SqlServer, PostgreSQL, MySQL | 4 |
| MongoDB | `AddEncinaMongoDBSharding<TEntity, TId>()` | MongoDB | 1 |
| **Total** | | | **13** |

### MongoDB Dual-Mode

MongoDB supports two distinct sharding modes controlled by `MongoDbShardingOptions.UseNativeSharding`:

```text
┌─────────────────────────────────────────────────────────────┐
│                    Native Mode (default)                     │
│  App ──► mongos ──► shard1 / shard2 / shard3                │
│  • MongoDB handles routing transparently                     │
│  • Configure shard key via ConfigureShardKey                 │
│  • Recommended for production                                │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                  App-Level Mode (fallback)                    │
│  App ──► IShardRouter ──► mongod1 / mongod2 / mongod3       │
│  • Encina routes at application level                        │
│  • Requires AddEncinaSharding<TEntity>() topology            │
│  • Use for dev/test or when mongos is unavailable            │
└─────────────────────────────────────────────────────────────┘
```

### Cross-Shard Query Execution

The `IShardedQueryExecutor` implements scatter-gather with configurable behavior:

| Option | Default | Description |
|--------|---------|-------------|
| `MaxParallelism` | -1 (unlimited) | Maximum concurrent shard queries |
| `Timeout` | 30 seconds | Operation timeout |
| `AllowPartialResults` | true | Return results even if some shards fail |

Results are returned as `ShardedQueryResult<T>` which tracks successful shards, failed shards, and aggregate results.

**Design decision**: Cross-shard ACID transactions are **not supported**. The application must use the Saga pattern (from Encina.Messaging) for distributed workflows that span shards. This avoids the complexity and performance costs of two-phase commit.

### Observability

| Category | Instruments | Details |
|----------|------------|---------|
| **Metrics** (7 instruments) | `encina.sharding.route.decisions` (Counter), `encina.sharding.route.duration_ns` (Histogram), `encina.sharding.topology.shards.active` (ObservableGauge), `encina.sharding.scatter.duration_ms` (Histogram), `encina.sharding.scatter.shard.duration_ms` (Histogram), `encina.sharding.scatter.partial_failures` (Counter), `encina.sharding.scatter.queries.active` (UpDownCounter) | Meter: "Encina" |
| **Traces** (3 activities) | Routing, ScatterGather, ShardQuery | ActivitySource: "Encina.Sharding" |
| **Configuration** | `ShardingMetricsOptions` with `EnableRoutingMetrics`, `EnableScatterGatherMetrics`, `EnableHealthMetrics`, `EnableTracing` | All enabled by default |

All tracing methods use `ActivitySource.HasListeners()` guards for zero-cost when no listener is attached.

## Consequences

### Positive

1. **Linear horizontal scalability** — Add shards to distribute load; hash routing ensures uniform distribution
2. **Provider-agnostic** — Same routing and topology abstractions across all 13 database providers
3. **Sub-microsecond routing** — Hash and range routers use pre-computed data structures (binary search on cached arrays)
4. **Production-ready observability** — Full OpenTelemetry integration with 7 metric instruments and 3 trace activities
5. **Migration planning** — `IShardRebalancer` calculates exactly which key ranges move when topology changes
6. **Minimal overhead for non-sharded apps** — All sharding code is opt-in; disabled by default

### Negative

1. **No cross-shard JOINs** — Queries are scoped to individual shards; cross-shard queries require scatter-gather
2. **Operational complexity** — Schema migrations must be coordinated across all shards
3. **Application responsibility** — Data migration during rebalancing is the application's responsibility; Encina only plans the migration
4. **Shard key immutability** — Changing an entity's shard key requires manual migration (delete from old shard, insert to new)

### Neutral

1. **Monitoring infrastructure required** — OpenTelemetry collector/dashboard needed to consume metrics and traces
2. **Connection pool sizing** — Each shard needs its own connection pool; total connection count scales with shard count

## Alternatives Considered

### 1. Vertical Scaling Only

**Rejected** — Single-node databases have hard limits on storage, throughput, and geographic distribution. Does not solve data residency requirements.

### 2. Read Replicas Only

**Rejected** — Addresses read scaling but not write throughput, data volume, or geographic distribution. Encina already supports read/write separation (see `docs/features/read-write-separation.md`); sharding complements it.

### 3. MongoDB-Only Sharding (Rely on mongos Exclusively)

**Rejected** — Would limit sharding to MongoDB users. The application-level approach serves all 13 providers. MongoDB users get the best of both worlds: native mongos (recommended) with app-level fallback.

### 4. Proxy-Based Approach (Vitess/Citus)

**Rejected** — Adds infrastructure dependency, limits to specific databases, and reduces application control. The application-level approach is more portable and customizable.

### 5. Two-Phase Commit for Cross-Shard Transactions

**Deferred** — 2PC adds significant complexity and latency. The Saga pattern from Encina.Messaging provides eventual consistency with compensation, which is sufficient for most use cases. 2PC may be revisited post-1.0 if demand materializes.

## Related Decisions

- **ADR-001** (Railway Oriented Programming) — All sharding APIs return `Either<EncinaError, T>`
- **ADR-009** (Remove Oracle Provider) — Reduces provider count from 16 to 13; sharding was designed for 13 providers

## References

- Issue [#289](https://github.com/dlrivada/Encina/issues/289) — Original sharding feature request
- Issue [#637](https://github.com/dlrivada/Encina/issues/637) — Rebalancing and migration planning
- Issue [#290](https://github.com/dlrivada/Encina/issues/290) — Scatter-gather query execution
- [Consistent Hashing (Wikipedia)](https://en.wikipedia.org/wiki/Consistent_hashing)
- [xxHash Algorithm](https://cyan4973.github.io/xxHash/)

## Notes

- **Pre-1.0**: Data migration tooling (beyond `IShardRebalancer` planning) is planned for a future release
- **Dapper dependency**: Dapper sharding requires ADO provider registration first (documented in configuration guide)
- **Virtual node count**: 150 default provides good balance between ring uniformity and memory usage; configurable for advanced users
- **Health checks**: `ShardingMetricsOptions.HealthCheckInterval` defaults to 30 seconds for periodic shard health monitoring
