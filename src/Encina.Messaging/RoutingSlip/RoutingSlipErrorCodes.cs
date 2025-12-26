namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Error codes for routing slip operations.
/// </summary>
public static class RoutingSlipErrorCodes
{
    /// <summary>
    /// The routing slip was not found.
    /// </summary>
    public const string NotFound = "routingslip.not_found";

    /// <summary>
    /// The routing slip is in an invalid status for the requested operation.
    /// </summary>
    public const string InvalidStatus = "routingslip.invalid_status";

    /// <summary>
    /// Failed to deserialize the routing slip data.
    /// </summary>
    public const string DeserializationFailed = "routingslip.deserialization_failed";

    /// <summary>
    /// A step in the routing slip failed.
    /// </summary>
    public const string StepFailed = "routingslip.step_failed";

    /// <summary>
    /// Compensation for a step failed.
    /// </summary>
    public const string CompensationFailed = "routingslip.compensation_failed";

    /// <summary>
    /// The routing slip timed out.
    /// </summary>
    public const string Timeout = "routingslip.timeout";

    /// <summary>
    /// A step handler was cancelled.
    /// </summary>
    public const string HandlerCancelled = "routingslip.handler.cancelled";

    /// <summary>
    /// A step handler failed with an exception.
    /// </summary>
    public const string HandlerFailed = "routingslip.handler.failed";

    /// <summary>
    /// No steps defined in the routing slip.
    /// </summary>
    public const string NoSteps = "routingslip.no_steps";

    /// <summary>
    /// The routing slip has already completed.
    /// </summary>
    public const string AlreadyCompleted = "routingslip.already_completed";
}
