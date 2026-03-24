namespace Encina.Security.Secrets.Caching;

/// <summary>
/// Configuration options for distributed secrets caching via <c>CachingSecretReaderDecorator</c>.
/// </summary>
/// <remarks>
/// <para>
/// These options control the PubSub-based cross-instance cache invalidation and cache key
/// conventions for secrets. The core caching toggle (<see cref="SecretsOptions.EnableCaching"/>)
/// and TTL (<see cref="SecretsOptions.DefaultCacheDuration"/>) remain on <see cref="SecretsOptions"/>.
/// </para>
/// <para>
/// PubSub-based invalidation (<see cref="EnablePubSubInvalidation"/>) broadcasts cache
/// evictions to all application instances through a configurable channel, ensuring
/// cross-instance consistency without manual cache management.
/// </para>
/// <para>
/// Convention alignment: these options follow the same patterns as
/// <c>Encina.Security.ABAC.PolicyCachingOptions</c> (key prefix, PubSub channel, cache tag).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaSecrets(options =>
/// {
///     options.EnableCaching = true;
///     options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
///     options.Caching.EnablePubSubInvalidation = true;
///     options.Caching.InvalidationChannel = "my-app:secrets:invalidate";
/// });
/// </code>
/// </example>
public sealed class SecretCachingOptions
{
    /// <summary>
    /// Gets or sets whether cross-instance cache invalidation via PubSub is enabled.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, write operations (set, rotate) publish an invalidation message
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
    /// Default is <c>"encina:secrets:cache:invalidate"</c>.
    /// </remarks>
    public string InvalidationChannel { get; set; } = "encina:secrets:cache:invalidate";

    /// <summary>
    /// Gets or sets the key prefix for all secrets cache entries.
    /// </summary>
    /// <remarks>
    /// Cache keys are constructed as <c>{CacheKeyPrefix}:v:{secretName}</c> for string values,
    /// <c>{CacheKeyPrefix}:t:{secretName}:{typeName}</c> for typed values, and
    /// <c>{CacheKeyPrefix}:lkg:{secretName}</c> for last-known-good fallback values.
    /// Use a unique prefix to avoid key collisions with other cache entries.
    /// Default is <c>"encina:secrets"</c>.
    /// </remarks>
    public string CacheKeyPrefix { get; set; } = "encina:secrets";

    /// <summary>
    /// Gets or sets the tag applied to all cached secret entries for bulk invalidation.
    /// </summary>
    /// <remarks>
    /// Tags enable efficient bulk eviction via <c>ICacheProvider.RemoveByTagAsync</c>
    /// when the cache provider supports it.
    /// Default is <c>"secrets"</c>.
    /// </remarks>
    public string CacheTag { get; set; } = "secrets";
}
