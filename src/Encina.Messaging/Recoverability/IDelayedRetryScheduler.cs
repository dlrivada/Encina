namespace Encina.Messaging.Recoverability;

/// <summary>
/// Schedules delayed retries for message processing.
/// </summary>
/// <remarks>
/// <para>
/// Implementations typically persist the retry request using the Scheduling pattern
/// or an external scheduler like Hangfire/Quartz.
/// </para>
/// <para>
/// The scheduler is responsible for:
/// <list type="bullet">
/// <item><description>Persisting the retry request</description></item>
/// <item><description>Scheduling execution after the specified delay</description></item>
/// <item><description>Re-dispatching the request through the pipeline when due</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IDelayedRetryScheduler
{
    /// <summary>
    /// Schedules a delayed retry for a failed request.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request to retry.</param>
    /// <param name="context">The recoverability context with failure history.</param>
    /// <param name="delay">The delay before the retry should be executed.</param>
    /// <param name="delayedRetryAttempt">The current delayed retry attempt (0-based).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the scheduling operation.</returns>
    Task ScheduleRetryAsync<TRequest>(
        TRequest request,
        RecoverabilityContext context,
        TimeSpan delay,
        int delayedRetryAttempt,
        CancellationToken cancellationToken = default)
        where TRequest : notnull;

    /// <summary>
    /// Cancels a previously scheduled retry.
    /// </summary>
    /// <param name="recoverabilityContextId">The ID of the recoverability context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the retry was found and cancelled, false otherwise.</returns>
    Task<bool> CancelScheduledRetryAsync(
        Guid recoverabilityContextId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a scheduled delayed retry stored for later execution.
/// </summary>
public interface IDelayedRetryMessage
{
    /// <summary>
    /// Gets the unique identifier for this delayed retry.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the recoverability context ID this retry belongs to.
    /// </summary>
    Guid RecoverabilityContextId { get; }

    /// <summary>
    /// Gets the fully qualified type name of the request.
    /// </summary>
    string RequestType { get; }

    /// <summary>
    /// Gets the serialized request content.
    /// </summary>
    string RequestContent { get; }

    /// <summary>
    /// Gets the serialized recoverability context.
    /// </summary>
    string ContextContent { get; }

    /// <summary>
    /// Gets the delayed retry attempt number (0-based).
    /// </summary>
    int DelayedRetryAttempt { get; }

    /// <summary>
    /// Gets when the retry was scheduled (UTC).
    /// </summary>
    DateTime ScheduledAtUtc { get; }

    /// <summary>
    /// Gets when the retry should execute (UTC).
    /// </summary>
    DateTime ExecuteAtUtc { get; }

    /// <summary>
    /// Gets the correlation ID from the original request.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets when the retry was processed (UTC), or null if pending.
    /// </summary>
    DateTime? ProcessedAtUtc { get; }

    /// <summary>
    /// Gets the error message if processing failed.
    /// </summary>
    string? ErrorMessage { get; }
}

/// <summary>
/// Factory for creating delayed retry messages.
/// </summary>
public interface IDelayedRetryMessageFactory
{
    /// <summary>
    /// Creates a new delayed retry message.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="recoverabilityContextId">The recoverability context ID.</param>
    /// <param name="requestType">The request type name.</param>
    /// <param name="requestContent">The serialized request.</param>
    /// <param name="contextContent">The serialized context.</param>
    /// <param name="delayedRetryAttempt">The delayed retry attempt number.</param>
    /// <param name="scheduledAtUtc">When the retry was scheduled.</param>
    /// <param name="executeAtUtc">When the retry should execute.</param>
    /// <param name="correlationId">The correlation ID.</param>
    /// <returns>A new delayed retry message.</returns>
    IDelayedRetryMessage Create(
        Guid id,
        Guid recoverabilityContextId,
        string requestType,
        string requestContent,
        string contextContent,
        int delayedRetryAttempt,
        DateTime scheduledAtUtc,
        DateTime executeAtUtc,
        string? correlationId);
}

/// <summary>
/// Store for delayed retry messages.
/// </summary>
public interface IDelayedRetryStore
{
    /// <summary>
    /// Adds a delayed retry message to the store.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(IDelayedRetryMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending delayed retry messages that are due for execution.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of pending messages.</returns>
    Task<IEnumerable<IDelayedRetryMessage>> GetPendingMessagesAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a delayed retry as processed.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a delayed retry as failed.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkAsFailedAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a delayed retry message.
    /// </summary>
    /// <param name="recoverabilityContextId">The recoverability context ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the message was found and deleted.</returns>
    Task<bool> DeleteByContextIdAsync(Guid recoverabilityContextId, CancellationToken cancellationToken = default);
}
