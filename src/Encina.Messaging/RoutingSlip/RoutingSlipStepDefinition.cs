using LanguageExt;

namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Represents a single step in a routing slip itinerary.
/// </summary>
/// <typeparam name="TData">The type of data being routed.</typeparam>
public sealed class RoutingSlipStepDefinition<TData>
    where TData : class
{
    /// <summary>
    /// Gets the step name (for logging/debugging/identification).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the execution function for this step.
    /// </summary>
    public Func<TData, RoutingSlipContext<TData>, CancellationToken, ValueTask<Either<EncinaError, TData>>> Execute { get; }

    /// <summary>
    /// Gets the optional compensation function for this step.
    /// </summary>
    public Func<TData, RoutingSlipContext<TData>, CancellationToken, Task>? Compensate { get; }

    /// <summary>
    /// Gets optional metadata associated with this step.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingSlipStepDefinition{TData}"/> class.
    /// </summary>
    /// <param name="name">The step name.</param>
    /// <param name="execute">The execution function.</param>
    /// <param name="compensate">Optional compensation function.</param>
    /// <param name="metadata">Optional metadata.</param>
    public RoutingSlipStepDefinition(
        string name,
        Func<TData, RoutingSlipContext<TData>, CancellationToken, ValueTask<Either<EncinaError, TData>>> execute,
        Func<TData, RoutingSlipContext<TData>, CancellationToken, Task>? compensate = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(execute);

        Name = name;
        Execute = execute;
        Compensate = compensate;
        Metadata = metadata ?? new Dictionary<string, object?>();
    }
}
