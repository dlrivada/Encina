namespace SimpleMediator.Messaging.Choreography;

/// <summary>
/// Error codes for choreography saga operations.
/// </summary>
public static class ChoreographyErrorCodes
{
    /// <summary>
    /// Saga with the given correlation ID was not found.
    /// </summary>
    public const string SagaNotFound = "choreography.saga.not_found";

    /// <summary>
    /// Saga with the given correlation ID already exists.
    /// </summary>
    public const string SagaAlreadyExists = "choreography.saga.already_exists";

    /// <summary>
    /// Event reaction failed.
    /// </summary>
    public const string ReactionFailed = "choreography.reaction.failed";

    /// <summary>
    /// Event reaction threw an exception.
    /// </summary>
    public const string ReactionException = "choreography.reaction.exception";

    /// <summary>
    /// Compensation action failed.
    /// </summary>
    public const string CompensationFailed = "choreography.compensation.failed";

    /// <summary>
    /// Saga timeout exceeded.
    /// </summary>
    public const string SagaTimeout = "choreography.saga.timeout";

    /// <summary>
    /// No handler found for event in choreography.
    /// </summary>
    public const string NoHandlerFound = "choreography.handler.not_found";

    /// <summary>
    /// Saga is in an invalid state for the requested operation.
    /// </summary>
    public const string InvalidState = "choreography.saga.invalid_state";

    /// <summary>
    /// State persistence failed.
    /// </summary>
    public const string PersistenceFailed = "choreography.persistence.failed";
}
