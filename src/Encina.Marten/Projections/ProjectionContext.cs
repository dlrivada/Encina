namespace Encina.Marten.Projections;

/// <summary>
/// Provides context information when applying events to projections.
/// </summary>
/// <remarks>
/// <para>
/// The projection context contains metadata about the event being processed,
/// including the stream identifier, event sequence number, and timestamp.
/// </para>
/// <para>
/// <b>Use Cases</b>:
/// <list type="bullet">
/// <item><description>Setting read model IDs from stream IDs</description></item>
/// <item><description>Tracking when projections were last updated</description></item>
/// <item><description>Implementing version-based optimistic concurrency</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProjectionContext
{
    /// <summary>
    /// Gets the unique identifier of the event stream.
    /// </summary>
    /// <remarks>
    /// This typically corresponds to the aggregate ID.
    /// </remarks>
    public Guid StreamId { get; init; }

    /// <summary>
    /// Gets the sequence number of the event within the stream.
    /// </summary>
    /// <remarks>
    /// Sequence numbers are 1-based and monotonically increasing within a stream.
    /// </remarks>
    public long SequenceNumber { get; init; }

    /// <summary>
    /// Gets the global position of the event across all streams.
    /// </summary>
    /// <remarks>
    /// This is useful for tracking projection progress during rebuilds
    /// and for implementing catch-up subscriptions.
    /// </remarks>
    public long GlobalPosition { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the correlation ID for tracing related operations.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the causation ID linking to the causing event or command.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Gets the type name of the event.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets custom metadata associated with the event.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// Creates a new <see cref="ProjectionContext"/> with default values.
    /// </summary>
    public ProjectionContext()
    {
    }

    /// <summary>
    /// Creates a new <see cref="ProjectionContext"/> with specified values.
    /// </summary>
    /// <param name="streamId">The stream identifier.</param>
    /// <param name="sequenceNumber">The event sequence number.</param>
    /// <param name="globalPosition">The global position.</param>
    /// <param name="timestamp">The event timestamp.</param>
    public ProjectionContext(
        Guid streamId,
        long sequenceNumber,
        long globalPosition,
        DateTime timestamp)
    {
        StreamId = streamId;
        SequenceNumber = sequenceNumber;
        GlobalPosition = globalPosition;
        Timestamp = timestamp;
    }
}
