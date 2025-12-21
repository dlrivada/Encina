namespace SimpleMediator.Messaging.Choreography;

/// <summary>
/// Marker interface for choreography-based sagas.
/// </summary>
/// <remarks>
/// <para>
/// Choreography sagas are event-driven distributed transactions where each
/// participant listens to events and decides what to do next. This contrasts
/// with orchestration sagas where a central coordinator controls the flow.
/// </para>
/// <para>
/// <b>Key Characteristics</b>:
/// <list type="bullet">
/// <item><description><b>Decentralized</b>: No central coordinator</description></item>
/// <item><description><b>Event-Driven</b>: Services react to events</description></item>
/// <item><description><b>Loosely Coupled</b>: Services don't know about each other</description></item>
/// <item><description><b>Scalable</b>: Easy to add new participants</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Event Flow Example (Order Processing)</b>:
/// <code>
/// OrderCreated → InventoryReserved → PaymentProcessed → OrderShipped → OrderCompleted
///                                  ↘ (failure) → PaymentFailed → InventoryReleased → OrderCancelled
/// </code>
/// </para>
/// </remarks>
public interface IChoreographySaga
{
    /// <summary>
    /// Gets the unique correlation ID for this saga instance.
    /// </summary>
    /// <remarks>
    /// The correlation ID is used to track all events belonging to this saga flow.
    /// </remarks>
    string CorrelationId { get; }
}

/// <summary>
/// Choreography saga with typed state data.
/// </summary>
/// <typeparam name="TState">The type of state accumulated during the saga.</typeparam>
public interface IChoreographySaga<TState> : IChoreographySaga
    where TState : class, new()
{
    /// <summary>
    /// Gets or sets the current state of the saga.
    /// </summary>
    TState State { get; set; }
}
