namespace SimpleMediator.Caching;

/// <summary>
/// Marker interface for queries that should be cached.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on query types as an alternative to the <see cref="CacheAttribute"/>.
/// This interface-based approach allows for more flexible caching configuration via DI.
/// </para>
/// <para>
/// When a query implements this interface, the <see cref="QueryCachingPipelineBehavior{TRequest,TResponse}"/>
/// will check for a registered <see cref="ICacheConfiguration{TRequest}"/> to determine caching behavior.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Mark query as cacheable
/// public record GetProductQuery(Guid ProductId) : IQuery&lt;Product&gt;, ICacheableQuery;
///
/// // Configure caching in DI
/// services.AddSingleton&lt;ICacheConfiguration&lt;GetProductQuery&gt;&gt;(
///     new CacheConfiguration&lt;GetProductQuery&gt;
///     {
///         Duration = TimeSpan.FromMinutes(5),
///         KeyGenerator = (query, ctx) => $"product:{query.ProductId}"
///     });
/// </code>
/// </example>
public interface ICacheableQuery
{
}

/// <summary>
/// Configuration for caching a specific request type.
/// </summary>
/// <typeparam name="TRequest">The type of request to configure caching for.</typeparam>
public interface ICacheConfiguration<TRequest>
{
    /// <summary>
    /// Gets the cache duration for this request type.
    /// </summary>
    TimeSpan Duration { get; }

    /// <summary>
    /// Gets a value indicating whether to use sliding expiration.
    /// </summary>
    bool SlidingExpiration { get; }

    /// <summary>
    /// Gets the maximum absolute expiration for sliding expiration.
    /// </summary>
    TimeSpan? MaxAbsoluteExpiration { get; }

    /// <summary>
    /// Gets the cache priority.
    /// </summary>
    CachePriority Priority { get; }

    /// <summary>
    /// Gets a value indicating whether to vary by user.
    /// </summary>
    bool VaryByUser { get; }

    /// <summary>
    /// Gets a value indicating whether to vary by tenant.
    /// </summary>
    bool VaryByTenant { get; }

    /// <summary>
    /// Generates a cache key for the given request.
    /// </summary>
    /// <param name="request">The request to generate a key for.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The cache key.</returns>
    string GenerateKey(TRequest request, IRequestContext context);
}

/// <summary>
/// Default implementation of <see cref="ICacheConfiguration{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">The type of request to configure caching for.</typeparam>
public sealed class CacheConfiguration<TRequest> : ICacheConfiguration<TRequest>
{
    /// <summary>
    /// Gets or sets the cache duration.
    /// </summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets a value indicating whether to use sliding expiration.
    /// </summary>
    public bool SlidingExpiration { get; init; }

    /// <summary>
    /// Gets or sets the maximum absolute expiration for sliding expiration.
    /// </summary>
    public TimeSpan? MaxAbsoluteExpiration { get; init; }

    /// <summary>
    /// Gets or sets the cache priority.
    /// </summary>
    public CachePriority Priority { get; init; } = CachePriority.Normal;

    /// <summary>
    /// Gets or sets a value indicating whether to vary by user.
    /// </summary>
    public bool VaryByUser { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to vary by tenant.
    /// </summary>
    public bool VaryByTenant { get; init; } = true;

    /// <summary>
    /// Gets or sets the key generator function.
    /// </summary>
    public Func<TRequest, IRequestContext, string>? KeyGenerator { get; init; }

    /// <inheritdoc/>
    public string GenerateKey(TRequest request, IRequestContext context)
    {
        if (KeyGenerator is not null)
        {
            return KeyGenerator(request, context);
        }

        // Default key generation
        var typeName = typeof(TRequest).Name;
        var hash = request?.GetHashCode() ?? 0;

        var parts = new List<string>();
        if (VaryByTenant && !string.IsNullOrEmpty(context.TenantId))
        {
            parts.Add($"t:{context.TenantId}");
        }
        if (VaryByUser && !string.IsNullOrEmpty(context.UserId))
        {
            parts.Add($"u:{context.UserId}");
        }
        parts.Add(typeName);
        parts.Add(hash.ToString("x8", System.Globalization.CultureInfo.InvariantCulture));

        return string.Join(":", parts);
    }
}
