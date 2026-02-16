namespace Encina.Sharding.Resharding;

/// <summary>
/// Represents the current phase of an online resharding operation.
/// </summary>
/// <remarks>
/// <para>
/// The resharding workflow progresses through six sequential phases:
/// Planning → Copying → Replicating → Verifying → CuttingOver → CleaningUp → Completed.
/// </para>
/// <para>
/// The <see cref="RolledBack"/> and <see cref="Failed"/> states are terminal states
/// that indicate the operation was aborted or encountered an unrecoverable error.
/// </para>
/// </remarks>
public enum ReshardingPhase
{
    /// <summary>
    /// The resharding plan is being generated from topology differences.
    /// </summary>
    Planning,

    /// <summary>
    /// Existing rows are being bulk-copied from source shards to target shards.
    /// </summary>
    Copying,

    /// <summary>
    /// Change data capture is replicating incremental changes that occurred during the copy phase.
    /// </summary>
    Replicating,

    /// <summary>
    /// Row counts and checksums are being validated between source and target shards.
    /// </summary>
    Verifying,

    /// <summary>
    /// The topology is being atomically switched with a brief read-only window.
    /// </summary>
    CuttingOver,

    /// <summary>
    /// Migrated rows are being removed from source shards after a retention period.
    /// </summary>
    CleaningUp,

    /// <summary>
    /// The resharding operation completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The resharding operation was rolled back to the previous topology.
    /// </summary>
    RolledBack,

    /// <summary>
    /// The resharding operation failed with an unrecoverable error.
    /// </summary>
    Failed
}
