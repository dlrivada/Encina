namespace Encina.Security.AntiTampering.Abstractions;

/// <summary>
/// Provides nonce storage for replay attack protection.
/// </summary>
/// <remarks>
/// <para>
/// Implementations track used nonces to prevent replay attacks. Each nonce should be
/// unique per request and stored for the duration of the timestamp tolerance window
/// to ensure that previously seen requests cannot be replayed.
/// </para>
/// <para>
/// Built-in implementations include <c>InMemoryNonceStore</c> for single-instance
/// deployments and <c>DistributedCacheNonceStore</c> for multi-instance scenarios
/// using <c>IDistributedCache</c>.
/// </para>
/// <para>
/// Implementations must be thread-safe and suitable for high-throughput concurrent access.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check and store a nonce atomically
/// var isNew = await nonceStore.TryAddAsync("abc123", TimeSpan.FromMinutes(10), ct);
/// if (!isNew)
/// {
///     // Nonce was already used - potential replay attack
/// }
/// </code>
/// </example>
public interface INonceStore
{
    /// <summary>
    /// Attempts to add a nonce to the store atomically.
    /// </summary>
    /// <param name="nonce">The nonce value to add.</param>
    /// <param name="expiry">The duration after which the nonce entry can be evicted.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the nonce was added (first time seen);
    /// <c>false</c> if the nonce already exists (potential replay).
    /// </returns>
    /// <remarks>
    /// This operation must be atomic: either the nonce is added and <c>true</c> is returned,
    /// or the nonce already exists and <c>false</c> is returned. No partial states are allowed.
    /// </remarks>
    ValueTask<bool> TryAddAsync(
        string nonce,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a nonce already exists in the store.
    /// </summary>
    /// <param name="nonce">The nonce value to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>true</c> if the nonce exists (already used);
    /// <c>false</c> if the nonce is not found.
    /// </returns>
    ValueTask<bool> ExistsAsync(
        string nonce,
        CancellationToken cancellationToken = default);
}
