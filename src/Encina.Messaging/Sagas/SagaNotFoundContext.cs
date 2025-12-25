namespace Encina.Messaging.Sagas;

/// <summary>
/// Provides context and actions for handling saga not found scenarios.
/// </summary>
/// <remarks>
/// This class is passed to <see cref="IHandleSagaNotFound{TMessage}"/> handlers
/// to provide information about the failed correlation and available actions.
/// </remarks>
public sealed class SagaNotFoundContext
{
    private readonly Func<string, CancellationToken, Task>? _moveToDeadLetterAsync;
    private SagaNotFoundAction _action = SagaNotFoundAction.None;
    private string? _deadLetterReason;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaNotFoundContext"/> class.
    /// </summary>
    /// <param name="sagaId">The saga ID that was not found.</param>
    /// <param name="sagaType">The expected saga type name.</param>
    /// <param name="messageType">The type of the message that failed to correlate.</param>
    /// <param name="moveToDeadLetterAsync">Optional delegate to move message to DLQ.</param>
    public SagaNotFoundContext(
        Guid sagaId,
        string sagaType,
        Type messageType,
        Func<string, CancellationToken, Task>? moveToDeadLetterAsync = null)
    {
        SagaId = sagaId;
        SagaType = sagaType ?? throw new ArgumentNullException(nameof(sagaType));
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        _moveToDeadLetterAsync = moveToDeadLetterAsync;
    }

    /// <summary>
    /// Gets the saga ID that was not found.
    /// </summary>
    public Guid SagaId { get; }

    /// <summary>
    /// Gets the expected saga type name.
    /// </summary>
    public string SagaType { get; }

    /// <summary>
    /// Gets the type of the message that failed to correlate.
    /// </summary>
    public Type MessageType { get; }

    /// <summary>
    /// Gets the action that was chosen by the handler.
    /// </summary>
    public SagaNotFoundAction Action => _action;

    /// <summary>
    /// Gets the reason provided when moving to dead letter queue.
    /// </summary>
    public string? DeadLetterReason => _deadLetterReason;

    /// <summary>
    /// Gets a value indicating whether the handler chose to ignore the message.
    /// </summary>
    public bool WasIgnored => _action == SagaNotFoundAction.Ignored;

    /// <summary>
    /// Gets a value indicating whether the message was moved to the dead letter queue.
    /// </summary>
    public bool WasMovedToDeadLetter => _action == SagaNotFoundAction.MovedToDeadLetter;

    /// <summary>
    /// Marks the message as ignored (no further action needed).
    /// </summary>
    /// <remarks>
    /// Use this when the missing saga is expected or acceptable,
    /// such as duplicate messages or messages arriving after saga completion.
    /// </remarks>
    public void Ignore()
    {
        _action = SagaNotFoundAction.Ignored;
    }

    /// <summary>
    /// Moves the message to the dead letter queue for later investigation.
    /// </summary>
    /// <param name="reason">The reason for moving to DLQ.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no dead letter handler is configured.
    /// </exception>
    public async Task MoveToDeadLetterAsync(string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (_moveToDeadLetterAsync == null)
        {
            throw new InvalidOperationException(
                "Dead letter handling is not configured. " +
                "Ensure UseDeadLetterQueue is enabled in MessagingConfiguration.");
        }

        await _moveToDeadLetterAsync(reason, cancellationToken).ConfigureAwait(false);
        _deadLetterReason = reason;
        _action = SagaNotFoundAction.MovedToDeadLetter;
    }
}

/// <summary>
/// Represents the action taken when a saga is not found.
/// </summary>
public enum SagaNotFoundAction
{
    /// <summary>
    /// No action was explicitly taken by the handler.
    /// </summary>
    None = 0,

    /// <summary>
    /// The message was explicitly ignored.
    /// </summary>
    Ignored = 1,

    /// <summary>
    /// The message was moved to the dead letter queue.
    /// </summary>
    MovedToDeadLetter = 2
}
