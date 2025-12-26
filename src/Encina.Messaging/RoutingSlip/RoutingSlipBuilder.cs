namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Factory for creating routing slip definitions.
/// </summary>
/// <remarks>
/// <para>
/// This is the entry point for the routing slip fluent API.
/// Use <see cref="Create{TData}"/> to start defining a routing slip.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var routingSlip = RoutingSlipBuilder.Create&lt;OrderData&gt;("ProcessOrder")
///     .Step("Validate Order")
///         .Execute(async (data, ctx, ct) =>
///         {
///             var result = await _validator.ValidateAsync(data, ct);
///             return result.IsValid
///                 ? Right&lt;EncinaError, OrderData&gt;(data)
///                 : Left&lt;EncinaError, OrderData&gt;(new EncinaError(result.Errors));
///         })
///     .Step("Process Payment")
///         .Execute(async (data, ctx, ct) =>
///         {
///             var result = await _paymentService.ProcessAsync(data, ct);
///
///             // Dynamically add a step based on payment result
///             if (result.RequiresVerification)
///             {
///                 ctx.AddStepNext(new RoutingSlipStepDefinition&lt;OrderData&gt;(
///                     "Verify Payment",
///                     async (d, c, t) => await VerifyPaymentAsync(d, t),
///                     async (d, c, t) => await CancelVerificationAsync(d, t)));
///             }
///
///             return Right&lt;EncinaError, OrderData&gt;(data with { PaymentId = result.Id });
///         })
///         .Compensate(async (data, ctx, ct) =>
///         {
///             await _paymentService.RefundAsync(data.PaymentId, ct);
///         })
///     .Step("Ship Order")
///         .Execute(async (data, ctx, ct) =>
///         {
///             var tracking = await _shippingService.ShipAsync(data, ct);
///             return Right&lt;EncinaError, OrderData&gt;(data with { TrackingNumber = tracking });
///         })
///         .Compensate(async (data, ctx, ct) =>
///         {
///             await _shippingService.CancelShipmentAsync(data.TrackingNumber, ct);
///         })
///     .OnCompletion(async (data, ctx, ct) =>
///     {
///         await _notificationService.NotifyOrderCompletedAsync(data, ct);
///     })
///     .WithTimeout(TimeSpan.FromMinutes(5))
///     .Build();
/// </code>
/// </example>
public static class RoutingSlipBuilder
{
    /// <summary>
    /// Creates a new routing slip definition with the specified type name.
    /// </summary>
    /// <typeparam name="TData">The type of data being routed.</typeparam>
    /// <param name="slipType">The routing slip type name (used for persistence and identification).</param>
    /// <returns>A new routing slip builder.</returns>
    public static RoutingSlipBuilder<TData> Create<TData>(string slipType)
        where TData : class, new()
        => new(slipType);
}

/// <summary>
/// Fluent builder for defining routing slips.
/// </summary>
/// <typeparam name="TData">The type of data being routed.</typeparam>
public sealed class RoutingSlipBuilder<TData>
    where TData : class, new()
{
    private readonly string _slipType;
    private readonly List<RoutingSlipStepDefinition<TData>> _steps = [];
    private Func<TData, RoutingSlipContext<TData>, CancellationToken, Task>? _onCompletion;
    private TimeSpan? _timeout;

    internal RoutingSlipBuilder(string slipType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slipType);
        _slipType = slipType;
    }

    /// <summary>
    /// Adds a step to the routing slip.
    /// </summary>
    /// <param name="stepName">Optional name for the step (for logging/debugging).</param>
    /// <returns>A step builder for configuring the step.</returns>
    public RoutingSlipStepBuilder<TData> Step(string? stepName = null)
    {
        return new RoutingSlipStepBuilder<TData>(this, stepName ?? $"Step {_steps.Count + 1}");
    }

    /// <summary>
    /// Configures a completion handler that runs after all steps complete successfully.
    /// </summary>
    /// <param name="onCompletion">The completion handler.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RoutingSlipBuilder<TData> OnCompletion(
        Func<TData, RoutingSlipContext<TData>, CancellationToken, Task> onCompletion)
    {
        ArgumentNullException.ThrowIfNull(onCompletion);
        _onCompletion = onCompletion;
        return this;
    }

    /// <summary>
    /// Configures a completion handler that runs after all steps complete successfully.
    /// </summary>
    /// <param name="onCompletion">The completion handler (without context).</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RoutingSlipBuilder<TData> OnCompletion(Func<TData, CancellationToken, Task> onCompletion)
    {
        ArgumentNullException.ThrowIfNull(onCompletion);
        _onCompletion = (data, _, ct) => onCompletion(data, ct);
        return this;
    }

    /// <summary>
    /// Configures a timeout for the entire routing slip.
    /// </summary>
    /// <param name="timeout">The maximum duration before the routing slip times out.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public RoutingSlipBuilder<TData> WithTimeout(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero, nameof(timeout));
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Builds the routing slip definition.
    /// </summary>
    /// <returns>An immutable routing slip definition ready for execution.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no steps have been defined.</exception>
    public BuiltRoutingSlipDefinition<TData> Build()
    {
        if (_steps.Count == 0)
        {
            throw new InvalidOperationException("At least one step must be defined in the routing slip.");
        }

        return new BuiltRoutingSlipDefinition<TData>(
            _slipType,
            [.. _steps],
            _onCompletion,
            _timeout);
    }

    /// <summary>
    /// Gets the routing slip type name.
    /// </summary>
    public string SlipType => _slipType;

    internal void AddStep(RoutingSlipStepDefinition<TData> step)
    {
        _steps.Add(step);
    }
}

/// <summary>
/// Represents a built, immutable routing slip definition ready for execution.
/// </summary>
/// <typeparam name="TData">The type of data being routed.</typeparam>
public sealed class BuiltRoutingSlipDefinition<TData>
    where TData : class, new()
{
    /// <summary>
    /// Gets the routing slip type name.
    /// </summary>
    public string SlipType { get; }

    /// <summary>
    /// Gets the initial steps in the routing slip.
    /// </summary>
    /// <remarks>
    /// Note: Additional steps may be added during execution via the context.
    /// </remarks>
    public IReadOnlyList<RoutingSlipStepDefinition<TData>> Steps { get; }

    /// <summary>
    /// Gets the optional completion handler.
    /// </summary>
    public Func<TData, RoutingSlipContext<TData>, CancellationToken, Task>? OnCompletion { get; }

    /// <summary>
    /// Gets the optional timeout for the routing slip.
    /// </summary>
    public TimeSpan? Timeout { get; }

    internal BuiltRoutingSlipDefinition(
        string slipType,
        IReadOnlyList<RoutingSlipStepDefinition<TData>> steps,
        Func<TData, RoutingSlipContext<TData>, CancellationToken, Task>? onCompletion,
        TimeSpan? timeout)
    {
        SlipType = slipType;
        Steps = steps;
        OnCompletion = onCompletion;
        Timeout = timeout;
    }

    /// <summary>
    /// Gets the initial number of steps in the routing slip.
    /// </summary>
    public int InitialStepCount => Steps.Count;
}
