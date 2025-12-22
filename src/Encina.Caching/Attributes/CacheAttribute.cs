namespace Encina.Caching;

/// <summary>
/// Marks a request for automatic caching of its response.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a request class, the <see cref="QueryCachingPipelineBehavior{TRequest,TResponse}"/>
/// will automatically cache successful responses and return cached values for subsequent identical requests.
/// </para>
/// <para>
/// <b>Cache Key Generation</b>:
/// By default, the cache key is generated from the request type and its properties.
/// You can customize the key using the <see cref="KeyTemplate"/> property.
/// </para>
/// <para>
/// <b>Tenant and User Isolation</b>:
/// Cache keys automatically include tenant ID. Set <see cref="VaryByUser"/> to include user ID.
/// </para>
/// <para>
/// <b>Expiration</b>:
/// Use <see cref="DurationSeconds"/> for absolute expiration or <see cref="SlidingExpiration"/>
/// for sliding expiration (resets on each access).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple caching with 5-minute expiration
/// [Cache(DurationSeconds = 300)]
/// public record GetProductQuery(Guid ProductId) : IQuery&lt;Product&gt;;
///
/// // Custom cache key template
/// [Cache(DurationSeconds = 600, KeyTemplate = "product:{ProductId}")]
/// public record GetProductQuery(Guid ProductId) : IQuery&lt;Product&gt;;
///
/// // Vary by user (separate cache per user)
/// [Cache(DurationSeconds = 300, VaryByUser = true)]
/// public record GetUserDashboardQuery : IQuery&lt;Dashboard&gt;;
///
/// // Sliding expiration (resets on each access)
/// [Cache(DurationSeconds = 60, SlidingExpiration = true)]
/// public record GetConfigurationQuery : IQuery&lt;Configuration&gt;;
///
/// // High priority (retained longer during memory pressure)
/// [Cache(DurationSeconds = 3600, Priority = CachePriority.High)]
/// public record GetExpensiveDataQuery : IQuery&lt;ExpensiveData&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class CacheAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the cache duration in seconds.
    /// </summary>
    /// <value>The default is 300 seconds (5 minutes).</value>
    public int DurationSeconds { get; init; } = 300;

    /// <summary>
    /// Gets or sets the cache key template.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use placeholders in the format <c>{PropertyName}</c> to include request property values.
    /// The tenant ID is automatically prepended.
    /// </para>
    /// <para>
    /// Example: <c>"product:{ProductId}:details"</c> produces <c>"tenant123:product:abc-123:details"</c>
    /// </para>
    /// </remarks>
    /// <value>If null, a default key is generated from the request type and properties.</value>
    public string? KeyTemplate { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to vary the cache by user ID.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, each user gets their own cached copy.
    /// Use this for user-specific data like preferences or personalized content.
    /// </remarks>
    /// <value>The default is <c>false</c> (shared cache across users).</value>
    public bool VaryByUser { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to vary the cache by tenant ID.
    /// </summary>
    /// <remarks>
    /// When <c>true</c> (default), each tenant gets their own cached copy.
    /// Only set to <c>false</c> for truly global data that is identical across all tenants.
    /// </remarks>
    /// <value>The default is <c>true</c> (tenant isolation).</value>
    public bool VaryByTenant { get; init; } = true;

    /// <summary>
    /// Gets or sets the cache priority.
    /// </summary>
    /// <remarks>
    /// Priority affects eviction order when the cache is full.
    /// Not all cache providers support priority-based eviction.
    /// </remarks>
    /// <value>The default is <see cref="CachePriority.Normal"/>.</value>
    public CachePriority Priority { get; init; } = CachePriority.Normal;

    /// <summary>
    /// Gets or sets a value indicating whether to use sliding expiration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the expiration timer resets each time the cached value is accessed.
    /// This is useful for frequently accessed data that should stay cached as long as it's being used.
    /// </para>
    /// <para>
    /// When <c>false</c>, the cache entry expires after a fixed duration regardless of access patterns.
    /// </para>
    /// </remarks>
    /// <value>The default is <c>false</c> (absolute expiration).</value>
    public bool SlidingExpiration { get; init; }

    /// <summary>
    /// Gets or sets the maximum absolute expiration in seconds.
    /// </summary>
    /// <remarks>
    /// Only applies when <see cref="SlidingExpiration"/> is <c>true</c>.
    /// This prevents an item from being cached indefinitely due to continuous access.
    /// </remarks>
    /// <value>The default is <c>null</c> (no maximum).</value>
    public int? MaxAbsoluteExpirationSeconds { get; init; }

    /// <summary>
    /// Gets the cache duration as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

    /// <summary>
    /// Gets the maximum absolute expiration as a <see cref="TimeSpan"/>, if set.
    /// </summary>
    public TimeSpan? MaxAbsoluteExpiration =>
        MaxAbsoluteExpirationSeconds.HasValue
            ? TimeSpan.FromSeconds(MaxAbsoluteExpirationSeconds.Value)
            : null;
}
