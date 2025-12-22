namespace Encina.Messaging.Choreography;

/// <summary>
/// Represents a scope for handling events in a choreography saga.
/// </summary>
/// <remarks>
/// This interface provides context for event handlers within a choreography flow,
/// including access to the correlation ID and the ability to publish new events.
/// </remarks>
public interface IEventHandlerScope
{
    /// <summary>
    /// Gets the correlation ID for the current saga flow.
    /// </summary>
    /// <remarks>
    /// All events in a choreography saga share the same correlation ID,
    /// allowing tracking of the entire flow.
    /// </remarks>
    string CorrelationId { get; }

    /// <summary>
    /// Publishes an event to continue the choreography flow.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish.</typeparam>
    /// <param name="domainEvent">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : class;

    /// <summary>
    /// Records a compensation action to execute if the saga fails.
    /// </summary>
    /// <param name="compensationAction">The action to execute for compensation.</param>
    void AddCompensation(Func<CancellationToken, Task> compensationAction);
}
