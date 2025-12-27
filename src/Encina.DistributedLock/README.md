# Encina.DistributedLock

Distributed lock abstractions for Encina - Coordinate access to shared resources across multiple instances.

## Installation

```bash
dotnet add package Encina.DistributedLock
```

For specific providers:
```bash
dotnet add package Encina.DistributedLock.InMemory   # For testing
dotnet add package Encina.DistributedLock.Redis      # For production (Redis, Garnet, Valkey, etc.)
dotnet add package Encina.DistributedLock.SqlServer  # For SQL Server environments
```

## Quick Start

### In-Memory (Testing/Development)

```csharp
services.AddEncinaDistributedLockInMemory();
```

### Redis (Production)

```csharp
services.AddEncinaDistributedLockRedis("localhost:6379");
```

### SQL Server

```csharp
services.AddEncinaDistributedLockSqlServer("Server=.;Database=MyApp;Trusted_Connection=True;");
```

## Usage

```csharp
public class OrderProcessor
{
    private readonly IDistributedLockProvider _lockProvider;

    public OrderProcessor(IDistributedLockProvider lockProvider)
    {
        _lockProvider = lockProvider;
    }

    public async Task ProcessOrderAsync(string orderId, CancellationToken ct)
    {
        // Try to acquire a lock with timeout
        await using var lockHandle = await _lockProvider.TryAcquireAsync(
            resource: $"order:{orderId}",
            expiry: TimeSpan.FromMinutes(5),      // Lock auto-expires after 5 minutes
            wait: TimeSpan.FromSeconds(30),        // Wait up to 30 seconds for lock
            retry: TimeSpan.FromMilliseconds(100), // Retry every 100ms
            cancellationToken: ct);

        if (lockHandle is null)
        {
            throw new LockAcquisitionException($"order:{orderId}");
        }

        // Critical section - only one instance can process this order
        await ProcessOrderInternalAsync(orderId, ct);
    }
}
```

## Interface

```csharp
public interface IDistributedLockProvider
{
    // Try to acquire lock with timeout
    Task<IAsyncDisposable?> TryAcquireAsync(
        string resource,
        TimeSpan expiry,
        TimeSpan wait,
        TimeSpan retry,
        CancellationToken cancellationToken);

    // Acquire lock indefinitely (waits forever)
    Task<IAsyncDisposable> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken cancellationToken);

    // Check if resource is locked
    Task<bool> IsLockedAsync(string resource, CancellationToken cancellationToken);

    // Extend lock expiry
    Task<bool> ExtendAsync(string resource, TimeSpan extension, CancellationToken cancellationToken);
}
```

## Lock Handle

The returned lock handle implements `ILockHandle` with additional properties:

```csharp
public interface ILockHandle : IAsyncDisposable
{
    string Resource { get; }
    string LockId { get; }
    DateTime AcquiredAtUtc { get; }
    DateTime ExpiresAtUtc { get; }
    bool IsReleased { get; }

    Task<bool> ExtendAsync(TimeSpan extension, CancellationToken cancellationToken = default);
}
```

## Use Cases

### Saga Coordination
```csharp
await using var lock = await _lockProvider.TryAcquireAsync(
    $"saga:payment:{orderId}",
    TimeSpan.FromMinutes(5),
    TimeSpan.FromSeconds(30),
    TimeSpan.FromMilliseconds(500),
    ct);
```

### Cache Stampede Prevention
```csharp
await using var lock = await _lockProvider.TryAcquireAsync(
    $"cache:rebuild:{cacheKey}",
    TimeSpan.FromSeconds(30),
    TimeSpan.FromSeconds(5),
    TimeSpan.FromMilliseconds(100),
    ct);
```

### Scheduled Task Execution
```csharp
await using var lock = await _lockProvider.TryAcquireAsync(
    $"task:cleanup",
    TimeSpan.FromMinutes(30),
    TimeSpan.Zero, // No wait - fail immediately if locked
    TimeSpan.Zero,
    ct);
```

## Provider Comparison

| Feature | InMemory | Redis | SQL Server |
|---------|----------|-------|------------|
| Multi-instance | No | Yes | Yes |
| Persistence | No | Optional | Yes |
| Performance | Fastest | Fast | Moderate |
| Use case | Testing | Production | DB environments |

## Configuration Options

### Base Options

```csharp
services.AddEncinaDistributedLock(options =>
{
    options.KeyPrefix = "myapp";
    options.DefaultExpiry = TimeSpan.FromMinutes(5);
    options.DefaultWait = TimeSpan.FromSeconds(30);
    options.DefaultRetry = TimeSpan.FromMilliseconds(100);
});
```

### Redis Options

```csharp
services.AddEncinaDistributedLockRedis("localhost:6379", options =>
{
    options.Database = 1;
    options.KeyPrefix = "myapp";
    options.ProviderHealthCheck.Enabled = true;
});
```

### SQL Server Options

```csharp
services.AddEncinaDistributedLockSqlServer(connectionString, options =>
{
    options.KeyPrefix = "myapp";
    options.ProviderHealthCheck.Enabled = true;
});
```

## Health Checks

Enable health checks for monitoring:

```csharp
services.AddEncinaDistributedLockRedis("localhost:6379", options =>
{
    options.ProviderHealthCheck.Enabled = true;
    options.ProviderHealthCheck.Name = "redis-lock";
});
```

## Wire Compatibility

The Redis provider is compatible with:
- Redis
- Garnet
- Valkey
- Dragonfly
- KeyDB

## Related Packages

- `Encina.DistributedLock` - Core abstractions
- `Encina.DistributedLock.InMemory` - In-memory provider for testing
- `Encina.DistributedLock.Redis` - Redis provider
- `Encina.DistributedLock.SqlServer` - SQL Server provider
- `Encina.Caching` - Caching with integrated lock support
