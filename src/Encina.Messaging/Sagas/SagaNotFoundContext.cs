using Encina.Messaging.DeadLetter;
using LanguageExt;

namespace Encina.Messaging.Sagas;

/// <summary>
/// Provides context and actions for handling saga not found scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This class is passed to <see cref="IHandleSagaNotFound{TMessage}"/> handlers
/// to provide information about the failed correlation and available actions.
/// </para>
/// <para>
/// <see cref="MoveToDeadLetterAsync"/> stores the message in the dead letter queue when
/// <see cref="ISagaNotFoundDispatcher"/> dispatches the context with the dead letter queue registered and
/// <c>DeadLetterOptions.IntegrateWithSagas</c> on. The dead letter is keyed by <see cref="SourceMessageId"/>, the
/// identity the caller holds for the message (for example the transport message id): a redelivery of the same
/// message keeps one dead letter, and without an id nothing is stored (Encina never guesses a key).
/// </para>
/// </remarks>
public sealed class SagaNotFoundContext
{
    private Func<CancellationToken, Task<Either<EncinaError, Unit>>>? _moveToDeadLetterAsync;
    private SagaNotFoundAction _action = SagaNotFoundAction.None;
    private string? _deadLetterReason;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaNotFoundContext"/> class.
    /// </summary>
    /// <param name="sagaId">The saga ID that was not found.</param>
    /// <param name="sagaType">The expected saga type name.</param>
    /// <param name="messageType">The type of the message that failed to correlate.</param>
    /// <param name="sourceMessageId">
    /// The identity of the message that the caller holds (for example the transport message id); the dead letter
    /// queue keys the message by it. It must be unique among the dead letters of the saga source (saga ids are
    /// GUIDs). Without it, <see cref="MoveToDeadLetterAsync"/> returns
    /// <see cref="DeadLetterErrorCodes.SourceMessageIdRequired"/>.
    /// </param>
    public SagaNotFoundContext(
        Guid sagaId,
        string sagaType,
        Type messageType,
        string? sourceMessageId = null)
    {
        SagaId = sagaId;
        SagaType = sagaType ?? throw new ArgumentNullException(nameof(sagaType));
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        SourceMessageId = sourceMessageId;
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
    /// Gets the identity of the message the caller holds, which keys its dead letter; <see langword="null"/> when
    /// the caller supplied none.
    /// </summary>
    public string? SourceMessageId { get; }

    /// <summary>
    /// Gets the action that was chosen by the handler.
    /// </summary>
    public SagaNotFoundAction Action => _action;

    /// <summary>
    /// Gets the reason provided when moving to dead letter queue. It stays in memory: the dead letter records
    /// the error code <see cref="SagaErrorCodes.NotFound"/>, never this text.
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
        FailedMove = null;
    }

    /// <summary>
    /// The error of the last <see cref="MoveToDeadLetterAsync"/> call when it failed and the handler then neither
    /// moved the message nor ignored it; <see cref="ISagaNotFoundDispatcher"/> returns it, so the caller does not
    /// acknowledge a message that is in no dead letter queue.
    /// </summary>
    internal EncinaError? FailedMove { get; private set; }

    /// <summary>
    /// Moves the message to the dead letter queue for later investigation.
    /// </summary>
    /// <param name="reason">The reason for moving to DLQ; kept in <see cref="DeadLetterReason"/>, never stored.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>Right</c> when the message was stored (or was already stored for the same <see cref="SourceMessageId"/>);
    /// <c>Left</c> with <see cref="DeadLetterErrorCodes.NotConfigured"/> when the dead letter queue is not registered
    /// or <c>DeadLetterOptions.IntegrateWithSagas</c> is off, with
    /// <see cref="DeadLetterErrorCodes.SourceMessageIdRequired"/> when no <see cref="SourceMessageId"/> was supplied,
    /// or with the capture's error when storing failed. Only a <c>Right</c> marks the message as moved.
    /// </returns>
    public async ValueTask<Either<EncinaError, Unit>> MoveToDeadLetterAsync(string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var moved = await MoveCoreAsync(cancellationToken).ConfigureAwait(false);
        if (moved.IsRight)
        {
            _deadLetterReason = reason;
            _action = SagaNotFoundAction.MovedToDeadLetter;
            FailedMove = null;
        }
        else
        {
            FailedMove = moved.LeftToArray()[0];
        }

        return moved;
    }

    private async Task<Either<EncinaError, Unit>> MoveCoreAsync(CancellationToken cancellationToken)
    {
        if (_moveToDeadLetterAsync is null)
        {
            return EncinaErrors.Create(
                DeadLetterErrorCodes.NotConfigured,
                "Dead letter handling is not configured: register the dead letter queue and keep DeadLetterOptions.IntegrateWithSagas on.");
        }

        if (string.IsNullOrWhiteSpace(SourceMessageId))
        {
            return EncinaErrors.Create(
                DeadLetterErrorCodes.SourceMessageIdRequired,
                "A dead letter of a saga-not-found message needs the message's identity (sourceMessageId).");
        }

        return await _moveToDeadLetterAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Connects <see cref="MoveToDeadLetterAsync"/> to the dead letter capture, or disconnects it with
    /// <see langword="null"/>. Internal: only <see cref="ISagaNotFoundDispatcher"/> wires it, on every dispatch.
    /// </summary>
    internal void UseDeadLetter(Func<CancellationToken, Task<Either<EncinaError, Unit>>>? moveToDeadLetterAsync)
    {
        _moveToDeadLetterAsync = moveToDeadLetterAsync;
        FailedMove = null;
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
