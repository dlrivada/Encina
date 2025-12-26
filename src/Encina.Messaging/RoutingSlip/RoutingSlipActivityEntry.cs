namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Represents a completed step in the routing slip activity log.
/// </summary>
/// <typeparam name="TData">The type of data being routed.</typeparam>
public sealed class RoutingSlipActivityEntry<TData>
    where TData : class
{
    /// <summary>
    /// Gets the step name.
    /// </summary>
    public string StepName { get; }

    /// <summary>
    /// Gets the data state after this step executed.
    /// </summary>
    public TData DataAfterExecution { get; }

    /// <summary>
    /// Gets the compensation function, if any.
    /// </summary>
    public Func<TData, RoutingSlipContext<TData>, CancellationToken, Task>? Compensate { get; }

    /// <summary>
    /// Gets when this step was executed.
    /// </summary>
    public DateTime ExecutedAtUtc { get; }

    /// <summary>
    /// Gets optional metadata associated with this step execution.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingSlipActivityEntry{TData}"/> class.
    /// </summary>
    /// <param name="stepName">The step name.</param>
    /// <param name="dataAfterExecution">The data state after execution.</param>
    /// <param name="compensate">The compensation function.</param>
    /// <param name="executedAtUtc">When the step executed.</param>
    /// <param name="metadata">Optional metadata.</param>
    public RoutingSlipActivityEntry(
        string stepName,
        TData dataAfterExecution,
        Func<TData, RoutingSlipContext<TData>, CancellationToken, Task>? compensate,
        DateTime executedAtUtc,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stepName);
        ArgumentNullException.ThrowIfNull(dataAfterExecution);

        StepName = stepName;
        DataAfterExecution = dataAfterExecution;
        Compensate = compensate;
        ExecutedAtUtc = executedAtUtc;
        Metadata = metadata ?? new Dictionary<string, object?>();
    }
}
