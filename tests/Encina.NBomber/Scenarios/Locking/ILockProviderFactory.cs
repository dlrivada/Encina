using Encina.DistributedLock;

namespace Encina.NBomber.Scenarios.Locking;

/// <summary>
/// Factory interface for creating distributed lock providers for load testing.
/// Provides lifecycle management and configuration for lock providers.
/// </summary>
public interface ILockProviderFactory : IAsyncDisposable
{
    /// <summary>
    /// Gets the name of the lock provider (e.g., "redis", "sqlserver", "inmemory").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the category of the lock provider.
    /// </summary>
    LockProviderCategory Category { get; }

    /// <summary>
    /// Gets the lock provider options.
    /// </summary>
    LockProviderOptions Options { get; }

    /// <summary>
    /// Gets a value indicating whether the provider is available and ready.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Initializes the lock provider and any required infrastructure (e.g., containers).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new lock provider instance.
    /// </summary>
    /// <returns>A configured lock provider.</returns>
    IDistributedLockProvider CreateLockProvider();
}

/// <summary>
/// Categories of lock providers for load testing.
/// </summary>
public enum LockProviderCategory
{
    /// <summary>In-memory locking (single process).</summary>
    InMemory,

    /// <summary>Redis-based distributed locking.</summary>
    Redis,

    /// <summary>SQL Server-based distributed locking.</summary>
    SqlServer
}

/// <summary>
/// Configuration options for lock provider factories.
/// </summary>
public sealed class LockProviderOptions
{
    /// <summary>
    /// Gets or sets the key prefix for lock resources.
    /// </summary>
    public string KeyPrefix { get; set; } = "nbomber:lock";

    /// <summary>
    /// Gets or sets the default lock expiration time.
    /// </summary>
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the default wait timeout for lock acquisition.
    /// </summary>
    public TimeSpan DefaultWaitTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the default retry interval when waiting for a lock.
    /// </summary>
    public TimeSpan DefaultRetryInterval { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the number of contention buckets (shared resources).
    /// </summary>
    public int ContentionBuckets { get; set; } = 10;

    /// <summary>
    /// Gets or sets the Redis connection string (for Redis providers).
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the Redis container image (for containerized testing).
    /// </summary>
    public string RedisImage { get; set; } = "redis:7-alpine";

    /// <summary>
    /// Gets or sets the SQL Server connection string (for SQL Server providers).
    /// </summary>
    public string? SqlServerConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the SQL Server container image (for containerized testing).
    /// </summary>
    public string SqlServerImage { get; set; } = "mcr.microsoft.com/mssql/server:2022-latest";
}
