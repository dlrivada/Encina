namespace Encina.Sharding.Resharding;

/// <summary>
/// Describes a request to generate a resharding plan based on topology changes.
/// </summary>
/// <param name="OldTopology">The current (source) shard topology.</param>
/// <param name="NewTopology">The desired (target) shard topology.</param>
/// <param name="EntityTypeConstraints">
/// Optional set of entity types to constrain the resharding scope.
/// When <c>null</c> or empty, all entity types are included in the migration plan.
/// </param>
public sealed record ReshardingRequest(
    ShardTopology OldTopology,
    ShardTopology NewTopology,
    IReadOnlySet<Type>? EntityTypeConstraints = null)
{
    /// <summary>
    /// Gets the current topology.
    /// </summary>
    public ShardTopology OldTopology { get; } = OldTopology
        ?? throw new ArgumentNullException(nameof(OldTopology));

    /// <summary>
    /// Gets the desired topology.
    /// </summary>
    public ShardTopology NewTopology { get; } = NewTopology
        ?? throw new ArgumentNullException(nameof(NewTopology));
}
