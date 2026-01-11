# Encina CI/CD Workflow Templates

> **Version**: 1.0.0
> **Issue**: [#173](https://github.com/dlrivada/Encina/issues/173)
> **Milestone**: v0.11.0 - Testing Infrastructure

This document describes the reusable GitHub Actions workflow templates provided by Encina for testing .NET 10 applications.

---

## Overview

Encina provides three reusable workflow templates for comprehensive CI/CD pipelines:

| Template | Purpose | Complexity |
|----------|---------|------------|
| [`encina-test.yml`](#encina-testyml) | Basic unit tests with coverage | Low |
| [`encina-matrix.yml`](#encina-matrixyml) | Matrix testing (OS × Database) | Medium |
| [`encina-full-ci.yml`](#encina-full-ciyml) | Complete CI pipeline | High |

All templates are designed for .NET 10 applications and follow GitHub Actions best practices.

---

## Quick Start

### Basic Testing

```yaml
# .github/workflows/test.yml
name: Test

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  test:
    uses: dlrivada/Encina/.github/workflows/templates/encina-test.yml@main
    with:
      coverage-threshold: '80'
```

### Matrix Testing

```yaml
# .github/workflows/matrix-test.yml
name: Matrix Test

on:
  push:
    branches: [main]

jobs:
  test:
    uses: dlrivada/Encina/.github/workflows/templates/encina-matrix.yml@main
    with:
      test-os: '["ubuntu-latest", "windows-latest"]'
      test-databases: '["postgresql", "sqlserver", "sqlite"]'
```

### Full CI Pipeline

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main]
    tags: ["v*"]
  pull_request:
    branches: [main]

jobs:
  ci:
    uses: dlrivada/Encina/.github/workflows/templates/encina-full-ci.yml@main
    with:
      coverage-threshold: '80'
      run-integration-tests: true
      run-mutation-tests: false
      pack-nuget: true
    secrets:
      NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
```

---

## Template Reference

### encina-test.yml

Basic test workflow for unit tests with code coverage.

#### Inputs

| Input | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `solution` | string | No | `*.slnx` | Solution file path |
| `configuration` | string | No | `Release` | Build configuration |
| `dotnet-version` | string | No | `10.0.x` | .NET SDK version |
| `runs-on` | string | No | `ubuntu-latest` | Runner OS |
| `coverage-threshold` | string | No | `0` | Minimum coverage % |
| `run-integration-tests` | boolean | No | `false` | Include integration tests |
| `test-filter` | string | No | `` | Test filter expression |
| `exclude-patterns` | string | No | `LoadTests` | Comma-separated exclude patterns |
| `upload-coverage` | boolean | No | `true` | Upload coverage artifact |
| `timeout-minutes` | number | No | `30` | Job timeout |

#### Artifacts

- `coverage-report-{os}` - HTML coverage report
- `test-results-{os}` - Test result files (.trx, .xml)

#### Example: Custom Configuration

```yaml
jobs:
  test:
    uses: dlrivada/Encina/.github/workflows/templates/encina-test.yml@main
    with:
      solution: 'src/MyApp/MyApp.csproj'
      runs-on: 'windows-latest'
      coverage-threshold: '85'
      test-filter: 'Category!=Slow'
      exclude-patterns: 'LoadTests,IntegrationTests,E2ETests'
```

---

### encina-matrix.yml

Matrix testing across multiple operating systems and database providers.

#### Inputs

| Input | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `solution` | string | No | `*.slnx` | Solution file path |
| `configuration` | string | No | `Release` | Build configuration |
| `dotnet-version` | string | No | `10.0.x` | .NET SDK version |
| `test-os` | string (JSON) | No | `["ubuntu-latest"]` | OS runners array |
| `test-databases` | string (JSON) | No | `["sqlite"]` | Database providers array |
| `fail-fast` | boolean | No | `false` | Cancel all jobs on failure |
| `max-parallel` | number | No | `4` | Maximum parallel jobs |
| `timeout-minutes` | number | No | `45` | Job timeout |
| `test-filter` | string | No | `` | Test filter expression |

#### Supported Databases

| Database | Docker Image | Port | Linux | Windows | macOS |
|----------|--------------|------|-------|---------|-------|
| `postgresql` | postgres:16 | 5432 | ✅ | ✅ | ✅ |
| `sqlserver` | mssql/server:2022 | 1433 | ✅ | ✅ | ❌ |
| `mysql` | mysql:8 | 3306 | ✅ | ❌ | ✅ |
| `sqlite` | N/A | N/A | ✅ | ✅ | ✅ |
| `redis` | redis:7-alpine | 6379 | ✅ | ✅ | ✅ |
| `mongodb` | mongo:7 | 27017 | ✅ | ✅ | ✅ |

#### Connection String Environment Variables

The workflow sets these environment variables automatically:

```text
ConnectionStrings__PostgreSQL=Host=localhost;Port=5432;...
ConnectionStrings__SqlServer=Server=localhost,1433;...
ConnectionStrings__MySQL=Server=localhost;Port=3306;...
ConnectionStrings__SQLite=Data Source=encina_test.db
ConnectionStrings__Redis=localhost:6379
ConnectionStrings__MongoDB=mongodb://localhost:27017/encina_test
```

#### Example: Full Matrix

```yaml
jobs:
  matrix:
    uses: dlrivada/Encina/.github/workflows/templates/encina-matrix.yml@main
    with:
      test-os: '["ubuntu-latest", "windows-latest", "macos-latest"]'
      test-databases: '["postgresql", "sqlserver", "sqlite", "redis"]'
      max-parallel: 6
      fail-fast: false
```

---

### encina-full-ci.yml

Complete CI pipeline with build, test, analysis, and packaging.

#### Inputs

| Input | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `solution` | string | No | `*.slnx` | Solution file path |
| `configuration` | string | No | `Release` | Build configuration |
| `dotnet-version` | string | No | `10.0.x` | .NET SDK version |
| `runs-on` | string | No | `ubuntu-latest` | Primary runner |
| `coverage-threshold` | string | No | `0` | Minimum coverage % |
| `run-integration-tests` | boolean | No | `false` | Run integration tests |
| `run-architecture-tests` | boolean | No | `true` | Run architecture tests |
| `run-mutation-tests` | boolean | No | `false` | Run Stryker mutation tests |
| `mutation-score-threshold` | string | No | `0` | Minimum mutation score % |
| `enforce-formatting` | boolean | No | `true` | Enforce dotnet format |
| `treat-warnings-as-errors` | boolean | No | `true` | -warnaserror flag |
| `pack-nuget` | boolean | No | `true` | Create NuGet packages |
| `publish-nuget` | boolean | No | `false` | Publish packages |
| `nuget-source` | string | No | `nuget.org` | NuGet source URL |
| `timeout-minutes` | number | No | `60` | Job timeout |

#### Secrets

| Secret | Required | Description |
|--------|----------|-------------|
| `NUGET_API_KEY` | No | API key for NuGet publishing |

#### Pipeline Stages

```text
┌─────────────────┐
│  Build & Analyze │
│  - Restore       │
│  - Format check  │
│  - Build         │
└────────┬────────┘
         │
    ┌────┴────┐
    ▼         ▼
┌─────────┐ ┌─────────────────┐
│ Unit    │ │ Integration     │
│ Tests   │ │ Tests (optional)│
└────┬────┘ └────────┬────────┘
     │               │
     └───────┬───────┘
             │
    ┌────────┼────────┐
    ▼        ▼        ▼
┌───────┐ ┌───────┐ ┌─────────┐
│ Arch  │ │Mutation│ │ Pack    │
│ Tests │ │ Tests  │ │ NuGet   │
└───────┘ └───────┘ └────┬────┘
                         │
                    ┌────┴────┐
                    ▼         │
               ┌─────────┐   │
               │ Publish │◄──┘ (on tag)
               │ NuGet   │
               └─────────┘
```

#### Artifacts

- `build-output` - Compiled binaries (1 day retention)
- `coverage-report` - HTML coverage report
- `unit-test-results` - Unit test results
- `integration-test-results` - Integration test results
- `mutation-reports` - Stryker mutation reports
- `nuget-packages` - NuGet packages (.nupkg)

#### Example: Production Configuration

```yaml
jobs:
  ci:
    uses: dlrivada/Encina/.github/workflows/templates/encina-full-ci.yml@main
    with:
      coverage-threshold: '85'
      run-integration-tests: true
      run-architecture-tests: true
      run-mutation-tests: ${{ github.event_name == 'push' && github.ref == 'refs/heads/main' }}
      mutation-score-threshold: '80'
      enforce-formatting: true
      treat-warnings-as-errors: true
      pack-nuget: true
      publish-nuget: true
    secrets:
      NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}
```

---

## Best Practices

### 1. Start Simple

Begin with `encina-test.yml` and add complexity as needed:

```yaml
# Phase 1: Basic tests
uses: .../encina-test.yml@main

# Phase 2: Add database testing
uses: .../encina-matrix.yml@main

# Phase 3: Full pipeline
uses: .../encina-full-ci.yml@main
```

### 2. Use Coverage Thresholds

Set realistic coverage thresholds to prevent regressions:

```yaml
with:
  coverage-threshold: '80'  # Start at 80%, increase over time
```

### 3. Cache Optimization

All templates include NuGet and tool caching. Ensure your projects use `packages.lock.json`:

```xml
<PropertyGroup>
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
</PropertyGroup>
```

### 4. Matrix Testing Strategy

Start with SQLite for speed, add databases incrementally:

```yaml
# Fast feedback
test-databases: '["sqlite"]'

# Comprehensive (nightly/weekly)
test-databases: '["postgresql", "sqlserver", "mysql", "sqlite"]'
```

### 5. Mutation Testing

Run mutation tests only on main branch or manually:

```yaml
run-mutation-tests: ${{ github.ref == 'refs/heads/main' }}
```

---

## Integration with Encina Testing

These templates work seamlessly with Encina's testing packages:

| Package | Description |
|---------|-------------|
| `Encina.Testing` | Core testing utilities and assertions |
| `Encina.Testing.Shouldly` | Shouldly-based assertions |
| `Encina.Testing.Bogus` | Test data generation |
| `Encina.Testing.WireMock` | HTTP API mocking |
| `Encina.Testing.Respawn` | Database reset |
| `Encina.Testing.Verify` | Snapshot testing |

### Example Test Project

```csharp
[Trait("Category", "Unit")]
[Trait("Database", "PostgreSQL")]
public class OrderRepositoryTests : IAsyncLifetime
{
    private readonly ITestDatabase _db;

    public OrderRepositoryTests()
    {
        // Use connection string from environment
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL");
        _db = new TestDatabase(connectionString);
    }

    public async Task InitializeAsync() => await _db.CreateSchemaAsync();
    public async Task DisposeAsync() => await _db.DropSchemaAsync();

    [Fact]
    public async Task CreateOrder_ShouldPersist()
    {
        // Arrange
        var repository = new OrderRepository(_db.Connection);
        var order = new OrderBuilder().Build();

        // Act
        await repository.CreateAsync(order);

        // Assert
        var retrieved = await repository.GetByIdAsync(order.Id);
        retrieved.Should().NotBeNull();
    }
}
```

---

## Troubleshooting

### Common Issues

#### 1. Docker Services Not Starting

Docker service containers only work on Linux runners. For Windows/macOS, use Testcontainers:

```csharp
[Trait("Category", "Integration")]
public class IntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();
}
```

#### 2. Coverage Report Not Found

Ensure ReportGenerator is installed:

```xml
<ItemGroup>
  <PackageReference Include="ReportGenerator" Version="5.*" PrivateAssets="all" />
</ItemGroup>
```

Or in `.config/dotnet-tools.json`:

```json
{
  "tools": {
    "dotnet-reportgenerator-globaltool": {
      "version": "5.4.3",
      "commands": ["reportgenerator"]
    }
  }
}
```

#### 3. Mutation Tests Timeout

Mutation testing can be slow. Increase timeout or limit scope:

```yaml
with:
  timeout-minutes: 120
```

Or configure Stryker to exclude slow tests:

```json
{
  "stryker-config": {
    "mutate": ["src/**/*.cs"],
    "ignore-mutations": ["string", "regex"]
  }
}
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-01-03 | Initial release with three templates |

---

## References

- [GitHub Actions Reusable Workflows](https://docs.github.com/en/actions/using-workflows/reusing-workflows)
- [.NET SDK Actions](https://github.com/actions/setup-dotnet)
- [Issue #173](https://github.com/dlrivada/Encina/issues/173)
