using LanguageExt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Secrets;

/// <summary>
/// A decorator that adds in-memory caching to any <see cref="ISecretProvider"/> implementation.
/// </summary>
/// <remarks>
/// <para>
/// Caches read operations (<see cref="GetSecretAsync"/>, <see cref="GetSecretVersionAsync"/>,
/// <see cref="ExistsAsync"/>) using <see cref="IMemoryCache"/> with a configurable TTL.
/// </para>
/// <para>
/// <b>ROP-aware caching:</b> Only <c>Right</c> (successful) results are cached. <c>Left</c>
/// (error) results are never cached, allowing subsequent calls to retry the operation.
/// </para>
/// <para>
/// Write operations (<see cref="SetSecretAsync"/>, <see cref="DeleteSecretAsync"/>) invalidate
/// the corresponding cache entries only when they succeed (<c>Right</c>).
/// </para>
/// <para>
/// When <see cref="SecretCacheOptions.Enabled"/> is <c>false</c>, all calls are passed
/// directly to the inner provider without caching.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register via DI (recommended)
/// services.AddSingleton&lt;ISecretProvider, MyVaultProvider&gt;();
/// services.AddEncinaSecretsCaching(options =>
/// {
///     options.DefaultTtl = TimeSpan.FromMinutes(10);
/// });
///
/// // Manual construction
/// var cached = new CachedSecretProvider(innerProvider, memoryCache, cacheOptions, logger);
/// </code>
/// </example>
public sealed class CachedSecretProvider : ISecretProvider
{
    private const string CacheKeyPrefix = "encina:secrets:";
    private const string VersionCacheKeyPrefix = "encina:secrets:v:";
    private const string ExistsCacheKeyPrefix = "encina:secrets:exists:";

    private readonly ISecretProvider _inner;
    private readonly IMemoryCache _cache;
    private readonly SecretCacheOptions _options;
    private readonly ILogger<CachedSecretProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedSecretProvider"/> class.
    /// </summary>
    /// <param name="inner">The inner secret provider to wrap.</param>
    /// <param name="cache">The memory cache instance.</param>
    /// <param name="options">The cache configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public CachedSecretProvider(
        ISecretProvider inner,
        IMemoryCache cache,
        IOptions<SecretCacheOptions> options,
        ILogger<CachedSecretProvider> logger)
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
    public async ValueTask<Either<EncinaError, Secret>> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await _inner.GetSecretAsync(name, cancellationToken);
        }

        var cacheKey = CacheKeyPrefix + name;

        if (_cache.TryGetValue(cacheKey, out Secret? cached) && cached is not null)
        {
            _logger.LogDebug("Cache hit for secret '{SecretName}'.", name);
            return cached;
        }

        var result = await _inner.GetSecretAsync(name, cancellationToken);

        // Cache only Right (success) results; Left (errors) are never cached
        result.IfRight(secret =>
        {
            _cache.Set(cacheKey, secret, _options.DefaultTtl);
        });

        _logger.LogDebug("Cache miss for secret '{SecretName}'.", name);
        return result;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Secret>> GetSecretVersionAsync(string name, string version, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await _inner.GetSecretVersionAsync(name, version, cancellationToken);
        }

        var cacheKey = VersionCacheKeyPrefix + name + ":" + version;

        if (_cache.TryGetValue(cacheKey, out Secret? cached) && cached is not null)
        {
            _logger.LogDebug("Cache hit for secret '{SecretName}' version '{Version}'.", name, version);
            return cached;
        }

        var result = await _inner.GetSecretVersionAsync(name, version, cancellationToken);

        result.IfRight(secret =>
        {
            _cache.Set(cacheKey, secret, _options.DefaultTtl);
        });

        _logger.LogDebug("Cache miss for secret '{SecretName}' version '{Version}'.", name, version);
        return result;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, SecretMetadata>> SetSecretAsync(string name, string value, SecretOptions? options = null, CancellationToken cancellationToken = default)
    {
        var result = await _inner.SetSecretAsync(name, value, options, cancellationToken);

        // Invalidate cache only on successful write
        if (_options.Enabled)
        {
            result.IfRight(_ =>
            {
                InvalidateCacheForSecret(name);
                _logger.LogDebug("Cache invalidated for secret '{SecretName}' after set.", name);
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, Unit>> DeleteSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        var result = await _inner.DeleteSecretAsync(name, cancellationToken);

        // Invalidate cache only on successful delete
        if (_options.Enabled)
        {
            result.IfRight(_ =>
            {
                InvalidateCacheForSecret(name);
                _logger.LogDebug("Cache invalidated for secret '{SecretName}' after delete.", name);
            });
        }

        return result;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IEnumerable<string>>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        // List operations are not cached as the result set can change frequently
        return _inner.ListSecretsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, bool>> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await _inner.ExistsAsync(name, cancellationToken);
        }

        var cacheKey = ExistsCacheKeyPrefix + name;

        if (_cache.TryGetValue(cacheKey, out bool cached))
        {
            _logger.LogDebug("Cache hit for exists check on secret '{SecretName}'.", name);
            return cached;
        }

        var result = await _inner.ExistsAsync(name, cancellationToken);

        // Cache only Right results
        result.IfRight(exists =>
        {
            _cache.Set(cacheKey, exists, _options.DefaultTtl);
        });

        _logger.LogDebug("Cache miss for exists check on secret '{SecretName}'.", name);
        return result;
    }

    private void InvalidateCacheForSecret(string name)
    {
        _cache.Remove(CacheKeyPrefix + name);
        _cache.Remove(ExistsCacheKeyPrefix + name);
        // Note: Versioned entries are not invalidated individually because we cannot
        // enumerate all cached versions. They will expire naturally via TTL.
    }
}
