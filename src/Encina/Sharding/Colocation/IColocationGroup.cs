namespace Encina.Sharding.Colocation;

/// <summary>
/// Represents a group of entities that are co-located on the same shard.
/// </summary>
/// <remarks>
/// <para>
/// A co-location group defines a root entity and a set of co-located entities that
/// share the same shard key and are guaranteed to reside on the same physical shard.
/// This enables efficient local JOINs and shard-local transactions between related entities.
/// </para>
/// <para>
/// Co-location groups can be defined declaratively using <see cref="ColocatedWithAttribute"/>
/// or programmatically using <see cref="ColocationGroupBuilder"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Programmatic definition via builder
/// var group = new ColocationGroupBuilder()
///     .WithRootEntity&lt;Order&gt;()
///     .AddColocatedEntity&lt;OrderItem&gt;()
///     .AddColocatedEntity&lt;OrderPayment&gt;()
///     .WithSharedShardKeyProperty("CustomerId")
///     .Build();
///
/// // group.RootEntity == typeof(Order)
/// // group.ColocatedEntities contains typeof(OrderItem) and typeof(OrderPayment)
/// </code>
/// </example>
public interface IColocationGroup
{
    /// <summary>
    /// Gets the root entity type that defines the co-location group.
    /// </summary>
    /// <remarks>
    /// The root entity owns the shard topology. All co-located entities are routed
    /// based on the root entity's shard key.
    /// </remarks>
    Type RootEntity { get; }

    /// <summary>
    /// Gets the list of entity types co-located with the root entity.
    /// </summary>
    /// <remarks>
    /// This list does not include the root entity itself, only the dependent entities
    /// that are co-located with it.
    /// </remarks>
    IReadOnlyList<Type> ColocatedEntities { get; }

    /// <summary>
    /// Gets the name of the shared shard key property across all entities in the group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is an optional metadata hint indicating the property name that entities
    /// in the group use as their shard key (e.g., "CustomerId"). This is informational
    /// and used for documentation and diagnostics — actual shard key extraction uses
    /// <see cref="IShardable"/>, <see cref="ICompoundShardable"/>, or <see cref="ShardKeyAttribute"/>.
    /// </para>
    /// </remarks>
    string SharedShardKeyProperty { get; }
}
