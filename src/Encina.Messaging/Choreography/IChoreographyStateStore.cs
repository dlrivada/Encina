namespace Encina.Messaging.Choreography;

/// <summary>
/// Stores the state of choreography sagas for recovery and tracking.
/// </summary>
/// <remarks>
/// <para>
/// The choreography state store persists:
/// <list type="bullet">
/// <item><description>Saga state data</description></item>
/// <item><description>Registered compensation actions</description></item>
/// <item><description>Event history for the saga flow</description></item>
/// <item><description>Saga status (Running, Completed, Failed, Compensating)</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IChoreographyStateStore
{
    /// <summary>
    /// Creates a new saga with the given correlation ID.
    /// </summary>
    /// <param name="correlationId">The unique correlation ID for the saga.</param>
    /// <param name="sagaType">The type name of the saga.</param>
    /// <param name="initialState">The initial serialized state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateAsync(
        string correlationId,
        string sagaType,
        string? initialState,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the saga state by correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga state if found, null otherwise.</returns>
    Task<IChoreographyState?> GetAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the saga state.
    /// </summary>
    /// <param name="state">The updated saga state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateAsync(
        IChoreographyState state,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an event to the saga's event history.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="eventType">The type name of the event.</param>
    /// <param name="eventPayload">The serialized event payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddEventAsync(
        string correlationId,
        string eventType,
        string eventPayload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a compensation action to the saga.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="compensationId">Unique ID for this compensation action.</param>
    /// <param name="compensationPayload">Serialized compensation action details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddCompensationAsync(
        string correlationId,
        string compensationId,
        string compensationPayload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending compensations for a saga.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of compensation payloads in reverse order (most recent first).</returns>
    Task<IReadOnlyList<string>> GetPendingCompensationsAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a compensation as completed.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="compensationId">The compensation ID to mark as complete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkCompensationCompletedAsync(
        string correlationId,
        string compensationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stuck sagas that need intervention.
    /// </summary>
    /// <param name="timeout">Time after which a saga is considered stuck.</param>
    /// <param name="batchSize">Maximum number of sagas to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<IChoreographyState>> GetStuckSagasAsync(
        TimeSpan timeout,
        int batchSize,
        CancellationToken cancellationToken = default);
}
