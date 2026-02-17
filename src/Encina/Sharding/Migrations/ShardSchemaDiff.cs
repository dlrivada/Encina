namespace Encina.Sharding.Migrations;

/// <summary>
/// Describes schema differences between a shard and the baseline shard.
/// </summary>
/// <remarks>
/// <para>
/// Produced by drift detection when comparing each shard's schema against the designated
/// baseline. An empty <see cref="TableDiffs"/> collection means the shard's schema matches
/// the baseline exactly.
/// </para>
/// </remarks>
/// <param name="ShardId">The shard whose schema was compared.</param>
/// <param name="BaselineShardId">The shard used as the reference (baseline) schema.</param>
/// <param name="TableDiffs">The list of table-level differences found.</param>
public sealed record ShardSchemaDiff(
    string ShardId,
    string BaselineShardId,
    IReadOnlyList<TableDiff> TableDiffs)
{
    /// <summary>Gets the compared shard identifier.</summary>
    public string ShardId { get; } = !string.IsNullOrWhiteSpace(ShardId)
        ? ShardId
        : throw new ArgumentException("Shard ID cannot be null or whitespace.", nameof(ShardId));

    /// <summary>Gets the baseline shard identifier.</summary>
    public string BaselineShardId { get; } = !string.IsNullOrWhiteSpace(BaselineShardId)
        ? BaselineShardId
        : throw new ArgumentException("Baseline shard ID cannot be null or whitespace.", nameof(BaselineShardId));
}
