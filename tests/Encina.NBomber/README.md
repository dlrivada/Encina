# Encina.NBomber

NBomber-based load testing harness for Encina features.

## Overview

This project provides comprehensive load testing scenarios for five categories:

| Category | Description | Providers |
|----------|-------------|-----------|
| **Database** | Transaction management, tenant isolation, replica distribution | 13 DB providers |
| **Messaging** | In-memory pub/sub, dispatcher patterns | inmemory |
| **Caching** | Memory cache, Redis, hybrid L1/L2 | memory, redis, hybrid |
| **Locking** | Distributed lock coordination | inmemory, redis, sqlserver |
| **Brokers** | Message broker pub/sub | rabbitmq, kafka, nats, mqtt |

## Quick Start

### Database Load Tests

```bash
# Run all database features for a provider
dotnet run --project tests/Encina.NBomber -- \
    --scenario db-uow \
    --provider efcore-sqlite \
    --feature UnitOfWork \
    --duration 00:01:00
```

### Messaging Load Tests

```bash
# Test InMemoryBus scenarios
dotnet run --project tests/Encina.NBomber -- \
    --scenario messaging \
    --messaging-feature InMemoryBus \
    --duration 00:01:00

# Test Dispatcher scenarios
dotnet run --project tests/Encina.NBomber -- \
    --scenario messaging \
    --messaging-feature Dispatcher \
    --duration 00:01:00
```

### Caching Load Tests

```bash
# Memory cache tests
dotnet run --project tests/Encina.NBomber -- \
    --scenario caching \
    --caching-provider memory \
    --duration 00:01:00

# Redis cache tests
dotnet run --project tests/Encina.NBomber -- \
    --scenario caching \
    --caching-provider redis \
    --duration 00:01:00
```

### Distributed Locking Load Tests

```bash
# In-memory lock tests
dotnet run --project tests/Encina.NBomber -- \
    --scenario locking \
    --locking-provider inmemory \
    --duration 00:01:00

# Redis lock tests
dotnet run --project tests/Encina.NBomber -- \
    --scenario locking \
    --locking-provider redis \
    --duration 00:01:00
```

### Message Broker Load Tests

```bash
# RabbitMQ tests
dotnet run --project tests/Encina.NBomber -- \
    --scenario brokers \
    --broker-provider rabbitmq \
    --duration 00:01:00

# Kafka tests
dotnet run --project tests/Encina.NBomber -- \
    --scenario brokers \
    --broker-provider kafka \
    --duration 00:01:00
```

## CLI Options

### Global Options

| Option | Description | Default |
|--------|-------------|---------|
| `--scenario` | Scenario category | Required |
| `--duration` | Test duration in `HH:MM:SS` format | `00:01:00` |

### Database Options

| Option | Description | Default |
|--------|-------------|---------|
| `--provider` | Database provider name | `efcore-sqlite` |
| `--feature` | Feature: `UnitOfWork`, `Tenancy`, `ReadWrite`, `All` | `All` |

### Messaging Options

| Option | Description | Default |
|--------|-------------|---------|
| `--messaging-feature` | Feature: `InMemoryBus`, `Dispatcher`, `All` | `All` |

### Caching Options

| Option | Description | Default |
|--------|-------------|---------|
| `--caching-provider` | Provider: `memory`, `redis`, `hybrid`, `all` | `memory` |

### Locking Options

| Option | Description | Default |
|--------|-------------|---------|
| `--locking-provider` | Provider: `inmemory`, `redis`, `sqlserver`, `all` | `inmemory` |

### Broker Options

| Option | Description | Default |
|--------|-------------|---------|
| `--broker-provider` | Provider: `rabbitmq`, `kafka`, `nats`, `mqtt`, `all` | `rabbitmq` |
| `--broker-feature` | Feature: `RabbitMQ`, `Kafka`, `NATS`, `MQTT`, `All` | `All` |

## Scenarios by Category

### Database Scenarios

#### Supported Providers

| Category | Providers |
|----------|-----------|
| ADO.NET | `ado-sqlite`, `ado-sqlserver`, `ado-postgresql`, `ado-mysql` |
| Dapper | `dapper-sqlite`, `dapper-sqlserver`, `dapper-postgresql`, `dapper-mysql` |
| EF Core | `efcore-sqlite`, `efcore-sqlserver`, `efcore-postgresql`, `efcore-mysql` |
| MongoDB | `mongodb` |

#### Unit of Work Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `uow-concurrent-transactions` | 100/sec | Tests multiple transactions executing simultaneously |
| `uow-rollback-under-load` | 50/sec | Tests rollback behavior under high concurrency |
| `uow-connection-pool-pressure` | 200/sec | Tests connection pool exhaustion behavior |

#### Multi-Tenancy Scenarios

| Scenario | Configuration | Description |
|----------|---------------|-------------|
| `tenancy-isolation` | 50 concurrent users | Tests tenant data isolation (100 tenants) |
| `tenancy-context-switching` | 100/sec | Tests rapid tenant context switches |

#### Read/Write Separation Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `readwrite-replica-distribution` | 150/sec | Tests distribution across simulated replicas |
| `readwrite-roundrobin-validation` | 100/sec | Validates round-robin load balancing |
| `readwrite-leastconnections-validation` | 75/sec | Validates least-connections algorithm |

### Messaging Scenarios

#### InMemoryBus Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `inmemory-concurrent-publish` | 200/sec | High-rate message publishing throughput |
| `inmemory-handler-registration` | 100/sec | Concurrent subscribe/unsubscribe operations |
| `inmemory-handler-execution` | 150/sec | Multiple handlers per message type |

#### Dispatcher Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `dispatcher-parallel-throughput` | 100/sec | Parallel notification dispatch |
| `dispatcher-sequential-throughput` | 100/sec | Sequential notification dispatch |
| `dispatcher-pipeline-overhead` | 150/sec | Pipeline behavior chain overhead |

### Caching Scenarios

#### Memory Cache Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `memory-get-set-throughput` | 500/sec | High-rate read/write (100B-10KB values) |
| `memory-concurrent-access` | 200/sec | Thread-safe operations on same keys |
| `memory-eviction-pressure` | 100/sec | Behavior under memory pressure |

#### Redis Cache Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `redis-get-set-throughput-redis` | 200/sec | Read/write with connection pooling |
| `redis-concurrent-access-redis` | 100/sec | GetOrSet with stampede protection |
| `redis-expiration-accuracy-redis` | 50/sec | TTL precision under load |
| `redis-pipeline-batching-redis` | 50/sec | Pipeline vs individual comparison |

#### Hybrid Cache Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `hybrid-l1-throughput` | 500/sec | L1 memory cache hit rate |
| `hybrid-l2-fallback` | 200/sec | L2 Redis fallback on L1 miss |
| `hybrid-invalidation` | 100/sec | Cross-tier cache invalidation |

### Locking Scenarios

#### InMemory Lock Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `inmemory-lock-contention` | 200/sec | 50+ clients competing for 5-10 resources |
| `inmemory-lock-throughput` | 500/sec | Maximum throughput without contention |

#### Redis Lock Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `redis-lock-contention-redis` | 100/sec | Concurrent clients competing for locks |
| `redis-lock-release-timing-redis` | 50/sec | Lock release latency measurement |
| `redis-lock-renewal-redis` | 30/sec | Lock extension under contention |
| `redis-lock-timeout-accuracy-redis` | 50/sec | TTL expiration precision |

#### SQL Server Lock Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `sqlserver-lock-contention-sqlserver` | 50/sec | sp_getapplock under contention |
| `sqlserver-lock-deadlock-recovery-sqlserver` | 30/sec | Deadlock detection and recovery |
| `sqlserver-lock-throughput-sqlserver` | 100/sec | Maximum throughput baseline |

### Broker Scenarios

#### RabbitMQ Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `rabbitmq-publish-throughput` | 200/sec | Single message publish rate |
| `rabbitmq-consume-throughput` | 100/sec | Consumer acknowledgment rate |
| `rabbitmq-batch-publish` | 50/sec | Batch publish with confirms |
| `rabbitmq-publisher-confirms` | 50/sec | Delivery guarantee verification |

#### Kafka Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `kafka-produce-throughput` | 200/sec | Single message production |
| `kafka-batch-produce-throughput` | 50/sec | Batch vs individual comparison |
| `kafka-consume-throughput` | 100/sec | Consumer group processing |
| `kafka-partition-distribution` | 100/sec | Even distribution across partitions |

#### NATS Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `nats-publish-throughput` | 500/sec | Core NATS fire-and-forget |
| `nats-request-reply` | 100/sec | Request-reply pattern |
| `nats-jetstream-publish` | 200/sec | JetStream persistent publishing |

#### MQTT Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `mqtt-publish-qos0` | 500/sec | QoS 0 fire-and-forget |
| `mqtt-publish-qos1` | 200/sec | QoS 1 at-least-once delivery |
| `mqtt-subscribe-throughput` | 100/sec | Subscriber reception rate |

## Prerequisites

### For SQLite / In-Memory Tests

No external dependencies required.

### For SQL Server, PostgreSQL, MySQL, Redis

Docker must be running. The load tests use Testcontainers to automatically start containers.

Alternatively, set environment variables to use existing services:

```bash
# Database connection strings
export SQLSERVER_CONNECTION_STRING="Server=localhost,1433;..."
export POSTGRES_CONNECTION_STRING="Host=localhost;Port=5432;..."
export MYSQL_CONNECTION_STRING="Server=localhost;Port=3306;..."
export MONGODB_CONNECTION_STRING="mongodb://localhost:27017/?replicaSet=rs0"

# Redis
export REDIS_CONNECTION_STRING="localhost:6379"

# Message Brokers
export RABBITMQ_CONNECTION_STRING="amqp://guest:guest@localhost:5672"
export NATS_CONNECTION_STRING="nats://localhost:4222"
export MQTT_HOST="localhost"
export MQTT_PORT="1883"
```

### For Kafka

Kafka requires Testcontainers due to its complexity (ZooKeeper coordination).

## Output

Results are written to `artifacts/nbomber/` with:

- JSON reports with detailed metrics
- HTML reports for visualization
- Console summary

## Performance Thresholds

Threshold configuration files in `ci/`:

| File | Category |
|------|----------|
| `nbomber-database-thresholds.json` | Database load tests |
| `nbomber-messaging-thresholds.json` | Messaging load tests |
| `nbomber-caching-thresholds.json` | Caching load tests |
| `nbomber-locking-thresholds.json` | Locking load tests |
| `nbomber-brokers-thresholds.json` | Broker load tests |

### Expected Performance by Category

#### Database Providers

| Provider | Expected Ops/Sec | Mean Latency |
|----------|------------------|--------------|
| `*-sqlite` | 2,000-3,000+ | <20ms |
| `*-sqlserver` | 800-1,000+ | <50ms |
| `*-postgresql` | 1,000-1,200+ | <45ms |
| `*-mysql` | 900-1,100+ | <48ms |
| `mongodb` | 1,200+ | <35ms |

#### Messaging

| Feature | Expected Ops/Sec | Mean Latency |
|---------|------------------|--------------|
| InMemoryBus | 1,000+ | <10ms |
| Dispatcher | 500+ | <25ms |

#### Caching

| Provider | Expected Ops/Sec | Mean Latency |
|----------|------------------|--------------|
| Memory | 10,000+ | <2ms |
| Redis | 5,000+ | <10ms |
| Hybrid | 8,000+ | <5ms |

#### Locking

| Provider | Expected Ops/Sec | Mean Latency |
|----------|------------------|--------------|
| InMemory | 2,000+ | <10ms |
| Redis | 500+ | <30ms |
| SQL Server | 200+ | <60ms |

#### Brokers

| Provider | Expected Ops/Sec | Mean Latency |
|----------|------------------|--------------|
| RabbitMQ | 500+ | <50ms |
| Kafka | 1,000+ | <30ms |
| NATS | 2,000+ | <15ms |
| MQTT | 500+ | <40ms |

## CI/CD Integration

Load tests run automatically via `.github/workflows/load-tests.yml`:

| Job | Schedule | Matrix |
|-----|----------|--------|
| `run-database-load-tests` | Weekly (Sat 2:00 AM UTC) | efcore-sqlite, efcore-sqlserver, efcore-postgresql, efcore-mysql |
| `run-messaging-load-tests` | Weekly | InMemoryBus, Dispatcher |
| `run-caching-load-tests` | Weekly | memory, redis, hybrid |
| `run-locking-load-tests` | Weekly | inmemory, redis, sqlserver |
| `run-broker-load-tests` | Weekly | rabbitmq, kafka, nats, mqtt |

Manual triggers available via GitHub Actions workflow dispatch.

## Project Structure

```
tests/Encina.NBomber/
├── Program.cs                              # CLI entry point
├── Scenarios/
│   ├── Database/
│   │   ├── IDatabaseProviderFactory.cs     # Provider interface
│   │   ├── DatabaseProviderFactoryBase.cs
│   │   ├── DatabaseProviderRegistry.cs     # Provider name mapping
│   │   ├── DatabaseScenarioContext.cs      # Shared state
│   │   ├── UnitOfWorkScenarioFactory.cs
│   │   ├── TenancyScenarioFactory.cs
│   │   ├── ReadWriteSeparationScenarioFactory.cs
│   │   └── Providers/
│   │       ├── AdoProviderFactories.cs
│   │       ├── DapperProviderFactories.cs
│   │       ├── EFCoreProviderFactories.cs
│   │       └── MongoDbProviderFactory.cs
│   ├── Messaging/
│   │   ├── MessagingScenarioContext.cs
│   │   ├── MessagingScenarioRunner.cs
│   │   ├── InMemoryBusScenarioFactory.cs
│   │   ├── DispatcherScenarioFactory.cs
│   │   └── Providers/
│   │       └── MessagingProviderFactory.cs
│   ├── Caching/
│   │   ├── CacheScenarioContext.cs
│   │   ├── CachingScenarioRunner.cs
│   │   ├── MemoryCacheScenarioFactory.cs
│   │   ├── RedisCacheScenarioFactory.cs
│   │   ├── HybridCacheScenarioFactory.cs
│   │   └── Providers/
│   │       ├── MemoryCacheProviderFactory.cs
│   │       ├── RedisCacheProviderFactory.cs
│   │       └── HybridCacheProviderFactory.cs
│   ├── Locking/
│   │   ├── LockScenarioContext.cs
│   │   ├── LockingScenarioRunner.cs
│   │   ├── InMemoryLockScenarioFactory.cs
│   │   ├── RedisLockScenarioFactory.cs
│   │   ├── SqlServerLockScenarioFactory.cs
│   │   └── Providers/
│   │       ├── InMemoryLockProviderFactory.cs
│   │       ├── RedisLockProviderFactory.cs
│   │       └── SqlServerLockProviderFactory.cs
│   └── Brokers/
│       ├── BrokerScenarioContext.cs
│       ├── BrokerScenarioRunner.cs
│       ├── RabbitMQScenarioFactory.cs
│       ├── KafkaScenarioFactory.cs
│       ├── NATSScenarioFactory.cs
│       ├── MQTTScenarioFactory.cs
│       └── Providers/
│           ├── RabbitMQProviderFactory.cs
│           ├── KafkaProviderFactory.cs
│           ├── NATSProviderFactory.cs
│           └── MQTTProviderFactory.cs
└── README.md                               # This file
```

## Related Documentation

- [Load Test Baselines](../../docs/testing/load-test-baselines.md) - Performance expectations for all categories
- [CLAUDE.md](../../CLAUDE.md) - Testing standards and guidelines
