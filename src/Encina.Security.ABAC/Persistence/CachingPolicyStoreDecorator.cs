using Encina.Caching;
using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Security.ABAC.Persistence;

/// <summary>
/// Decorator that wraps an <see cref="IPolicyStore"/> with cache-aside reads
/// (via <see cref="ICacheProvider.GetOrSetAsync{T}"/> for stampede protection) and
/// write-through invalidation on mutations.
/// </summary>
/// <remarks>
/// <para>
/// <b>Read operations</b> use the cache-aside pattern: on cache miss, the inner store is
/// queried and the result is cached for <see cref="PolicyCachingOptions.Duration"/>. The
/// <see cref="ICacheProvider.GetOrSetAsync{T}"/> method provides stampede protection —
/// concurrent cold-cache requests result in a single database query.
/// </para>
/// <para>
/// <b>Write operations</b> use write-through invalidation: persist to the inner store first,
/// then remove the specific and bulk cache keys, and optionally publish a
/// <see cref="PolicyCacheInvalidationMessage"/> to the PubSub channel for cross-instance
/// cache eviction.
/// </para>
/// <para>
/// <b>Count and existence</b> operations delegate directly to the inner store without caching,
/// as they are primarily used by health checks and need fresh data.
/// </para>
/// <para>
/// <b>Resilience</b>: all cache operations are wrapped in try/catch. Cache failures are logged
/// at <c>Warning</c> level and the decorator falls back to the inner store — cache errors
/// never propagate to the caller.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typically created by DI inside AddEncinaABAC when PolicyCaching.Enabled = true:
/// var decorator = new CachingPolicyStoreDecorator(
///     innerStore, cacheProvider, pubSubProvider, options.PolicyCaching, logger);
/// </code>
/// </example>
public sealed partial class CachingPolicyStoreDecorator : IPolicyStore
{
    private readonly IPolicyStore _inner;
    private readonly ICacheProvider _cache;
    private readonly IPubSubProvider? _pubSub;
    private readonly PolicyCachingOptions _options;
    private readonly ILogger<CachingPolicyStoreDecorator> _logger;

    // ── Cache Key Builders ─────────────────────────────────────────

    private string AllPolicySetsKey => $"{_options.CacheKeyPrefix}:policy-sets:all";
    private string AllPoliciesKey => $"{_options.CacheKeyPrefix}:policies:all";
    private string PolicySetKey(string id) => $"{_options.CacheKeyPrefix}:policy-set:{id}";
    private string PolicyKey(string id) => $"{_options.CacheKeyPrefix}:policy:{id}";

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingPolicyStoreDecorator"/> class.
    /// </summary>
    /// <param name="inner">The inner policy store to decorate.</param>
    /// <param name="cache">The cache provider for read/write operations.</param>
    /// <param name="pubSub">
    /// Optional pub/sub provider for cross-instance invalidation.
    /// When <c>null</c>, PubSub invalidation is silently skipped.
    /// </param>
    /// <param name="options">The policy caching configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public CachingPolicyStoreDecorator(
        IPolicyStore inner,
        ICacheProvider cache,
        IPubSubProvider? pubSub,
        PolicyCachingOptions options,
        ILogger<CachingPolicyStoreDecorator> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _cache = cache;
        _pubSub = pubSub;
        _options = options;
        _logger = logger;
    }

    // ── PolicySet Read Operations (Cache-Aside) ────────────────────

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<PolicySet>>> GetAllPolicySetsAsync(
        CancellationToken cancellationToken = default)
    {
        return await CacheAsideReadAsync(
            AllPolicySetsKey,
            ct => _inner.GetAllPolicySetsAsync(ct),
            nameof(GetAllPolicySetsAsync),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<PolicySet>>> GetPolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
    {
        return await CacheAsideOptionReadAsync(
            PolicySetKey(policySetId),
            ct => _inner.GetPolicySetAsync(policySetId, ct),
            nameof(GetPolicySetAsync),
            cancellationToken).ConfigureAwait(false);
    }

    // ── PolicySet Write Operations (Write-Through + Invalidation) ──

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> SavePolicySetAsync(
        PolicySet policySet,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.SavePolicySetAsync(policySet, cancellationToken).ConfigureAwait(false);

        if (result.IsRight)
        {
            await InvalidateCacheAsync(
                "PolicySet", policySet.Id,
                [PolicySetKey(policySet.Id), AllPolicySetsKey],
                "Save", cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> DeletePolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.DeletePolicySetAsync(policySetId, cancellationToken).ConfigureAwait(false);

        if (result.IsRight)
        {
            await InvalidateCacheAsync(
                "PolicySet", policySetId,
                [PolicySetKey(policySetId), AllPolicySetsKey],
                "Delete", cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    // ── PolicySet Pass-Through Operations ──────────────────────────

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, bool>> ExistsPolicySetAsync(
        string policySetId,
        CancellationToken cancellationToken = default)
        => _inner.ExistsPolicySetAsync(policySetId, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, int>> GetPolicySetCountAsync(
        CancellationToken cancellationToken = default)
        => _inner.GetPolicySetCountAsync(cancellationToken);

    // ── Standalone Policy Read Operations (Cache-Aside) ────────────

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, IReadOnlyList<Policy>>> GetAllStandalonePoliciesAsync(
        CancellationToken cancellationToken = default)
    {
        return await CacheAsideReadAsync(
            AllPoliciesKey,
            ct => _inner.GetAllStandalonePoliciesAsync(ct),
            nameof(GetAllStandalonePoliciesAsync),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Option<Policy>>> GetPolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        return await CacheAsideOptionReadAsync(
            PolicyKey(policyId),
            ct => _inner.GetPolicyAsync(policyId, ct),
            nameof(GetPolicyAsync),
            cancellationToken).ConfigureAwait(false);
    }

    // ── Standalone Policy Write Operations (Write-Through) ─────────

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> SavePolicyAsync(
        Policy policy,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.SavePolicyAsync(policy, cancellationToken).ConfigureAwait(false);

        if (result.IsRight)
        {
            await InvalidateCacheAsync(
                "Policy", policy.Id,
                [PolicyKey(policy.Id), AllPoliciesKey],
                "Save", cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, Unit>> DeletePolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default)
    {
        var result = await _inner.DeletePolicyAsync(policyId, cancellationToken).ConfigureAwait(false);

        if (result.IsRight)
        {
            await InvalidateCacheAsync(
                "Policy", policyId,
                [PolicyKey(policyId), AllPoliciesKey],
                "Delete", cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    // ── Standalone Policy Pass-Through Operations ──────────────────

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, bool>> ExistsPolicyAsync(
        string policyId,
        CancellationToken cancellationToken = default)
        => _inner.ExistsPolicyAsync(policyId, cancellationToken);

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, int>> GetPolicyCountAsync(
        CancellationToken cancellationToken = default)
        => _inner.GetPolicyCountAsync(cancellationToken);

    // ── Cache-Aside Helpers ────────────────────────────────────────

    /// <summary>
    /// Cache-aside read using <see cref="ICacheProvider.GetOrSetAsync{T}"/> for stampede
    /// protection. If the inner store returns an error, the error is propagated without
    /// caching. Cache infrastructure failures fall back to the inner store.
    /// </summary>
    private async ValueTask<Either<EncinaError, T>> CacheAsideReadAsync<T>(
        string cacheKey,
        Func<CancellationToken, ValueTask<Either<EncinaError, T>>> factory,
        string operationName,
        CancellationToken cancellationToken) where T : class
    {
        try
        {
            var value = await _cache.GetOrSetAsync(
                cacheKey,
                async ct =>
                {
                    var result = await factory(ct).ConfigureAwait(false);
                    return result.Match(
                        Right: v => v,
                        Left: e => throw new StoreResultException(e));
                },
                _options.Duration,
                cancellationToken).ConfigureAwait(false);

            return value;
        }
        catch (StoreResultException ex)
        {
            // Inner store returned Left(error) — propagate without caching
            return ex.Error;
        }
        catch (Exception ex)
        {
            // Cache infrastructure failure — fallback to inner store directly
            LogCacheError(_logger, operationName, cacheKey, ex);
            return await factory(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Cache-aside read for <see cref="Option{T}"/> results. Uses manual Get/Set to avoid
    /// serialization issues with LanguageExt <c>Option&lt;T&gt;</c>. Only caches <c>Some</c>
    /// results — <c>None</c> results are not cached, allowing the next read to query the store.
    /// </summary>
    private async ValueTask<Either<EncinaError, Option<T>>> CacheAsideOptionReadAsync<T>(
        string cacheKey,
        Func<CancellationToken, ValueTask<Either<EncinaError, Option<T>>>> factory,
        string operationName,
        CancellationToken cancellationToken) where T : class
    {
        // 1. Try cache read
        try
        {
            var cached = await _cache.GetAsync<T>(cacheKey, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                LogCacheHit(_logger, operationName, cacheKey);
                return Some(cached);
            }
        }
        catch (Exception ex)
        {
            LogCacheError(_logger, operationName, cacheKey, ex);
        }

        // 2. Cache miss — load from inner store
        var result = await factory(cancellationToken).ConfigureAwait(false);

        // 3. Cache successful Some results only
        if (result.IsRight)
        {
            var option = result.Match(Right: v => v, Left: _ => Option<T>.None);
            if (option.IsSome)
            {
                var value = option.Match(Some: v => v, None: () => default!);
                await TryCacheAsync(cacheKey, value, cancellationToken).ConfigureAwait(false);
            }
        }

        return result;
    }

    // ── Write Helpers ──────────────────────────────────────────────

    private async ValueTask TryCacheAsync<T>(string key, T value, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.SetAsync(key, value, _options.Duration, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogCacheWriteError(_logger, key, ex);
        }
    }

    /// <summary>
    /// Invalidates local cache keys and optionally publishes a PubSub message for
    /// cross-instance invalidation.
    /// </summary>
    private async ValueTask InvalidateCacheAsync(
        string entityType,
        string entityId,
        string[] cacheKeys,
        string operation,
        CancellationToken cancellationToken)
    {
        // 1. Local cache invalidation
        foreach (var key in cacheKeys)
        {
            try
            {
                await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogCacheInvalidationError(_logger, key, ex);
            }
        }

        LogCacheInvalidated(_logger, entityType, entityId, operation);

        // 2. Cross-instance PubSub invalidation
        if (_options.EnablePubSubInvalidation && _pubSub is not null)
        {
            try
            {
                var message = new PolicyCacheInvalidationMessage(
                    entityType, entityId, operation, DateTime.UtcNow);

                await _pubSub.PublishAsync(
                    _options.InvalidationChannel,
                    message,
                    cancellationToken).ConfigureAwait(false);

                LogPubSubPublished(_logger, entityType, entityId, _options.InvalidationChannel);
            }
            catch (Exception ex)
            {
                LogPubSubError(_logger, entityType, entityId, _options.InvalidationChannel, ex);
            }
        }
    }

    // ── Internal Control Flow ──────────────────────────────────────

    /// <summary>
    /// Internal exception used for control flow within <see cref="CacheAsideReadAsync{T}"/>
    /// when the inner store returns an error. This allows <see cref="ICacheProvider.GetOrSetAsync{T}"/>
    /// to skip caching the error result.
    /// </summary>
    private sealed class StoreResultException(EncinaError error) : Exception
    {
        public EncinaError Error { get; } = error;
    }

    // ── LoggerMessage Source Generators ─────────────────────────────

    [LoggerMessage(
        EventId = 1, Level = LogLevel.Debug,
        Message = "ABAC policy cache hit for {Operation} with key {CacheKey}")]
    private static partial void LogCacheHit(
        ILogger logger, string operation, string cacheKey);

    [LoggerMessage(
        EventId = 2, Level = LogLevel.Warning,
        Message = "ABAC policy cache read error for {Operation} with key {CacheKey}")]
    private static partial void LogCacheError(
        ILogger logger, string operation, string cacheKey, Exception exception);

    [LoggerMessage(
        EventId = 3, Level = LogLevel.Warning,
        Message = "ABAC policy cache write error for key {CacheKey}")]
    private static partial void LogCacheWriteError(
        ILogger logger, string cacheKey, Exception exception);

    [LoggerMessage(
        EventId = 4, Level = LogLevel.Debug,
        Message = "ABAC policy cache invalidated for {EntityType}:{EntityId} (operation: {Operation})")]
    private static partial void LogCacheInvalidated(
        ILogger logger, string entityType, string entityId, string operation);

    [LoggerMessage(
        EventId = 5, Level = LogLevel.Warning,
        Message = "ABAC policy cache invalidation error for key {CacheKey}")]
    private static partial void LogCacheInvalidationError(
        ILogger logger, string cacheKey, Exception exception);

    [LoggerMessage(
        EventId = 6, Level = LogLevel.Debug,
        Message = "ABAC policy cache invalidation published for {EntityType}:{EntityId} to channel {Channel}")]
    private static partial void LogPubSubPublished(
        ILogger logger, string entityType, string entityId, string channel);

    [LoggerMessage(
        EventId = 7, Level = LogLevel.Warning,
        Message = "ABAC policy PubSub publish error for {EntityType}:{EntityId} on channel {Channel}")]
    private static partial void LogPubSubError(
        ILogger logger, string entityType, string entityId, string channel, Exception exception);
}
