using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling;

/// <summary>
/// Async interface for mapping domain events to integration events.
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type to map from.</typeparam>
/// <typeparam name="TIntegrationEvent">The integration event type to map to.</typeparam>
/// <remarks>
/// <para>
/// Use this interface when the mapping requires async operations, such as:
/// <list type="bullet">
///   <item><description>Looking up additional data from a database.</description></item>
///   <item><description>Calling external services for enrichment.</description></item>
///   <item><description>Fetching configuration from remote sources.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IAsyncDomainEventToIntegrationEventMapper<in TDomainEvent, TIntegrationEvent>
    where TDomainEvent : IDomainEvent
    where TIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Asynchronously maps a domain event to an integration event.
    /// </summary>
    /// <param name="domainEvent">The domain event to map.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the corresponding integration event.</returns>
    Task<TIntegrationEvent> MapAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result-oriented async mapper that can fail with an error.
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type to map from.</typeparam>
/// <typeparam name="TIntegrationEvent">The integration event type to map to.</typeparam>
/// <typeparam name="TError">The error type for failed mappings.</typeparam>
public interface IFallibleDomainEventToIntegrationEventMapper<in TDomainEvent, TIntegrationEvent, TError>
    where TDomainEvent : IDomainEvent
    where TIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Maps a domain event to an integration event, returning an Either for ROP.
    /// </summary>
    /// <param name="domainEvent">The domain event to map.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either an error or the integration event.</returns>
    Task<Either<TError, TIntegrationEvent>> MapAsync(
        TDomainEvent domainEvent,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Error type for integration event mapping failures.
/// </summary>
/// <param name="Message">Description of the mapping failure.</param>
/// <param name="ErrorCode">Machine-readable error code.</param>
/// <param name="DomainEventType">The type of domain event that failed to map.</param>
/// <param name="IntegrationEventType">The target integration event type.</param>
/// <param name="InnerException">Optional inner exception.</param>
public sealed record IntegrationEventMappingError(
    string Message,
    string ErrorCode,
    Type DomainEventType,
    Type IntegrationEventType,
    Exception? InnerException = null)
{
    /// <summary>
    /// Creates an error for when a required field is missing.
    /// </summary>
    public static IntegrationEventMappingError MissingField<TDomain, TIntegration>(string fieldName)
        where TDomain : IDomainEvent
        where TIntegration : IIntegrationEvent =>
        new(
            $"Required field '{fieldName}' is missing or null",
            "MAPPING_MISSING_FIELD",
            typeof(TDomain),
            typeof(TIntegration));

    /// <summary>
    /// Creates an error for when a validation fails during mapping.
    /// </summary>
    public static IntegrationEventMappingError ValidationFailed<TDomain, TIntegration>(string reason)
        where TDomain : IDomainEvent
        where TIntegration : IIntegrationEvent =>
        new(
            $"Validation failed: {reason}",
            "MAPPING_VALIDATION_FAILED",
            typeof(TDomain),
            typeof(TIntegration));

    /// <summary>
    /// Creates an error for when an external lookup fails.
    /// </summary>
    public static IntegrationEventMappingError LookupFailed<TDomain, TIntegration>(
        string resource,
        Exception? exception = null)
        where TDomain : IDomainEvent
        where TIntegration : IIntegrationEvent =>
        new(
            $"Failed to lookup resource: {resource}",
            "MAPPING_LOOKUP_FAILED",
            typeof(TDomain),
            typeof(TIntegration),
            exception);
}

/// <summary>
/// Extension methods for integration event mapping.
/// </summary>
public static class IntegrationEventMappingExtensions
{
    /// <summary>
    /// Maps a domain event to an integration event using the provided mapper.
    /// </summary>
    /// <typeparam name="TDomain">The domain event type.</typeparam>
    /// <typeparam name="TIntegration">The integration event type.</typeparam>
    /// <param name="domainEvent">The domain event to map.</param>
    /// <param name="mapper">The mapper to use.</param>
    /// <returns>The mapped integration event.</returns>
    public static TIntegration MapTo<TDomain, TIntegration>(
        this TDomain domainEvent,
        IDomainEventToIntegrationEventMapper<TDomain, TIntegration> mapper)
        where TDomain : IDomainEvent
        where TIntegration : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(mapper);

        return mapper.Map(domainEvent);
    }

    /// <summary>
    /// Asynchronously maps a domain event to an integration event.
    /// </summary>
    /// <typeparam name="TDomain">The domain event type.</typeparam>
    /// <typeparam name="TIntegration">The integration event type.</typeparam>
    /// <param name="domainEvent">The domain event to map.</param>
    /// <param name="mapper">The async mapper to use.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the mapped integration event.</returns>
    public static Task<TIntegration> MapToAsync<TDomain, TIntegration>(
        this TDomain domainEvent,
        IAsyncDomainEventToIntegrationEventMapper<TDomain, TIntegration> mapper,
        CancellationToken cancellationToken = default)
        where TDomain : IDomainEvent
        where TIntegration : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        ArgumentNullException.ThrowIfNull(mapper);

        return mapper.MapAsync(domainEvent, cancellationToken);
    }

    /// <summary>
    /// Maps a collection of domain events to integration events.
    /// </summary>
    /// <typeparam name="TDomain">The domain event type.</typeparam>
    /// <typeparam name="TIntegration">The integration event type.</typeparam>
    /// <param name="domainEvents">The domain events to map.</param>
    /// <param name="mapper">The mapper to use.</param>
    /// <returns>An enumerable of mapped integration events.</returns>
    public static IEnumerable<TIntegration> MapAll<TDomain, TIntegration>(
        this IEnumerable<TDomain> domainEvents,
        IDomainEventToIntegrationEventMapper<TDomain, TIntegration> mapper)
        where TDomain : IDomainEvent
        where TIntegration : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        ArgumentNullException.ThrowIfNull(mapper);

        return MapAllIterator(domainEvents, mapper);
    }

    private static IEnumerable<TIntegration> MapAllIterator<TDomain, TIntegration>(
        IEnumerable<TDomain> domainEvents,
        IDomainEventToIntegrationEventMapper<TDomain, TIntegration> mapper)
        where TDomain : IDomainEvent
        where TIntegration : IIntegrationEvent
    {
        foreach (var domainEvent in domainEvents)
        {
            yield return mapper.Map(domainEvent);
        }
    }

    /// <summary>
    /// Asynchronously maps a collection of domain events to integration events.
    /// </summary>
    /// <typeparam name="TDomain">The domain event type.</typeparam>
    /// <typeparam name="TIntegration">The integration event type.</typeparam>
    /// <param name="domainEvents">The domain events to map.</param>
    /// <param name="mapper">The async mapper to use.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task containing the mapped integration events.</returns>
    public static async Task<IReadOnlyList<TIntegration>> MapAllAsync<TDomain, TIntegration>(
        this IEnumerable<TDomain> domainEvents,
        IAsyncDomainEventToIntegrationEventMapper<TDomain, TIntegration> mapper,
        CancellationToken cancellationToken = default)
        where TDomain : IDomainEvent
        where TIntegration : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        ArgumentNullException.ThrowIfNull(mapper);

        var results = new List<TIntegration>();
        foreach (var domainEvent in domainEvents)
        {
            var result = await mapper.MapAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Attempts to map a domain event, returning None if mapping fails.
    /// </summary>
    /// <typeparam name="TDomain">The domain event type.</typeparam>
    /// <typeparam name="TIntegration">The integration event type.</typeparam>
    /// <param name="domainEvent">The domain event to map.</param>
    /// <param name="mapper">The mapper to use.</param>
    /// <returns>Some(integration event) on success, None on failure.</returns>
    public static Option<TIntegration> TryMapTo<TDomain, TIntegration>(
        this TDomain domainEvent,
        IDomainEventToIntegrationEventMapper<TDomain, TIntegration> mapper)
        where TDomain : IDomainEvent
        where TIntegration : IIntegrationEvent
    {
        if (domainEvent is null || mapper is null)
        {
            return None;
        }

        try
        {
            return Some(mapper.Map(domainEvent));
        }
        catch
        {
            return None;
        }
    }

    /// <summary>
    /// Creates a composite mapper that applies multiple mappers in sequence.
    /// </summary>
    /// <typeparam name="TDomain">The domain event type.</typeparam>
    /// <typeparam name="TIntermediate">The intermediate event type.</typeparam>
    /// <typeparam name="TIntegration">The final integration event type.</typeparam>
    /// <param name="firstMapper">The first mapper (domain to intermediate).</param>
    /// <param name="secondMapper">The second mapper (intermediate to integration).</param>
    /// <returns>A composite mapper.</returns>
    public static IDomainEventToIntegrationEventMapper<TDomain, TIntegration> Compose<TDomain, TIntermediate, TIntegration>(
        this IDomainEventToIntegrationEventMapper<TDomain, TIntermediate> firstMapper,
        IDomainEventToIntegrationEventMapper<TIntermediate, TIntegration> secondMapper)
        where TDomain : IDomainEvent
        where TIntermediate : IDomainEvent, IIntegrationEvent
        where TIntegration : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(firstMapper);
        ArgumentNullException.ThrowIfNull(secondMapper);

        return new CompositeMapper<TDomain, TIntermediate, TIntegration>(firstMapper, secondMapper);
    }

    private sealed class CompositeMapper<TDomain, TIntermediate, TIntegration>(
        IDomainEventToIntegrationEventMapper<TDomain, TIntermediate> first,
        IDomainEventToIntegrationEventMapper<TIntermediate, TIntegration> second)
        : IDomainEventToIntegrationEventMapper<TDomain, TIntegration>
        where TDomain : IDomainEvent
        where TIntermediate : IDomainEvent, IIntegrationEvent
        where TIntegration : IIntegrationEvent
    {
        public TIntegration Map(TDomain domainEvent)
        {
            var intermediate = first.Map(domainEvent);
            return second.Map(intermediate);
        }
    }
}

/// <summary>
/// Interface for publishing integration events (typically via Outbox pattern).
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publishes an integration event.
    /// </summary>
    /// <typeparam name="TEvent">The integration event type.</typeparam>
    /// <param name="integrationEvent">The event to publish.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;

    /// <summary>
    /// Publishes multiple integration events.
    /// </summary>
    /// <typeparam name="TEvent">The integration event type.</typeparam>
    /// <param name="events">The events to publish.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PublishManyAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}

/// <summary>
/// Result-oriented publisher that returns Either for failures.
/// </summary>
/// <typeparam name="TError">The error type for publishing failures.</typeparam>
public interface IFallibleIntegrationEventPublisher<TError>
{
    /// <summary>
    /// Attempts to publish an integration event.
    /// </summary>
    /// <typeparam name="TEvent">The integration event type.</typeparam>
    /// <param name="integrationEvent">The event to publish.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Either an error or Unit on success.</returns>
    Task<Either<TError, Unit>> PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}

/// <summary>
/// Error type for integration event publishing failures.
/// </summary>
/// <param name="Message">Description of the publishing failure.</param>
/// <param name="ErrorCode">Machine-readable error code.</param>
/// <param name="EventType">The type of event that failed to publish.</param>
/// <param name="EventId">The ID of the event that failed.</param>
/// <param name="InnerException">Optional inner exception.</param>
public sealed record IntegrationEventPublishError(
    string Message,
    string ErrorCode,
    Type EventType,
    Guid EventId,
    Exception? InnerException = null)
{
    /// <summary>
    /// Creates an error for when serialization fails.
    /// </summary>
    public static IntegrationEventPublishError SerializationFailed<TEvent>(
        Guid eventId,
        Exception exception)
        where TEvent : IIntegrationEvent =>
        new(
            $"Failed to serialize event: {exception.Message}",
            "PUBLISH_SERIALIZATION_FAILED",
            typeof(TEvent),
            eventId,
            exception);

    /// <summary>
    /// Creates an error for when the outbox store fails.
    /// </summary>
    public static IntegrationEventPublishError OutboxStoreFailed<TEvent>(
        Guid eventId,
        Exception exception)
        where TEvent : IIntegrationEvent =>
        new(
            $"Failed to store event in outbox: {exception.Message}",
            "PUBLISH_OUTBOX_FAILED",
            typeof(TEvent),
            eventId,
            exception);

    /// <summary>
    /// Creates an error for when the message broker fails.
    /// </summary>
    public static IntegrationEventPublishError BrokerFailed<TEvent>(
        Guid eventId,
        string brokerName,
        Exception exception)
        where TEvent : IIntegrationEvent =>
        new(
            $"Failed to publish to broker '{brokerName}': {exception.Message}",
            "PUBLISH_BROKER_FAILED",
            typeof(TEvent),
            eventId,
            exception);
}
