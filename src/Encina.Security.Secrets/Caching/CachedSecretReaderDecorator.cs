using Encina.Security.Secrets.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.Secrets.Caching;

/// <summary>
/// A decorator that adds in-memory caching to any <see cref="ISecretReader"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Caches read operations using <see cref="IMemoryCache"/> with a configurable TTL.
/// </para>
/// <para>
/// <b>ROP-aware caching:</b> Only <c>Right</c> (successful) results are cached. <c>Left</c>
/// (error) results are never cached, allowing subsequent calls to retry the operation.
/// </para>
/// <para>
/// <b>Per-secret TTL:</b> When a <see cref="SecretReference"/> specifies a
/// <see cref="SecretReference.CacheDuration"/>, it overrides the default from
/// <see cref="SecretsOptions.DefaultCacheDuration"/>.
/// </para>
/// <para>
/// When <see cref="SecretsOptions.EnableCaching"/> is <c>false</c>, all calls are passed
/// directly to the inner reader without caching.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Registered automatically by AddEncinaSecrets when EnableCaching is true
/// services.AddEncinaSecrets(options =>
/// {
///     options.EnableCaching = true;
///     options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
/// });
/// </code>
/// </example>
public sealed class CachedSecretReaderDecorator : ISecretReader
{
    private const string CacheKeyPrefix = "encina:secrets:";
    private const string TypedCacheKeyPrefix = "encina:secrets:typed:";

    private readonly ISecretReader _inner;
    private readonly IMemoryCache _cache;
    private readonly SecretsOptions _options;
    private readonly ILogger<CachedSecretReaderDecorator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedSecretReaderDecorator"/> class.
    /// </summary>
    /// <param name="inner">The inner secret reader to wrap.</param>
    /// <param name="cache">The memory cache instance.</param>
    /// <param name="options">The secrets configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public CachedSecretReaderDecorator(
        ISecretReader inner,
        IMemoryCache cache,
        IOptions<SecretsOptions> options,
        ILogger<CachedSecretReaderDecorator> logger)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _inner = inner;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, string>> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableCaching)
        {
            return await _inner.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        }

        var cacheKey = CacheKeyPrefix + secretName;

        if (_cache.TryGetValue(cacheKey, out string? cached) && cached is not null)
        {
            Log.CacheHit(_logger, secretName);
            return cached;
        }

        var result = await _inner.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);

        // Cache only Right (success) results; Left (errors) are never cached
        result.IfRight(value =>
        {
            _cache.Set(cacheKey, value, _options.DefaultCacheDuration);
            Log.CacheMiss(_logger, secretName);
        });

        return result;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, T>> GetSecretAsync<T>(
        string secretName,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!_options.EnableCaching)
        {
            return await _inner.GetSecretAsync<T>(secretName, cancellationToken).ConfigureAwait(false);
        }

        var cacheKey = TypedCacheKeyPrefix + typeof(T).FullName + ":" + secretName;

        if (_cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
        {
            Log.CacheHit(_logger, secretName);
            return cached;
        }

        var result = await _inner.GetSecretAsync<T>(secretName, cancellationToken).ConfigureAwait(false);

        // Cache only Right (success) results
        result.IfRight(value =>
        {
            _cache.Set(cacheKey, value, _options.DefaultCacheDuration);
            Log.CacheMiss(_logger, secretName);
        });

        return result;
    }

    /// <summary>
    /// Invalidates the cache entry for the specified secret.
    /// </summary>
    /// <param name="secretName">The name of the secret to invalidate.</param>
    public void Invalidate(string secretName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        _cache.Remove(CacheKeyPrefix + secretName);
        Log.CacheInvalidated(_logger, secretName);
    }
}
