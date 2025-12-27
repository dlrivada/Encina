namespace Encina.Messaging.ScatterGather;

/// <summary>
/// Error codes for scatter-gather operations.
/// </summary>
public static class ScatterGatherErrorCodes
{
    /// <summary>
    /// No scatter handlers were configured.
    /// </summary>
    public const string NoScatterHandlers = "scattergather.no_scatter_handlers";

    /// <summary>
    /// No gather handler was configured.
    /// </summary>
    public const string GatherNotConfigured = "scattergather.gather_not_configured";

    /// <summary>
    /// A scatter handler failed during execution.
    /// </summary>
    public const string ScatterFailed = "scattergather.scatter_failed";

    /// <summary>
    /// The gather handler failed during execution.
    /// </summary>
    public const string GatherFailed = "scattergather.gather_failed";

    /// <summary>
    /// The operation was cancelled.
    /// </summary>
    public const string Cancelled = "scattergather.cancelled";

    /// <summary>
    /// A handler failed with an exception.
    /// </summary>
    public const string HandlerFailed = "scattergather.handler_failed";

    /// <summary>
    /// The operation timed out.
    /// </summary>
    public const string Timeout = "scattergather.timeout";

    /// <summary>
    /// All scatter handlers failed.
    /// </summary>
    public const string AllScattersFailed = "scattergather.all_scatters_failed";

    /// <summary>
    /// Quorum was not reached within the timeout.
    /// </summary>
    public const string QuorumNotReached = "scattergather.quorum_not_reached";

    /// <summary>
    /// Invalid configuration was provided.
    /// </summary>
    public const string InvalidConfiguration = "scattergather.invalid_configuration";
}
