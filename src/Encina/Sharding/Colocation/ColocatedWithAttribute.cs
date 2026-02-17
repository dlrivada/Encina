namespace Encina.Sharding.Colocation;

/// <summary>
/// Declares that an entity is co-located with a root entity for sharding purposes.
/// Co-located entities share the same shard key and are guaranteed to reside on the same shard,
/// enabling efficient local JOINs within a shard.
/// </summary>
/// <remarks>
/// <para>
/// Apply this attribute to child or dependent entities that must always be stored on the same
/// shard as their root entity. The root entity defines the shard topology, and all co-located
/// entities are routed using the root entity's shard key.
/// </para>
/// <para>
/// <strong>Requirements:</strong>
/// <list type="bullet">
/// <item>Both the root entity and the co-located entity must be shardable (implement
/// <see cref="IShardable"/>, <see cref="ICompoundShardable"/>, or have properties marked
/// with <see cref="ShardKeyAttribute"/>).</item>
/// <item>The shard key types must be compatible (assignable) between root and co-located entity.</item>
/// <item>An entity cannot be co-located with itself.</item>
/// <item>An entity can belong to only one co-location group.</item>
/// </list>
/// </para>
/// <para>
/// Validation is performed at startup during service registration. A
/// <see cref="ColocationViolationException"/> is thrown if any constraint is violated.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Root entity defines the shard topology
/// public class Order : IShardable
/// {
///     public OrderId Id { get; init; }
///     public string CustomerId { get; init; }
///     public string GetShardKey() => CustomerId;
/// }
///
/// // Child entity co-located with Order — always on the same shard
/// [ColocatedWith(typeof(Order))]
/// public class OrderItem : IShardable
/// {
///     public OrderItemId Id { get; init; }
///     public string CustomerId { get; init; }
///     public string GetShardKey() => CustomerId;
/// }
///
/// // Another child entity co-located with Order
/// [ColocatedWith(typeof(Order))]
/// public class OrderPayment : IShardable
/// {
///     public PaymentId Id { get; init; }
///     public string CustomerId { get; init; }
///     public string GetShardKey() => CustomerId;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ColocatedWithAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ColocatedWithAttribute"/> class.
    /// </summary>
    /// <param name="rootEntityType">
    /// The root entity type that defines the co-location group. All entities
    /// with the same root entity type will be routed to the same shard.
    /// </param>
    public ColocatedWithAttribute(Type rootEntityType)
    {
        ArgumentNullException.ThrowIfNull(rootEntityType);
        RootEntityType = rootEntityType;
    }

    /// <summary>
    /// Gets the root entity type that defines the co-location group.
    /// </summary>
    public Type RootEntityType { get; }
}
