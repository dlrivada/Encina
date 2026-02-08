using System.Data.Common;
using Encina.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.EntityFrameworkCore.Caching;

/// <summary>
/// EF Core interceptor that provides second-level query caching by intercepting
/// <see cref="DbCommand"/> execution and <c>SaveChanges</c> operations.
/// </summary>
/// <remarks>
/// <para>
/// <b>Read Caching</b>: During query execution (<see cref="ReaderExecutingAsync"/>),
/// the interceptor generates a cache key from the SQL command, checks the cache for
/// existing results, and short-circuits database execution on cache hits by returning a
/// <see cref="CachedDataReader"/>. On cache misses, the executed result is captured in
/// <see cref="ReaderExecutedAsync"/>, serialized as a <see cref="CachedQueryResult"/>,
/// and stored in the cache for subsequent requests.
/// </para>
/// <para>
/// <b>Write Invalidation</b>: When <c>SaveChanges</c> is called, the interceptor
/// identifies modified entity types from the <see cref="ChangeTracker"/> and invalidates
/// all cached queries involving those entity types using pattern-based cache removal.
/// </para>
/// <para>
/// <b>Error Handling</b>: Cache errors are handled according to the
/// <see cref="QueryCacheOptions.ThrowOnCacheErrors"/> setting. By default, cache failures
/// are logged and the query falls through to normal database execution, ensuring the
/// application remains functional even when the cache backend is unavailable.
/// </para>
/// <para>
/// <b>Multi-Tenancy</b>: When an <see cref="IRequestContext"/> is available with a
/// <c>TenantId</c>, cache keys include tenant isolation to prevent cross-tenant data leaks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registration via service collection
/// services.AddEncinaEntityFrameworkCore&lt;AppDbContext&gt;(config =>
/// {
///     config.UseQueryCache = true;
///     config.QueryCacheOptions.DefaultExpiration = TimeSpan.FromMinutes(10);
///     config.QueryCacheOptions.ExcludeType&lt;AuditLog&gt;();
/// });
///
/// // The interceptor is added automatically to DbContext interceptors
/// </code>
/// </example>
public sealed class QueryCacheInterceptor : DbCommandInterceptor, ISaveChangesInterceptor
{
    private readonly ICacheProvider _cacheProvider;
    private readonly IQueryCacheKeyGenerator _keyGenerator;
    private readonly QueryCacheOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueryCacheInterceptor> _logger;

    // Thread-safe storage for the pending cache key generated during ReaderExecuting,
    // consumed by ReaderExecuted to store the result in cache.
    private static readonly AsyncLocal<QueryCacheKey?> PendingCacheKey = new();

    // Thread-safe storage for entity types modified during SaveChanges,
    // used in SavedChanges to invalidate affected cache entries.
    private static readonly AsyncLocal<HashSet<string>?> PendingInvalidations = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryCacheInterceptor"/> class.
    /// </summary>
    /// <param name="cacheProvider">The cache provider for storing and retrieving cached query results.</param>
    /// <param name="keyGenerator">The cache key generator for creating deterministic cache keys from SQL commands.</param>
    /// <param name="options">The query cache configuration options.</param>
    /// <param name="serviceProvider">The service provider for resolving optional dependencies like <c>IRequestContext</c>.</param>
    /// <param name="logger">The logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c>.</exception>
    public QueryCacheInterceptor(
        ICacheProvider cacheProvider,
        IQueryCacheKeyGenerator keyGenerator,
        IOptions<QueryCacheOptions> options,
        IServiceProvider serviceProvider,
        ILogger<QueryCacheInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(keyGenerator);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _cacheProvider = cacheProvider;
        _keyGenerator = keyGenerator;
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    // ──────────────────────────────────────────────
    //  Read Caching: ReaderExecuting (cache lookup)
    // ──────────────────────────────────────────────

    /// <inheritdoc/>
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        if (!ShouldCache(eventData))
        {
            return base.ReaderExecuting(command, eventData, result);
        }

        var cacheKey = GenerateCacheKey(command, eventData.Context!);

        if (IsExcluded(cacheKey))
        {
            return base.ReaderExecuting(command, eventData, result);
        }

        try
        {
            // Synchronous cache lookup — GetAsync().GetAwaiter().GetResult() is not ideal
            // but DbCommandInterceptor requires a sync path. Most cache providers support
            // sync access internally. The async path (ReaderExecutingAsync) is preferred.
            var cached = _cacheProvider.GetAsync<CachedQueryResult>(cacheKey.Key, CancellationToken.None)
                .GetAwaiter().GetResult();

            if (cached is not null)
            {
                QueryCacheLog.CacheHit(_logger, cacheKey.Key);
                return InterceptionResult<DbDataReader>.SuppressWithResult(
                    new CachedDataReader(cached));
            }
        }
        catch (Exception ex)
        {
            HandleCacheError("cache lookup", cacheKey.Key, ex);
        }

        // Cache miss — store the key for ReaderExecuted to populate
        PendingCacheKey.Value = cacheKey;
        QueryCacheLog.CacheMiss(_logger, cacheKey.Key);

        return base.ReaderExecuting(command, eventData, result);
    }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        if (!ShouldCache(eventData))
        {
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken)
                .ConfigureAwait(false);
        }

        var cacheKey = GenerateCacheKey(command, eventData.Context!);

        if (IsExcluded(cacheKey))
        {
            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            var cached = await _cacheProvider.GetAsync<CachedQueryResult>(
                cacheKey.Key, cancellationToken).ConfigureAwait(false);

            if (cached is not null)
            {
                QueryCacheLog.CacheHit(_logger, cacheKey.Key);
                return InterceptionResult<DbDataReader>.SuppressWithResult(
                    new CachedDataReader(cached));
            }
        }
        catch (Exception ex)
        {
            HandleCacheError("cache lookup", cacheKey.Key, ex);
        }

        // Cache miss — store the key for ReaderExecuted to populate
        PendingCacheKey.Value = cacheKey;
        QueryCacheLog.CacheMiss(_logger, cacheKey.Key);

        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken)
            .ConfigureAwait(false);
    }

    // ──────────────────────────────────────────────
    //  Read Caching: ReaderExecuted (cache population)
    // ──────────────────────────────────────────────

    /// <inheritdoc/>
    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        var cacheKey = PendingCacheKey.Value;
        PendingCacheKey.Value = null;

        if (cacheKey is null)
        {
            return base.ReaderExecuted(command, eventData, result);
        }

        try
        {
            var cachedResult = MaterializeReader(result);
            _cacheProvider.SetAsync(cacheKey.Key, cachedResult, _options.DefaultExpiration, CancellationToken.None)
                .GetAwaiter().GetResult();

            QueryCacheLog.CachePopulated(_logger, cacheKey.Key, cachedResult.Rows.Count);

            return new CachedDataReader(cachedResult);
        }
        catch (Exception ex)
        {
            HandleCacheError("cache population", cacheKey.Key, ex);
            return base.ReaderExecuted(command, eventData, result);
        }
    }

    /// <inheritdoc/>
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = PendingCacheKey.Value;
        PendingCacheKey.Value = null;

        if (cacheKey is null)
        {
            return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            var cachedResult = await MaterializeReaderAsync(result, cancellationToken)
                .ConfigureAwait(false);

            await _cacheProvider.SetAsync(
                cacheKey.Key, cachedResult, _options.DefaultExpiration, cancellationToken)
                .ConfigureAwait(false);

            QueryCacheLog.CachePopulated(_logger, cacheKey.Key, cachedResult.Rows.Count);

            return new CachedDataReader(cachedResult);
        }
        catch (Exception ex)
        {
            HandleCacheError("cache population", cacheKey.Key, ex);
            return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    // ──────────────────────────────────────────────
    //  Write Invalidation: SaveChanges
    // ──────────────────────────────────────────────

    /// <inheritdoc/>
    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureAffectedEntityTypes(eventData.Context);
        return result;
    }

    /// <inheritdoc/>
    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureAffectedEntityTypes(eventData.Context);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc/>
    public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        InvalidateAffectedCacheEntriesSync();
        return result;
    }

    /// <inheritdoc/>
    public async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await InvalidateAffectedCacheEntriesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc/>
    public void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        // Clear pending invalidations on failure — the save didn't succeed,
        // so the cache should not be invalidated.
        PendingInvalidations.Value = null;
    }

    /// <inheritdoc/>
    public Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        PendingInvalidations.Value = null;
        return Task.CompletedTask;
    }

    // ──────────────────────────────────────────────
    //  Private helpers: Cache key & filtering
    // ──────────────────────────────────────────────

    /// <summary>
    /// Determines whether the current command should be cached.
    /// </summary>
    private bool ShouldCache(CommandEventData eventData)
    {
        return _options.Enabled && eventData.Context is not null;
    }

    /// <summary>
    /// Generates a cache key from the command and context, optionally including
    /// tenant information when <see cref="IRequestContext"/> is available.
    /// </summary>
    private QueryCacheKey GenerateCacheKey(DbCommand command, DbContext context)
    {
        var requestContext = ResolveRequestContext();

        return requestContext is not null
            ? _keyGenerator.Generate(command, context, requestContext)
            : _keyGenerator.Generate(command, context);
    }

    /// <summary>
    /// Checks whether any of the entity types in the cache key are excluded from caching.
    /// </summary>
    private bool IsExcluded(QueryCacheKey cacheKey)
    {
        if (_options.ExcludedEntityTypes.Count == 0)
        {
            return false;
        }

        foreach (var entityType in cacheKey.EntityTypes)
        {
            if (_options.ExcludedEntityTypes.Contains(entityType))
            {
                QueryCacheLog.EntityTypeExcluded(_logger, entityType);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves the current <see cref="IRequestContext"/> from the service provider,
    /// using reflection to avoid a hard dependency on <c>Encina.AspNetCore</c>.
    /// </summary>
    private IRequestContext? ResolveRequestContext()
    {
        try
        {
            // Direct resolution — works within Encina pipeline
            var requestContext = _serviceProvider.GetService(typeof(IRequestContext)) as IRequestContext;
            if (requestContext is not null)
            {
                return requestContext;
            }

            // Reflection-based resolution — works with ASP.NET Core middleware
            var accessorType = Type.GetType("Encina.AspNetCore.IRequestContextAccessor, Encina.AspNetCore");
            if (accessorType is not null)
            {
                var accessor = _serviceProvider.GetService(accessorType);
                if (accessor is not null)
                {
                    var property = accessorType.GetProperty("RequestContext");
                    return property?.GetValue(accessor) as IRequestContext;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            QueryCacheLog.FailedToResolveRequestContext(_logger, ex);
            return null;
        }
    }

    // ──────────────────────────────────────────────
    //  Private helpers: Reader materialization
    // ──────────────────────────────────────────────

    /// <summary>
    /// Materializes a <see cref="DbDataReader"/> into a <see cref="CachedQueryResult"/>
    /// by reading all rows synchronously and capturing column schema.
    /// </summary>
    private static CachedQueryResult MaterializeReader(DbDataReader reader)
    {
        var columns = CaptureColumnSchema(reader);
        var rows = new List<object?[]>();

        while (reader.Read())
        {
            var values = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                values[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(values);
        }

        return new CachedQueryResult
        {
            Columns = columns,
            Rows = rows,
            CachedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Materializes a <see cref="DbDataReader"/> into a <see cref="CachedQueryResult"/>
    /// by reading all rows asynchronously and capturing column schema.
    /// </summary>
    private static async Task<CachedQueryResult> MaterializeReaderAsync(
        DbDataReader reader,
        CancellationToken cancellationToken)
    {
        var columns = CaptureColumnSchema(reader);
        var rows = new List<object?[]>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var values = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                values[i] = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false)
                    ? null
                    : reader.GetValue(i);
            }

            rows.Add(values);
        }

        return new CachedQueryResult
        {
            Columns = columns,
            Rows = rows,
            CachedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Captures column schema metadata from the reader for serialization.
    /// </summary>
    private static List<CachedColumnSchema> CaptureColumnSchema(DbDataReader reader)
    {
        var columns = new List<CachedColumnSchema>(reader.FieldCount);

        for (var i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(new CachedColumnSchema(
                Name: reader.GetName(i),
                Ordinal: i,
                DataTypeName: reader.GetDataTypeName(i),
                FieldType: reader.GetFieldType(i).AssemblyQualifiedName ?? reader.GetFieldType(i).FullName ?? "System.Object",
                AllowDBNull: true)); // Default to nullable; exact nullability requires GetSchemaTable()
        }

        return columns;
    }

    // ──────────────────────────────────────────────
    //  Private helpers: Cache invalidation
    // ──────────────────────────────────────────────

    /// <summary>
    /// Captures entity types that have been modified in the current <c>SaveChanges</c> operation.
    /// </summary>
    private void CaptureAffectedEntityTypes(DbContext? context)
    {
        if (!_options.Enabled || context is null)
        {
            return;
        }

        var affectedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                affectedTypes.Add(entry.Metadata.ClrType.Name);
            }
        }

        if (affectedTypes.Count > 0)
        {
            PendingInvalidations.Value = affectedTypes;
            QueryCacheLog.CapturedEntityTypesForInvalidation(_logger, affectedTypes.Count);
        }
    }

    /// <summary>
    /// Invalidates cached queries for all affected entity types (synchronous path).
    /// </summary>
    private void InvalidateAffectedCacheEntriesSync()
    {
        var affectedTypes = PendingInvalidations.Value;
        PendingInvalidations.Value = null;

        if (affectedTypes is null or { Count: 0 })
        {
            return;
        }

        foreach (var entityType in affectedTypes)
        {
            try
            {
                var pattern = $"{_options.KeyPrefix}:*:{entityType}:*";
                _cacheProvider.RemoveByPatternAsync(pattern, CancellationToken.None)
                    .GetAwaiter().GetResult();
                QueryCacheLog.CacheInvalidated(_logger, entityType, pattern);
            }
            catch (Exception ex)
            {
                HandleCacheError("cache invalidation", entityType, ex);
            }
        }
    }

    /// <summary>
    /// Invalidates cached queries for all affected entity types (asynchronous path).
    /// </summary>
    private async Task InvalidateAffectedCacheEntriesAsync(CancellationToken cancellationToken)
    {
        var affectedTypes = PendingInvalidations.Value;
        PendingInvalidations.Value = null;

        if (affectedTypes is null or { Count: 0 })
        {
            return;
        }

        foreach (var entityType in affectedTypes)
        {
            try
            {
                var pattern = $"{_options.KeyPrefix}:*:{entityType}:*";
                await _cacheProvider.RemoveByPatternAsync(pattern, cancellationToken)
                    .ConfigureAwait(false);
                QueryCacheLog.CacheInvalidated(_logger, entityType, pattern);
            }
            catch (Exception ex)
            {
                HandleCacheError("cache invalidation", entityType, ex);
            }
        }
    }

    // ──────────────────────────────────────────────
    //  Private helpers: Error handling
    // ──────────────────────────────────────────────

    /// <summary>
    /// Handles cache operation errors based on the configured error policy.
    /// </summary>
    /// <param name="operation">The cache operation that failed (for logging context).</param>
    /// <param name="key">The cache key or entity type involved.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <exception cref="InvalidOperationException">
    /// Re-thrown when <see cref="QueryCacheOptions.ThrowOnCacheErrors"/> is <c>true</c>.
    /// </exception>
    private void HandleCacheError(string operation, string key, Exception exception)
    {
        if (_options.ThrowOnCacheErrors)
        {
            throw new InvalidOperationException(
                $"Query cache {operation} failed for key '{key}'. " +
                "Set QueryCacheOptions.ThrowOnCacheErrors = false to swallow cache errors.",
                exception);
        }

        QueryCacheLog.CacheOperationFailed(_logger, operation, key, exception);
    }
}

// ──────────────────────────────────────────────
//  LoggerMessage source-generated log methods
// ──────────────────────────────────────────────

internal static partial class QueryCacheLog
{
    [LoggerMessage(
        EventId = 200,
        Level = LogLevel.Debug,
        Message = "Query cache hit for key '{CacheKey}'")]
    public static partial void CacheHit(ILogger logger, string cacheKey);

    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Debug,
        Message = "Query cache miss for key '{CacheKey}'")]
    public static partial void CacheMiss(ILogger logger, string cacheKey);

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Debug,
        Message = "Query cache populated for key '{CacheKey}' with {RowCount} rows")]
    public static partial void CachePopulated(ILogger logger, string cacheKey, int rowCount);

    [LoggerMessage(
        EventId = 203,
        Level = LogLevel.Debug,
        Message = "Query cache invalidated for entity type '{EntityType}' using pattern '{Pattern}'")]
    public static partial void CacheInvalidated(ILogger logger, string entityType, string pattern);

    [LoggerMessage(
        EventId = 204,
        Level = LogLevel.Debug,
        Message = "Captured {Count} entity types for cache invalidation")]
    public static partial void CapturedEntityTypesForInvalidation(ILogger logger, int count);

    [LoggerMessage(
        EventId = 205,
        Level = LogLevel.Debug,
        Message = "Entity type '{EntityType}' is excluded from query caching")]
    public static partial void EntityTypeExcluded(ILogger logger, string entityType);

    [LoggerMessage(
        EventId = 206,
        Level = LogLevel.Warning,
        Message = "Query cache {Operation} failed for key '{Key}'")]
    public static partial void CacheOperationFailed(
        ILogger logger, string operation, string key, Exception exception);

    [LoggerMessage(
        EventId = 207,
        Level = LogLevel.Warning,
        Message = "Failed to resolve IRequestContext for query cache key generation")]
    public static partial void FailedToResolveRequestContext(ILogger logger, Exception exception);
}
