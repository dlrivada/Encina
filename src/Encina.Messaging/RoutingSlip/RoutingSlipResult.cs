namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Represents the result of executing a routing slip.
/// </summary>
/// <typeparam name="TData">The type of data being routed.</typeparam>
public sealed class RoutingSlipResult<TData>
    where TData : class
{
    /// <summary>
    /// Gets the routing slip identifier.
    /// </summary>
    public Guid RoutingSlipId { get; }

    /// <summary>
    /// Gets the final data after all steps executed.
    /// </summary>
    public TData FinalData { get; }

    /// <summary>
    /// Gets the number of steps that were executed.
    /// </summary>
    public int StepsExecuted { get; }

    /// <summary>
    /// Gets the number of steps that were dynamically added during execution.
    /// </summary>
    public int StepsAdded { get; }

    /// <summary>
    /// Gets the number of steps that were skipped or removed.
    /// </summary>
    public int StepsRemoved { get; }

    /// <summary>
    /// Gets the total execution duration.
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the activity log of executed steps.
    /// </summary>
    public IReadOnlyList<RoutingSlipActivityEntry<TData>> ActivityLog { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingSlipResult{TData}"/> class.
    /// </summary>
    public RoutingSlipResult(
        Guid routingSlipId,
        TData finalData,
        int stepsExecuted,
        int stepsAdded,
        int stepsRemoved,
        TimeSpan duration,
        IReadOnlyList<RoutingSlipActivityEntry<TData>> activityLog)
    {
        RoutingSlipId = routingSlipId;
        FinalData = finalData;
        StepsExecuted = stepsExecuted;
        StepsAdded = stepsAdded;
        StepsRemoved = stepsRemoved;
        Duration = duration;
        ActivityLog = activityLog;
    }
}
