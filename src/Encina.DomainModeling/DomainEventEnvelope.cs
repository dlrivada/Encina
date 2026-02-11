using LanguageExt;

namespace Encina.DomainModeling;

/// <summary>
/// Metadata that can be attached to domain events for tracing and auditing.
/// </summary>
public interface IDomainEventMetadata
{
    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// All events in a single operation share the same correlation ID.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the causation ID - the ID of the command or event that caused this event.
    /// </summary>
    string? CausationId { get; }

    /// <summary>
    /// Gets the ID of the user who triggered this event.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the tenant ID for multi-tenant applications.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Gets additional metadata as key-value pairs.
    /// </summary>
    IReadOnlyDictionary<string, string> AdditionalMetadata { get; }
}

/// <summary>
/// Default implementation of domain event metadata.
/// </summary>
/// <param name="CorrelationId">The correlation ID for distributed tracing.</param>
/// <param name="CausationId">The ID of the command or event that caused this event.</param>
/// <param name="UserId">The ID of the user who triggered this event.</param>
/// <param name="TenantId">The tenant ID for multi-tenant applications.</param>
/// <param name="AdditionalMetadata">Additional metadata as key-value pairs.</param>
public sealed record DomainEventMetadata(
    string? CorrelationId = null,
    string? CausationId = null,
    string? UserId = null,
    string? TenantId = null,
    IReadOnlyDictionary<string, string>? AdditionalMetadata = null) : IDomainEventMetadata
{
    /// <summary>
    /// Gets the additional metadata, defaulting to an empty dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, string> AdditionalMetadata { get; init; } =
        AdditionalMetadata ?? new Dictionary<string, string>();

    /// <summary>
    /// Creates an empty metadata instance.
    /// </summary>
    public static DomainEventMetadata Empty => new();

    /// <summary>
    /// Creates metadata with only a correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <returns>A new metadata instance.</returns>
    public static DomainEventMetadata WithCorrelation(string correlationId) =>
        new(CorrelationId: correlationId);

    /// <summary>
    /// Creates metadata with correlation and causation IDs.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="causationId">The causation ID.</param>
    /// <returns>A new metadata instance.</returns>
    public static DomainEventMetadata WithCausation(string correlationId, string causationId) =>
        new(CorrelationId: correlationId, CausationId: causationId);
}

/// <summary>
/// Wraps a domain event with metadata for publishing and handling.
/// </summary>
/// <typeparam name="TEvent">The type of the domain event.</typeparam>
/// <remarks>
/// <para>
/// The envelope pattern separates the event data from cross-cutting concerns like
/// tracing metadata, allowing events to remain focused on their domain semantics.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var orderPlaced = new OrderPlaced(orderId, customerId);
/// var envelope = DomainEventEnvelope.Create(orderPlaced, metadata);
///
/// // Access the event and metadata
/// var eventId = envelope.Event.EventId;
/// var correlationId = envelope.Metadata.CorrelationId;
/// </code>
/// </example>
public sealed record DomainEventEnvelope<TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    /// Gets the wrapped domain event.
    /// </summary>
    public required TEvent Event { get; init; }

    /// <summary>
    /// Gets the metadata associated with this event.
    /// </summary>
    public required IDomainEventMetadata Metadata { get; init; }

    /// <summary>
    /// Gets the timestamp when this envelope was created (UTC).
    /// </summary>
    public DateTime EnvelopeCreatedAtUtc { get; init; } = TimeProvider.System.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Gets a unique identifier for this envelope instance.
    /// </summary>
    public Guid EnvelopeId { get; init; } = Guid.NewGuid();
}

/// <summary>
/// Factory methods for creating domain event envelopes.
/// </summary>
public static class DomainEventEnvelope
{
    /// <summary>
    /// Creates an envelope wrapping the specified domain event with metadata.
    /// </summary>
    /// <typeparam name="TEvent">The type of the domain event.</typeparam>
    /// <param name="event">The domain event to wrap.</param>
    /// <param name="metadata">The metadata to attach.</param>
    /// <returns>A new envelope containing the event and metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when event or metadata is null.</exception>
    public static DomainEventEnvelope<TEvent> Create<TEvent>(TEvent @event, IDomainEventMetadata metadata)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(metadata);

        return new DomainEventEnvelope<TEvent>
        {
            Event = @event,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates an envelope wrapping the specified domain event with empty metadata.
    /// </summary>
    /// <typeparam name="TEvent">The type of the domain event.</typeparam>
    /// <param name="event">The domain event to wrap.</param>
    /// <returns>A new envelope containing the event with empty metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when event is null.</exception>
    public static DomainEventEnvelope<TEvent> Create<TEvent>(TEvent @event)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        return new DomainEventEnvelope<TEvent>
        {
            Event = @event,
            Metadata = DomainEventMetadata.Empty
        };
    }

    /// <summary>
    /// Creates an envelope with correlation ID metadata.
    /// </summary>
    /// <typeparam name="TEvent">The type of the domain event.</typeparam>
    /// <param name="event">The domain event to wrap.</param>
    /// <param name="correlationId">The correlation ID for tracing.</param>
    /// <returns>A new envelope containing the event and correlation metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when event is null.</exception>
    public static DomainEventEnvelope<TEvent> WithCorrelation<TEvent>(TEvent @event, string correlationId)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        return new DomainEventEnvelope<TEvent>
        {
            Event = @event,
            Metadata = DomainEventMetadata.WithCorrelation(correlationId)
        };
    }
}

/// <summary>
/// Extension methods for domain events and envelopes.
/// </summary>
public static class DomainEventExtensions
{
    /// <summary>
    /// Wraps a domain event in an envelope with the specified metadata.
    /// </summary>
    /// <typeparam name="TEvent">The type of the domain event.</typeparam>
    /// <param name="event">The domain event to wrap.</param>
    /// <param name="metadata">The metadata to attach.</param>
    /// <returns>A new envelope containing the event and metadata.</returns>
    public static DomainEventEnvelope<TEvent> WithMetadata<TEvent>(
        this TEvent @event,
        IDomainEventMetadata metadata)
        where TEvent : IDomainEvent =>
        DomainEventEnvelope.Create(@event, metadata);

    /// <summary>
    /// Wraps a domain event in an envelope with empty metadata.
    /// </summary>
    /// <typeparam name="TEvent">The type of the domain event.</typeparam>
    /// <param name="event">The domain event to wrap.</param>
    /// <returns>A new envelope containing the event with empty metadata.</returns>
    public static DomainEventEnvelope<TEvent> ToEnvelope<TEvent>(this TEvent @event)
        where TEvent : IDomainEvent =>
        DomainEventEnvelope.Create(@event);

    /// <summary>
    /// Wraps a domain event in an envelope with correlation ID.
    /// </summary>
    /// <typeparam name="TEvent">The type of the domain event.</typeparam>
    /// <param name="event">The domain event to wrap.</param>
    /// <param name="correlationId">The correlation ID for tracing.</param>
    /// <returns>A new envelope containing the event and correlation metadata.</returns>
    public static DomainEventEnvelope<TEvent> WithCorrelation<TEvent>(
        this TEvent @event,
        string correlationId)
        where TEvent : IDomainEvent =>
        DomainEventEnvelope.WithCorrelation(@event, correlationId);

    /// <summary>
    /// Maps a domain event envelope to a new envelope with a different event type.
    /// </summary>
    /// <typeparam name="TSource">The source event type.</typeparam>
    /// <typeparam name="TTarget">The target event type.</typeparam>
    /// <param name="envelope">The source envelope.</param>
    /// <param name="mapper">The function to transform the event.</param>
    /// <returns>A new envelope with the transformed event and preserved metadata.</returns>
    public static DomainEventEnvelope<TTarget> Map<TSource, TTarget>(
        this DomainEventEnvelope<TSource> envelope,
        Func<TSource, TTarget> mapper)
        where TSource : IDomainEvent
        where TTarget : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(mapper);

        return new DomainEventEnvelope<TTarget>
        {
            Event = mapper(envelope.Event),
            Metadata = envelope.Metadata,
            EnvelopeId = envelope.EnvelopeId,
            EnvelopeCreatedAtUtc = envelope.EnvelopeCreatedAtUtc
        };
    }
}
