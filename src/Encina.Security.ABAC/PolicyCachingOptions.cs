namespace Encina.Security.ABAC;

/// <summary>
/// Configuration options for ABAC policy caching via <c>CachingPolicyStoreDecorator</c>.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="Enabled"/> is <c>true</c>, the <c>CachingPolicyStoreDecorator</c> wraps
/// the underlying <see cref="Persistence.IPolicyStore"/> with cache-aside reads (via
/// <c>ICacheProvider.GetOrSetAsync</c> for stampede protection) and write-through
/// invalidation on mutations.
/// </para>
/// <para>
/// PubSub-based invalidation (<see cref="EnablePubSubInvalidation"/>) broadcasts cache
/// evictions to all application instances through a configurable channel, ensuring
/// cross-instance consistency without manual cache management.
/// </para>
/// <para>
/// Convention alignment: these options follow the same patterns as
/// <c>Encina.Caching.CachingOptions</c> (key prefix, duration, PubSub channel).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaABAC(options =>
/// {
///     options.UsePersistentPAP = true;
///     options.PolicyCaching.Enabled = true;
///     options.PolicyCaching.Duration = TimeSpan.FromMinutes(15);
///     options.PolicyCaching.EnablePubSubInvalidation = true;
/// });
/// </code>
/// </example>
public sealed class PolicyCachingOptions
{
    /// <summary>
    /// Gets or sets whether policy caching is enabled.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the <c>CachingPolicyStoreDecorator</c> is registered to wrap
    /// the inner <see cref="Persistence.IPolicyStore"/>, providing cache-aside reads
    /// with stampede protection and write-through invalidation.
    /// Requires an <c>ICacheProvider</c> to be registered (from Encina.Caching.*).
    /// Default is <c>false</c>.
    /// </remarks>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the cache duration (TTL) for cached policy data.
    /// </summary>
    /// <remarks>
    /// All policy cache entries share this duration. After expiration, the next read
    /// triggers a fresh database query (via the inner store) and re-populates the cache.
    /// Default is 10 minutes.
    /// </remarks>
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets whether cross-instance cache invalidation via PubSub is enabled.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, write operations (save, delete) publish an invalidation message
    /// to the <see cref="InvalidationChannel"/>, and all subscribers evict their local
    /// cache entries. Requires an <c>IPubSubProvider</c> to be registered.
    /// Default is <c>true</c>.
    /// </remarks>
    public bool EnablePubSubInvalidation { get; set; } = true;

    /// <summary>
    /// Gets or sets the PubSub channel name for cache invalidation messages.
    /// </summary>
    /// <remarks>
    /// All application instances subscribe to this channel to receive invalidation
    /// notifications. Use a unique channel name per environment to avoid cross-environment
    /// cache evictions.
    /// Default is <c>"abac:cache:invalidate"</c>.
    /// </remarks>
    public string InvalidationChannel { get; set; } = "abac:cache:invalidate";

    /// <summary>
    /// Gets or sets the tag applied to all cached policy entries for bulk invalidation.
    /// </summary>
    /// <remarks>
    /// Tags enable efficient bulk eviction via <c>ICacheProvider.RemoveByTagAsync</c>
    /// when the cache provider supports it.
    /// Default is <c>"abac-policies"</c>.
    /// </remarks>
    public string CacheTag { get; set; } = "abac-policies";

    /// <summary>
    /// Gets or sets the key prefix for all ABAC policy cache entries.
    /// </summary>
    /// <remarks>
    /// Cache keys are constructed as <c>{CacheKeyPrefix}:policy-set:{id}</c> or
    /// <c>{CacheKeyPrefix}:policy:{id}</c>. Use a unique prefix to avoid key
    /// collisions with other cache entries.
    /// Default is <c>"abac"</c>.
    /// </remarks>
    public string CacheKeyPrefix { get; set; } = "abac";
}
