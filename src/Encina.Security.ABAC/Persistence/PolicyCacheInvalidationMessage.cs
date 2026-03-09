namespace Encina.Security.ABAC.Persistence;

/// <summary>
/// Message published via PubSub when a policy store mutation occurs, enabling
/// cross-instance cache invalidation.
/// </summary>
/// <remarks>
/// <para>
/// The <c>CachingPolicyStoreDecorator</c> publishes this message after successful
/// write operations (save, delete) to the configured
/// <see cref="PolicyCachingOptions.InvalidationChannel"/>. All subscribing instances
/// evict their local cache entries for the affected entity.
/// </para>
/// <para>
/// This record is designed for serialization via <see cref="System.Text.Json.JsonSerializer"/>
/// for cross-instance broadcasting through any <c>IPubSubProvider</c> implementation.
/// </para>
/// </remarks>
/// <param name="EntityType">
/// The type of entity that was modified (e.g., <c>"PolicySet"</c> or <c>"Policy"</c>).
/// </param>
/// <param name="EntityId">
/// The identifier of the specific entity that was modified, or <c>null</c> for bulk operations
/// that affect all entities of the given type.
/// </param>
/// <param name="Operation">
/// The operation that triggered the invalidation (e.g., <c>"Save"</c>, <c>"Delete"</c>).
/// </param>
/// <param name="TimestampUtc">
/// The UTC timestamp when the invalidation event occurred.
/// </param>
public sealed record PolicyCacheInvalidationMessage(
    string EntityType,
    string? EntityId,
    string Operation,
    DateTime TimestampUtc);
