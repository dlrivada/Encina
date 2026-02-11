namespace Encina.Sharding;

/// <summary>
/// Marks an entity as supporting compound shard keys for multi-field routing.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on entities that require compound shard keys composed
/// of multiple ordered components (e.g., <c>{region, customerId}</c>). This enables
/// routing strategies such as range-on-first-key + hash-on-second-key.
/// </para>
/// <para>
/// <strong>Precedence:</strong> When the <see cref="CompoundShardKeyExtractor"/> resolves
/// a shard key, <see cref="ICompoundShardable"/> takes the highest priority, followed by
/// multiple <see cref="ShardKeyAttribute"/> properties with <see cref="ShardKeyAttribute.Order"/>,
/// then <see cref="IShardable"/>, and finally a single <see cref="ShardKeyAttribute"/> property.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Order : ICompoundShardable
/// {
///     public OrderId Id { get; init; }
///     public string Region { get; init; }
///     public string CustomerId { get; init; }
///
///     public CompoundShardKey GetCompoundShardKey()
///         => new(Region, CustomerId);
/// }
/// </code>
/// </example>
public interface ICompoundShardable
{
    /// <summary>
    /// Gets the compound shard key used for multi-field routing of this entity.
    /// </summary>
    /// <returns>
    /// A <see cref="CompoundShardKey"/> with one or more ordered components.
    /// </returns>
    CompoundShardKey GetCompoundShardKey();
}
