using LanguageExt;

namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Builder for configuring a single step in a routing slip.
/// </summary>
/// <typeparam name="TData">The type of data being routed.</typeparam>
public sealed class RoutingSlipStepBuilder<TData>
    where TData : class, new()
{
    private readonly RoutingSlipBuilder<TData> _parent;
    private readonly string _name;
    private Func<TData, RoutingSlipContext<TData>, CancellationToken, ValueTask<Either<EncinaError, TData>>>? _execute;
    private Func<TData, RoutingSlipContext<TData>, CancellationToken, Task>? _compensate;
    private Dictionary<string, object?>? _metadata;

    internal RoutingSlipStepBuilder(RoutingSlipBuilder<TData> parent, string name)
    {
        _parent = parent;
        _name = name;
    }

    /// <summary>
    /// Sets the execution function for this step.
    /// </summary>
    /// <param name="execute">The function to execute.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RoutingSlipStepBuilder<TData> Execute(
        Func<TData, RoutingSlipContext<TData>, CancellationToken, ValueTask<Either<EncinaError, TData>>> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        return this;
    }

    /// <summary>
    /// Sets the execution function for this step (simplified overload without context).
    /// </summary>
    /// <param name="execute">The function to execute.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RoutingSlipStepBuilder<TData> Execute(
        Func<TData, CancellationToken, ValueTask<Either<EncinaError, TData>>> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = (data, _, ct) => execute(data, ct);
        return this;
    }

    /// <summary>
    /// Sets the execution function for this step (simplified synchronous overload).
    /// </summary>
    /// <param name="execute">The function to execute.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RoutingSlipStepBuilder<TData> Execute(
        Func<TData, Either<EncinaError, TData>> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = (data, _, _) => ValueTask.FromResult(execute(data));
        return this;
    }

    /// <summary>
    /// Sets the compensation function for this step.
    /// </summary>
    /// <param name="compensate">The compensation function.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RoutingSlipStepBuilder<TData> Compensate(
        Func<TData, RoutingSlipContext<TData>, CancellationToken, Task> compensate)
    {
        ArgumentNullException.ThrowIfNull(compensate);
        _compensate = compensate;
        return this;
    }

    /// <summary>
    /// Sets the compensation function for this step (simplified overload without context).
    /// </summary>
    /// <param name="compensate">The compensation function.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RoutingSlipStepBuilder<TData> Compensate(
        Func<TData, CancellationToken, Task> compensate)
    {
        ArgumentNullException.ThrowIfNull(compensate);
        _compensate = (data, _, ct) => compensate(data, ct);
        return this;
    }

    /// <summary>
    /// Sets the compensation function for this step (simplified synchronous overload).
    /// </summary>
    /// <param name="compensate">The compensation action.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RoutingSlipStepBuilder<TData> Compensate(Action<TData> compensate)
    {
        ArgumentNullException.ThrowIfNull(compensate);
        _compensate = (data, _, _) =>
        {
            compensate(data);
            return Task.CompletedTask;
        };
        return this;
    }

    /// <summary>
    /// Adds metadata to this step.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RoutingSlipStepBuilder<TData> WithMetadata(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _metadata ??= [];
        _metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds a new step to the routing slip.
    /// </summary>
    /// <param name="stepName">Optional name for the step.</param>
    /// <returns>A new step builder.</returns>
    public RoutingSlipStepBuilder<TData> Step(string? stepName = null)
    {
        Complete();
        return _parent.Step(stepName);
    }

    /// <summary>
    /// Configures a completion handler for the routing slip.
    /// </summary>
    /// <param name="onCompletion">The completion handler.</param>
    /// <returns>The parent builder for fluent chaining.</returns>
    public RoutingSlipBuilder<TData> OnCompletion(
        Func<TData, RoutingSlipContext<TData>, CancellationToken, Task> onCompletion)
    {
        Complete();
        return _parent.OnCompletion(onCompletion);
    }

    /// <summary>
    /// Configures a timeout for the routing slip.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The parent builder for fluent chaining.</returns>
    public RoutingSlipBuilder<TData> WithTimeout(TimeSpan timeout)
    {
        Complete();
        return _parent.WithTimeout(timeout);
    }

    /// <summary>
    /// Builds the routing slip definition.
    /// </summary>
    /// <returns>The built routing slip definition.</returns>
    public BuiltRoutingSlipDefinition<TData> Build()
    {
        Complete();
        return _parent.Build();
    }

    private void Complete()
    {
        if (_execute is null)
        {
            throw new InvalidOperationException($"Step '{_name}' must have an Execute function defined.");
        }

        _parent.AddStep(new RoutingSlipStepDefinition<TData>(
            _name,
            _execute,
            _compensate,
            _metadata));
    }
}
