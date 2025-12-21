namespace SimpleMediator.Caching;

/// <summary>
/// Provides cache operations for storing and retrieving data.
/// </summary>
/// <remarks>
/// <para>
/// This interface is the primary abstraction for cache operations in SimpleMediator.
/// Implementations are provided for different cache backends including:
/// </para>
/// <list type="bullet">
/// <item><description>Memory (IMemoryCache) - Fast local caching</description></item>
/// <item><description>Redis (StackExchange.Redis) - Distributed caching with rich features</description></item>
/// <item><description>Garnet (Microsoft) - High-performance Redis-compatible cache</description></item>
/// <item><description>Valkey - Redis fork backed by AWS/Google/Linux Foundation</description></item>
/// <item><description>Dragonfly - 25x Redis throughput</description></item>
/// <item><description>KeyDB - Multi-threaded Redis</description></item>
/// <item><description>NCache - Native .NET distributed cache</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class ProductQueryHandler : IQueryHandler&lt;GetProductQuery, Product&gt;
/// {
///     private readonly ICacheProvider _cache;
///     private readonly IProductRepository _repository;
///
///     public async ValueTask&lt;Either&lt;MediatorError, Product&gt;&gt; Handle(
///         GetProductQuery query,
///         IRequestContext context,
///         CancellationToken cancellationToken)
///     {
///         var cacheKey = $"product:{query.ProductId}";
///
///         return await _cache.GetOrSetAsync(
///             cacheKey,
///             async ct => await _repository.GetByIdAsync(query.ProductId, ct),
///             TimeSpan.FromMinutes(5),
///             cancellationToken);
///     }
/// }
/// </code>
/// </example>
public interface ICacheProvider
{
    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The cached value if found; otherwise, <c>null</c>.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Expiration time. If null, uses the default expiration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Removes all values matching a pattern from the cache.
    /// </summary>
    /// <param name="pattern">The pattern to match (e.g., "product:*", "user:123:*").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Pattern syntax depends on the cache provider. Most providers support glob-style patterns:
    /// <list type="bullet">
    /// <item><description><c>*</c> matches any sequence of characters</description></item>
    /// <item><description><c>?</c> matches any single character</description></item>
    /// <item><description><c>[abc]</c> matches any character in the set</description></item>
    /// </list>
    /// </remarks>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    /// <param name="key">The cache key to check.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a value from the cache, or creates and caches it if not found.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Factory function to create the value if not cached.</param>
    /// <param name="expiration">Expiration time. If null, uses the default expiration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The cached or newly created value.</returns>
    /// <remarks>
    /// This method provides atomic get-or-set semantics where supported by the cache provider.
    /// For distributed caches, this typically uses a lock to prevent cache stampede.
    /// </remarks>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets a value in the cache with sliding expiration.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="slidingExpiration">The sliding expiration window.</param>
    /// <param name="absoluteExpiration">Absolute expiration (overrides sliding after this time). Pass null for no absolute limit.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetWithSlidingExpirationAsync<T>(
        string key,
        T value,
        TimeSpan slidingExpiration,
        TimeSpan? absoluteExpiration,
        CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes the expiration time of a cached item without retrieving it.
    /// </summary>
    /// <param name="key">The cache key to refresh.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><c>true</c> if the key was found and refreshed; otherwise, <c>false</c>.</returns>
    Task<bool> RefreshAsync(string key, CancellationToken cancellationToken);
}
