namespace Encina.Marten;

/// <summary>
/// Error codes for Marten-related errors.
/// </summary>
public static class MartenErrorCodes
{
    /// <summary>
    /// Error code when an aggregate is not found.
    /// </summary>
    public const string AggregateNotFound = "marten.aggregate_not_found";

    /// <summary>
    /// Error code when loading an aggregate fails.
    /// </summary>
    public const string LoadFailed = "marten.load_failed";

    /// <summary>
    /// Error code when saving an aggregate fails.
    /// </summary>
    public const string SaveFailed = "marten.save_failed";

    /// <summary>
    /// Error code when creating an aggregate fails.
    /// </summary>
    public const string CreateFailed = "marten.create_failed";

    /// <summary>
    /// Error code when a concurrency conflict occurs.
    /// </summary>
    public const string ConcurrencyConflict = "marten.concurrency_conflict";

    /// <summary>
    /// Error code when trying to create an aggregate without events.
    /// </summary>
    public const string NoEventsToCreate = "marten.no_events_to_create";

    /// <summary>
    /// Error code when a stream already exists.
    /// </summary>
    public const string StreamAlreadyExists = "marten.stream_already_exists";

    /// <summary>
    /// Error code when publishing domain events fails.
    /// </summary>
    public const string PublishEventsFailed = "marten.publish_events_failed";
}
