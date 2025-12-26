using LanguageExt;

namespace Encina.Messaging.Sagas.LowCeremony;

/// <summary>
/// Builder for configuring a single saga step.
/// </summary>
/// <typeparam name="TData">The type of data accumulated during the saga.</typeparam>
/// <remarks>
/// <para>
/// Each step requires an <see cref="Execute"/> action and optionally a <see cref="Compensate"/> action.
/// Compensation runs in reverse order when a later step fails.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// .Step("Process Payment")
///     .Execute(async (data, ctx, ct) =>
///     {
///         var result = await encina.Send(new ProcessPayment(data.OrderId), ct);
///         return result.Map(r => data with { PaymentId = r.Id });
///     })
///     .Compensate(async (data, ctx, ct) =>
///     {
///         if (data.PaymentId.HasValue)
///             await encina.Send(new RefundPayment(data.PaymentId.Value), ct);
///     })
/// </code>
/// </example>
public sealed class SagaStepBuilder<TData>
    where TData : class, new()
{
    private readonly SagaDefinition<TData> _parent;
    private readonly string _stepName;
    private Func<TData, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, TData>>>? _execute;
    private Func<TData, IRequestContext, CancellationToken, Task>? _compensate;

    internal SagaStepBuilder(SagaDefinition<TData> parent, string stepName)
    {
        _parent = parent;
        _stepName = stepName;
    }

    /// <summary>
    /// Configures the execution action for this step.
    /// </summary>
    /// <param name="execute">
    /// The async function to execute. Returns <c>Right</c> with updated data on success,
    /// or <c>Left</c> with an error on failure.
    /// </param>
    /// <returns>This step builder for fluent chaining.</returns>
    public SagaStepBuilder<TData> Execute(
        Func<TData, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, TData>>> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = execute;
        return this;
    }

    /// <summary>
    /// Configures the execution action for this step (simplified overload without context).
    /// </summary>
    /// <param name="execute">
    /// The async function to execute. Returns <c>Right</c> with updated data on success,
    /// or <c>Left</c> with an error on failure.
    /// </param>
    /// <returns>This step builder for fluent chaining.</returns>
    public SagaStepBuilder<TData> Execute(
        Func<TData, CancellationToken, ValueTask<Either<EncinaError, TData>>> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = (data, _, ct) => execute(data, ct);
        return this;
    }

    /// <summary>
    /// Configures the compensation action for this step.
    /// </summary>
    /// <param name="compensate">
    /// The async function to undo this step's effects. Called when a later step fails.
    /// </param>
    /// <returns>The parent saga definition for adding more steps.</returns>
    public SagaDefinition<TData> Compensate(
        Func<TData, IRequestContext, CancellationToken, Task> compensate)
    {
        ArgumentNullException.ThrowIfNull(compensate);
        _compensate = compensate;
        return AddStepAndReturn();
    }

    /// <summary>
    /// Configures the compensation action for this step (simplified overload without context).
    /// </summary>
    /// <param name="compensate">
    /// The async function to undo this step's effects. Called when a later step fails.
    /// </param>
    /// <returns>The parent saga definition for adding more steps.</returns>
    public SagaDefinition<TData> Compensate(
        Func<TData, CancellationToken, Task> compensate)
    {
        ArgumentNullException.ThrowIfNull(compensate);
        _compensate = (data, _, ct) => compensate(data, ct);
        return AddStepAndReturn();
    }

    /// <summary>
    /// Adds the next step without compensation (for non-reversible or final steps).
    /// </summary>
    /// <param name="stepName">Optional name for the next step.</param>
    /// <returns>A new step builder for the next step.</returns>
    public SagaStepBuilder<TData> Step(string? stepName = null)
    {
        AddStepToParent();
        return _parent.Step(stepName);
    }

    /// <summary>
    /// Configures a timeout for the entire saga (convenience method).
    /// </summary>
    /// <param name="timeout">The maximum duration before the saga times out.</param>
    /// <returns>The parent saga definition for further configuration.</returns>
    public SagaDefinition<TData> WithTimeout(TimeSpan timeout)
    {
        AddStepToParent();
        return _parent.WithTimeout(timeout);
    }

    /// <summary>
    /// Builds the saga definition (convenience method).
    /// </summary>
    /// <returns>An immutable saga definition ready for execution.</returns>
    public BuiltSagaDefinition<TData> Build()
    {
        AddStepToParent();
        return _parent.Build();
    }

    private void AddStepToParent()
    {
        if (_execute == null)
        {
            throw new InvalidOperationException($"Step '{_stepName}' must have an Execute action defined.");
        }

        _parent.AddStep(new SagaStepDefinition<TData>(_stepName, _execute, _compensate));
    }

    private SagaDefinition<TData> AddStepAndReturn()
    {
        AddStepToParent();
        return _parent;
    }
}
