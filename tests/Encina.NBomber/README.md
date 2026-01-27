# Encina.NBomber

NBomber-based load testing harness for Encina database operations.

## Overview

This project provides comprehensive load testing scenarios for:

- **Unit of Work** - Transaction management under concurrent load
- **Multi-Tenancy** - Tenant isolation and context switching
- **Read/Write Separation** - Replica distribution and load balancing

## Supported Providers

| Category | Providers |
|----------|-----------|
| ADO.NET | `ado-sqlite`, `ado-sqlserver`, `ado-postgresql`, `ado-mysql` |
| Dapper | `dapper-sqlite`, `dapper-sqlserver`, `dapper-postgresql`, `dapper-mysql` |
| EF Core | `efcore-sqlite`, `efcore-sqlserver`, `efcore-postgresql`, `efcore-mysql` |
| MongoDB | `mongodb` |

## Quick Start

### Run All Features for a Provider

```bash
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --provider efcore-sqlite \
    --feature All \
    --duration 00:02:00
```

### Run Specific Feature

```bash
# Unit of Work scenarios
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --provider efcore-postgresql \
    --feature UnitOfWork \
    --duration 00:01:00

# Multi-Tenancy scenarios
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --provider dapper-sqlserver \
    --feature Tenancy \
    --duration 00:01:00

# Read/Write Separation scenarios
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --provider ado-mysql \
    --feature ReadWrite \
    --duration 00:01:00
```

### Run Multiple Providers

```bash
dotnet run --project tests/Encina.NBomber/Encina.NBomber.csproj -- \
    --provider "efcore-sqlite,efcore-postgresql" \
    --feature UnitOfWork \
    --duration 00:01:00
```

## CLI Options

| Option | Description | Default |
|--------|-------------|---------|
| `--scenario` | Scenario name (e.g., `db-uow`) | Required for database tests |
| `--provider` | Comma-separated provider names | `efcore-sqlite` |
| `--feature` | Feature to test: `UnitOfWork`, `Tenancy`, `ReadWrite`, `All` | `All` |
| `--duration` | Test duration in `HH:MM:SS` format | `00:01:00` |

## Scenarios

### Unit of Work Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `uow-concurrent-transactions` | 100/sec | Tests multiple transactions executing simultaneously |
| `uow-rollback-under-load` | 50/sec | Tests rollback behavior under high concurrency |
| `uow-connection-pool-pressure` | 200/sec | Tests connection pool exhaustion behavior |

### Multi-Tenancy Scenarios

| Scenario | Configuration | Description |
|----------|---------------|-------------|
| `tenancy-isolation` | 50 concurrent users | Tests tenant data isolation (100 tenants) |
| `tenancy-context-switching` | 100/sec | Tests rapid tenant context switches |

### Read/Write Separation Scenarios

| Scenario | Rate | Description |
|----------|------|-------------|
| `readwrite-replica-distribution` | 150/sec | Tests distribution across simulated replicas |
| `readwrite-roundrobin-validation` | 100/sec | Validates round-robin load balancing |
| `readwrite-leastconnections-validation` | 75/sec | Validates least-connections algorithm |

## Prerequisites

### For SQLite

No external dependencies - uses in-memory database.

### For SQL Server, PostgreSQL, MySQL

Docker must be running. The load tests use Testcontainers to automatically start database containers.

Alternatively, set environment variables to use existing databases:

```bash
export SQLSERVER_CONNECTION_STRING="Server=localhost,1433;..."
export POSTGRES_CONNECTION_STRING="Host=localhost;Port=5432;..."
export MYSQL_CONNECTION_STRING="Server=localhost;Port=3306;..."
```

### For MongoDB

Docker must be running. MongoDB requires a replica set for transaction support. Testcontainers handles this automatically.

```bash
export MONGODB_CONNECTION_STRING="mongodb://localhost:27017/?replicaSet=rs0"
```

## Output

Results are written to `artifacts/nbomber/` with:

- JSON reports with detailed metrics
- HTML reports for visualization
- Console summary

## Performance Thresholds

See `ci/nbomber-database-thresholds.json` for CI/CD thresholds.

Expected performance by provider:

| Provider | Expected Ops/Sec | Mean Latency |
|----------|------------------|--------------|
| `*-sqlite` | 2,000-3,000+ | <20ms |
| `*-sqlserver` | 800-1,000+ | <50ms |
| `*-postgresql` | 1,000-1,200+ | <45ms |
| `*-mysql` | 900-1,100+ | <48ms |
| `mongodb` | 1,200+ | <35ms |

## CI/CD Integration

Database load tests run automatically:

- **Schedule**: Every Saturday at 2:00 AM UTC
- **Manual**: Via GitHub Actions workflow dispatch

See `.github/workflows/load-tests.yml` for configuration.

## Project Structure

```
tests/Encina.NBomber/
├── Program.cs                          # CLI entry point
├── Scenarios/
│   └── Database/
│       ├── IDatabaseProviderFactory.cs # Provider interface
│       ├── DatabaseProviderFactoryBase.cs
│       ├── DatabaseProviderRegistry.cs # Provider name mapping
│       ├── DatabaseScenarioContext.cs  # Shared state
│       ├── DatabaseScenarioBase.cs     # Base class helpers
│       ├── DatabaseWarmup.cs           # Connection warmup
│       ├── UnitOfWorkScenarioFactory.cs
│       ├── TenancyScenarioFactory.cs
│       ├── ReadWriteSeparationScenarioFactory.cs
│       └── Providers/
│           ├── AdoProviderFactories.cs   # ADO.NET providers
│           ├── DapperProviderFactories.cs # Dapper providers
│           ├── EFCoreProviderFactories.cs # EF Core providers
│           └── MongoDbProviderFactory.cs  # MongoDB provider
└── README.md                           # This file
```

## Related Documentation

- [Load Test Baselines](../../docs/testing/load-test-baselines.md) - Performance expectations
- [CLAUDE.md](../../CLAUDE.md) - Testing standards and guidelines
