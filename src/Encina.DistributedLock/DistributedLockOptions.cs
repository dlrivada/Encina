using Encina.Messaging.Health;

namespace Encina.DistributedLock;

/// <summary>
/// Base options for distributed lock providers.
/// </summary>
public class DistributedLockOptions
{
    /// <summary>
    /// Gets or sets the key prefix for all lock keys.
    /// </summary>
    /// <remarks>
    /// Use this to namespace locks in shared environments.
    /// For example, setting this to "myapp" will prefix all lock keys with "myapp:".
    /// </remarks>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default lock expiry time.
    /// </summary>
    /// <remarks>
    /// This is used when no explicit expiry is provided.
    /// Default is 30 seconds.
    /// </remarks>
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the default wait time when acquiring a lock.
    /// </summary>
    /// <remarks>
    /// This is used when no explicit wait time is provided.
    /// Default is 10 seconds.
    /// </remarks>
    public TimeSpan DefaultWait { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the default retry interval when waiting for a lock.
    /// </summary>
    /// <remarks>
    /// This is used when no explicit retry interval is provided.
    /// Default is 100 milliseconds.
    /// </remarks>
    public TimeSpan DefaultRetry { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the health check options for this provider.
    /// </summary>
    public ProviderHealthCheckOptions ProviderHealthCheck { get; set; } = new();
}
