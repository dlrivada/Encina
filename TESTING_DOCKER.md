# Testing with Docker - SimpleMediator

This document explains how to run integration tests for database providers using Docker containers.

## Why Docker for Testing?

- **No local installations required**: Don't need to install SQL Server, PostgreSQL, MySQL, or Oracle locally
- **Consistent environment**: Same setup works on developer machines and CI/CD
- **Isolation**: Tests run against fresh databases every time
- **Real integration tests**: Test against actual database engines, not mocks

## Prerequisites

- Docker Desktop installed and running
- .NET 10 SDK
- ~5 GB disk space for database images

## Quick Start

### 1. Start all databases and run tests

```bash
dotnet run --file scripts/run-integration-tests.cs
```

This will:
1. Start SQL Server, PostgreSQL, and MySQL containers
2. Wait for them to be healthy
3. Run all integration tests
4. Stop containers when done

### 2. Test specific database

```bash
# Test only SQL Server
dotnet run --file scripts/run-integration-tests.cs -- --database sqlserver

# Test only PostgreSQL
dotnet run --file scripts/run-integration-tests.cs -- --database postgres

# Test only MySQL
dotnet run --file scripts/run-integration-tests.cs -- --database mysql
```

### 3. Manual container management

If you want to keep containers running between test runs:

```bash
# Start all databases
docker-compose up -d

# Run tests without starting/stopping containers
dotnet run --file scripts/run-integration-tests.cs -- --skip-docker

# Stop all databases
docker-compose down

# Stop and remove volumes (clean slate)
docker-compose down -v
```

## Database Connection Strings

Connection strings are defined in `tests/appsettings.Testing.json`:

- **SQL Server**: `localhost:1433`, user: `sa`, password: `SimpleMediator123!`
- **PostgreSQL**: `localhost:5432`, user: `mediator`, password: `SimpleMediator123!`
- **MySQL**: `localhost:3306`, user: `mediator`, password: `SimpleMediator123!`
- **Oracle**: `localhost:1521`, user: `system`, password: `SimpleMediator123!`
- **SQLite**: In-memory (no container needed)

## Docker Compose Services

### SQL Server
- **Image**: `mcr.microsoft.com/mssql/server:2022-latest`
- **Port**: 1433
- **Startup time**: ~15 seconds

### PostgreSQL
- **Image**: `postgres:16-alpine`
- **Port**: 5432
- **Startup time**: ~5 seconds

### MySQL
- **Image**: `mysql:8.0`
- **Port**: 3306
- **Startup time**: ~10 seconds

### Oracle XE
- **Image**: `container-registry.oracle.com/database/express:21.3.0-xe`
- **Port**: 1521
- **Startup time**: ~60 seconds (slow, skipped by default)
- **Note**: Requires accepting Oracle license

## Testing Strategy

### Unit Tests (existing)
- Fast, no external dependencies
- Mock database interactions
- Run in CI on every commit

### Integration Tests (new)
- Use Docker containers
- Test actual database operations
- Test Outbox, Inbox, Sagas, Scheduling patterns
- Run locally before PR, in CI on main branch

### Test Categories

Tests are marked with categories:

```csharp
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public class OutboxStoreSqlServerIntegrationTests { }
```

Filter by category:
```bash
# Run only integration tests
dotnet test --filter "Category=Integration"

# Run only SQL Server integration tests
dotnet test --filter "Category=Integration&Database=SqlServer"

# Run unit tests only (exclude integration)
dotnet test --filter "Category!=Integration"
```

## CI/CD Integration

### GitHub Actions Workflow

```yaml
- name: Start databases
  run: docker-compose up -d sqlserver postgres mysql

- name: Wait for databases
  run: sleep 30

- name: Run integration tests
  run: dotnet test --filter "Category=Integration"

- name: Stop databases
  run: docker-compose down
```

### Local Development Workflow

1. Start containers once in the morning:
   ```bash
   docker-compose up -d
   ```

2. Develop and run tests repeatedly:
   ```bash
   dotnet test --filter "Category=Integration"
   ```

3. Stop containers at end of day:
   ```bash
   docker-compose down
   ```

## Troubleshooting

### Port already in use

If you have local databases running:

```bash
# Check what's using the port
netstat -ano | findstr :1433  # Windows
lsof -i :1433                  # Linux/Mac

# Either stop local service or change port in docker-compose.yml
```

### Container won't start

```bash
# Check logs
docker-compose logs sqlserver

# Remove volumes and try again
docker-compose down -v
docker-compose up -d
```

### Tests fail with connection timeout

```bash
# Verify container is healthy
docker ps

# Check health status
docker inspect simplemediator-sqlserver --format='{{.State.Health.Status}}'

# If unhealthy, check logs
docker-compose logs sqlserver
```

### Oracle container issues

Oracle XE is large (~2GB) and slow to start (~60s). For faster testing:

1. Skip Oracle tests: `--database sqlserver`
2. Pull image once: `docker pull container-registry.oracle.com/database/express:21.3.0-xe`
3. Keep container running: `docker-compose up -d oracle` (don't stop it)

## Coverage Impact

Before Docker integration tests:
- **Total coverage**: 67.1%
- **Core coverage**: 91.3%
- **Providers coverage**: 8.8% (Dapper), 43.3% (EF Core)

Target after integration tests:
- **Total coverage**: 90%+
- **Providers coverage**: 85%+

## Next Steps

1. ✅ Docker setup complete
2. ⏳ Create integration tests for all Dapper providers
3. ⏳ Create integration tests for all ADO providers
4. ⏳ Test Outbox pattern end-to-end
5. ⏳ Test Inbox pattern end-to-end
6. ⏳ Test Saga pattern end-to-end
7. ⏳ Test Scheduling pattern end-to-end
8. ⏳ Achieve 90%+ total coverage

## Resources

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [SQL Server Docker Image](https://hub.docker.com/_/microsoft-mssql-server)
- [PostgreSQL Docker Image](https://hub.docker.com/_/postgres)
- [MySQL Docker Image](https://hub.docker.com/_/mysql)
- [Oracle XE Docker Image](https://container-registry.oracle.com/)
