using LanguageExt;

namespace Encina.Messaging.Sagas.LowCeremony;

/// <summary>
/// Factory for creating saga definitions with minimal boilerplate.
/// </summary>
/// <remarks>
/// <para>
/// This is the entry point for the low-ceremony saga API.
/// Use <see cref="Create{TData}"/> to start defining a saga.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var saga = SagaDefinition.Create&lt;OrderData&gt;("ProcessOrder")
///     .Step("Reserve Inventory")
///         .Execute(async (data, ctx, ct) =>
///         {
///             var result = await encina.Send(new ReserveInventory(data.OrderId), ct);
///             return result.Map(r => data with { ReservationId = r.Id });
///         })
///         .Compensate(async (data, ctx, ct) =>
///         {
///             if (data.ReservationId.HasValue)
///                 await encina.Send(new CancelReservation(data.ReservationId.Value), ct);
///         })
///     .Step("Process Payment")
///         .Execute(async (data, ctx, ct) =>
///         {
///             var result = await encina.Send(new ProcessPayment(data.OrderId), ct);
///             return result.Map(r => data with { PaymentId = r.Id });
///         })
///         .Compensate(async (data, ctx, ct) =>
///         {
///             if (data.PaymentId.HasValue)
///                 await encina.Send(new RefundPayment(data.PaymentId.Value), ct);
///         })
///     .WithTimeout(TimeSpan.FromMinutes(5))
///     .Build();
/// </code>
/// </example>
public static class SagaDefinition
{
    /// <summary>
    /// Creates a new saga definition with the specified type name.
    /// </summary>
    /// <typeparam name="TData">The type of data accumulated during the saga.</typeparam>
    /// <param name="sagaType">The saga type name (used for persistence and identification).</param>
    /// <returns>A new saga definition builder.</returns>
    public static SagaDefinition<TData> Create<TData>(string sagaType)
        where TData : class, new()
        => new(sagaType);
}

/// <summary>
/// Fluent builder for defining sagas with minimal boilerplate.
/// </summary>
/// <typeparam name="TData">The type of data accumulated during the saga.</typeparam>
/// <remarks>
/// <para>
/// This is the low-ceremony alternative to inheriting from <c>Saga&lt;TData&gt;</c>.
/// Define saga steps inline using a fluent API.
/// </para>
/// <para>
/// <b>Benefits</b>:
/// <list type="bullet">
/// <item><description>No class inheritance required</description></item>
/// <item><description>Steps defined inline with clear intent</description></item>
/// <item><description>Compensation logic co-located with execution</description></item>
/// <item><description>Optional timeout per saga</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class SagaDefinition<TData>
    where TData : class, new()
{
    private readonly string _sagaType;
    private readonly List<SagaStepDefinition<TData>> _steps = [];
    private TimeSpan? _timeout;

    internal SagaDefinition(string sagaType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaType);
        _sagaType = sagaType;
    }

    /// <summary>
    /// Adds a step to the saga.
    /// </summary>
    /// <param name="stepName">Optional name for the step (for logging/debugging).</param>
    /// <returns>A step builder for configuring the step.</returns>
    public SagaStepBuilder<TData> Step(string? stepName = null)
    {
        return new SagaStepBuilder<TData>(this, stepName ?? $"Step {_steps.Count + 1}");
    }

    /// <summary>
    /// Configures a timeout for the entire saga.
    /// </summary>
    /// <param name="timeout">The maximum duration before the saga times out.</param>
    /// <returns>This saga definition for fluent chaining.</returns>
    public SagaDefinition<TData> WithTimeout(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Builds the saga definition.
    /// </summary>
    /// <returns>An immutable saga definition ready for execution.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no steps have been defined.</exception>
    public BuiltSagaDefinition<TData> Build()
    {
        if (_steps.Count == 0)
        {
            throw new InvalidOperationException("At least one step must be defined in the saga.");
        }

        return new BuiltSagaDefinition<TData>(
            _sagaType,
            [.. _steps],
            _timeout);
    }

    internal void AddStep(SagaStepDefinition<TData> step)
    {
        _steps.Add(step);
    }

    /// <summary>
    /// Gets the saga type name.
    /// </summary>
    public string SagaType => _sagaType;
}

/// <summary>
/// Represents a built, immutable saga definition ready for execution.
/// </summary>
/// <typeparam name="TData">The type of data accumulated during the saga.</typeparam>
public sealed class BuiltSagaDefinition<TData>
    where TData : class, new()
{
    /// <summary>
    /// Gets the saga type name.
    /// </summary>
    public string SagaType { get; }

    /// <summary>
    /// Gets the ordered list of steps in the saga.
    /// </summary>
    public IReadOnlyList<SagaStepDefinition<TData>> Steps { get; }

    /// <summary>
    /// Gets the optional timeout for the saga.
    /// </summary>
    public TimeSpan? Timeout { get; }

    internal BuiltSagaDefinition(
        string sagaType,
        IReadOnlyList<SagaStepDefinition<TData>> steps,
        TimeSpan? timeout)
    {
        SagaType = sagaType;
        Steps = steps;
        Timeout = timeout;
    }

    /// <summary>
    /// Gets the total number of steps in the saga.
    /// </summary>
    public int StepCount => Steps.Count;
}

/// <summary>
/// Represents a single step in a saga definition.
/// </summary>
/// <typeparam name="TData">The type of data accumulated during the saga.</typeparam>
public sealed class SagaStepDefinition<TData>
    where TData : class
{
    /// <summary>
    /// Gets the step name (for logging/debugging).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the execution function for this step.
    /// </summary>
    public Func<TData, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, TData>>> Execute { get; }

    /// <summary>
    /// Gets the optional compensation function for this step.
    /// </summary>
    public Func<TData, IRequestContext, CancellationToken, Task>? Compensate { get; }

    internal SagaStepDefinition(
        string name,
        Func<TData, IRequestContext, CancellationToken, ValueTask<Either<EncinaError, TData>>> execute,
        Func<TData, IRequestContext, CancellationToken, Task>? compensate)
    {
        Name = name;
        Execute = execute;
        Compensate = compensate;
    }
}
