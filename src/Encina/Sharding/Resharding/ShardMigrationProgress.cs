namespace Encina.Sharding.Resharding;

/// <summary>
/// Tracks the progress of a single shard migration step.
/// </summary>
/// <param name="RowsCopied">Number of rows copied from source to target during the copy phase.</param>
/// <param name="RowsReplicated">Number of rows replicated via CDC during the replication phase.</param>
/// <param name="IsVerified">Whether the verification phase has passed for this step.</param>
public sealed record ShardMigrationProgress(
    long RowsCopied,
    long RowsReplicated,
    bool IsVerified);
