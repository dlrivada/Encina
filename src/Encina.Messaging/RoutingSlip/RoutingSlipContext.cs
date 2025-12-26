namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Context provided to routing slip steps during execution.
/// </summary>
/// <remarks>
/// <para>
/// This context allows steps to inspect and modify the routing slip's itinerary.
/// Steps can dynamically add, remove, or reorder upcoming steps based on
/// the result of their execution.
/// </para>
/// </remarks>
/// <typeparam name="TData">The type of data being routed.</typeparam>
public sealed class RoutingSlipContext<TData>
    where TData : class
{
    private readonly List<RoutingSlipStepDefinition<TData>> _remainingSteps;
    private readonly List<RoutingSlipActivityEntry<TData>> _activityLog;

    internal RoutingSlipContext(
        Guid routingSlipId,
        string slipType,
        IRequestContext requestContext,
        List<RoutingSlipStepDefinition<TData>> remainingSteps,
        List<RoutingSlipActivityEntry<TData>> activityLog)
    {
        RoutingSlipId = routingSlipId;
        SlipType = slipType;
        RequestContext = requestContext;
        _remainingSteps = remainingSteps;
        _activityLog = activityLog;
    }

    /// <summary>
    /// Gets the unique identifier of the routing slip.
    /// </summary>
    public Guid RoutingSlipId { get; }

    /// <summary>
    /// Gets the routing slip type name.
    /// </summary>
    public string SlipType { get; }

    /// <summary>
    /// Gets the request context.
    /// </summary>
    public IRequestContext RequestContext { get; }

    /// <summary>
    /// Gets the current step index (0-based, among executed steps).
    /// </summary>
    public int CurrentStepIndex => _activityLog.Count;

    /// <summary>
    /// Gets the number of remaining steps in the itinerary.
    /// </summary>
    public int RemainingStepCount => _remainingSteps.Count;

    /// <summary>
    /// Gets the activity log of completed steps.
    /// </summary>
    public IReadOnlyList<RoutingSlipActivityEntry<TData>> ActivityLog => _activityLog;

    /// <summary>
    /// Adds a step to the end of the itinerary.
    /// </summary>
    /// <param name="step">The step to add.</param>
    public void AddStep(RoutingSlipStepDefinition<TData> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _remainingSteps.Add(step);
    }

    /// <summary>
    /// Adds a step immediately after the current step.
    /// </summary>
    /// <param name="step">The step to insert.</param>
    public void AddStepNext(RoutingSlipStepDefinition<TData> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _remainingSteps.Insert(0, step);
    }

    /// <summary>
    /// Adds a step at a specific position in the itinerary.
    /// </summary>
    /// <param name="index">The zero-based index where the step should be inserted.</param>
    /// <param name="step">The step to insert.</param>
    public void InsertStep(int index, RoutingSlipStepDefinition<TData> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _remainingSteps.Count);
        _remainingSteps.Insert(index, step);
    }

    /// <summary>
    /// Removes a step at a specific position from the itinerary.
    /// </summary>
    /// <param name="index">The zero-based index of the step to remove.</param>
    /// <returns>True if the step was removed; false if the index was out of range.</returns>
    public bool RemoveStepAt(int index)
    {
        if (index < 0 || index >= _remainingSteps.Count)
        {
            return false;
        }

        _remainingSteps.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Clears all remaining steps from the itinerary.
    /// </summary>
    /// <remarks>
    /// Use this to terminate the routing slip early without executing remaining steps.
    /// </remarks>
    public void ClearRemainingSteps()
    {
        _remainingSteps.Clear();
    }

    /// <summary>
    /// Gets the names of remaining steps.
    /// </summary>
    /// <returns>A list of step names in execution order.</returns>
    public IReadOnlyList<string> GetRemainingStepNames()
    {
        return _remainingSteps.Select(s => s.Name).ToList();
    }

    internal void RecordActivity(RoutingSlipActivityEntry<TData> entry)
    {
        _activityLog.Add(entry);
    }

    internal List<RoutingSlipStepDefinition<TData>> GetRemainingSteps() => _remainingSteps;

    internal List<RoutingSlipActivityEntry<TData>> GetActivityLog() => _activityLog;
}
