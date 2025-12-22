# Integration Testing with Docker

Encina uses Docker containers for integration testing against real database engines. This ensures high-quality, production-like testing without requiring local database installations.

## Prerequisites

- Docker Desktop installed and running
- .NET 10 SDK
- Approximately 5 GB disk space for database images

## Quick Start

### Run All Integration Tests

```bash
dotnet run --file scripts/run-integration-tests.cs
```

This command will:

1. Start SQL Server, PostgreSQL, and MySQL containers
2. Wait for databases to become healthy
3. Execute all integration tests
4. Stop containers upon completion

### Test Specific Database Provider

```bash
# SQL Server only
dotnet run --file scripts/run-integration-tests.cs -- --database sqlserver

# PostgreSQL only
dotnet run --file scripts/run-integration-tests.cs -- --database postgres

# MySQL only
dotnet run --file scripts/run-integration-tests.cs -- --database mysql
```

### Manual Container Management

For faster iteration during development:

```bash
# Start all databases
docker-compose up -d

# Run tests against running containers
dotnet run --file scripts/run-integration-tests.cs -- --skip-docker

# Stop databases
docker-compose down

# Clean slate (remove volumes)
docker-compose down -v
```

## Database Configuration

### Connection Strings

Connection strings are defined in `tests/appsettings.Testing.json`:

| Database   | Host          | Port | User      | Password            |
|------------|---------------|------|-----------|---------------------|
| SQL Server | localhost     | 1433 | sa        | Encina123!  |
| PostgreSQL | localhost     | 5432 | mediator  | Encina123!  |
| MySQL      | localhost     | 3306 | mediator  | Encina123!  |
| Oracle XE  | localhost     | 1521 | system    | Encina123!  |
| SQLite     | In-memory     | N/A  | N/A       | N/A                 |

### Docker Images

| Database   | Image                                                 | Startup Time |
|------------|-------------------------------------------------------|--------------|
| SQL Server | `mcr.microsoft.com/mssql/server:2022-latest`          | ~15 seconds  |
| PostgreSQL | `postgres:16-alpine`                                  | ~5 seconds   |
| MySQL      | `mysql:8.0`                                           | ~10 seconds  |
| Oracle XE  | `container-registry.oracle.com/database/express:21.3.0-xe` | ~60 seconds  |

> **Note**: Oracle XE requires accepting the Oracle license agreement and has significantly longer startup time.

## Test Organization

### Test Categories

Integration tests use xUnit traits for categorization:

```csharp
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class OutboxStoreSqlServerIntegrationTests
{
    // Tests run against real SQL Server in Docker
}
```

### Filtering Tests

```bash
# Run only integration tests
dotnet test --filter "Category=Integration"

# Run specific database provider tests
dotnet test --filter "Category=Integration&Database=SqlServer"

# Exclude integration tests (unit tests only)
dotnet test --filter "Category!=Integration"
```

### Test Coverage

Integration tests cover:

- **Outbox Pattern**: Message persistence, retrieval, and processing
- **Inbox Pattern**: Idempotency guarantees and deduplication
- **Saga Pattern**: State persistence and compensation logic
- **Scheduling Pattern**: Delayed and recurring message execution
- **Transaction Pattern**: Commit/rollback behavior with Railway Oriented Programming

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Start database containers
  run: docker-compose up -d sqlserver postgres mysql

- name: Wait for databases to be healthy
  run: sleep 30

- name: Run integration tests
  run: dotnet test --filter "Category=Integration"

- name: Stop database containers
  run: docker-compose down
```

### Local Development Workflow

**Morning Setup:**

```bash
docker-compose up -d
```

**Development Loop:**

```bash
# Make changes
dotnet test --filter "Category=Integration"
```

**End of Day:**

```bash
docker-compose down
```

## Troubleshooting

### Port Conflicts

If local databases are already running on standard ports:

```bash
# Windows: Check port usage
netstat -ano | findstr :1433

# Linux/macOS: Check port usage
lsof -i :1433

# Solution: Stop local service or modify docker-compose.yml ports
```

### Container Startup Failures

```bash
# View container logs
docker-compose logs sqlserver

# Clean start
docker-compose down -v
docker-compose up -d
```

### Connection Timeouts

```bash
# Verify container health
docker ps

# Check specific container health status
docker inspect Encina-sqlserver --format='{{.State.Health.Status}}'

# If unhealthy, examine logs
docker-compose logs sqlserver
```

### Oracle-Specific Issues

Oracle XE has a large image size (~2 GB) and slow startup (~60 seconds):

**Solutions:**

- Skip Oracle tests during development: `--database sqlserver`
- Pull image once: `docker pull container-registry.oracle.com/database/express:21.3.0-xe`
- Keep Oracle container running between test runs

## Architecture

### Test Infrastructure

The `Encina.TestInfrastructure` project provides:

- **Database Fixtures**: Per-provider fixtures implementing `IAsyncLifetime`
- **Schema Builders**: Database-specific DDL for Outbox, Inbox, Sagas, Scheduling
- **Test Data Builders**: Fluent builders for test entities
- **Testcontainers Integration**: Programmatic container lifecycle management

### Provider Matrix

| Provider             | Integration | Contract | Property | Load |
|----------------------|-------------|----------|----------|------|
| Dapper.SqlServer     | ✅          | ✅       | ✅       | ✅   |
| Dapper.PostgreSQL    | ✅          | ✅       | ✅       | ✅   |
| Dapper.MySQL         | ✅          | ✅       | ✅       | ✅   |
| Dapper.Oracle        | ✅          | ✅       | ✅       | ✅   |
| Dapper.Sqlite        | ✅          | ✅       | ✅       | ✅   |
| ADO.SqlServer        | ✅          | ✅       | ✅       | ✅   |
| ADO.PostgreSQL       | ✅          | ✅       | ✅       | ✅   |
| ADO.MySQL            | ❌          | ✅       | ✅       | ✅   |
| ADO.Oracle           | ❌          | ✅       | ✅       | ✅   |
| ADO.Sqlite           | ❌          | ✅       | ✅       | ✅   |

> **Note**: ADO MySQL/Oracle/Sqlite use Testcontainers-based integration tests in Contract/Property/Load projects instead of separate Integration projects.

## Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [xUnit Trait Attributes](https://xunit.net/docs/getting-started/netcore/cmdline#traits)
- [SQL Server Docker Hub](https://hub.docker.com/_/microsoft-mssql-server)
- [PostgreSQL Docker Hub](https://hub.docker.com/_/postgres)
- [MySQL Docker Hub](https://hub.docker.com/_/mysql)
- [Oracle Container Registry](https://container-registry.oracle.com/)
