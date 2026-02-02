using LanguageExt;

namespace Encina.Marten;

/// <summary>
/// Provides query capabilities for events based on metadata such as correlation and causation IDs.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables debugging and tracing of event workflows by allowing queries
/// that filter events based on their correlation and causation metadata.
/// </para>
/// <para><b>Use Cases:</b></para>
/// <list type="bullet">
/// <item><description>Finding all events triggered by a single user request (correlation)</description></item>
/// <item><description>Tracing the causal chain of events (causation)</description></item>
/// <item><description>Debugging distributed workflows</description></item>
/// <item><description>Audit trail analysis</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Find all events from a single request
/// var events = await query.GetEventsByCorrelationIdAsync("request-123");
///
/// // Trace what caused a specific event
/// var chain = await query.GetCausalChainAsync(eventId);
/// </code>
/// </example>
public interface IEventMetadataQuery
{
    /// <summary>
    /// Retrieves all events that share the specified correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to search for.</param>
    /// <param name="options">Optional pagination and filtering options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of events matching the correlation ID.</returns>
    /// <remarks>
    /// Correlation ID typically links all events from a single logical operation,
    /// such as a user request or a saga execution.
    /// </remarks>
    Task<Either<EncinaError, EventQueryResult>> GetEventsByCorrelationIdAsync(
        string correlationId,
        EventQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all events that share the specified causation ID.
    /// </summary>
    /// <param name="causationId">The causation ID to search for.</param>
    /// <param name="options">Optional pagination and filtering options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of events that were caused by the specified causation.</returns>
    /// <remarks>
    /// Causation ID links an event to what directly caused it (usually the command
    /// or previous event that triggered it).
    /// </remarks>
    Task<Either<EncinaError, EventQueryResult>> GetEventsByCausationIdAsync(
        string causationId,
        EventQueryOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the causal chain of events starting from the specified event.
    /// </summary>
    /// <param name="eventId">The ID of the starting event.</param>
    /// <param name="direction">The direction to traverse the chain.</param>
    /// <param name="maxDepth">Maximum depth to traverse (default 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An ordered list of events in the causal chain.</returns>
    /// <remarks>
    /// <para>
    /// When direction is <see cref="CausalChainDirection.Ancestors"/>, returns events
    /// that caused the specified event (following CausationId links backward).
    /// </para>
    /// <para>
    /// When direction is <see cref="CausalChainDirection.Descendants"/>, returns events
    /// that were caused by the specified event (following events whose CausationId matches).
    /// </para>
    /// </remarks>
    Task<Either<EncinaError, IReadOnlyList<EventWithMetadata>>> GetCausalChainAsync(
        Guid eventId,
        CausalChainDirection direction = CausalChainDirection.Ancestors,
        int maxDepth = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single event by its unique identifier with full metadata.
    /// </summary>
    /// <param name="eventId">The unique identifier of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event with its metadata, or an error if not found.</returns>
    Task<Either<EncinaError, EventWithMetadata>> GetEventByIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Direction for traversing a causal chain.
/// </summary>
public enum CausalChainDirection
{
    /// <summary>
    /// Traverse backward to find events that caused this event.
    /// </summary>
    Ancestors,

    /// <summary>
    /// Traverse forward to find events caused by this event.
    /// </summary>
    Descendants,
}

/// <summary>
/// Options for event metadata queries.
/// </summary>
public sealed class EventQueryOptions
{
    /// <summary>
    /// Gets or sets the number of items to skip for pagination.
    /// </summary>
    public int Skip { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items to return.
    /// </summary>
    /// <remarks>
    /// Default is 100. Maximum allowed is 1000.
    /// </remarks>
    public int Take { get; set; } = 100;

    /// <summary>
    /// Gets or sets the stream ID to filter by.
    /// </summary>
    public Guid? StreamId { get; set; }

    /// <summary>
    /// Gets or sets the event type names to filter by.
    /// </summary>
    public IReadOnlyList<string>? EventTypes { get; set; }

    /// <summary>
    /// Gets or sets the minimum timestamp (inclusive) to filter by.
    /// </summary>
    public DateTimeOffset? FromTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the maximum timestamp (inclusive) to filter by.
    /// </summary>
    public DateTimeOffset? ToTimestamp { get; set; }
}

/// <summary>
/// Result of an event metadata query with pagination information.
/// </summary>
public sealed class EventQueryResult
{
    /// <summary>
    /// Gets the events matching the query.
    /// </summary>
    public required IReadOnlyList<EventWithMetadata> Events { get; init; }

    /// <summary>
    /// Gets the total count of matching events (before pagination).
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets whether there are more events available beyond this page.
    /// </summary>
    public required bool HasMore { get; init; }
}

/// <summary>
/// Represents an event with its associated metadata.
/// </summary>
public sealed class EventWithMetadata
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the stream (aggregate) ID the event belongs to.
    /// </summary>
    public required Guid StreamId { get; init; }

    /// <summary>
    /// Gets the version within the stream.
    /// </summary>
    public required long Version { get; init; }

    /// <summary>
    /// Gets the global sequence number across all streams.
    /// </summary>
    public required long Sequence { get; init; }

    /// <summary>
    /// Gets the event type name.
    /// </summary>
    public required string EventTypeName { get; init; }

    /// <summary>
    /// Gets the deserialized event data.
    /// </summary>
    public required object Data { get; init; }

    /// <summary>
    /// Gets the timestamp when the event was stored.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the causation ID linking to the causing event or command.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Gets custom headers/metadata associated with the event.
    /// </summary>
    public IReadOnlyDictionary<string, object> Headers { get; init; } =
        new Dictionary<string, object>();
}
