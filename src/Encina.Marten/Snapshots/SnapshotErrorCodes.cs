namespace Encina.Marten.Snapshots;

/// <summary>
/// Error codes for snapshot-related operations.
/// </summary>
public static class SnapshotErrorCodes
{
    /// <summary>
    /// Prefix for all snapshot error codes.
    /// </summary>
    public const string Prefix = "snapshot";

    /// <summary>
    /// Error code when loading a snapshot fails.
    /// </summary>
    public const string LoadFailed = "snapshot.load_failed";

    /// <summary>
    /// Error code when saving a snapshot fails.
    /// </summary>
    public const string SaveFailed = "snapshot.save_failed";

    /// <summary>
    /// Error code when deleting snapshots fails.
    /// </summary>
    public const string DeleteFailed = "snapshot.delete_failed";

    /// <summary>
    /// Error code when pruning old snapshots fails.
    /// </summary>
    public const string PruneFailed = "snapshot.prune_failed";

    /// <summary>
    /// Error code when restoring from a snapshot fails.
    /// </summary>
    public const string RestoreFailed = "snapshot.restore_failed";

    /// <summary>
    /// Error code when the snapshot version is invalid.
    /// </summary>
    public const string InvalidVersion = "snapshot.invalid_version";

    /// <summary>
    /// Error code when aggregate type does not support snapshotting.
    /// </summary>
    public const string NotSnapshotable = "snapshot.not_snapshotable";

    /// <summary>
    /// Error code when snapshot creation is skipped (not an error, informational).
    /// </summary>
    public const string CreationSkipped = "snapshot.creation_skipped";
}
