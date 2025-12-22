namespace Encina.EventStoreDB;

/// <summary>
/// Error codes specific to EventStoreDB operations.
/// </summary>
public static class EventStoreDbErrorCodes
{
    /// <summary>
    /// Aggregate was not found in the event store.
    /// </summary>
    public const string AggregateNotFound = "eventstoredb.aggregate.not_found";

    /// <summary>
    /// Failed to load aggregate from event store.
    /// </summary>
    public const string LoadFailed = "eventstoredb.aggregate.load_failed";

    /// <summary>
    /// Failed to save aggregate to event store.
    /// </summary>
    public const string SaveFailed = "eventstoredb.aggregate.save_failed";

    /// <summary>
    /// Failed to create new aggregate stream.
    /// </summary>
    public const string CreateFailed = "eventstoredb.aggregate.create_failed";

    /// <summary>
    /// Concurrency conflict detected during save operation.
    /// </summary>
    public const string ConcurrencyConflict = "eventstoredb.aggregate.concurrency_conflict";

    /// <summary>
    /// Stream already exists when trying to create.
    /// </summary>
    public const string StreamAlreadyExists = "eventstoredb.stream.already_exists";

    /// <summary>
    /// No events provided when creating aggregate.
    /// </summary>
    public const string NoEventsToCreate = "eventstoredb.aggregate.no_events";

    /// <summary>
    /// Failed to deserialize event from store.
    /// </summary>
    public const string DeserializationFailed = "eventstoredb.event.deserialization_failed";

    /// <summary>
    /// Failed to serialize event for store.
    /// </summary>
    public const string SerializationFailed = "eventstoredb.event.serialization_failed";

    /// <summary>
    /// Connection to EventStoreDB failed.
    /// </summary>
    public const string ConnectionFailed = "eventstoredb.connection.failed";
}
