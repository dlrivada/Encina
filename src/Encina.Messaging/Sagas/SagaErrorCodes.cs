namespace Encina.Messaging.Sagas;

/// <summary>
/// Error codes for saga operations.
/// </summary>
public static class SagaErrorCodes
{
    /// <summary>
    /// The saga was not found.
    /// </summary>
    public const string NotFound = "saga.not_found";

    /// <summary>
    /// Failed to start the saga.
    /// </summary>
    public const string StartFailed = "saga.start_failed";

    /// <summary>
    /// No steps defined in the saga definition.
    /// </summary>
    public const string NoSteps = "saga.no_steps";

    /// <summary>
    /// A saga step was not configured with an execute handler.
    /// </summary>
    public const string StepNotConfigured = "saga.step_not_configured";

    /// <summary>
    /// Invalid saga status for operation.
    /// </summary>
    public const string InvalidStatus = "saga.invalid_status";

    /// <summary>
    /// Failed to deserialize saga data.
    /// </summary>
    public const string DeserializationFailed = "saga.deserialization_failed";

    /// <summary>
    /// Saga step execution failed.
    /// </summary>
    public const string StepFailed = "saga.step_failed";

    /// <summary>
    /// Saga compensation failed.
    /// </summary>
    public const string CompensationFailed = "saga.compensation_failed";

    /// <summary>
    /// Saga exceeded its configured timeout.
    /// </summary>
    public const string Timeout = "saga.timeout";

    /// <summary>
    /// Saga handler was cancelled.
    /// </summary>
    public const string HandlerCancelled = "saga.handler.cancelled";

    /// <summary>
    /// Saga handler failed with an exception.
    /// </summary>
    public const string HandlerFailed = "saga.handler.failed";
}
