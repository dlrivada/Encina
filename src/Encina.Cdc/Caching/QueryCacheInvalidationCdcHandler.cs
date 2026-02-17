using System.Text.Json;
using Encina.Caching;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Caching.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.Cdc.Caching;

/// <summary>
/// CDC handler that invalidates query cache entries when database changes are detected.
/// Supports local cache invalidation via <see cref="ICacheProvider"/> and cross-instance
/// broadcast via <see cref="IPubSubProvider"/>.
/// </summary>
/// <remarks>
/// <para>
/// This handler complements the existing <c>QueryCacheInterceptor</c> by detecting changes
/// from any source: other application instances, direct SQL updates, database migrations,
/// and external microservices. When a change is detected, matching cache entries are
/// invalidated using the pattern <c>{prefix}:*:{entityType}:*</c>.
/// </para>
/// <para>
/// The handler is designed to be resilient: cache invalidation failures are logged but
/// do not block the CDC pipeline. This ensures that a temporary cache or pub/sub outage
/// does not prevent CDC from processing subsequent events.
/// </para>
/// </remarks>
internal sealed class QueryCacheInvalidationCdcHandler : IChangeEventHandler<JsonElement>
{
    private readonly ICacheProvider _cacheProvider;
    private readonly IPubSubProvider? _pubSubProvider;
    private readonly QueryCacheInvalidationOptions _options;
    private readonly ILogger<QueryCacheInvalidationCdcHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryCacheInvalidationCdcHandler"/> class.
    /// </summary>
    /// <param name="cacheProvider">The cache provider for local cache invalidation.</param>
    /// <param name="pubSubProvider">Optional pub/sub provider for cross-instance broadcast.</param>
    /// <param name="options">Configuration options for cache invalidation behavior.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public QueryCacheInvalidationCdcHandler(
        ICacheProvider cacheProvider,
        IPubSubProvider? pubSubProvider,
        IOptions<QueryCacheInvalidationOptions> options,
        ILogger<QueryCacheInvalidationCdcHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _cacheProvider = cacheProvider;
        _pubSubProvider = pubSubProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(
        JsonElement entity, ChangeContext context)
    {
        return await InvalidateCacheForTableAsync(context, "insert").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(
        JsonElement before, JsonElement after, ChangeContext context)
    {
        return await InvalidateCacheForTableAsync(context, "update").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(
        JsonElement entity, ChangeContext context)
    {
        return await InvalidateCacheForTableAsync(context, "delete").ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, Unit>> InvalidateCacheForTableAsync(
        ChangeContext context, string operation)
    {
        var tableName = context.TableName;

        // Start tracing activity for the entire invalidation flow.
        // Activity is disposed by the completion/failed/skipped methods.
        var activity = CacheInvalidationActivitySource.StartInvalidation(tableName, operation);

        // Apply table filtering: skip if Tables is configured and table not in set
        if (_options.Tables is not null &&
            !_options.Tables.Contains(tableName, StringComparer.OrdinalIgnoreCase))
        {
            CdcCacheInvalidationLog.TableFilteredOut(_logger, tableName);
            CacheInvalidationActivitySource.InvalidationSkipped(activity);
            return Right(unit);
        }

        // Resolve entity type from table name
        var entityType = CdcTableNameResolver.ResolveEntityType(
            tableName, _options.TableToEntityTypeMappings);

        // Generate cache key pattern: {prefix}:*:{entityType}:*
        var pattern = $"{_options.CacheKeyPrefix}:*:{entityType}:*";

        CacheInvalidationActivitySource.SetResolution(activity, entityType, pattern);
        CdcCacheInvalidationLog.InvalidatingCache(_logger, tableName, entityType, pattern);

        // Local cache invalidation
        try
        {
            await _cacheProvider.RemoveByPatternAsync(pattern, context.CancellationToken)
                .ConfigureAwait(false);
            CdcCacheInvalidationLog.CacheInvalidated(_logger, entityType);
            CacheInvalidationMetrics.RecordInvalidation(tableName, operation);
        }
        catch (Exception ex)
        {
            CdcCacheInvalidationLog.CacheInvalidationFailed(_logger, ex, tableName);
            CacheInvalidationMetrics.RecordError(tableName, operation, "cache_failure");
            CacheInvalidationActivitySource.InvalidationFailed(activity, ex.Message);
            // Do not block CDC pipeline - log and continue
            return Right(unit);
        }

        // Broadcast to other instances via pub/sub
        var broadcastSent = false;
        if (_options.UsePubSubBroadcast && _pubSubProvider is not null)
        {
            var broadcastActivity = CacheInvalidationActivitySource.StartBroadcast(
                _options.PubSubChannel, pattern);

            try
            {
                CdcCacheInvalidationLog.BroadcastingInvalidation(
                    _logger, pattern, _options.PubSubChannel);

                await _pubSubProvider.PublishAsync(
                    _options.PubSubChannel, pattern, context.CancellationToken)
                    .ConfigureAwait(false);

                broadcastSent = true;
                CacheInvalidationMetrics.RecordBroadcast(tableName, operation);
                CacheInvalidationActivitySource.CompleteBroadcast(broadcastActivity, isSuccess: true);
            }
            catch (Exception ex)
            {
                CdcCacheInvalidationLog.PubSubBroadcastFailed(_logger, ex, pattern);
                CacheInvalidationMetrics.RecordError(tableName, operation, "broadcast_failure");
                CacheInvalidationActivitySource.CompleteBroadcast(
                    broadcastActivity, isSuccess: false, ex.Message);
                // Do not block CDC pipeline - log and continue
            }
        }

        CacheInvalidationActivitySource.InvalidationCompleted(activity, broadcastSent);
        return Right(unit);
    }
}
