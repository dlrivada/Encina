namespace Encina.DomainModeling;

/// <summary>
/// Marker interface for domain events in Domain-Driven Design.
/// </summary>
/// <remarks>
/// <para>
/// Domain events represent something that happened in the domain that domain experts care about.
/// They are immutable and represent facts that have already occurred.
/// </para>
/// <para>
/// Domain events are typically used for:
/// <list type="bullet">
///   <item><description>Cross-aggregate communication within the same bounded context.</description></item>
///   <item><description>Triggering side effects (e.g., sending emails, updating read models).</description></item>
///   <item><description>Event sourcing (recording all changes as events).</description></item>
/// </list>
/// </para>
/// <para>
/// For cross-bounded-context or cross-service communication, use <see cref="IIntegrationEvent"/> instead.
/// </para>
/// </remarks>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when this event occurred (UTC).
    /// </summary>
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Base record for domain events providing default implementations.
/// </summary>
/// <remarks>
/// <para>
/// Using a record type ensures immutability and provides value-based equality by default.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record OrderPlaced(
///     Guid OrderId,
///     string CustomerName,
///     decimal Total) : DomainEvent;
///
/// // Usage in aggregate:
/// public void PlaceOrder(string customerName, decimal total)
/// {
///     // ... business logic ...
///     RaiseDomainEvent(new OrderPlaced(Id, customerName, total));
/// }
/// </code>
/// </example>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredAtUtc { get; init; } = TimeProvider.System.GetUtcNow().UtcDateTime;
}

/// <summary>
/// Extended domain event with metadata for distributed tracing and event sourcing.
/// </summary>
/// <remarks>
/// <para>
/// This extended version includes:
/// <list type="bullet">
///   <item><description>CorrelationId: Links related events across operations.</description></item>
///   <item><description>CausationId: Identifies the event that caused this event.</description></item>
///   <item><description>AggregateId: The ID of the aggregate that raised this event.</description></item>
///   <item><description>AggregateVersion: The version of the aggregate after this event.</description></item>
/// </list>
/// </para>
/// </remarks>
public abstract record RichDomainEvent : DomainEvent
{
    /// <summary>
    /// Gets or sets the correlation ID for distributed tracing.
    /// All events in a single operation share the same correlation ID.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets or sets the causation ID - the ID of the event that caused this event.
    /// </summary>
    public string? CausationId { get; init; }

    /// <summary>
    /// Gets or sets the ID of the aggregate that raised this event.
    /// </summary>
    public Guid AggregateId { get; init; }

    /// <summary>
    /// Gets or sets the version of the aggregate after this event was applied.
    /// </summary>
    public long AggregateVersion { get; init; }

    /// <summary>
    /// Gets or sets the version of this event type schema.
    /// Used for event versioning and upcasting.
    /// </summary>
    public int EventVersion { get; init; } = 1;
}
