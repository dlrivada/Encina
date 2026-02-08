namespace Encina.Messaging.Caching;

/// <summary>
/// Messaging-level configuration options for EF Core query caching.
/// </summary>
/// <remarks>
/// <para>
/// These options control the basic query cache behavior at the messaging configuration level.
/// They are mapped to the provider-specific <c>QueryCacheOptions</c> (in <c>Encina.EntityFrameworkCore.Caching</c>)
/// when using <c>AddEncinaEntityFrameworkCore</c>.
/// </para>
/// <para>
/// For advanced configuration (e.g., excluding specific entity types), use the provider-specific
/// options via <c>services.Configure&lt;QueryCacheOptions&gt;()</c> or the standalone
/// <c>services.AddQueryCaching(options =&gt; ...)</c> extension method.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseQueryCache = true;
///     config.QueryCacheOptions.DefaultExpiration = TimeSpan.FromMinutes(10);
///     config.QueryCacheOptions.KeyPrefix = "myapp:qc";
///     config.QueryCacheOptions.ThrowOnCacheErrors = false;
/// });
/// </code>
/// </example>
public sealed class QueryCacheMessagingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether query caching is enabled.
    /// </summary>
    /// <value>The default is <c>true</c> when the feature is opted-in via <c>UseQueryCache = true</c>.</value>
    /// <remarks>
    /// When disabled, the query cache interceptor is a no-op and adds no overhead to query execution.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default expiration time for cached query results.
    /// </summary>
    /// <value>The default is 5 minutes.</value>
    /// <remarks>
    /// Cached entries are also invalidated automatically when related entities
    /// are modified via <c>SaveChanges</c>.
    /// </remarks>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the key prefix for all query cache entries.
    /// </summary>
    /// <value>The default is <c>"sm:qc"</c> (Encina query cache).</value>
    /// <remarks>
    /// Use a unique prefix to avoid collisions with other cache entries.
    /// </remarks>
    public string KeyPrefix { get; set; } = "sm:qc";

    /// <summary>
    /// Gets or sets a value indicating whether to throw on cache errors.
    /// </summary>
    /// <value>The default is <c>false</c> (resilient mode).</value>
    /// <remarks>
    /// <para>
    /// When <c>false</c> (default), cache errors are swallowed and the query executes against
    /// the database normally, providing resilient degradation.
    /// </para>
    /// <para>
    /// When <c>true</c>, cache errors propagate as exceptions for early detection during development.
    /// </para>
    /// </remarks>
    public bool ThrowOnCacheErrors { get; set; }
}
