namespace Encina.Sharding.Colocation;

/// <summary>
/// Immutable record representing a co-location group of sharded entities.
/// </summary>
/// <remarks>
/// <para>
/// A co-location group ensures that the root entity and all its co-located entities
/// are stored on the same shard. This enables efficient local JOINs and shard-local
/// transactions.
/// </para>
/// </remarks>
/// <param name="RootEntity">The root entity type that defines the co-location group.</param>
/// <param name="ColocatedEntities">The entity types co-located with the root entity.</param>
/// <param name="SharedShardKeyProperty">
/// Optional property name hint shared across entities in the group (e.g., "CustomerId").
/// </param>
public sealed record ColocationGroup(
    Type RootEntity,
    IReadOnlyList<Type> ColocatedEntities,
    string SharedShardKeyProperty) : IColocationGroup;
