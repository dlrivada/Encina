namespace SimpleMediator.Caching;

/// <summary>
/// Configuration options for SimpleMediator caching.
/// </summary>
public sealed class CachingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether query caching is enabled.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    public bool EnableQueryCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether cache invalidation is enabled.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    public bool EnableCacheInvalidation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether distributed idempotency is enabled.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    public bool EnableDistributedIdempotency { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether distributed locks are enabled.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    public bool EnableDistributedLocks { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether pub/sub for cross-instance invalidation is enabled.
    /// </summary>
    /// <value>The default is <c>true</c>.</value>
    public bool EnablePubSubInvalidation { get; set; } = true;

    /// <summary>
    /// Gets or sets the default cache duration.
    /// </summary>
    /// <value>The default is 5 minutes.</value>
    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the default cache priority.
    /// </summary>
    /// <value>The default is <see cref="CachePriority.Normal"/>.</value>
    public CachePriority DefaultPriority { get; set; } = CachePriority.Normal;

    /// <summary>
    /// Gets or sets the key prefix for all cache entries.
    /// </summary>
    /// <value>The default is <c>"sm"</c> (SimpleMediator).</value>
    public string KeyPrefix { get; set; } = "sm";

    /// <summary>
    /// Gets or sets the channel name for cache invalidation pub/sub.
    /// </summary>
    /// <value>The default is <c>"sm:cache:invalidate"</c>.</value>
    public string InvalidationChannel { get; set; } = "sm:cache:invalidate";

    /// <summary>
    /// Gets or sets the key prefix for idempotency entries.
    /// </summary>
    /// <value>The default is <c>"sm:idempotency"</c>.</value>
    public string IdempotencyKeyPrefix { get; set; } = "sm:idempotency";

    /// <summary>
    /// Gets or sets the default idempotency TTL.
    /// </summary>
    /// <value>The default is 24 hours.</value>
    public TimeSpan IdempotencyTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Gets or sets the key prefix for distributed locks.
    /// </summary>
    /// <value>The default is <c>"sm:lock"</c>.</value>
    public string LockKeyPrefix { get; set; } = "sm:lock";

    /// <summary>
    /// Gets or sets the default lock expiry.
    /// </summary>
    /// <value>The default is 30 seconds.</value>
    public TimeSpan DefaultLockExpiry { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the default lock wait time.
    /// </summary>
    /// <value>The default is 10 seconds.</value>
    public TimeSpan DefaultLockWait { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the default lock retry interval.
    /// </summary>
    /// <value>The default is 200 milliseconds.</value>
    public TimeSpan DefaultLockRetry { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Gets or sets a value indicating whether to throw on cache errors.
    /// </summary>
    /// <remarks>
    /// When <c>false</c>, cache errors are logged but the request continues without caching.
    /// When <c>true</c>, cache errors propagate and fail the request.
    /// </remarks>
    /// <value>The default is <c>false</c> (resilient mode).</value>
    public bool ThrowOnCacheErrors { get; set; }

    /// <summary>
    /// Gets or sets the serializer options for cache values.
    /// </summary>
    public CacheSerializerOptions SerializerOptions { get; set; } = new();
}

/// <summary>
/// Serialization options for cache values.
/// </summary>
public sealed class CacheSerializerOptions
{
    /// <summary>
    /// Gets or sets the serializer type to use.
    /// </summary>
    /// <value>The default is <see cref="CacheSerializerType.SystemTextJson"/>.</value>
    public CacheSerializerType SerializerType { get; set; } = CacheSerializerType.SystemTextJson;

    /// <summary>
    /// Gets or sets a value indicating whether to compress cached values.
    /// </summary>
    /// <value>The default is <c>false</c>.</value>
    public bool EnableCompression { get; set; }

    /// <summary>
    /// Gets or sets the minimum size in bytes before compression is applied.
    /// </summary>
    /// <value>The default is 1024 bytes.</value>
    public int CompressionThreshold { get; set; } = 1024;
}

/// <summary>
/// Specifies the serializer type for cache values.
/// </summary>
public enum CacheSerializerType
{
    /// <summary>
    /// Use System.Text.Json for serialization.
    /// </summary>
    SystemTextJson = 0,

    /// <summary>
    /// Use MessagePack for serialization (faster, smaller).
    /// </summary>
    MessagePack = 1
}
