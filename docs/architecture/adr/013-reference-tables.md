# ADR-013: Reference Tables (Global Data Replication)

## Status

**Accepted** — February 2026

## Context

ADR-010 introduced database sharding with four routing strategies and 13-provider support. While sharding distributes data for scalability, it creates a problem for **small, shared lookup tables** (countries, currencies, categories) that are needed by queries on every shard.

### Problem Statement

| Challenge | Description |
|-----------|-------------|
| **Cross-shard JOINs** | Sharded entities (e.g., Orders) need to JOIN with lookup tables (e.g., Countries). Without local copies, every such query requires a scatter-gather to the shard holding the lookup data. |
| **Latency** | Cross-shard JOINs add network round-trips. For tables used in most queries, this latency compounds significantly. |
| **Complexity** | Application code must handle cross-shard data fetching, caching, and invalidation manually without framework support. |
| **Consistency model** | Reference data changes rarely but must eventually reach all shards. Strong consistency is typically not required. |

### Real-World Example

An e-commerce platform with Orders sharded by CustomerId:

```sql
SELECT o.Id, o.Total, c.Name AS CountryName, cur.Symbol
FROM Orders o
JOIN Countries c ON o.CountryId = c.Id
JOIN Currencies cur ON o.CurrencyId = cur.Id
WHERE o.CustomerId = @CustomerId
```

Without reference tables, this query either:

1. **Scatter-gathers** to fetch Countries and Currencies from their home shard (slow, complex)
2. **Denormalizes** country/currency names into Orders (data duplication, stale on name changes)
3. **Application caches** the lookup data (custom code, cache invalidation complexity)

### Design Constraints

1. **Reuse sharding infrastructure** — Leverage `ShardTopology`, `IShardRouter`, connection factories from ADR-010
2. **Provider coherence** — Same interface across all 13 database providers (ADO.NET x 4, Dapper x 4, EF Core x 4, MongoDB x 1)
3. **Opt-in** — No overhead when reference tables are not configured (pay-for-what-you-use)
4. **Multiple strategies** — Different tables may need different refresh approaches
5. **Observability** — Full OpenTelemetry metrics and tracing integration
6. **ROP compliance** — All operations return `Either<EncinaError, T>`

## Decision

### Architecture: Replication Model

We chose a **primary-to-all broadcast replication** model where one designated shard holds the authoritative copy, and data is replicated to all other shards:

```text
              Primary Shard
              (authoritative source)

              Countries: 250 rows
                      |
              +-------+-------+
              |       |       |
              v       v       v
          shard-1  shard-2  shard-3
          250 rows 250 rows 250 rows
          (replica) (replica) (replica)
```

### Key Design Decisions

#### 1. Full-Table Replication (Not Row-Level)

We replicate the **entire table** on each sync cycle rather than individual changed rows.

**Rationale**:

- Reference tables are small (< 100K rows) by definition
- Full-table upsert is simpler and more reliable than row-level CDC tracking
- Eliminates the need for per-row change tracking on every provider
- Hash comparison provides an efficient skip mechanism when data hasn't changed
- Recovery from any inconsistent state is automatic (full replace)

**Trade-off**: More network bandwidth per sync vs. simpler implementation and guaranteed convergence.

#### 2. XxHash64 for Change Detection

We use XxHash64 (non-cryptographic) hash of serialized table data to detect changes:

**Rationale**:

- ~10 GB/s throughput on modern CPUs — negligible cost even for 100K-row tables
- 64-bit hash provides sufficient collision resistance for change detection (not security)
- Deterministic: rows sorted by PK, serialized to JSON, fed into XxHash64 sequentially
- Cross-provider consistency: same hash algorithm regardless of database engine

**Alternative considered**: Row version tracking (SQL Server `rowversion`, PostgreSQL `xmin`). Rejected because it's provider-specific and doesn't work across heterogeneous shard databases.

#### 3. Three Refresh Strategies

We provide three strategies instead of one to cover different use cases:

| Strategy | Trade-off |
|----------|-----------|
| `CdcDriven` | Lowest latency, but requires CDC infrastructure |
| `Polling` | Moderate latency, zero infrastructure requirements |
| `Manual` | No automatic sync, full control for deployment-time data |

**Rationale**: A single strategy would either over-engineer simple cases (CDC for static country codes) or under-serve dynamic cases (polling for frequently changing pricing data).

#### 4. Provider-Agnostic Store Interface

`IReferenceTableStore` abstracts the bulk upsert mechanism:

| Provider | SQL |
|----------|-----|
| SQLite | `INSERT OR REPLACE INTO ...` |
| SQL Server | `MERGE ... WHEN MATCHED THEN UPDATE` |
| PostgreSQL | `INSERT ... ON CONFLICT DO UPDATE` |
| MySQL | `INSERT ... ON DUPLICATE KEY UPDATE` |
| MongoDB | `BulkWriteAsync` with `ReplaceOneModel` |

**Rationale**: Each database engine has a different upsert syntax. Abstracting behind `IReferenceTableStore` allows the replicator to be provider-agnostic while each provider uses the most efficient native mechanism.

#### 5. Entity Metadata via Reflection (Cached)

`EntityMetadataCache` discovers table names, column names, and primary keys from `[Table]`, `[Column]`, and `[Key]` attributes:

**Rationale**:

- Reuses standard `System.ComponentModel.DataAnnotations` attributes (no new attribute invention)
- Single reflection pass cached via `ConcurrentDictionary` for O(1) subsequent lookups
- Works with all providers (ADO.NET, Dapper, EF Core, MongoDB)

**Alternative considered**: EF Core's `IModel` metadata. Rejected because it only works for EF Core provider, not ADO.NET/Dapper.

## Alternatives Considered

### 1. Application-Level Caching

Cache reference data in memory (or Redis) at the application layer.

| Aspect | Evaluation |
|--------|------------|
| Pros | Simple, no database changes, works across providers |
| Cons | Cannot participate in SQL JOINs, cache invalidation complexity, memory usage per instance |
| Verdict | **Rejected for SQL JOIN use case** — but complementary for application-layer reads |

### 2. Materialized Views

Use database materialized views to maintain copies of reference data.

| Aspect | Evaluation |
|--------|------------|
| Pros | Database-native, SQL-optimized |
| Cons | Provider-specific (PostgreSQL only), no cross-database support, manual refresh management |
| Verdict | **Rejected** — not portable across 13 providers |

### 3. Denormalization

Copy reference data fields directly into sharded entities.

| Aspect | Evaluation |
|--------|------------|
| Pros | Fastest reads, no JOINs needed, simplest queries |
| Cons | Data duplication, stale on reference data updates, increases entity size, complex updates |
| Verdict | **Rejected as primary approach** — but can be used alongside reference tables |

### 4. Federated Queries / Linked Servers

Use database federation features to query across shards.

| Aspect | Evaluation |
|--------|------------|
| Pros | Real-time data, no replication lag |
| Cons | Provider-specific, complex setup, query performance depends on network, single points of failure |
| Verdict | **Rejected** — not portable, adds latency to every query |

## Consequences

### Positive

1. **Efficient local JOINs**: All shards have local copies of reference data, eliminating cross-shard traffic for lookup queries
2. **Provider-agnostic**: Same `IReferenceTableStore` interface across all 13 providers
3. **Flexible strategies**: CdcDriven, Polling, and Manual cover all common use cases
4. **Observable**: Full OpenTelemetry integration with 5 metrics, activity enrichment, and 15 error codes
5. **Health-aware**: Built-in health check with three-state model (Healthy/Degraded/Unhealthy)
6. **ROP-compliant**: All operations return `Either<EncinaError, T>` with descriptive error codes

### Negative

1. **Storage duplication**: Each reference table is fully copied to every shard
2. **Eventual consistency**: Replica data lags behind the primary (seconds to minutes)
3. **Full-table sync**: Even single-row changes trigger full-table replication (mitigated by hash-based skip)
4. **Table size constraints**: Practical limit of ~100K rows per reference table

### Risks

| Risk | Mitigation |
|------|------------|
| Reference table grows beyond recommended size | Monitoring + health check alerts when replication duration exceeds thresholds |
| Network partition during replication | Partial failure handling in `ReplicationResult`; failed shards retry on next cycle |
| Hash collision (false negative on change detection) | XxHash64 provides 64-bit collision resistance; probability is negligible for reference table sizes |
| Primary shard unavailable | Health check reports Unhealthy; manual failover by changing `PrimaryShardId` |

## Related

- [ADR-010: Database Sharding](010-database-sharding.md) — Foundation sharding architecture
- [ADR-012: Sharded Read/Write Separation](012-sharded-read-write-separation.md) — Per-shard read replicas
- [GitHub Issue #639](https://github.com/dlrivada/Encina/issues/639) — Reference Tables / Global Data Replication
- [GitHub Issue #289](https://github.com/dlrivada/Encina/issues/289) — Database Sharding (parent)
- [GitHub Issue #308](https://github.com/dlrivada/Encina/issues/308) — CDC Pattern
- [GitHub Issue #647](https://github.com/dlrivada/Encina/issues/647) — Co-Location Groups
