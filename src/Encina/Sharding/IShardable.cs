namespace Encina.Sharding;

/// <summary>
/// Marks an entity as shardable, providing a shard key for routing.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on entities that participate in database sharding.
/// The shard key determines which shard the entity is stored in.
/// </para>
/// <para>
/// Alternatively, use <see cref="ShardKeyAttribute"/> to mark a property as the shard key
/// without implementing this interface. The <see cref="ShardKeyExtractor"/> utility supports both approaches.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : IShardable
/// {
///     public OrderId Id { get; init; }
///     public string CustomerId { get; init; }
///
///     public string GetShardKey() => CustomerId;
/// }
/// </code>
/// </example>
public interface IShardable
{
    /// <summary>
    /// Gets the shard key value used for routing this entity to the appropriate shard.
    /// </summary>
    /// <returns>A string representation of the shard key.</returns>
    string GetShardKey();
}
