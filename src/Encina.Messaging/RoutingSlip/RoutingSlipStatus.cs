namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Status values for a routing slip.
/// </summary>
public static class RoutingSlipStatus
{
    /// <summary>
    /// The routing slip is currently executing steps.
    /// </summary>
    public const string Running = "Running";

    /// <summary>
    /// All steps completed successfully.
    /// </summary>
    public const string Completed = "Completed";

    /// <summary>
    /// A step failed and compensation is in progress.
    /// </summary>
    public const string Compensating = "Compensating";

    /// <summary>
    /// All compensation steps completed successfully.
    /// </summary>
    public const string Compensated = "Compensated";

    /// <summary>
    /// The routing slip failed (step or compensation failed).
    /// </summary>
    public const string Failed = "Failed";

    /// <summary>
    /// The routing slip exceeded its timeout.
    /// </summary>
    public const string TimedOut = "TimedOut";
}
