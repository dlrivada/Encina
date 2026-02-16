namespace Encina.Sharding.Resharding;

/// <summary>
/// Describes a single data migration step between two shards for a specific key range.
/// </summary>
/// <param name="SourceShardId">The shard that currently owns the data.</param>
/// <param name="TargetShardId">The shard that will own the data after resharding.</param>
/// <param name="KeyRange">The hash ring range being migrated.</param>
/// <param name="EstimatedRows">The estimated number of rows to be migrated.</param>
public sealed record ShardMigrationStep(
    string SourceShardId,
    string TargetShardId,
    KeyRange KeyRange,
    long EstimatedRows)
{
    /// <summary>
    /// Gets the source shard identifier.
    /// </summary>
    public string SourceShardId { get; } = !string.IsNullOrWhiteSpace(SourceShardId)
        ? SourceShardId
        : throw new ArgumentException("Source shard ID cannot be null or whitespace.", nameof(SourceShardId));

    /// <summary>
    /// Gets the target shard identifier.
    /// </summary>
    public string TargetShardId { get; } = !string.IsNullOrWhiteSpace(TargetShardId)
        ? TargetShardId
        : throw new ArgumentException("Target shard ID cannot be null or whitespace.", nameof(TargetShardId));

    /// <summary>
    /// Gets the key range being migrated.
    /// </summary>
    public KeyRange KeyRange { get; } = KeyRange
        ?? throw new ArgumentNullException(nameof(KeyRange));
}
