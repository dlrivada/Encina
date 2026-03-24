using Encina.Caching;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Diagnostics;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Security.Secrets.Caching;

/// <summary>
/// Decorator that wraps an <see cref="ISecretReader"/> with cache-aside reads
/// via <see cref="ICacheProvider.GetOrSetAsync{T}"/> for distributed caching with
/// stampede protection, and optional last-known-good stale fallback for resilience.
/// </summary>
/// <remarks>
/// <para>
/// <b>Read operations</b> use <see cref="ICacheProvider.GetOrSetAsync{T}"/> for atomic
/// get-or-set semantics with stampede protection. The factory throws
/// <see cref="StoreResultException"/> on <c>Left</c> results so that errors are never
/// cached — only <c>Right</c> (successful) results are stored.
/// </para>
/// <para>
/// <b>Staleness fallback:</b> When resilience is enabled and a last-known-good value exists,
/// the decorator serves stale data instead of propagating resilience errors (circuit breaker
/// open, timeout, provider unavailable).
/// </para>
/// <para>
/// <b>Resilience:</b> All cache operations are wrapped in try/catch. Cache failures are logged
/// at <c>Warning</c> level and the decorator falls back to the inner reader — cache errors
/// never propagate to the caller.
/// </para>
/// <para>
/// <b>Cache keys:</b>
/// <list type="bullet">
/// <item><c>{prefix}:v:{name}</c> — String secret value</item>
/// <item><c>{prefix}:t:{name}:{typeName}</c> — Typed secret value</item>
/// <item><c>{prefix}:lkg:{name}</c> — Last-known-good (string)</item>
/// <item><c>{prefix}:lkg:t:{name}:{typeName}</c> — Last-known-good (typed)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered automatically by AddEncinaSecrets when EnableCaching is true:
/// services.AddEncinaSecrets(options =>
/// {
///     options.EnableCaching = true;
///     options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
///     options.EnableResilience = true;
///     options.Resilience.MaxStaleDuration = TimeSpan.FromHours(4);
/// });
/// </code>
/// </example>
public sealed class CachingSecretReaderDecorator : ISecretReader
{
    private readonly ISecretReader _inner;
    private readonly ICacheProvider _cache;
    private readonly SecretCachingOptions _cachingOptions;
    private readonly SecretsOptions _secretsOptions;
    private readonly ILogger<CachingSecretReaderDecorator> _logger;
    private readonly SecretsMetrics? _metrics;

    // ── Cache Key Builders ─────────────────────────────────────────

    private string ValueKey(string name) => $"{_cachingOptions.CacheKeyPrefix}:v:{name}";
    private string TypedKey(string name, string typeName) => $"{_cachingOptions.CacheKeyPrefix}:t:{name}:{typeName}";
    private string LkgKey(string name) => $"{_cachingOptions.CacheKeyPrefix}:lkg:{name}";
    private string TypedLkgKey(string name, string typeName) => $"{_cachingOptions.CacheKeyPrefix}:lkg:t:{name}:{typeName}";

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingSecretReaderDecorator"/> class.
    /// </summary>
    /// <param name="inner">The inner secret reader to wrap.</param>
    /// <param name="cache">The cache provider for read/write operations.</param>
    /// <param name="cachingOptions">The secrets caching configuration.</param>
    /// <param name="secretsOptions">The main secrets configuration (provides TTL and resilience settings).</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics recorder for cache and stale fallback telemetry.</param>
    public CachingSecretReaderDecorator(
        ISecretReader inner,
        ICacheProvider cache,
        SecretCachingOptions cachingOptions,
        SecretsOptions secretsOptions,
        ILogger<CachingSecretReaderDecorator> logger,
        SecretsMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(cachingOptions);
        ArgumentNullException.ThrowIfNull(secretsOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _cache = cache;
        _cachingOptions = cachingOptions;
        _secretsOptions = secretsOptions;
        _logger = logger;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        if (!_secretsOptions.EnableCaching)
        {
            return await _inner.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        }

        var cacheKey = ValueKey(secretName);
        var lkgKey = LkgKey(secretName);

        // 1. Check cache for hit detection
        try
        {
            var cached = await _cache.GetAsync<string>(cacheKey, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                Log.CacheHit(_logger, secretName);
                _metrics?.RecordCacheHit(secretName);
                return cached;
            }
        }
        catch (Exception ex)
        {
            // Cache read failure — fall through to inner reader via GetOrSetAsync
            Log.CacheError(_logger, secretName, cacheKey, ex);
        }

        // 2. Cache miss — use GetOrSetAsync for stampede protection
        Log.CacheMiss(_logger, secretName);
        _metrics?.RecordCacheMiss(secretName);

        try
        {
            var value = await _cache.GetOrSetAsync(
                cacheKey,
                async ct =>
                {
                    var result = await _inner.GetSecretAsync(secretName, ct).ConfigureAwait(false);
                    return result.Match(
                        Right: v =>
                        {
                            _ = StoreLastKnownGoodAsync(lkgKey, v, ct);
                            return v;
                        },
                        Left: e => throw new StoreResultException(e));
                },
                _secretsOptions.DefaultCacheDuration,
                cancellationToken).ConfigureAwait(false);

            return value;
        }
        catch (StoreResultException ex)
        {
            // Inner reader returned Left — check stale fallback
            if (IsResilienceError(ex.Error))
            {
                var stale = await TryGetLastKnownGoodAsync<string>(secretName, lkgKey, cancellationToken).ConfigureAwait(false);
                if (stale is not null)
                {
                    Log.CacheStaleFallbackServed(_logger, secretName);
                    _metrics?.RecordStaleFallback(secretName);
                    SecretsActivitySource.RecordStaleFallbackEvent(
                        System.Diagnostics.Activity.Current, secretName);
                    return stale;
                }
            }

            return ex.Error;
        }
        catch (Exception ex)
        {
            // Cache infrastructure failure — fallback to inner reader
            Log.CacheError(_logger, secretName, cacheKey, ex);
            return await _inner.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, T>> GetSecretAsync<T>(
        string secretName,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        if (!_secretsOptions.EnableCaching)
        {
            return await _inner.GetSecretAsync<T>(secretName, cancellationToken).ConfigureAwait(false);
        }

        var typeName = typeof(T).FullName ?? typeof(T).Name;
        var cacheKey = TypedKey(secretName, typeName);
        var lkgKey = TypedLkgKey(secretName, typeName);

        // 1. Check cache for hit detection
        try
        {
            var cached = await _cache.GetAsync<T>(cacheKey, cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                Log.CacheHit(_logger, secretName);
                _metrics?.RecordCacheHit(secretName);
                return cached;
            }
        }
        catch (Exception ex)
        {
            // Cache read failure — fall through to inner reader via GetOrSetAsync
            Log.CacheError(_logger, secretName, cacheKey, ex);
        }

        // 2. Cache miss — use GetOrSetAsync for stampede protection
        Log.CacheMiss(_logger, secretName);
        _metrics?.RecordCacheMiss(secretName);

        try
        {
            var value = await _cache.GetOrSetAsync(
                cacheKey,
                async ct =>
                {
                    var result = await _inner.GetSecretAsync<T>(secretName, ct).ConfigureAwait(false);
                    return result.Match(
                        Right: v =>
                        {
                            _ = StoreLastKnownGoodAsync(lkgKey, v, ct);
                            return v;
                        },
                        Left: e => throw new StoreResultException(e));
                },
                _secretsOptions.DefaultCacheDuration,
                cancellationToken).ConfigureAwait(false);

            return value;
        }
        catch (StoreResultException ex)
        {
            // Inner reader returned Left — check stale fallback
            if (IsResilienceError(ex.Error))
            {
                var stale = await TryGetLastKnownGoodAsync<T>(secretName, lkgKey, cancellationToken).ConfigureAwait(false);
                if (stale is not null)
                {
                    Log.CacheStaleFallbackServed(_logger, secretName);
                    _metrics?.RecordStaleFallback(secretName);
                    SecretsActivitySource.RecordStaleFallbackEvent(
                        System.Diagnostics.Activity.Current, secretName);
                    return stale;
                }
            }

            return ex.Error;
        }
        catch (Exception ex)
        {
            // Cache infrastructure failure — fallback to inner reader
            Log.CacheError(_logger, secretName, cacheKey, ex);
            return await _inner.GetSecretAsync<T>(secretName, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Invalidates all cache entries for the specified secret, including typed variants
    /// and last-known-good copies.
    /// </summary>
    /// <param name="secretName">The name of the secret to invalidate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task InvalidateAsync(string secretName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        try
        {
            // Remove explicit known keys for the secret
            await _cache.RemoveAsync(ValueKey(secretName), cancellationToken).ConfigureAwait(false);
            await _cache.RemoveAsync(LkgKey(secretName), cancellationToken).ConfigureAwait(false);

            // Remove typed variants via pattern (type suffix is unknown at invalidation time)
            await _cache.RemoveByPatternAsync(
                $"{_cachingOptions.CacheKeyPrefix}:t:{secretName}:*",
                cancellationToken).ConfigureAwait(false);
            await _cache.RemoveByPatternAsync(
                $"{_cachingOptions.CacheKeyPrefix}:lkg:t:{secretName}:*",
                cancellationToken).ConfigureAwait(false);

            Log.CacheInvalidated(_logger, secretName);
        }
        catch (Exception ex)
        {
            Log.CacheInvalidationError(_logger, secretName, ex);
        }
    }

    // ── Cache Helpers ──────────────────────────────────────────────

    private async Task StoreLastKnownGoodAsync<T>(string lkgKey, T value, CancellationToken cancellationToken)
    {
        if (!_secretsOptions.EnableResilience || _secretsOptions.Resilience.MaxStaleDuration <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            await _cache.SetAsync(lkgKey, value, _secretsOptions.Resilience.MaxStaleDuration, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.CacheWriteError(_logger, lkgKey, ex);
        }
    }

    private async Task<T?> TryGetLastKnownGoodAsync<T>(string secretName, string lkgKey, CancellationToken cancellationToken)
    {
        if (!_secretsOptions.EnableResilience || _secretsOptions.Resilience.MaxStaleDuration <= TimeSpan.Zero)
        {
            return default;
        }

        try
        {
            return await _cache.GetAsync<T>(lkgKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.CacheError(_logger, secretName, lkgKey, ex);
            return default;
        }
    }

    private static bool IsResilienceError(EncinaError error)
    {
        return error.GetCode().Match(
            Some: code => code is SecretsErrors.CircuitBreakerOpenCode
                or SecretsErrors.ResilienceTimeoutCode
                or SecretsErrors.ProviderUnavailableCode,
            None: () => false);
    }

    /// <summary>
    /// Exception used internally to propagate <c>Left</c> results from the
    /// <see cref="ICacheProvider.GetOrSetAsync{T}"/> factory without caching them.
    /// </summary>
    private sealed class StoreResultException(EncinaError error) : Exception
    {
        public EncinaError Error { get; } = error;
    }
}
