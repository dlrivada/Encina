# Database Load Test Performance Baselines

This document describes the expected performance characteristics for database load tests across all providers.

## Overview

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

## Related Files

- `tests/Encina.NBomber/Scenarios/Database/` - Scenario implementations
- `tests/Encina.LoadTests/profiles/nbomber.database-*.json` - Profile configurations
- `ci/nbomber-database-thresholds.json` - CI threshold configurations
- `.github/workflows/load-tests.yml` - CI/CD workflow
