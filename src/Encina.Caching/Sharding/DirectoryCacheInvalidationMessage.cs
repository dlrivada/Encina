namespace Encina.Caching.Sharding;

/// <summary>
/// Message published via <c>IPubSubProvider</c> when a shard directory mapping
/// is modified, enabling cross-instance L1 cache invalidation.
/// </summary>
/// <param name="Key">The shard key that was modified.</param>
/// <param name="ShardId">The shard ID associated with the key, or <c>null</c> if the mapping was removed.</param>
/// <param name="IsRemoval">Whether this represents a mapping removal.</param>
public sealed record DirectoryCacheInvalidationMessage(
    string Key,
    string? ShardId,
    bool IsRemoval);
