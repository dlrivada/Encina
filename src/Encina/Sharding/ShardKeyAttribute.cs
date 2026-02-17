namespace Encina.Sharding;

/// <summary>
/// Marks a property as a shard key component for an entity.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute as an alternative to implementing <see cref="IShardable"/> or
/// <see cref="ICompoundShardable"/>. The <see cref="CompoundShardKeyExtractor"/> utility
/// can extract shard keys from entities using this attribute, the <see cref="IShardable"/>
/// interface, or the <see cref="ICompoundShardable"/> interface.
/// </para>
/// <para>
/// For single-component shard keys, mark one property with this attribute. For compound
/// shard keys, mark multiple properties and use the <see cref="Order"/> property to specify
/// the component ordering (0-based). Duplicate <see cref="Order"/> values across multiple
/// properties on the same entity will cause an extraction error.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Single shard key
/// public class Order
/// {
///     public OrderId Id { get; init; }
///
///     [ShardKey]
///     public string CustomerId { get; init; }
/// }
///
/// // Compound shard key with explicit ordering
/// public class RegionalOrder
/// {
///     public OrderId Id { get; init; }
///
///     [ShardKey(Order = 0)]
///     public string Region { get; init; }
///
///     [ShardKey(Order = 1)]
///     public string CustomerId { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class ShardKeyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the 0-based order of this component within a compound shard key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When multiple properties are marked with <see cref="ShardKeyAttribute"/>,
    /// the <see cref="Order"/> value determines the component position in the
    /// resulting <see cref="CompoundShardKey"/>. Lower values come first.
    /// </para>
    /// <para>
    /// Duplicate <see cref="Order"/> values across properties on the same entity
    /// will result in an extraction error from <see cref="CompoundShardKeyExtractor"/>.
    /// </para>
    /// </remarks>
    public int Order { get; set; }
}
