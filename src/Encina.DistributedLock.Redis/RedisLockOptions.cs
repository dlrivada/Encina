namespace Encina.DistributedLock.Redis;

/// <summary>
/// Options for the Redis distributed lock provider.
/// </summary>
public sealed class RedisLockOptions : DistributedLockOptions
{
    /// <summary>
    /// Gets or sets the Redis database number to use.
    /// </summary>
    /// <remarks>
    /// Default is 0. Use different database numbers to isolate lock keys
    /// from other Redis data in the same instance.
    /// </remarks>
    public int Database { get; set; }
}
