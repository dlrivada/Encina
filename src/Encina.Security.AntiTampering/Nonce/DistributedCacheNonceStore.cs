using Encina.Caching;
using Encina.Security.AntiTampering.Abstractions;

namespace Encina.Security.AntiTampering.Nonce;

/// <summary>
/// Distributed cache-backed implementation of <see cref="INonceStore"/> for multi-instance deployments.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ICacheProvider"/> from Encina.Caching to store nonces in a shared
/// distributed cache (Redis, Valkey, Garnet, etc.). This ensures that a nonce used
/// against any instance in a cluster is visible to all others.
/// </para>
/// <para>
/// All nonce keys are prefixed with <c>"encina:nonce:"</c> to avoid collisions with
/// other cache entries.
/// </para>
/// <para>
/// <b>Atomicity limitation</b>: The check-then-set operation is not perfectly atomic
/// with all cache providers. For strict atomicity, use a Redis-based provider with
/// Lua scripting or a distributed lock. In practice, the time window between check
/// and set is negligible for most anti-replay scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register with DI
/// services.AddEncinaMemoryCache(); // or AddEncinaRedisCache()
/// services.AddSingleton&lt;INonceStore, DistributedCacheNonceStore&gt;();
///
/// // Usage
/// var added = await nonceStore.TryAddAsync("nonce-123", TimeSpan.FromMinutes(10));
/// </code>
/// </example>
public sealed class DistributedCacheNonceStore : INonceStore
{
    /// <summary>
    /// Prefix for all nonce cache keys to avoid namespace collisions.
    /// </summary>
    internal const string KeyPrefix = "encina:nonce:";

    /// <summary>
    /// Marker value stored in the cache to indicate a nonce has been used.
    /// </summary>
    private const byte NonceMarker = 1;

    private readonly ICacheProvider _cacheProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DistributedCacheNonceStore"/> class.
    /// </summary>
    /// <param name="cacheProvider">The distributed cache provider for nonce storage.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cacheProvider"/> is null.
    /// </exception>
    public DistributedCacheNonceStore(ICacheProvider cacheProvider)
    {
        ArgumentNullException.ThrowIfNull(cacheProvider);

        _cacheProvider = cacheProvider;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This implementation uses a check-then-set pattern:
    /// <list type="number">
    /// <item><description>Check if the nonce already exists via <see cref="ICacheProvider.ExistsAsync"/>.</description></item>
    /// <item><description>If not found, set the nonce with the specified TTL via <see cref="ICacheProvider.SetAsync{T}"/>.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note</b>: This operation is not perfectly atomic across all distributed cache providers.
    /// For strict atomicity guarantees, use a Redis provider with Lua scripting or wrap with
    /// a distributed lock. The time window between check and set is negligible for most
    /// anti-replay scenarios.
    /// </para>
    /// </remarks>
    public async ValueTask<bool> TryAddAsync(
        string nonce,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        var cacheKey = BuildKey(nonce);

        var exists = await _cacheProvider.ExistsAsync(cacheKey, cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return false;
        }

        await _cacheProvider.SetAsync(cacheKey, NonceMarker, expiry, cancellationToken)
            .ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async ValueTask<bool> ExistsAsync(
        string nonce,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        var cacheKey = BuildKey(nonce);

        return await _cacheProvider.ExistsAsync(cacheKey, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the full cache key for a nonce, including the namespace prefix.
    /// </summary>
    private static string BuildKey(string nonce) => string.Concat(KeyPrefix, nonce);
}
