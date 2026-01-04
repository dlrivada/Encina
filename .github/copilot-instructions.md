# Copilot Instructions for Encina

## Project Overview

Encina is a **.NET 10** toolkit for building resilient applications using **Railway Oriented Programming (ROP)**. All operations return `Either<EncinaError, T>` - no exceptions for business logic. This is a **pre-1.0** project: breaking changes are acceptable and preferred over compatibility hacks.

## Architecture

```
src/
├── Encina/                    # Core: IEncina, Pipeline, Commands, Queries
├── Encina.Messaging/          # Shared abstractions (IOutboxStore, ISagaStore)
├── Encina.EntityFrameworkCore/ # EF Core implementation
├── Encina.{Provider}/         # Provider-specific implementations
tests/
├── {Package}.Tests/           # Unit tests (required)
├── {Package}.IntegrationTests/ # Integration tests (Docker/Testcontainers)
```

**Core patterns**: `ICommand<T>`, `IQuery<T>`, `INotification`, pipeline behaviors for validation/caching/transactions.

## Critical Build Commands

The solution has 66+ projects. **Full builds may crash with CLR error 0x80131506**. Use solution filters:

```bash
# Safe full build (sequential)
dotnet build Encina.slnx -maxcpucount:1

# Recommended: Use solution filters
dotnet build Encina.Core.slnf          # Core + GuardClauses
dotnet build Encina.Testing.slnf       # Testing packages
dotnet test Encina.Core.slnf

# Run specific test project
dotnet test tests/Encina.Testing.Pact.Tests/Encina.Testing.Pact.Tests.csproj
```

## Code Conventions

### Naming Standards
- **Timestamps**: Always UTC with `AtUtc` suffix (`CreatedAtUtc`, `ProcessedAtUtc`)
- **Error properties**: `ErrorMessage` not `Error` (avoids CA1716)
- **Type properties**: `RequestType` or `NotificationType` not `MessageType`
- **Store classes**: `{Pattern}Store{Provider}` (e.g., `OutboxStoreEF`)

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
// Use ObjectDisposedException.ThrowIf for disposal checks
ObjectDisposedException.ThrowIf(_disposed, this);

// Use ArgumentNullException.ThrowIfNull for null checks  
ArgumentNullException.ThrowIfNull(command);
ArgumentException.ThrowIfNullOrWhiteSpace(providerName);
```

## Testing Patterns

### Test Organization
- **Unit tests**: `tests/{Package}.Tests/` - mock all dependencies
- **Integration tests**: `tests/{Package}.IntegrationTests/` - real databases via Testcontainers
- CI excludes `*IntegrationTests*` projects automatically

### Test Structure
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    const string paramName = nameof(paramName); // Use nameof for parameter names
    
    // Act
    var result = _sut.DoSomething();
    
    // Assert
    result.ShouldNotBeNull();
}
```

### Dispose Pattern in Tests
```csharp
public void Dispose()
{
    _sut.Dispose();
    
    try
    {
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, true);
    }
    catch (IOException) { }  // Don't mask test failures
    catch (UnauthorizedAccessException) { }
}
```

## Configuration Standards

### Opt-in Features
All messaging patterns are disabled by default:
```csharp
config.UseTransactions = true;  // Enable only what you need
config.UseOutbox = true;
config.UseInbox = true;
```

### IAsyncDisposable Pattern
```csharp
private async ValueTask DisposeAsyncCore()
{
    if (_disposed) return;
    
    // Use ConfigureAwait(false) to prevent deadlocks
    if (_serviceProvider is IAsyncDisposable asyncDisposable)
        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
    
    _disposed = true;
}
```

## Key Files
- `CLAUDE.md` - Comprehensive development guidelines (788 lines)
- `Directory.Build.props` - Shared MSBuild properties
- `Directory.Packages.props` - Central package management
- `.coderabbit.yaml` - CodeRabbit review configuration

## Language
- **Code/comments/docs**: English only
- **Commit messages**: English
- If you find Spanish comments in code, translate them to English
