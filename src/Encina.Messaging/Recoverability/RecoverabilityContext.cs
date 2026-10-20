using System.Collections.Immutable;
using LanguageExt;

namespace Encina.Messaging.Recoverability;

/// <summary>
/// Tracks the state of recoverability for a single message processing attempt.
/// </summary>
/// <remarks>
/// This context is maintained during the lifecycle of a message processing attempt
/// and tracks all retry attempts, errors, and timing information.
/// </remarks>
public sealed class RecoverabilityContext
{
    private readonly List<RetryAttempt> _retryHistory = [];
    private readonly object _lock = new();
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecoverabilityContext"/> class.
    /// </summary>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public RecoverabilityContext(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
    }

    /// <summary>
    /// Gets the unique identifier of the logical message's retry chain. It stays the same across
    /// every delayed retry of the message.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when processing first started (UTC).
    /// </summary>
    public DateTime StartedAtUtc { get; private set; }

    /// <summary>
    /// Restores the identity of a persisted retry chain, so a delayed retry continues the logical
    /// message's chain under its original id and start time. Internal: only the delayed retry
    /// processor restores a chain.
    /// </summary>
    internal void RestoreChain(Guid id, DateTime startedAtUtc)
    {
        Id = id;
        StartedAtUtc = startedAtUtc;
    }

    /// <summary>
    /// Gets or sets the current immediate retry attempt count (0-based).
    /// </summary>
    public int ImmediateRetryCount { get; private set; }

    /// <summary>
    /// Gets or sets the current delayed retry attempt count (0-based).
    /// </summary>
    public int DelayedRetryCount { get; private set; }

    /// <summary>
    /// Gets the total number of attempts made.
    /// </summary>
    public int TotalAttempts => ImmediateRetryCount + DelayedRetryCount + 1; // +1 for initial attempt

    /// <summary>
    /// Gets the most recent error, if any.
    /// </summary>
    public EncinaError? LastError { get; private set; }

    /// <summary>
    /// Gets the most recent exception, if any.
    /// </summary>
    public Exception? LastException { get; private set; }

    /// <summary>
    /// Gets the most recent error classification.
    /// </summary>
    public ErrorClassification LastClassification { get; private set; } = ErrorClassification.Unknown;

    /// <summary>
    /// Gets whether the current phase is delayed retries.
    /// </summary>
    public bool IsInDelayedRetryPhase { get; private set; }

    /// <summary>
    /// Gets the correlation ID from the request context.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Gets the idempotency key from the request, if present.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Gets the request type name.
    /// </summary>
    public string? RequestTypeName { get; init; }

    /// <summary>
    /// Gets an immutable copy of the retry history.
    /// </summary>
    public IReadOnlyList<RetryAttempt> RetryHistory
    {
        get
        {
            lock (_lock)
            {
                return _retryHistory.ToImmutableList();
            }
        }
    }

    /// <summary>
    /// Records a failed attempt in the history.
    /// </summary>
    /// <param name="error">The error from the attempt.</param>
    /// <param name="exception">The exception, if any.</param>
    /// <param name="classification">The error classification.</param>
    /// <param name="duration">How long the attempt took.</param>
    public void RecordFailedAttempt(
        EncinaError error,
        Exception? exception,
        ErrorClassification classification,
        TimeSpan? duration = null)
    {
        lock (_lock)
        {
            LastError = error;
            LastException = exception;
            LastClassification = classification;

            _retryHistory.Add(new RetryAttempt
            {
                AttemptNumber = TotalAttempts,
                IsImmediate = !IsInDelayedRetryPhase,
                AttemptedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
                Error = error,
                Exception = exception,
                Classification = classification,
                Duration = duration
            });
        }
    }

    /// <summary>
    /// Increments the immediate retry counter.
    /// </summary>
    public void IncrementImmediateRetry()
    {
        ImmediateRetryCount++;
    }

    /// <summary>
    /// Increments the delayed retry counter and marks the phase as delayed.
    /// </summary>
    public void IncrementDelayedRetry()
    {
        IsInDelayedRetryPhase = true;
        DelayedRetryCount++;
    }

    /// <summary>
    /// Transitions to the delayed retry phase without incrementing the counter.
    /// </summary>
    public void TransitionToDelayedPhase()
    {
        IsInDelayedRetryPhase = true;
    }

    /// <summary>
    /// Adopts the real failure of a delayed-retry re-dispatch (its last error, exception, classification,
    /// immediate retries and history) into the restored chain context. Internal: only the delayed retry
    /// processor does this.
    /// </summary>
    internal void AbsorbRedispatch(RecoverabilityContext redispatch)
    {
        ArgumentNullException.ThrowIfNull(redispatch);

        var history = redispatch.RetryHistory;

        lock (_lock)
        {
            LastError = redispatch.LastError;
            LastException = redispatch.LastException;
            LastClassification = redispatch.LastClassification;
            ImmediateRetryCount += redispatch.ImmediateRetryCount;
            _retryHistory.AddRange(history);
        }
    }

    /// <summary>
    /// Creates a <see cref="FailedMessage"/> from the current context.
    /// </summary>
    /// <param name="request">The original request.</param>
    /// <returns>A failed message record for DLQ handling.</returns>
    public FailedMessage CreateFailedMessage(object request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new FailedMessage
        {
            Id = Id,
            Request = request,
            RequestType = request.GetType().AssemblyQualifiedName ?? request.GetType().FullName ?? request.GetType().Name,
            Error = LastError ?? EncinaError.New("[recoverability.unknown] Unknown error"),
            Exception = LastException,
            CorrelationId = CorrelationId,
            IdempotencyKey = IdempotencyKey,
            TotalAttempts = TotalAttempts,
            ImmediateRetryAttempts = ImmediateRetryCount,
            DelayedRetryAttempts = DelayedRetryCount,
            FirstAttemptAtUtc = StartedAtUtc,
            FailedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            RetryHistory = RetryHistory
        };
    }
}
