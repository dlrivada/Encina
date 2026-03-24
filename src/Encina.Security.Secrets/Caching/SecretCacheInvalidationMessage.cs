namespace Encina.Security.Secrets.Caching;

/// <summary>
/// Message published via PubSub when a secret store mutation occurs, enabling
/// cross-instance cache invalidation.
/// </summary>
/// <remarks>
/// <para>
/// The <c>CachingSecretWriterDecorator</c> publishes this message after successful
/// write operations (set, rotate) to the configured
/// <see cref="SecretCachingOptions.InvalidationChannel"/>. All subscribing instances
/// evict their local cache entries for the affected secret.
/// </para>
/// <para>
/// This record is designed for serialization via <see cref="System.Text.Json.JsonSerializer"/>
/// for cross-instance broadcasting through any <c>IPubSubProvider</c> implementation.
/// </para>
/// </remarks>
/// <param name="SecretName">
/// The name of the secret that was modified. Bulk invalidation is controlled
/// by <paramref name="Operation"/> set to <c>"BulkInvalidate"</c>.
/// </param>
/// <param name="Operation">
/// The operation that triggered the invalidation (e.g., <c>"Set"</c>, <c>"Remove"</c>,
/// <c>"Rotate"</c>, <c>"BulkInvalidate"</c>).
/// </param>
/// <param name="InvalidatedAtUtc">
/// The UTC timestamp when the invalidation event occurred.
/// </param>
public sealed record SecretCacheInvalidationMessage(
    string SecretName,
    string Operation,
    DateTime InvalidatedAtUtc);
