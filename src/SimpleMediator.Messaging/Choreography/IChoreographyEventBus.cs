namespace SimpleMediator.Messaging.Choreography;

/// <summary>
/// Event bus for choreography saga communication.
/// </summary>
/// <remarks>
/// <para>
/// The choreography event bus routes events to registered reactions and maintains
/// saga context across the event flow.
/// </para>
/// <para>
/// <b>Event Flow</b>:
/// <list type="number">
/// <item><description>Event is published with correlation ID</description></item>
/// <item><description>Bus finds registered reactions for event type</description></item>
/// <item><description>Reactions execute with saga context</description></item>
/// <item><description>Reactions may publish new events continuing the flow</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IChoreographyEventBus
{
    /// <summary>
    /// Starts a new choreography saga with the given event.
    /// </summary>
    /// <typeparam name="TEvent">The type of the initiating event.</typeparam>
    /// <param name="domainEvent">The event that starts the saga.</param>
    /// <param name="correlationId">Optional correlation ID. If not provided, a new one is generated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The correlation ID for tracking the saga.</returns>
    Task<string> StartSagaAsync<TEvent>(
        TEvent domainEvent,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
        where TEvent : class;

    /// <summary>
    /// Publishes an event within an existing saga flow.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish.</typeparam>
    /// <param name="domainEvent">The event to publish.</param>
    /// <param name="correlationId">The correlation ID of the saga.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TEvent>(
        TEvent domainEvent,
        string correlationId,
        CancellationToken cancellationToken = default)
        where TEvent : class;

    /// <summary>
    /// Marks a saga as completed successfully.
    /// </summary>
    /// <param name="correlationId">The correlation ID of the saga.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CompleteSagaAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Triggers compensation for a saga due to failure.
    /// </summary>
    /// <param name="correlationId">The correlation ID of the saga.</param>
    /// <param name="errorMessage">The error that caused the failure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CompensateAsync(
        string correlationId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of a saga.
    /// </summary>
    /// <param name="correlationId">The correlation ID of the saga.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga state if found.</returns>
    Task<IChoreographyState?> GetSagaStateAsync(
        string correlationId,
        CancellationToken cancellationToken = default);
}
