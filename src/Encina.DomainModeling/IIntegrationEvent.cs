namespace Encina.DomainModeling;

/// <summary>
/// Marker interface for integration events used for cross-bounded-context or cross-service communication.
/// </summary>
/// <remarks>
/// <para>
/// Integration events are different from domain events:
/// <list type="bullet">
///   <item><description>Domain events: Internal to a bounded context, in-process.</description></item>
///   <item><description>Integration events: Cross-bounded-context or cross-service, often via message brokers.</description></item>
/// </list>
/// </para>
/// <para>
/// Integration events should be:
/// <list type="bullet">
///   <item><description>Versioned: Include a version number for schema evolution.</description></item>
///   <item><description>Self-describing: Include all necessary context.</description></item>
///   <item><description>Stable: Changes should be backward compatible when possible.</description></item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="IDomainEventToIntegrationEventMapper{TDomain,TIntegration}"/> to translate domain events
/// to integration events at bounded context boundaries.
/// </para>
/// </remarks>
public interface IIntegrationEvent
{
    /// <summary>
    /// Gets the unique identifier of this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when this event occurred (UTC).
    /// </summary>
    DateTime OccurredAtUtc { get; }

    /// <summary>
    /// Gets the version of this event type schema.
    /// </summary>
    int EventVersion { get; }

    /// <summary>
    /// Gets the correlation ID for distributed tracing.
    /// </summary>
    string? CorrelationId { get; }
}

/// <summary>
/// Base record for integration events providing default implementations.
/// </summary>
/// <example>
/// <code>
/// public sealed record OrderPlacedIntegrationEvent(
///     Guid OrderId,
///     string CustomerEmail,
///     decimal Total) : IntegrationEvent
/// {
///     public override int EventVersion => 2;
/// }
/// </code>
/// </example>
public abstract record IntegrationEvent : IIntegrationEvent
{
    /// <inheritdoc />
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    /// <inheritdoc />
    public virtual int EventVersion => 1;

    /// <inheritdoc />
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Interface for mapping domain events to integration events.
/// Implements the Anti-Corruption Layer pattern at bounded context boundaries.
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type to map from.</typeparam>
/// <typeparam name="TIntegrationEvent">The integration event type to map to.</typeparam>
/// <remarks>
/// <para>
/// This interface is part of the Anti-Corruption Layer pattern. It ensures that internal
/// domain models (domain events) are translated to stable public contracts (integration events)
/// before being published to other bounded contexts or services.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderPlacedMapper : IDomainEventToIntegrationEventMapper&lt;OrderPlaced, OrderPlacedIntegrationEvent&gt;
/// {
///     public OrderPlacedIntegrationEvent Map(OrderPlaced domainEvent)
///     {
///         return new OrderPlacedIntegrationEvent(
///             domainEvent.OrderId,
///             domainEvent.CustomerEmail,
///             domainEvent.Total)
///         {
///             CorrelationId = domainEvent.CorrelationId
///         };
///     }
/// }
/// </code>
/// </example>
public interface IDomainEventToIntegrationEventMapper<in TDomainEvent, out TIntegrationEvent>
    where TDomainEvent : IDomainEvent
    where TIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Maps a domain event to an integration event.
    /// </summary>
    /// <param name="domainEvent">The domain event to map.</param>
    /// <returns>The corresponding integration event.</returns>
    TIntegrationEvent Map(TDomainEvent domainEvent);
}
