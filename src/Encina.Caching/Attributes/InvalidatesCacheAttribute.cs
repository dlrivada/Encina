namespace Encina.Caching;

/// <summary>
/// Marks a request to invalidate cache entries after successful execution.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a command class, the <see cref="CacheInvalidationPipelineBehavior{TRequest,TResponse}"/>
/// will automatically invalidate matching cache entries after the command succeeds.
/// </para>
/// <para>
/// <b>Pattern Matching</b>:
/// Use glob-style patterns to match cache keys:
/// <list type="bullet">
/// <item><description><c>*</c> matches any sequence of characters</description></item>
/// <item><description><c>?</c> matches any single character</description></item>
/// <item><description><c>{PropertyName}</c> substitutes with request property values</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Cross-Instance Invalidation</b>:
/// When <see cref="BroadcastInvalidation"/> is <c>true</c> (default), invalidation messages
/// are published via Pub/Sub to invalidate caches on all application instances.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Invalidate specific product cache
/// [InvalidatesCache(KeyPattern = "product:{ProductId}:*")]
/// public record UpdateProductCommand(Guid ProductId, string Name) : ICommand&lt;Product&gt;;
///
/// // Invalidate all products in a category
/// [InvalidatesCache(KeyPattern = "products:category:{CategoryId}:*")]
/// public record UpdateCategoryCommand(Guid CategoryId) : ICommand;
///
/// // Invalidate multiple patterns
/// [InvalidatesCache(KeyPattern = "product:{ProductId}:*")]
/// [InvalidatesCache(KeyPattern = "products:list:*")]
/// [InvalidatesCache(KeyPattern = "search:*")]
/// public record DeleteProductCommand(Guid ProductId) : ICommand;
///
/// // Local invalidation only (no broadcast)
/// [InvalidatesCache(KeyPattern = "config:*", BroadcastInvalidation = false)]
/// public record UpdateConfigCommand : ICommand;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class InvalidatesCacheAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidatesCacheAttribute"/> class.
    /// </summary>
    public InvalidatesCacheAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidatesCacheAttribute"/> class.
    /// </summary>
    /// <param name="keyPattern">The cache key pattern to invalidate.</param>
    public InvalidatesCacheAttribute(string keyPattern)
    {
        KeyPattern = keyPattern;
    }

    /// <summary>
    /// Gets or sets the cache key pattern to invalidate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use glob-style patterns and property placeholders:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>"product:{ProductId}:*"</c> - Invalidate all cache for a specific product</description></item>
    /// <item><description><c>"products:*"</c> - Invalidate all product list caches</description></item>
    /// <item><description><c>"*"</c> - Invalidate all cache (use with caution!)</description></item>
    /// </list>
    /// </remarks>
    public string KeyPattern { get; init; } = "*";

    /// <summary>
    /// Gets or sets a value indicating whether to broadcast invalidation to other instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (default), a Pub/Sub message is published to notify all application
    /// instances to invalidate their local caches matching the pattern.
    /// </para>
    /// <para>
    /// Set to <c>false</c> for local-only invalidation (e.g., for configuration that's
    /// loaded from a shared database anyway).
    /// </para>
    /// </remarks>
    /// <value>The default is <c>true</c>.</value>
    public bool BroadcastInvalidation { get; init; } = true;

    /// <summary>
    /// Gets or sets the delay before invalidation occurs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this to delay invalidation for eventual consistency scenarios.
    /// For example, if you're updating a read replica that has replication lag.
    /// </para>
    /// </remarks>
    /// <value>The default is 0 (immediate invalidation).</value>
    public int DelayMilliseconds { get; init; }

    /// <summary>
    /// Gets the delay as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan Delay => TimeSpan.FromMilliseconds(DelayMilliseconds);
}
