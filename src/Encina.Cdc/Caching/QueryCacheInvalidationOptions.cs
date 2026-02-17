namespace Encina.Cdc.Caching;

/// <summary>
/// Configuration options for CDC-driven query cache invalidation.
/// Controls how database changes detected by CDC are translated into cache invalidation
/// commands and optionally broadcast to other application instances via pub/sub.
/// </summary>
/// <remarks>
/// <para>
/// This feature complements the existing <c>QueryCacheInterceptor</c> which only invalidates
/// cache entries when the same application instance calls <c>SaveChanges()</c>. CDC-driven
/// invalidation detects changes from any source: other instances, direct SQL, migrations,
/// or external microservices.
/// </para>
/// <para>
/// Cache keys are generated using the pattern <c>{CacheKeyPrefix}:*:{entityType}:*</c>
/// to match keys produced by <c>QueryCacheInterceptor</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// config.WithCacheInvalidation(opts =>
/// {
///     opts.CacheKeyPrefix = "sm:qc";
///     opts.UsePubSubBroadcast = true;
///     opts.Tables = ["Orders", "Products"];
///     opts.TableToEntityTypeMappings = new Dictionary&lt;string, string&gt;
///     {
///         ["dbo.Orders"] = "Order",
///         ["dbo.Products"] = "Product"
///     };
/// });
/// </code>
/// </example>
public sealed class QueryCacheInvalidationOptions
{
    /// <summary>
    /// Gets or sets the cache key prefix used when generating invalidation patterns.
    /// Must match the prefix used by <c>QueryCacheInterceptor</c> to ensure correct
    /// key pattern matching.
    /// Default is <c>"sm:qc"</c>.
    /// </summary>
    public string CacheKeyPrefix { get; set; } = "sm:qc";

    /// <summary>
    /// Gets or sets whether to broadcast cache invalidation messages to other
    /// application instances via <c>IPubSubProvider</c>.
    /// When <c>true</c>, invalidation patterns are published to the <see cref="PubSubChannel"/>
    /// so that all instances can invalidate their local caches.
    /// Default is <c>true</c>.
    /// </summary>
    public bool UsePubSubBroadcast { get; set; } = true;

    /// <summary>
    /// Gets or sets the pub/sub channel name used for broadcasting cache invalidation
    /// messages across application instances.
    /// Only used when <see cref="UsePubSubBroadcast"/> is <c>true</c>.
    /// Default is <c>"sm:cache:invalidate"</c>.
    /// </summary>
    public string PubSubChannel { get; set; } = "sm:cache:invalidate";

    /// <summary>
    /// Gets or sets the set of table names to monitor for cache invalidation.
    /// When <c>null</c>, all tables detected by CDC trigger cache invalidation.
    /// When set, only changes to the specified tables trigger invalidation.
    /// Table name comparison is case-insensitive.
    /// Default is <c>null</c> (all tables).
    /// </summary>
    public HashSet<string>? Tables { get; set; }

    /// <summary>
    /// Gets or sets explicit mappings from database table names to entity type names
    /// used in cache key generation. When a table name is not found in this dictionary,
    /// the handler falls back to stripping the schema prefix from the table name
    /// (e.g., <c>"dbo.Orders"</c> becomes <c>"Orders"</c>).
    /// Default is <c>null</c> (use schema-stripped table name as entity type).
    /// </summary>
    /// <example>
    /// <code>
    /// opts.TableToEntityTypeMappings = new Dictionary&lt;string, string&gt;
    /// {
    ///     ["dbo.Orders"] = "Order",        // Singular entity name
    ///     ["public.products"] = "Product"   // Schema-qualified to entity
    /// };
    /// </code>
    /// </example>
    public Dictionary<string, string>? TableToEntityTypeMappings { get; set; }
}
