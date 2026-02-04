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
    /// Error code when a concurrency conflict occurs during event stream append.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This error code is specific to Marten's event stream versioning model, which differs
    /// from entity-level versioning used by other providers (EF Core, Dapper, ADO.NET).
    /// </para>
    /// <para>
    /// In Marten, concurrency is based on the expected version of the event stream.
    /// When events are appended with <c>Events.Append(id, expectedVersion, events)</c>,
    /// if the stream has been modified by another process, a version conflict occurs.
    /// </para>
    /// <para>
    /// This corresponds to <c>RepositoryErrors.ConcurrencyConflictErrorCode</c> in the
    /// general repository abstraction, but with Marten-specific semantics.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Error code when an event metadata query fails.
    /// </summary>
    public const string QueryFailed = "marten.query_failed";

    /// <summary>
    /// Error code when a query has invalid parameters.
    /// </summary>
    public const string InvalidQuery = "marten.invalid_query";

    /// <summary>
    /// Error code when an event is not found.
    /// </summary>
    public const string EventNotFound = "marten.event_not_found";
}
