namespace Encina.Sharding.Resharding;

/// <summary>
/// Phase-specific checkpoint data for crash recovery during resharding.
/// </summary>
/// <param name="LastCopiedBatchPosition">
/// The position marker for the last successfully copied batch during the copy phase.
/// Used to resume bulk copy from the correct offset after a crash.
/// </param>
/// <param name="CdcPosition">
/// The CDC position marker (e.g., LSN, binlog offset, WAL position) for the replication phase.
/// Used to resume change capture from the correct position after a crash.
/// </param>
public sealed record ReshardingCheckpoint(
    long? LastCopiedBatchPosition,
    string? CdcPosition);
