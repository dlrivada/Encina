# Load Test Performance Baselines

This document describes the expected performance characteristics for all load test categories across all providers.

## Categories Overview

Encina load tests are organized into five categories:

| Category | Description | Providers | Scenarios |
|----------|-------------|-----------|-----------|
| **Database** | Transaction management, tenancy, read/write separation | 13 DB providers | UoW, Tenancy, ReadWrite |
| **Messaging** | In-memory pub/sub, dispatcher patterns | inmemory | InMemoryBus, Dispatcher |
| **Caching** | Memory cache, Redis, hybrid L1/L2 | memory, redis, hybrid | Throughput, Concurrent, Eviction |
| **Locking** | Distributed lock coordination | inmemory, redis, sqlserver | Contention, Renewal, Timeout |
| **Brokers** | Message broker pub/sub | rabbitmq, kafka, nats, mqtt | Publish, Consume, Partition |

---

# Database Load Tests

The Encina database load tests exercise three key features under concurrent load:

1. **Unit of Work (UoW)** - Transaction management and coordination
2. **Multi-Tenancy** - Tenant isolation and context switching
3. **Read/Write Separation** - Replica distribution and load balancing

## Expected Throughput by Provider

### Provider Performance Comparison

| Provider | Category | Expected Ops/Sec | Mean Latency | P95 Latency |
|----------|----------|------------------|--------------|-------------|
| `ado-sqlite` | ADO.NET | 3,000+ | <15ms | <60ms |
| `ado-sqlserver` | ADO.NET | 1,000+ | <45ms | <180ms |
| `ado-postgresql` | ADO.NET | 1,200+ | <40ms | <160ms |
| `ado-mysql` | ADO.NET | 1,100+ | <42ms | <170ms |
| `dapper-sqlite` | Dapper | 2,500+ | <18ms | <70ms |
| `dapper-sqlserver` | Dapper | 900+ | <48ms | <190ms |
| `dapper-postgresql` | Dapper | 1,100+ | <42ms | <170ms |
| `dapper-mysql` | Dapper | 1,000+ | <45ms | <180ms |
| `efcore-sqlite` | EF Core | 2,000+ | <20ms | <80ms |
| `efcore-sqlserver` | EF Core | 800+ | <50ms | <200ms |
| `efcore-postgresql` | EF Core | 1,000+ | <45ms | <180ms |
| `efcore-mysql` | EF Core | 900+ | <48ms | <190ms |
| `mongodb` | MongoDB | 1,200+ | <35ms | <120ms |

### Provider Category Comparison (Same Database)

For SQL Server operations, comparing provider overhead:

| Provider | Overhead | Best For |
|----------|----------|----------|
| ADO.NET | Minimal (~1%) | Maximum performance, raw SQL control |
| Dapper | Low (~5%) | Balance of performance and productivity |
| EF Core | Moderate (~15%) | Rich ORM features, change tracking |

## Feature-Specific Thresholds

### Unit of Work Scenarios

| Scenario | Min Throughput | Max Latency | Error Rate |
|----------|----------------|-------------|------------|
| `uow-concurrent-transactions` | 500 ops/sec | 75ms mean | <2% |
| `uow-rollback-under-load` | 300 ops/sec | 100ms mean | <5% |
| `uow-connection-pool-pressure` | 200 ops/sec | 150ms mean | <1% |

**Notes:**

- Transaction overhead adds 10-30% latency compared to non-transactional operations
- Rollback scenarios intentionally fail some operations (not counted as errors)
- Pool pressure tests may show higher latency due to connection wait times

### Multi-Tenancy Scenarios

| Scenario | Min Throughput | Max Latency | Error Rate |
|----------|----------------|-------------|------------|
| `tenancy-isolation` | 1,000 ops/sec | 40ms mean | 0% |
| `tenancy-context-switching` | 800 ops/sec | 50ms mean | 0% |

**Notes:**

- Tenant isolation must have **zero tolerance** for cross-tenant data access
- Context switching tests verify `AsyncLocal<T>` isolation across async boundaries
- 100 tenants simulated by default

### Read/Write Separation Scenarios

| Scenario | Min Throughput | Max Latency | Distribution |
|----------|----------------|-------------|--------------|
| `readwrite-replica-distribution` | 1,500 ops/sec | 30ms mean | <15% deviation |
| `readwrite-roundrobin-validation` | 1,500 ops/sec | 30ms mean | <5% deviation |
| `readwrite-leastconnections-validation` | 1,200 ops/sec | 40ms mean | Variable |

**Notes:**

- Round-robin should achieve near-perfect distribution (deviation <5%)
- Least-connections distribution varies based on actual load patterns
- Read-only operations should be significantly faster than writes

## MongoDB-Specific Considerations

### Replica Set Requirements

MongoDB transactions require a replica set configuration. The load tests use:

- Single-node replica set for testing (via Testcontainers)
- Production deployments should use 3+ node replica sets

### Transaction Limitations

| Feature | MongoDB Support |
|---------|-----------------|
| Multi-document transactions | Requires replica set |
| Read/Write separation | Native read preference support |
| Connection pooling | Managed by driver (not traditional IDbConnection) |

### Expected Performance

```
MongoDB operations (replica set):
- Insert: 1,500-2,000 ops/sec
- Read (count): 3,000-5,000 ops/sec
- Transaction commit: 800-1,200 ops/sec
```

## Factors Affecting Performance

### Connection Pool Configuration

| Factor | Impact | Recommendation |
|--------|--------|----------------|
| Pool size too small | Connection wait times | Match pool size to max concurrent users |
| Pool size too large | Memory overhead | Start with 10-20, scale as needed |
| Connection lifetime | Stale connections | 5-10 minute lifetime |

### Transaction Isolation Levels

| Level | Performance | Consistency |
|-------|-------------|-------------|
| Read Uncommitted | Fastest | Dirty reads possible |
| Read Committed | Fast | Default for most databases |
| Repeatable Read | Moderate | Locks held longer |
| Serializable | Slowest | Maximum consistency |

### Network Latency

| Scenario | Expected Latency |
|----------|------------------|
| Local Docker container | <1ms |
| Same datacenter | 1-5ms |
| Cross-region | 50-200ms |

## Running Load Tests

### Local Development

```bash
# Run all database load tests for a specific provider
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --provider efcore-sqlite \
    --feature All \
    --duration 00:02:00

# Run specific feature
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --provider efcore-postgresql \
    --feature UnitOfWork \
    --duration 00:01:00
```

### CI/CD

Database load tests run:

- **Schedule**: Every Saturday at 2:00 AM UTC
- **Manual**: Via workflow dispatch with provider selection

Results are uploaded as artifacts per provider.

## Interpreting Results

### Success Criteria

1. **Throughput**: Above minimum threshold for provider/feature
2. **Latency**: Below maximum threshold (mean and P95)
3. **Error Rate**: Below maximum allowed (varies by scenario)
4. **Distribution**: Within acceptable deviation (for read/write scenarios)

### Common Issues

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Low throughput | Container resource limits | Increase Docker resources |
| High P99 latency | GC pauses | Profile memory allocation |
| Connection timeouts | Pool exhaustion | Increase pool size or reduce concurrency |
| Transaction failures | Lock contention | Review isolation level |

## Related Files (Database)

- `tests/Encina.NBomber/Scenarios/Database/` - Scenario implementations
- `tests/Encina.LoadTests/profiles/nbomber.database-*.json` - Profile configurations
- `ci/nbomber-database-thresholds.json` - CI threshold configurations

---

# Messaging Load Tests

The messaging load tests exercise the in-memory message bus and dispatcher patterns.

## Messaging Feature Comparison

| Feature | Target Throughput | Target Latency | Use Case |
|---------|-------------------|----------------|----------|
| **InMemoryBus** | 1,000+ ops/sec | <10ms mean | High-throughput pub/sub within process |
| **Dispatcher** | 500+ ops/sec | <25ms mean | DI-resolved handlers with pipeline behaviors |

## InMemoryBus Scenarios

| Scenario | Description | Min Throughput | Max Latency |
|----------|-------------|----------------|-------------|
| `inmemory-concurrent-publish` | High-rate message publishing | 1,000 ops/sec | 10ms mean |
| `inmemory-handler-registration` | Concurrent subscribe/unsubscribe | 500 ops/sec | 20ms mean |
| `inmemory-handler-execution` | Multiple handlers per message type | 800 ops/sec | 15ms mean |

**Notes:**

- InMemoryBus uses lock-free `ConcurrentDictionary` for subscriptions
- Handler registration is thread-safe for concurrent access
- Multiple handlers can be registered for the same message type

## Dispatcher Scenarios

| Scenario | Description | Min Throughput | Max Latency |
|----------|-------------|----------------|-------------|
| `dispatcher-parallel-throughput` | Parallel notification dispatch | 500 ops/sec | 25ms mean |
| `dispatcher-sequential-throughput` | Sequential notification dispatch | 400 ops/sec | 30ms mean |
| `dispatcher-pipeline-overhead` | Pipeline behavior chain overhead | 600 ops/sec | 15ms mean |

**Notes:**

- Parallel dispatch uses `Task.WhenAll` for concurrent handler execution
- Sequential dispatch executes handlers one-by-one (predictable ordering)
- Pipeline behaviors add ~1-2ms per behavior in the chain

## Related Files (Messaging)

- `tests/Encina.NBomber/Scenarios/Messaging/` - Scenario implementations
- `ci/nbomber-messaging-thresholds.json` - CI threshold configurations

---

# Caching Load Tests

The caching load tests exercise memory cache, Redis cache, and hybrid (L1/L2) caching.

## Provider Performance Comparison

| Provider | Target Throughput | Target Latency | Description |
|----------|-------------------|----------------|-------------|
| **memory** | 10,000+ ops/sec | <2ms mean | In-memory cache, zero network overhead |
| **redis** | 5,000+ ops/sec | <10ms mean | Redis container with serialization |
| **hybrid** | 8,000+ ops/sec | <5ms mean | L1 memory + L2 Redis fallback |

## Memory Cache Scenarios

| Scenario | Description | Min Throughput | Notes |
|----------|-------------|----------------|-------|
| `memory-get-set-throughput` | High-rate read/write (100B-10KB values) | 5,000 ops/sec | Varying value sizes |
| `memory-concurrent-access` | Thread-safe operations on same keys | 3,000 ops/sec | GetOrSet, Exists operations |
| `memory-eviction-pressure` | Behavior under size limits | 1,000 ops/sec | Some eviction expected |

## Redis Cache Scenarios

| Scenario | Description | Min Throughput | Notes |
|----------|-------------|----------------|-------|
| `redis-get-set-throughput-redis` | Read/write with connection pooling | 2,000 ops/sec | 33% writes, 67% reads |
| `redis-concurrent-access-redis` | GetOrSet with stampede protection | 1,000 ops/sec | Lua script atomicity |
| `redis-expiration-accuracy-redis` | TTL precision under load | 500 ops/sec | <100ms drift expected |
| `redis-pipeline-batching-redis` | Pipeline vs individual comparison | 500 ops/sec | Batched operations faster |

## Hybrid Cache Scenarios

| Scenario | Description | Min Throughput | Notes |
|----------|-------------|----------------|-------|
| `hybrid-l1-throughput` | L1 memory cache hit rate | 8,000 ops/sec | Should approach memory speed |
| `hybrid-l2-fallback` | L2 Redis fallback on L1 miss | 2,000 ops/sec | Falls back to Redis |
| `hybrid-invalidation` | Cross-tier cache invalidation | 1,000 ops/sec | Must be consistent |

## Related Files (Caching)

- `tests/Encina.NBomber/Scenarios/Caching/` - Scenario implementations
- `ci/nbomber-caching-thresholds.json` - CI threshold configurations

---

# Distributed Locking Load Tests

The locking load tests exercise distributed lock providers under contention.

## Provider Performance Comparison

| Provider | Target Throughput | Target Latency | Description |
|----------|-------------------|----------------|-------------|
| **inmemory** | 2,000+ ops/sec | <10ms mean | Single-process locks (baseline) |
| **redis** | 500+ ops/sec | <30ms mean | Redlock algorithm implementation |
| **sqlserver** | 200+ ops/sec | <60ms mean | sp_getapplock based locks |

## Critical Metrics

All lock providers must guarantee:

1. **Mutual Exclusion**: Zero violations (100% isolation)
2. **Deadlock Freedom**: No indefinite blocking
3. **Lock Release**: <10ms average release time

## InMemory Lock Scenarios

| Scenario | Description | Success Rate | Notes |
|----------|-------------|--------------|-------|
| `inmemory-lock-contention` | 50+ clients, 5-10 resources | >90% | Timeouts expected |
| `inmemory-lock-throughput` | Maximum throughput without contention | >99% | Baseline measurement |

## Redis Lock Scenarios

| Scenario | Description | Success Rate | Notes |
|----------|-------------|--------------|-------|
| `redis-lock-contention-redis` | Concurrent clients competing for locks | >80% | High contention causes timeouts |
| `redis-lock-release-timing-redis` | Lock release latency measurement | >95% | Target <10ms release |
| `redis-lock-renewal-redis` | Lock extension under contention | >90% | Some renewals may fail |
| `redis-lock-timeout-accuracy-redis` | TTL expiration precision | >95% | <100ms drift expected |

## SQL Server Lock Scenarios

| Scenario | Description | Success Rate | Notes |
|----------|-------------|--------------|-------|
| `sqlserver-lock-contention-sqlserver` | sp_getapplock under contention | >75% | Higher latency expected |
| `sqlserver-lock-deadlock-recovery-sqlserver` | Deadlock detection and recovery | >90% | SQL Server handles deadlocks |
| `sqlserver-lock-throughput-sqlserver` | Maximum throughput baseline | >95% | Without contention |

## Related Files (Locking)

- `tests/Encina.NBomber/Scenarios/Locking/` - Scenario implementations
- `ci/nbomber-locking-thresholds.json` - CI threshold configurations

---

# Message Broker Load Tests

The broker load tests exercise message broker publish/subscribe patterns.

## Provider Performance Comparison

| Provider | Target Throughput | Target Latency | Best For |
|----------|-------------------|----------------|----------|
| **RabbitMQ** | 500+ ops/sec | <50ms mean | Reliable message delivery, complex routing |
| **Kafka** | 1,000+ ops/sec | <30ms mean | High-throughput event streaming |
| **NATS** | 2,000+ ops/sec | <15ms mean | Low-latency messaging |
| **MQTT** | 500+ ops/sec | <40ms mean | IoT and constrained environments |

## RabbitMQ Scenarios

| Scenario | Description | Min Throughput | Notes |
|----------|-------------|----------------|-------|
| `rabbitmq-publish-throughput` | Single message publish rate | 500 ops/sec | BasicPublishAsync |
| `rabbitmq-consume-throughput` | Consumer acknowledgment rate | 400 ops/sec | Manual ack |
| `rabbitmq-batch-publish` | Batch publish with confirms | 300 ops/sec | Publisher confirms |
| `rabbitmq-publisher-confirms` | Delivery guarantee verification | 200 ops/sec | 100% delivery confirmation |

## Kafka Scenarios

| Scenario | Description | Min Throughput | Notes |
|----------|-------------|----------------|-------|
| `kafka-produce-throughput` | Single message production | 1,000 ops/sec | ProduceAsync |
| `kafka-batch-produce-throughput` | Batch vs individual comparison | 500 ops/sec | Fire-and-forget + Flush |
| `kafka-consume-throughput` | Consumer group processing | 800 ops/sec | Auto-commit |
| `kafka-partition-distribution` | Even distribution across partitions | 1,000 ops/sec | <20% deviation |

## NATS Scenarios

| Scenario | Description | Min Throughput | Notes |
|----------|-------------|----------------|-------|
| `nats-publish-throughput` | Core NATS fire-and-forget | 2,000 ops/sec | No persistence |
| `nats-request-reply` | Request-reply pattern | 500 ops/sec | Synchronous messaging |
| `nats-jetstream-publish` | JetStream persistent publishing | 1,000 ops/sec | With acknowledgment |

## MQTT Scenarios

| Scenario | Description | Min Throughput | Notes |
|----------|-------------|----------------|-------|
| `mqtt-publish-qos0` | QoS 0 fire-and-forget | 1,000 ops/sec | Some loss acceptable |
| `mqtt-publish-qos1` | QoS 1 at-least-once | 500 ops/sec | With PUBACK acknowledgment |
| `mqtt-subscribe-throughput` | Subscriber reception rate | 800 ops/sec | <5% message loss |

## QoS Level Comparison (MQTT)

| Level | Name | Delivery | Overhead |
|-------|------|----------|----------|
| QoS 0 | At Most Once | Fire-and-forget | Lowest |
| QoS 1 | At Least Once | With acknowledgment | Moderate |
| QoS 2 | Exactly Once | Four-step handshake | Highest |

## Related Files (Brokers)

- `tests/Encina.NBomber/Scenarios/Brokers/` - Scenario implementations
- `ci/nbomber-brokers-thresholds.json` - CI threshold configurations

---

# Running Load Tests

## Local Development

```bash
# Database tests
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --scenario db-uow \
    --provider efcore-sqlite \
    --duration 00:01:00

# Messaging tests
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --scenario messaging \
    --messaging-feature InMemoryBus \
    --duration 00:01:00

# Caching tests
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --scenario caching \
    --caching-provider redis \
    --duration 00:01:00

# Locking tests
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --scenario locking \
    --locking-provider redis \
    --duration 00:01:00

# Broker tests
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --scenario brokers \
    --broker-provider rabbitmq \
    --duration 00:01:00
```

## CI/CD Schedule

All load tests run on a weekly schedule (Saturdays at 2:00 AM UTC) and can be triggered manually via workflow dispatch.

| Job | Trigger | Providers |
|-----|---------|-----------|
| `run-database-load-tests` | Schedule or manual | Matrix of 4 EF Core providers |
| `run-messaging-load-tests` | Schedule or manual | InMemoryBus, Dispatcher |
| `run-caching-load-tests` | Schedule or manual | Matrix: memory, redis, hybrid |
| `run-locking-load-tests` | Schedule or manual | Matrix: inmemory, redis, sqlserver |
| `run-broker-load-tests` | Schedule or manual | Matrix: rabbitmq, kafka, nats, mqtt |

## Interpreting Results

### Success Criteria

1. **Throughput**: Above minimum threshold for provider/feature
2. **Latency**: Below maximum threshold (mean and P95)
3. **Error Rate**: Below maximum allowed (varies by scenario)
4. **Isolation**: Zero violations for locking and tenancy scenarios

### Common Issues

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| Low throughput | Container resource limits | Increase Docker resources |
| High P99 latency | GC pauses | Profile memory allocation |
| Connection timeouts | Pool exhaustion | Increase pool size |
| Lock timeouts | High contention | Reduce concurrent clients or increase resources |
| Message loss | QoS 0 or network issues | Use QoS 1+ or check network stability |

---

# Global Configuration Files

| File | Description |
|------|-------------|
| `ci/nbomber-database-thresholds.json` | Database load test thresholds |
| `ci/nbomber-messaging-thresholds.json` | Messaging load test thresholds |
| `ci/nbomber-caching-thresholds.json` | Caching load test thresholds |
| `ci/nbomber-locking-thresholds.json` | Locking load test thresholds |
| `ci/nbomber-brokers-thresholds.json` | Broker load test thresholds |
| `.github/workflows/load-tests.yml` | CI/CD workflow for all categories |
