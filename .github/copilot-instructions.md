# Copilot Instructions for Encina

> This is a condensed version of `CLAUDE.md`. When in doubt, refer to the full guidelines there.

## Project Overview

Encina is a **.NET 10 / C# 14** toolkit for building resilient applications using **Railway Oriented Programming (ROP)**. All operations return `Either<EncinaError, T>` - no exceptions for business logic. This is a **pre-1.0** project: breaking changes are acceptable and preferred over compatibility hacks.

## Architecture

```
src/
├── Encina/                    # Core: IEncina, Pipeline, Commands, Queries
├── Encina.Messaging/          # Shared abstractions (IOutboxStore, ISagaStore)
├── Encina.EntityFrameworkCore/ # EF Core implementation
├── Encina.Dapper/             # Dapper implementation
├── Encina.ADO/                # ADO.NET implementation
├── Encina.{Provider}/         # Provider-specific implementations
tests/
├── Encina.UnitTests/          # Consolidated unit tests (~9,164 tests)
├── Encina.IntegrationTests/   # Integration tests (~2,251 tests, Docker/Testcontainers)
├── Encina.PropertyTests/      # Property-based tests (~352 tests)
├── Encina.ContractTests/      # API contract tests (~247 tests)
├── Encina.GuardTests/         # Guard clause tests (~1,037 tests)
├── Encina.LoadTests/          # Load testing harness
├── Encina.BenchmarkTests/     # BenchmarkDotNet benchmarks
├── Encina.TestInfrastructure/ # Shared test infrastructure
```

**Core patterns**: `ICommand<T>`, `IQuery<T>`, `INotification`, pipeline behaviors for validation/caching/transactions.

## Build Commands

```bash
# Build full solution
dotnet build Encina.slnx --configuration Release

# Run all tests
dotnet test Encina.slnx --configuration Release

# Run specific test project
dotnet test tests/Encina.UnitTests/Encina.UnitTests.csproj
```

## Multi-Provider Rule (MANDATORY)

All provider-dependent features MUST be implemented for ALL 13 database providers:

| Category | Providers | Count |
|----------|-----------|-------|
| **ADO.NET** | Sqlite, SqlServer, PostgreSQL, MySQL | 4 |
| **Dapper** | Sqlite, SqlServer, PostgreSQL, MySQL | 4 |
| **EF Core** | Sqlite, SqlServer, PostgreSQL, MySQL | 4 |
| **MongoDB** | MongoDB | 1 |

**Provider-specific SQL differences:**

| Provider | Parameters | LIMIT | Boolean | Notes |
|----------|------------|-------|---------|-------|
| SQLite | `@param` | `LIMIT @n` | `0/1` | String-based DateTime storage |
| SQL Server | `@param` | `TOP (@n)` | `bit` | Native DateTime, GUID |
| PostgreSQL | `@param` | `LIMIT @n` | `true/false` | Case-sensitive identifiers |
| MySQL | `@param` | `LIMIT @n` | `0/1` | Backtick identifiers |

Beyond the 13 database providers, there are specialized categories: **Caching (8)**, **Transport (10+)**, **Lock (4+)**, **Validation (3)**, **Cloud (3)**, **Resilience (3)**, **Observability (1+)**. See `CLAUDE.md` for full details.

## Code Conventions

### Naming Standards

- **Timestamps**: Always UTC with `AtUtc` suffix (`CreatedAtUtc`, `ProcessedAtUtc`)
- **Error properties**: `ErrorMessage` not `Error` (avoids CA1716)
- **Type properties**: `RequestType` or `NotificationType` not `MessageType`
- **Store classes**: `{Pattern}Store{Provider}` (e.g., `OutboxStoreEF`)
- **Messaging entities**: `OutboxMessage`, `InboxMessage`, `SagaState`, `ScheduledMessage`

### Public API Tracking

Uses `Microsoft.CodeAnalysis.PublicApiAnalyzers`:
- Add new public APIs to `PublicAPI.Unshipped.txt`
- Format: `Namespace.Type.Member(params) -> ReturnType`
- Nullable: `string!` (non-null), `string?` (nullable)

### Error Handling

```csharp
// Always use Either<EncinaError, T>, never throw for business logic
public async Task<Either<EncinaError, Order>> GetOrder(Guid id)
{
    var order = await _repo.FindAsync(id);
    return order is null
        ? Either<EncinaError, Order>.Left(EncinaErrors.Create("order.notfound", "Order not found"))
        : Either<EncinaError, Order>.Right(order);
}
```

### Guard Clauses

```csharp
ObjectDisposedException.ThrowIf(_disposed, this);
ArgumentNullException.ThrowIfNull(command);
ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
```

### Opt-in Configuration

All messaging patterns are disabled by default:

```csharp
config.UseTransactions = true;  // Enable only what you need
config.UseOutbox = true;
config.UseInbox = true;
```

## Testing Patterns

### Collection Fixtures (CRITICAL)

Integration tests use shared xUnit `[Collection]` fixtures to minimize Docker containers (~23 instead of ~71). **NEVER** create per-class fixtures for database tests.

**Existing collections:**

| Collection | Fixture | Notes |
|------------|---------|-------|
| `ADO-SqlServer` | `SqlServerFixture` | |
| `ADO-PostgreSQL` | `PostgreSqlFixture` | |
| `ADO-MySQL` | `MySqlFixture` | |
| `ADO-Sqlite` | `SqliteFixture` | `DisableParallelization = true` |
| `Dapper-SqlServer` | `SqlServerFixture` | |
| `Dapper-PostgreSQL` | `PostgreSqlFixture` | |
| `Dapper-MySQL` | `MySqlFixture` | |
| `Dapper-Sqlite` | `SqliteFixture` | `DisableParallelization = true` |
| `EFCore-SqlServer` | `EFCoreSqlServerFixture` | |
| `EFCore-PostgreSQL` | `EFCorePostgreSqlFixture` | |
| `EFCore-MySQL` | `EFCoreMySqlFixture` | |
| `EFCore-Sqlite` | `EFCoreSqliteFixture` | `DisableParallelization = true` |

**New test class template:**

```csharp
[Collection("ADO-PostgreSQL")]  // REQUIRED - shares fixture across all classes
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public class MyNewTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public MyNewTests(PostgreSqlFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync() => await _fixture.ClearAllDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;
}
```

**Rules:**
1. NEVER use `IClassFixture<T>` for database fixtures - always use `[Collection]`
2. NEVER call `new SqlServerFixture()` or `_fixture = new()` - inject via constructor
3. NEVER call `_fixture.DisposeAsync()` from tests - the collection owns the lifecycle
4. Use `_fixture.ClearAllDataAsync()` in `InitializeAsync()` for data cleanup

### SQLite Special Rules (Shared In-Memory DB)

- `CreateConnection()` returns the SAME shared connection object
- NEVER wrap it in `using`/`await using`
- NEVER pass it to wrappers that dispose (like `SchemaValidatingConnection`)
- When a disposable connection is needed, create: `new SqliteConnection(_fixture.ConnectionString)`

### Test Structure

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    const string paramName = nameof(paramName);

    // Act
    var result = _sut.DoSomething();

    // Assert
    result.ShouldNotBeNull();
}
```

### Test Output Conventions

All test outputs MUST go to `artifacts/`, never to the repository root:

| Output Type | Location |
|-------------|----------|
| Test results | `artifacts/test-results/` |
| Code coverage | `artifacts/coverage/` |
| Benchmark results | `artifacts/performance/` |
| Load test metrics | `artifacts/load-metrics/` |
| Mutation reports | `artifacts/stryker/` |

### BenchmarkDotNet Guidelines

```csharp
// WRONG - BenchmarkRunner.Run<T>() IGNORES --filter arguments
BenchmarkRunner.Run<MyBenchmarks>(config);

// CORRECT - BenchmarkSwitcher supports --filter, --list, --job, etc.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
```

- Always materialize `IQueryable<T>` with `.ToList()` in benchmark methods
- Verify filtering before full execution: `dotnet run -c Release -- --list flat --filter "*MyBenchmark*"`

## SQLite DateTime Format Incompatibility

Never use `datetime('now')` in SQLite Dapper queries. Always use parameterized `@NowUtc`:

```csharp
// WRONG - datetime('now') format incompatible with ISO 8601
var sql = "SELECT * FROM Messages WHERE ProcessedAtUtc < datetime('now')";

// CORRECT - Use parameterized DateTime from C#
var nowUtc = DateTime.UtcNow;
var sql = "SELECT * FROM Messages WHERE ProcessedAtUtc < @NowUtc";
await connection.QueryAsync<Message>(sql, new { NowUtc = nowUtc });
```

## .NET 10 / C# 14 Notes

Key C# 14 features: extension members (properties, operators, static), field keyword, null-conditional assignment, partial constructors/events, file-based apps (`dotnet run *.cs`).

Key .NET 10 breaking changes: SLNX format default, transitive package auditing, OpenAPI 3.1 schema changes, `WebHostBuilder`/`IWebHost` obsolete, Ubuntu default container images.

## Git Workflow

- **No Force Push** to main/master
- **Commit Messages**: Clear, descriptive, in English
- **No AI Attribution**: Do NOT include any AI signatures, co-author tags, or references to AI assistance in commits. All commits should appear as authored solely by the repository owner.

## Code Analysis

- **Zero Warnings**: All CA warnings must be addressed
- CA1848 (LoggerMessage delegates): Suppress if future work
- CA2263 (Generic overload): Suppress when dynamic serialization needed
- CA1716 (Keyword conflicts): Fix by renaming (e.g., `Error` -> `ErrorMessage`)

## Language

- **Code/comments/docs**: English only
- **Commit messages**: English
- If you find Spanish comments in code, translate them to English

## Key Files

- `CLAUDE.md` - Comprehensive development guidelines (source of truth)
- `Directory.Build.props` - Shared MSBuild properties
- `Directory.Packages.props` - Central package management
- `.coderabbit.yaml` - CodeRabbit AI review configuration
- `docs/plans/` - Active implementation plans (check before starting work)
