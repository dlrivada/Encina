namespace Encina.Marten.Projections;

/// <summary>
/// Error codes for projection operations.
/// </summary>
public static class ProjectionErrorCodes
{
    /// <summary>
    /// Prefix for all projection-related error codes.
    /// </summary>
    public const string Prefix = "PROJECTION";

    /// <summary>
    /// The read model was not found.
    /// </summary>
    public const string ReadModelNotFound = $"{Prefix}_READ_MODEL_NOT_FOUND";

    /// <summary>
    /// Failed to store the read model.
    /// </summary>
    public const string StoreFailed = $"{Prefix}_STORE_FAILED";

    /// <summary>
    /// Failed to delete the read model.
    /// </summary>
    public const string DeleteFailed = $"{Prefix}_DELETE_FAILED";

    /// <summary>
    /// Failed to query read models.
    /// </summary>
    public const string QueryFailed = $"{Prefix}_QUERY_FAILED";

    /// <summary>
    /// Failed to apply an event to a projection.
    /// </summary>
    public const string ApplyFailed = $"{Prefix}_APPLY_FAILED";

    /// <summary>
    /// Failed to rebuild the projection.
    /// </summary>
    public const string RebuildFailed = $"{Prefix}_REBUILD_FAILED";

    /// <summary>
    /// No handler found for the event type.
    /// </summary>
    public const string NoHandlerForEvent = $"{Prefix}_NO_HANDLER";

    /// <summary>
    /// Projection is already running.
    /// </summary>
    public const string AlreadyRunning = $"{Prefix}_ALREADY_RUNNING";

    /// <summary>
    /// Projection was cancelled.
    /// </summary>
    public const string Cancelled = $"{Prefix}_CANCELLED";

    /// <summary>
    /// Failed to get projection status.
    /// </summary>
    public const string StatusFailed = $"{Prefix}_STATUS_FAILED";

    /// <summary>
    /// The projection is not registered.
    /// </summary>
    public const string NotRegistered = $"{Prefix}_NOT_REGISTERED";
}
