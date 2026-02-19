# Docker Infrastructure Guide

Complete guide to Encina's Docker-based development infrastructure for databases, messaging, caching, and observability.

## Quick Start

```bash
# Start essential services (PostgreSQL, Redis, RabbitMQ)
docker compose --profile core up -d

# Start all databases
docker compose --profile databases up -d

# Start everything
docker compose --profile full up -d
```

## Available Profiles

| Profile | Services | Use Case |
|---------|----------|----------|
| `core` | PostgreSQL, Redis, RabbitMQ | Minimal development setup |
| `databases` | SQL Server, PostgreSQL, MySQL, Oracle, MongoDB | Database provider testing |
| `messaging` | RabbitMQ, Kafka, NATS, Mosquitto | Message transport testing |
| `caching` | Redis, Garnet, Valkey, Dragonfly, KeyDB | Cache provider testing |
| `cloud` | LocalStack, Azurite | AWS/Azure emulation |
| `observability` | Seq | Structured logging |
| `full` | All services | Complete infrastructure |

### Profile Combinations

```bash
# Databases + Messaging
docker compose --profile databases --profile messaging up -d

# Core + Observability
docker compose --profile core --profile observability up -d

# Cloud emulators only
docker compose --profile cloud up -d
```

## Services Reference

### Databases

| Service | Image | Port | Encina Provider | Profile |
|---------|-------|------|-----------------|---------|
| **SQL Server** | `mcr.microsoft.com/mssql/server:2022-latest` | 1433 | `Encina.ADO.SqlServer`, `Encina.Dapper.SqlServer` | `databases`, `full` |
| **PostgreSQL** | `postgres:16-alpine` | 5432 | `Encina.ADO.PostgreSQL`, `Encina.Dapper.PostgreSQL`, `Encina.Marten` | `databases`, `core`, `full` |
| **MySQL** | `mysql:8.0` | 3306 | `Encina.ADO.MySQL`, `Encina.Dapper.MySQL` | `databases`, `full` |
| **Oracle XE** | `container-registry.oracle.com/database/express:21.3.0-xe` | 1521 | `Encina.ADO.Oracle`, `Encina.Dapper.Oracle` | `databases`, `full` |
| **MongoDB** | `mongo:7` | 27017 | `Encina.MongoDB` | `databases`, `full` |

> **Note**: SQLite (`Encina.ADO.Sqlite`, `Encina.Dapper.Sqlite`) is file-based and doesn't require a container.

> **Note**: Marten (event sourcing) uses PostgreSQL as its backend.

### Messaging

| Service | Image | Port(s) | Encina Provider | Profile |
|---------|-------|---------|-----------------|---------|
| **RabbitMQ** | `rabbitmq:3-management-alpine` | 5672, 15672 (UI) | `Encina.RabbitMQ` | `messaging`, `core`, `full` |
| **Kafka** | `apache/kafka:3.7.0` | 29092 | `Encina.Kafka` | `messaging`, `full` |
| **NATS** | `nats:2-alpine` | 4222, 8222 | `Encina.NATS` | `messaging`, `full` |
| **Mosquitto** | `eclipse-mosquitto:2` | 1883, 9001 (WS) | `Encina.MQTT` | `messaging`, `full` |

### Caching

| Service | Image | Port | Encina Provider | Profile |
|---------|-------|------|-----------------|---------|
| **Redis** | `redis:7-alpine` | 6379 | `Encina.Caching.Redis`, `Encina.DistributedLock.Redis` | `caching`, `core`, `full` |
| **Garnet** | `ghcr.io/microsoft/garnet:latest` | 6380 | `Encina.Caching.Garnet` | `caching`, `full` |
| **Valkey** | `valkey/valkey:8-alpine` | 6381 | `Encina.Caching.Valkey` | `caching`, `full` |
| **Dragonfly** | `docker.dragonflydb.io/dragonflydb/dragonfly:latest` | 6382 | `Encina.Caching.Dragonfly` | `caching`, `full` |
| **KeyDB** | `eqalpha/keydb:latest` | 6383 | `Encina.Caching.KeyDB` | `caching`, `full` |

> All Redis alternatives are protocol-compatible. Use standard Redis clients with different ports.

### Cloud Emulators

| Service | Image | Port(s) | Emulates | Profile |
|---------|-------|---------|----------|---------|
| **HashiCorp Vault** | `hashicorp/vault:latest` | 8200 | HashiCorp Vault (dev mode) | `cloud`, `full` |
| **LocalStack** | `localstack/localstack:latest` | 4566 | AWS SQS, SNS, S3, DynamoDB, Secrets Manager | `cloud`, `full` |
| **Azurite** | `mcr.microsoft.com/azure-storage/azurite:latest` | 10000-10002 | Azure Blob, Queue, Table | `cloud`, `full` |

> **Note**: Azure Service Bus has no local emulator. Use Azure Service Bus for `Encina.AzureServiceBus` testing.
> **Note**: Azure Key Vault has no local emulator. Use Azure Key Vault for `Encina.Secrets.AzureKeyVault` testing.
> **Note**: Google Secret Manager has no local emulator. Use GCP for `Encina.Secrets.GoogleSecretManager` testing.

### Observability

| Service | Image | Port(s) | Purpose | Profile |
|---------|-------|---------|---------|---------|
| **Seq** | `datalust/seq:latest` | 5341, 8081 (UI) | Structured logging | `observability`, `full` |

For full observability stack (Prometheus, Jaeger, Loki, Grafana), see:

```bash
docker compose -f docker-compose.observability.yml up -d
```

## Connection Strings

### Default Credentials

All services use development-only default credentials. Override with environment variables or `.env` file.

| Service | Username | Password | Environment Variable |
|---------|----------|----------|---------------------|
| SQL Server | `sa` | `YourStrong@Passw0rd` | `SQL_PASSWORD` |
| PostgreSQL | `encina` | `YourStrong@Passw0rd` | `POSTGRES_PASSWORD` |
| MySQL | `encina` | `YourStrong@Passw0rd` | `MYSQL_PASSWORD` |
| Oracle | `system` | `YourStrong@Passw0rd` | `ORACLE_PASSWORD` |
| MongoDB | `encina` | `YourStrong@Passw0rd` | `MONGO_PASSWORD` |
| RabbitMQ | `guest` | `guest` | `RABBITMQ_PASSWORD` |
| Vault (dev) | - | `encina-dev-token` | `VAULT_TOKEN` |

### Connection String Examples

```csharp
// SQL Server
"Server=localhost,1433;Database=encina;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"

// PostgreSQL
"Host=localhost;Port=5432;Database=encina;Username=encina;Password=YourStrong@Passw0rd"

// MySQL
"Server=localhost;Port=3306;Database=encina;User=encina;Password=YourStrong@Passw0rd"

// Oracle
"Data Source=localhost:1521/XE;User Id=system;Password=YourStrong@Passw0rd"

// MongoDB
"mongodb://encina:YourStrong@Passw0rd@localhost:27017"

// Redis (and compatible caches)
"localhost:6379"      // Redis
"localhost:6380"      // Garnet
"localhost:6381"      // Valkey
"localhost:6382"      // Dragonfly
"localhost:6383"      // KeyDB

// RabbitMQ
"amqp://guest:guest@localhost:5672"

// Kafka
"localhost:29092"

// NATS
"nats://localhost:4222"

// MQTT
"mqtt://localhost:1883"

// HashiCorp Vault (dev mode)
"http://localhost:8200" // Token: encina-dev-token (VAULT_TOKEN env var)

// LocalStack (AWS - SQS, SNS, S3, DynamoDB, Secrets Manager)
"http://localhost:4566"

// Azurite (Azure Storage)
"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1"
```

## Provider Coverage Matrix

| Encina Provider | Container | Port | Status |
|-----------------|-----------|------|--------|
| `Encina.ADO.SqlServer` | sqlserver | 1433 | ✅ |
| `Encina.ADO.PostgreSQL` | postgres | 5432 | ✅ |
| `Encina.ADO.MySQL` | mysql | 3306 | ✅ |
| `Encina.ADO.Oracle` | oracle | 1521 | ✅ |
| `Encina.ADO.Sqlite` | (file-based) | N/A | ✅ |
| `Encina.Dapper.SqlServer` | sqlserver | 1433 | ✅ |
| `Encina.Dapper.PostgreSQL` | postgres | 5432 | ✅ |
| `Encina.Dapper.MySQL` | mysql | 3306 | ✅ |
| `Encina.Dapper.Oracle` | oracle | 1521 | ✅ |
| `Encina.Dapper.Sqlite` | (file-based) | N/A | ✅ |
| `Encina.EntityFrameworkCore` | (any SQL) | varies | ✅ |
| `Encina.MongoDB` | mongodb | 27017 | ✅ |
| `Encina.Marten` | postgres | 5432 | ✅ |
| `Encina.RabbitMQ` | rabbitmq | 5672 | ✅ |
| `Encina.Kafka` | kafka | 29092 | ✅ |
| `Encina.NATS` | nats | 4222 | ✅ |
| `Encina.MQTT` | mosquitto | 1883 | ✅ |
| `Encina.AmazonSQS` | localstack | 4566 | ✅ |
| `Encina.AzureServiceBus` | (no emulator) | - | ⚠️ Requires Azure |
| `Encina.Caching.Redis` | redis | 6379 | ✅ |
| `Encina.Caching.Garnet` | garnet | 6380 | ✅ |
| `Encina.Caching.Valkey` | valkey | 6381 | ✅ |
| `Encina.Caching.Dragonfly` | dragonfly | 6382 | ✅ |
| `Encina.Caching.KeyDB` | keydb | 6383 | ✅ |
| `Encina.Secrets.HashiCorpVault` | vault | 8200 | ✅ |
| `Encina.Secrets.AWSSecretsManager` | localstack | 4566 | ✅ |
| `Encina.Secrets.AzureKeyVault` | (no emulator) | - | ⚠️ Requires Azure |
| `Encina.Secrets.GoogleSecretManager` | (no emulator) | - | ⚠️ Requires GCP |
| `Encina.DistributedLock.Redis` | redis | 6379 | ✅ |
| `Encina.DistributedLock.SqlServer` | sqlserver | 1433 | ✅ |

## Common Operations

### View Container Status

```bash
docker compose ps
docker compose --profile full ps
```

### View Logs

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f postgres

# Last 100 lines
docker compose logs --tail 100 sqlserver
```

### Stop Services

```bash
# Stop keeping data
docker compose --profile full down

# Stop and remove volumes (clean slate)
docker compose --profile full down -v
```

### Health Checks

All containers include health checks. View status:

```bash
docker inspect encina-postgres --format='{{.State.Health.Status}}'
docker inspect encina-redis --format='{{.State.Health.Status}}'
```

### Resource Usage

```bash
docker stats --no-stream
```

## Troubleshooting

### Port Conflicts

```bash
# Check what's using a port (Windows)
netstat -ano | findstr :5432

# Check what's using a port (Linux/macOS)
lsof -i :5432
```

**Solution**: Stop local service or modify ports in `docker-compose.yml`.

### Oracle Startup Issues

Oracle XE has a large image (~2 GB) and slow startup (~60 seconds):

```bash
# Pre-pull the image
docker pull container-registry.oracle.com/database/express:21.3.0-xe

# Check startup progress
docker logs -f encina-oracle
```

### Container Won't Start

```bash
# View error logs
docker compose logs <service-name>

# Clean restart
docker compose --profile full down -v
docker compose --profile full up -d
```

### Insufficient Memory

Some services (SQL Server, Oracle) require significant memory:

```bash
# Check Docker Desktop memory allocation
# Recommended: 8 GB minimum for full profile
```

## CI/CD Integration

### GitHub Actions Example

```yaml
jobs:
  test:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_DB: encina
          POSTGRES_USER: encina
          POSTGRES_PASSWORD: test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4
      - name: Run tests
        run: dotnet test --filter "Category=Integration"
```

### Using Docker Compose in CI

```yaml
- name: Start infrastructure
  run: docker compose --profile core up -d

- name: Wait for services
  run: |
    docker compose --profile core ps
    sleep 30

- name: Run tests
  run: dotnet test

- name: Stop infrastructure
  run: docker compose --profile core down
```

## MCP Server Integration

MCP (Model Context Protocol) servers are configured for database access:

| MCP Server | Status | Connection |
|------------|--------|------------|
| `postgres` | ✅ Enabled | `postgresql://encina:...@host.docker.internal:5432/encina` |
| `mongodb` | ✅ Enabled | `mongodb://encina:...@host.docker.internal:27017` |
| `docker` | ✅ Enabled | Docker CLI access |

### Query Databases via MCP

```
# PostgreSQL
Use the postgres MCP server to query: SELECT * FROM users LIMIT 10;

# MongoDB
Use the mongodb MCP server to list databases
```

## Related Documentation

- [Integration Testing with Docker](../testing/integration-tests.md)
- [Testcontainers Direct Usage](../testing/testcontainers-direct-usage.md)
- [Observability Stack](../../observability/README.md)
- [ADR-008: Aspire vs Testcontainers](../architecture/adr/008-aspire-vs-testcontainers-testing-strategy.md)

## Files Reference

| File | Purpose |
|------|---------|
| `docker-compose.yml` | Main infrastructure services |
| `docker-compose.observability.yml` | OpenTelemetry stack (Prometheus, Jaeger, Loki, Grafana) |
| `infrastructure/mosquitto/mosquitto.conf` | MQTT broker configuration |
| `observability/otel-collector-config.yaml` | OpenTelemetry Collector configuration |
| `observability/prometheus.yml` | Prometheus scrape configuration |
| `observability/loki-config.yaml` | Loki log aggregation configuration |
