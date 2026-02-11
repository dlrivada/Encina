# Sharding Enhancement Analysis — Feature Gap Study

## Context

Issue #289 (Database Sharding) is complete with ~680+ tests, 13 providers, 4 routing strategies, scatter-gather, caching integration, full OpenTelemetry observability, and comprehensive documentation. This analysis evaluates what enhancements can make Encina's sharding more competitive, useful, and integrated with the rest of the framework.

**Methodology**: Analyzed Encina source code (81 sharding files + all major subsystems), compared against ShardingSphere, Vitess, Citus, MongoDB native sharding, ShardingCore (.NET), and common enterprise patterns.

**Date**: February 2026
**Related Issue**: #289 (Database Sharding — completed)

---

## Current Sharding Strengths (vs Industry)

These are areas where Encina is **equal or ahead** of the industry:

| Strength | Detail |
|----------|--------|
| **Provider-agnostic** | Only solution covering ADO.NET, Dapper, EF Core, MongoDB (13 providers). ShardingSphere=JDBC, Vitess=MySQL, Citus=PostgreSQL |
| **4 routing strategies** | Hash, Range, Directory, Geo. Most competitors have 1-2 |
| **OpenTelemetry native** | 7 metrics + 3 traces. No competitor has equivalent built-in observability |
| **Rebalancing planning** | `IShardRebalancer` with `AffectedKeyRange` — unique application-level feature |
| **ROP error handling** | All operations return `Either<EncinaError, T>` — consistent with framework philosophy |
| **Caching integration** | Directory, topology, scatter-gather result caching via `Encina.Caching.Sharding` |
| **Partial failure handling** | `ShardedQueryResult<T>` with `AllowPartialResults` — graceful degradation |
| **MongoDB dual-mode** | Native mongos + app-level routing — unique flexibility |

---

## Enhancement Proposals

### Tier 1 — High Impact, High Differentiation

These fill the most significant gaps and would make Encina's sharding notably more advanced.

---

#### 1. Distributed ID Generation (`Encina.IdGeneration`)

**Gap**: Every major sharding system has built-in distributed IDs (Snowflake, sequences, ObjectId). Encina has none. Users must solve this themselves, which is error-prone.

**Proposal**: New package `Encina.IdGeneration` with multiple strategies:

| Strategy | Algorithm | Bits | Sortable | Shard-Embeddable |
|----------|-----------|------|----------|-------------------|
| `SnowflakeId` | 41 timestamp + 10 machine/shard + 12 sequence | 64-bit `long` | Yes | Yes (shard ID in bits) |
| `UlidId` | 48 timestamp + 80 random (ULID spec) | 128-bit | Yes | No |
| `UuidV7Id` | Timestamp-prefixed UUID (RFC 9562) | 128-bit | Yes | No |
| `ShardPrefixedId` | `{shardId}-{sequence}` or `{shardId}-{ulid}` | String | Yes | Yes |

**Integration with sharding**:
- `IShardedIdGenerator<TEntity>`: generates IDs that embed the shard identifier
- Routing can extract shard from ID without separate shard key lookup (Instagram pattern)
- `IShardRouter.GetShardIdFromEntityId(TId id)` — reverse routing from ID

**Why high impact**: Eliminates the #1 friction point for sharding adoption. Every user faces this problem.

**Scope**: ~15-20 source files, new package, 13-provider service registration.

---

#### 2. Reference Tables / Global Data Replication

**Gap**: Citus has `reference tables`, ShardingSphere has `broadcast tables`, Vitess has `lookup vindexes`. Encina has nothing. This is the most common cross-shard JOIN problem.

**Proposal**: `[ReferenceTable]` attribute + `IReferenceTableReplicator` service:

- Small, read-heavy tables (countries, currencies, config) marked as `[ReferenceTable]`
- Replicated to ALL shards automatically on startup + CDC-triggered refresh
- Enables local JOINs within any shard without cross-shard traffic
- Write path: writes go to a "primary" copy, then replicates to all shards

**Configuration**:
```csharp
services.AddEncinaSharding<Order>(options => { ... })
    .AddReferenceTable<Country>(options => {
        options.RefreshStrategy = RefreshStrategy.CdcDriven; // or Polling, Manual
        options.PrimaryShardId = "shard-0"; // writes go here
    });
```

**Integration**: Connects with CDC (`ICdcConnector`) for change propagation.

**Scope**: ~10-12 source files in core + per-provider replication logic.

---

#### 3. Distributed Aggregation Helpers

**Gap**: No `CountAcrossShards()`, `SumAcrossShards()`, `AvgAcrossShards()`. Users manually aggregate from `ShardedQueryResult<T>.Results`. Citus, Vitess, ShardingSphere all push aggregations to shards.

**Proposal**: Extension methods on `IShardedQueryExecutor` / `IFunctionalShardedRepository`:

```csharp
// Fan out COUNT to all shards, sum results
Task<Either<EncinaError, long>> CountAcrossShards(predicate)
Task<Either<EncinaError, decimal>> SumAcrossShards(selector, predicate)
Task<Either<EncinaError, decimal>> AvgAcrossShards(selector, predicate) // two-phase: SUM + COUNT
Task<Either<EncinaError, T>> MinAcrossShards(selector, predicate)
Task<Either<EncinaError, T>> MaxAcrossShards(selector, predicate)
```

**Two-phase aggregation**: AVG uses SUM/COUNT from each shard, then combines (not average-of-averages). This is the correct approach used by Citus.

**Scope**: ~5-8 source files (extension methods + per-provider implementations).

---

#### 4. Compound Shard Keys

**Gap**: `IShardable.GetShardKey()` returns a single `string`. MongoDB, Citus, Vitess all support compound keys `{region, customerId}`. Users must manually concatenate.

**Proposal**: `CompoundShardKey` record + `[ShardKey]` multi-attribute support:

```csharp
// Option A: IShardable with compound key
public class Order : IShardable
{
    public string GetShardKey() => $"{Region}:{CustomerId}"; // current - works but brittle
}

// Option B: New CompoundShardKey
public class Order : ICompoundShardable
{
    public CompoundShardKey GetCompoundShardKey() => new("us-east", CustomerId);
}

// Option C: Multiple [ShardKey] attributes with ordering
public class Order
{
    [ShardKey(Order = 0)] public string Region { get; set; }
    [ShardKey(Order = 1)] public string CustomerId { get; set; }
}
```

**Impact**: Enables range-on-first-key + hash-on-second-key patterns (MongoDB's compound shard key model).

**Scope**: ~6-8 source files (ShardKeyExtractor, routers, attribute changes).

---

### Tier 2 — Integration with Existing Encina Features

These connect sharding with other Encina subsystems, creating a more cohesive framework.

---

#### 5. Multi-Tenancy + Sharding Integration

**Gap**: Multi-tenancy and sharding exist independently. No tenant-aware shard routing. Common SaaS pattern: small tenants share shards, large tenants get dedicated shards.

**Proposal**: `ITenantShardRouter` combining `ITenantEntity.TenantId` + `IShardRouter`:

- **Shared routing**: Hash/directory routes `TenantId -> ShardId`
- **Tenant isolation**: Large tenants get dedicated shards via directory override
- **Hybrid mode**: Hash routing by default + directory overrides for VIP tenants
- Connection factory: `IShardedTenantConnectionFactory` returns connection for `(TenantId, ShardId)` pair

**Why tier 2**: Requires careful design of `ITenantConnectionFactory` + `IShardedConnectionFactory` unification. Both exist today as separate hierarchies.

**Scope**: ~12-15 source files across core + per-provider registration.

---

#### 6. Per-Shard Resilience (Circuit Breakers + Retry)

**Gap**: Health monitoring exists (`IShardedDatabaseHealthMonitor`) but doesn't influence routing decisions. No per-shard circuit breakers. If shard-3 is slow, shard-1 and shard-2 are still unaffected — but only because scatter-gather handles partial failures, not because there's proactive isolation.

**Proposal**:

- `ShardCircuitBreakerPipelineBehavior`: one Polly circuit breaker per `(RequestType, ShardId)`
- **Health-aware routing**: `IHealthAwareShardRouter` wraps `IShardRouter`, excludes unhealthy shards from routing decisions
- **Per-shard retry**: Retry policy per shard (independent backoff)
- **Bulkhead isolation**: Separate thread/semaphore pool per shard
- Integration with `ShardHealthResult` — circuit opens when shard is `Unhealthy`

**Scope**: ~8-10 source files in `Encina.Polly` + core decorators.

---

#### 7. Read/Write Separation + Sharding

**Gap**: Both features exist independently. No "shard-1 has a primary + 2 read replicas" topology.

**Proposal**: Extend `ShardInfo` with replica connection strings:

```csharp
record ShardInfo
{
    string ShardId;
    string ConnectionString;         // Primary (writes)
    IReadOnlyList<string> ReplicaConnectionStrings; // Read replicas
    ReplicaSelectionStrategy ReplicaStrategy; // RoundRobin, Random, LeastLatency
    // ... existing properties
}
```

- `IShardedConnectionFactory.GetReadConnectionAsync(shardId)` — returns replica
- `IShardedConnectionFactory.GetWriteConnectionAsync(shardId)` — returns primary
- Scatter-gather automatically uses replicas for read-only queries
- Compatible with existing `IReadWriteConnectionFactory` patterns

**Scope**: ~10-12 source files (ShardInfo extension, factory interfaces, per-provider).

---

#### 8. Shard-Aware Outbox/Inbox

**Gap**: Outbox and Inbox patterns work on a single database. With sharding, each shard should have its own outbox table, and messages should be processed per-shard.

**Proposal**:

- `IShardedOutboxStore`: extends `IOutboxStore` with `shardId` parameter
- `ShardedOutboxProcessor`: processes outbox messages from each shard independently
- Shard-local transactions: entity change + outbox message in same shard transaction
- Inbox: route incoming messages to correct shard based on correlation/shard key

**Why valuable**: This is the only way to guarantee at-least-once delivery in a sharded setup. Without it, the outbox breaks in multi-shard scenarios.

**Scope**: ~8-10 source files (interfaces + per-provider store implementations).

---

#### 9. CDC Per-Shard (`IShardedCdcConnector`)

**Gap**: CDC captures changes from a single database. With sharding, you need one CDC connector per shard, aggregating events across shards.

**Proposal**: `IShardedCdcConnector` wrapping multiple `ICdcConnector` instances:

```csharp
interface IShardedCdcConnector
{
    IAsyncEnumerable<Either<EncinaError, ShardedChangeEvent>> StreamAllShardsAsync(CancellationToken ct);
    IAsyncEnumerable<Either<EncinaError, ChangeEvent>> StreamShardAsync(string shardId, CancellationToken ct);
}

record ShardedChangeEvent(string ShardId, ChangeEvent Event);
```

- One `CdcProcessor` per shard, or a multiplexed processor
- Position tracking per `(ShardId, ConnectorType)`
- Integrates with `CdcMessagingBridge` for per-shard event publishing

**Scope**: ~6-8 source files in `Encina.Cdc`.

---

### Tier 3 — Advanced Enterprise Features

These are sophisticated features found in mature sharding platforms but less commonly needed.

---

#### 10. Co-Location Groups

**Gap**: No way to ensure related entities (Order + OrderItem) land on the same shard. Citus has `colocation groups`.

**Proposal**: `[ColocatedWith(typeof(Order))]` attribute + co-location enforcement:

- Entities in the same co-location group use the same shard key field
- Router validates co-location at registration time
- Enables efficient local JOINs between co-located entities within a shard

**Scope**: ~5-6 source files (attribute, validation, router enhancement).

---

#### 11. Online Resharding Workflow

**Gap**: `IShardRebalancer` plans which keys move, but actual migration is manual. Every major system (Vitess, Citus, MongoDB) automates this.

**Proposal**: `IReshardingOrchestrator` coordinating:

1. **Plan**: `IShardRebalancer.CalculateAffectedKeyRanges()` (already exists)
2. **Copy**: Bulk-copy affected rows to new shard (use `IBulkOperations`)
3. **Replicate**: CDC captures changes to affected rows during copy (use `ICdcConnector`)
4. **Verify**: Count + checksum validation
5. **Cutover**: Atomic topology switch (brief read-only window)
6. **Cleanup**: Remove migrated rows from source shard

**Integration**: Reuses `IBulkOperations` (existing) + `ICdcConnector` (existing) + `IShardRebalancer` (existing).

**Scope**: ~15-20 source files. Complex orchestration. Consider as separate issue/epic.

---

#### 12. Shadow Sharding (Test Strategy)

**Gap**: ShardingSphere has shadow databases for production testing. No equivalent in Encina.

**Proposal**: `IShadowShardRouter` decorator:

- Dual-write: all writes go to production shards AND shadow shards
- Shadow reads: configurable percentage of reads go to shadow topology
- Metrics comparison: latency/results between production and shadow
- Use case: validate new routing strategy before migration

**Scope**: ~5-6 source files (decorator, options, metrics).

---

#### 13. Time-Based Sharding / Archival

**Gap**: No first-class time-based sharding pattern. `RangeShardRouter` can use dates but there's no archival workflow.

**Proposal**: `TimeBasedShardRouter` specialization:

- Automatic range boundaries by period (month, quarter, year)
- `ShardTier` enum: `Hot`, `Warm`, `Cold`, `Archived`
- Automatic tier transitions (Hot -> Warm after 90 days, Warm -> Cold after 1 year)
- Cold shards: read-only mode, different connection string (cheaper storage)
- `IShardArchiver`: moves cold shard data to archive storage

**Scope**: ~8-10 source files.

---

#### 14. Schema Migration Coordination

**Gap**: DDL changes must be applied to ALL shards. No coordination mechanism. Vitess has `ApplySchema`, ShardingSphere has migration pipelines.

**Proposal**: `IShardedMigrationCoordinator`:

- Discovers all shards from topology
- Applies migration script to each shard sequentially or in parallel
- Rollback support if any shard fails
- Progress reporting and partial failure handling
- Integration with EF Core migrations (`dotnet ef database update --shard all`)

**Scope**: ~8-10 source files. Provider-specific for EF Core migrations.

---

#### 15. Specification-Based Scatter-Gather (Convenience)

**Gap**: `IFunctionalShardedRepository` requires manual lambda for scatter-gather. No direct `Specification<T>` support.

**Proposal**: Add convenience overloads:

```csharp
interface IFunctionalShardedRepository<TEntity, TId>
{
    // NEW: Specification-based scatter-gather
    Task<Either<EncinaError, ShardedQueryResult<TEntity>>> QueryAllShardsAsync(
        Specification<TEntity> specification, CancellationToken ct);

    // NEW: Paged scatter-gather
    Task<Either<EncinaError, ShardedQueryResult<TEntity>>> QueryAllShardsPagedAsync(
        Specification<TEntity> specification, PaginationOptions pagination, CancellationToken ct);
}
```

**Scope**: ~4-5 source files (interface extension + per-provider implementations).

---

## Priority Matrix

| # | Enhancement | Impact | Effort | Dependencies | Issue Type |
|---|------------|--------|--------|-------------|------------|
| 1 | **Distributed ID Generation** | Critical | Medium | None | Standalone |
| 2 | **Reference Tables** | High | High | CDC (#308) | Standalone |
| 3 | **Distributed Aggregations** | High | Low | None | Standalone |
| 4 | **Compound Shard Keys** | High | Low | None | Standalone |
| 5 | **Multi-Tenancy + Sharding** | High | Medium | Multi-Tenancy (#282) | Standalone |
| 6 | **Per-Shard Resilience** | High | Medium | Polly integration | Standalone |
| 7 | **Read/Write + Sharding** | Medium | Medium | R/W Separation (#283) | Standalone |
| 8 | **Shard-Aware Outbox/Inbox** | Medium | Medium | Outbox pattern | Standalone |
| 9 | **CDC Per-Shard** | Medium | Low | CDC (#308) | Standalone |
| 10 | **Co-Location Groups** | Medium | Low | None | Standalone |
| 11 | **Online Resharding** | Medium | Very High | Bulk ops (#284), CDC (#308) | Epic |
| 12 | **Shadow Sharding** | Low | Medium | None | Standalone |
| 13 | **Time-Based Sharding** | Low | Medium | None | Standalone |
| 14 | **Schema Migration** | Low | Medium | EF Core migrations | Standalone |
| 15 | **Spec-Based Scatter-Gather** | Medium | Very Low | None | Standalone |

---

## Recommended Implementation Order

**Phase A** (Quick wins, low effort, high impact):
1. **#3 Distributed Aggregations** — extension methods, ~5 files
2. **#4 Compound Shard Keys** — attribute + extractor changes, ~6 files
3. **#15 Specification-Based Scatter-Gather** — convenience overloads, ~4 files

**Phase B** (Medium effort, high differentiation):
4. **#1 Distributed ID Generation** — new package, ~15 files
5. **#6 Per-Shard Resilience** — Polly decorators, ~8 files
6. **#9 CDC Per-Shard** — wrapper connector, ~6 files

**Phase C** (Integration features):
7. **#5 Multi-Tenancy + Sharding** — subsystem integration, ~12 files
8. **#7 Read/Write + Sharding** — ShardInfo extension, ~10 files
9. **#8 Shard-Aware Outbox/Inbox** — messaging integration, ~8 files

**Phase D** (Enterprise/Advanced):
10. **#2 Reference Tables** — replication infrastructure, ~10 files
11. **#10 Co-Location Groups** — validation + enforcement, ~5 files
12. **#11 Online Resharding** — complex orchestration (epic)
13. **#12-14** — Shadow sharding, time-based, schema migration

---

## Competitive Positioning After Enhancements

| Feature Category | Before (Current) | After Phase A | After Phase B | After All |
|-----------------|:-:|:-:|:-:|:-:|
| Routing | Complete | + Compound keys | Same | Same |
| Cross-Shard Queries | Manual | Aggregations + Specs | Same | Same |
| ID Generation | None | None | Snowflake/ULID | Same |
| Resilience | Health only | Same | Per-shard CB | Same |
| Feature Integration | Isolated | Same | Same | Full |
| Data Management | None | None | None | Reference tables, resharding |

**Key differentiator after all phases**: The ONLY sharding solution that is provider-agnostic (13 providers), fully observable (OpenTelemetry), integrated with CQRS/messaging/tenancy, and follows Railway Oriented Programming — all while being opt-in and composable.

---

## Issue Tracking

All enhancements from this study are tracked as individual GitHub issues. See the milestone and label mapping in the execution notes below.

### Milestone Assignment

| Milestone | Issues | Rationale |
|-----------|--------|-----------|
| **v0.12.0** (Database & Repository) | #1-4, #7, #9-15 | Data access layer features |
| **v0.15.0** (Messaging & EIP) | #8 | Messaging pattern |
| **v0.16.0** (Multi-Tenancy & Modular) | #5 | Tenancy feature |
| **v0.19.0** (Observability & Resilience) | #6 | Resilience feature |
