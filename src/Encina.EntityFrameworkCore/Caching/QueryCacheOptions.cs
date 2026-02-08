namespace Encina.EntityFrameworkCore.Caching;

/// <summary>
/// Configuration options for the EF Core query caching interceptor.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the <c>QueryCacheInterceptor</c>, which provides
/// automatic second-level caching for EF Core database queries. When enabled, query results
/// are cached and automatically invalidated when related entities are modified via <c>SaveChanges</c>.
/// </para>
/// <para>
/// All query caching is opt-in and disabled by default. Configure via dependency injection:
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.Configure&lt;QueryCacheOptions&gt;(options =>
/// {
///     options.Enabled = true;
///     options.DefaultExpiration = TimeSpan.FromMinutes(10);
///     options.KeyPrefix = "myapp:qc";
///     options.ThrowOnCacheErrors = false;
///     options.ExcludeType&lt;AuditLogEntry&gt;();
///     options.ExcludeType&lt;OutboxMessage&gt;();
/// });
/// </code>
/// </example>
public sealed class QueryCacheOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether query caching is enabled.
    /// </summary>
    /// <value>The default is <c>false</c> (opt-in).</value>
    /// <remarks>
    /// When disabled, the query cache interceptor is a no-op and adds no overhead to query execution.
    /// </remarks>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the default expiration time for cached query results.
    /// </summary>
    /// <value>The default is 5 minutes.</value>
    /// <remarks>
    /// Individual queries can override this value. Cached entries are also invalidated
    /// automatically when related entities are modified via <c>SaveChanges</c>.
    /// </remarks>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the key prefix for all query cache entries.
    /// </summary>
    /// <value>The default is <c>"sm:qc"</c> (Encina query cache).</value>
    /// <remarks>
    /// Use a unique prefix to avoid collisions with other cache entries.
    /// The full cache key format is: <c>{KeyPrefix}:{hash}</c>.
    /// </remarks>
    public string KeyPrefix { get; set; } = "sm:qc";

    /// <summary>
    /// Gets the set of entity type names excluded from query caching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Queries involving excluded entity types will never be cached. This is useful for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>High-write entities where caching provides little benefit (e.g., audit logs)</description></item>
    /// <item><description>Infrastructure entities managed by Encina (e.g., outbox messages)</description></item>
    /// <item><description>Entities with real-time requirements where stale data is unacceptable</description></item>
    /// </list>
    /// <para>
    /// Use <see cref="ExcludeType{TEntity}"/> to add types to this set.
    /// </para>
    /// </remarks>
    public HashSet<string> ExcludedEntityTypes { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to throw on cache errors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>false</c> (default), cache errors are swallowed and the query executes against
    /// the database as if caching were not configured. This provides resilient degradation
    /// when the cache backend is unavailable.
    /// </para>
    /// <para>
    /// When <c>true</c>, cache errors propagate as exceptions, which is useful for
    /// development and testing to detect misconfigurations early.
    /// </para>
    /// </remarks>
    /// <value>The default is <c>false</c> (resilient mode).</value>
    public bool ThrowOnCacheErrors { get; set; }

    /// <summary>
    /// Excludes an entity type from query caching.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to exclude.</typeparam>
    /// <returns>This instance for fluent configuration chaining.</returns>
    /// <remarks>
    /// Queries involving the excluded entity type will always execute against the database.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.ExcludeType&lt;AuditLogEntry&gt;()
    ///        .ExcludeType&lt;OutboxMessage&gt;()
    ///        .ExcludeType&lt;InboxMessage&gt;();
    /// </code>
    /// </example>
    public QueryCacheOptions ExcludeType<TEntity>()
    {
        ExcludedEntityTypes.Add(typeof(TEntity).Name);
        return this;
    }
}
