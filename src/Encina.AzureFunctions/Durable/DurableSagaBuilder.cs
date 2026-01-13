using LanguageExt;
using Microsoft.DurableTask;

namespace Encina.AzureFunctions.Durable;

/// <summary>
/// Builder for creating saga workflows using Durable Functions.
/// </summary>
/// <typeparam name="TData">The saga data type that flows through all steps.</typeparam>
/// <remarks>
/// <para>
/// This builder provides a fluent API for defining saga workflows that execute
/// a series of activities with automatic compensation on failure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var saga = DurableSagaBuilder.Create&lt;OrderData&gt;()
///     .Step("ReserveInventory")
///         .Execute("ReserveInventoryActivity")
///         .Compensate("ReleaseInventoryActivity")
///     .Step("ProcessPayment")
///         .Execute("ProcessPaymentActivity")
///         .Compensate("RefundPaymentActivity")
///     .Step("ShipOrder")
///         .Execute("ShipOrderActivity")
///         .Compensate("CancelShipmentActivity")
///     .Build();
///
/// var result = await saga.ExecuteAsync(context, initialData);
/// </code>
/// </example>
public sealed class DurableSagaBuilder<TData>
{
    private readonly List<DurableSagaStep<TData>> _steps = [];
    private TaskOptions? _defaultRetryOptions;
    private TimeSpan? _timeout;

    /// <summary>
    /// Adds a step to the saga.
    /// </summary>
    /// <param name="stepName">The name of the step (for logging and tracking).</param>
    /// <returns>A step builder for configuring the step.</returns>
    public DurableSagaStepBuilder<TData> Step(string stepName)
    {
        ArgumentException.ThrowIfNullOrEmpty(stepName);
        return new DurableSagaStepBuilder<TData>(this, stepName);
    }

    /// <summary>
    /// Sets default retry options for all steps.
    /// </summary>
    /// <param name="options">The retry options.</param>
    /// <returns>This builder for chaining.</returns>
    public DurableSagaBuilder<TData> WithDefaultRetryOptions(TaskOptions options)
    {
        _defaultRetryOptions = options;
        return this;
    }

    /// <summary>
    /// Sets a timeout for the entire saga execution.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>This builder for chaining.</returns>
    public DurableSagaBuilder<TData> WithTimeout(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Builds the saga definition.
    /// </summary>
    /// <returns>An executable saga definition.</returns>
    public DurableSaga<TData> Build()
    {
        if (_steps.Count == 0)
        {
            throw new InvalidOperationException("Saga must have at least one step.");
        }

        return new DurableSaga<TData>([.. _steps], _defaultRetryOptions, _timeout);
    }

    internal void AddStep(DurableSagaStep<TData> step)
    {
        _steps.Add(step);
    }

    internal TaskOptions? DefaultRetryOptions => _defaultRetryOptions;
}

/// <summary>
/// Builder for configuring a single saga step.
/// </summary>
/// <typeparam name="TData">The saga data type.</typeparam>
public sealed class DurableSagaStepBuilder<TData>
{
    private readonly DurableSagaBuilder<TData> _sagaBuilder;
    private readonly string _stepName;
    private string? _executeActivityName;
    private string? _compensateActivityName;
    private TaskOptions? _retryOptions;
    private bool _skipCompensationOnFailure;

    internal DurableSagaStepBuilder(DurableSagaBuilder<TData> sagaBuilder, string stepName)
    {
        _sagaBuilder = sagaBuilder;
        _stepName = stepName;
    }

    /// <summary>
    /// Sets the activity to execute for this step.
    /// </summary>
    /// <param name="activityName">The name of the activity function.</param>
    /// <returns>This builder for chaining.</returns>
    public DurableSagaStepBuilder<TData> Execute(string activityName)
    {
        ArgumentException.ThrowIfNullOrEmpty(activityName);
        _executeActivityName = activityName;
        return this;
    }

    /// <summary>
    /// Sets the compensation activity for this step.
    /// </summary>
    /// <param name="activityName">The name of the compensation activity function.</param>
    /// <returns>This builder for chaining.</returns>
    public DurableSagaStepBuilder<TData> Compensate(string activityName)
    {
        ArgumentException.ThrowIfNullOrEmpty(activityName);
        _compensateActivityName = activityName;
        return this;
    }

    /// <summary>
    /// Sets retry options for this step.
    /// </summary>
    /// <param name="options">The retry options.</param>
    /// <returns>This builder for chaining.</returns>
    public DurableSagaStepBuilder<TData> WithRetry(TaskOptions options)
    {
        _retryOptions = options;
        return this;
    }

    /// <summary>
    /// Marks this step to skip compensation if it fails (useful for idempotent operations).
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public DurableSagaStepBuilder<TData> SkipCompensationOnFailure()
    {
        _skipCompensationOnFailure = true;
        return this;
    }

    /// <summary>
    /// Adds the current step and starts a new one.
    /// </summary>
    /// <param name="stepName">The name of the next step.</param>
    /// <returns>A new step builder for the next step.</returns>
    public DurableSagaStepBuilder<TData> Step(string stepName)
    {
        FinalizeStep();
        return _sagaBuilder.Step(stepName);
    }

    /// <summary>
    /// Adds the current step and builds the saga.
    /// </summary>
    /// <returns>The built saga definition.</returns>
    public DurableSaga<TData> Build()
    {
        FinalizeStep();
        return _sagaBuilder.Build();
    }

    private void FinalizeStep()
    {
        if (string.IsNullOrEmpty(_executeActivityName))
        {
            throw new InvalidOperationException($"Step '{_stepName}' must have an Execute activity.");
        }

        var step = new DurableSagaStep<TData>
        {
            StepName = _stepName,
            ExecuteActivityName = _executeActivityName,
            CompensateActivityName = _compensateActivityName,
            RetryOptions = _retryOptions ?? _sagaBuilder.DefaultRetryOptions,
            SkipCompensationOnFailure = _skipCompensationOnFailure
        };

        _sagaBuilder.AddStep(step);
    }
}

/// <summary>
/// Represents a step in a durable saga.
/// </summary>
/// <typeparam name="TData">The saga data type.</typeparam>
public sealed class DurableSagaStep<TData> // NOSONAR S2326: TData ensures type-safe saga step composition
{
    /// <summary>
    /// Gets or sets the step name.
    /// </summary>
    public required string StepName { get; set; }

    /// <summary>
    /// Gets or sets the execute activity name.
    /// </summary>
    public required string ExecuteActivityName { get; set; }

    /// <summary>
    /// Gets or sets the compensation activity name (optional).
    /// </summary>
    public string? CompensateActivityName { get; set; }

    /// <summary>
    /// Gets or sets the retry options for this step.
    /// </summary>
    public TaskOptions? RetryOptions { get; set; }

    /// <summary>
    /// Gets or sets whether to skip compensation if this step fails.
    /// </summary>
    public bool SkipCompensationOnFailure { get; set; }
}

/// <summary>
/// An executable saga definition built from <see cref="DurableSagaBuilder{TData}"/>.
/// </summary>
/// <typeparam name="TData">The saga data type.</typeparam>
public sealed class DurableSaga<TData>
{
    private readonly IReadOnlyList<DurableSagaStep<TData>> _steps;
    private readonly TaskOptions? _defaultRetryOptions;

    internal DurableSaga(
        IReadOnlyList<DurableSagaStep<TData>> steps,
        TaskOptions? defaultRetryOptions,
        TimeSpan? timeout)
    {
        _steps = steps;
        _defaultRetryOptions = defaultRetryOptions;
        // timeout is reserved for future use when saga-level timeouts are implemented
        _ = timeout;
    }

    /// <summary>
    /// Gets the saga steps.
    /// </summary>
    public IReadOnlyList<DurableSagaStep<TData>> Steps => _steps;

    /// <summary>
    /// Executes the saga within a Durable Functions orchestration.
    /// </summary>
    /// <param name="context">The orchestration context.</param>
    /// <param name="initialData">The initial saga data.</param>
    /// <returns>Either the final saga data or an error with compensation results.</returns>
    public async Task<Either<DurableSagaError, TData>> ExecuteAsync(
        TaskOrchestrationContext context,
        TData initialData)
    {
        ArgumentNullException.ThrowIfNull(context);

        var executedSteps = new Stack<DurableSagaStep<TData>>();
        var currentData = initialData;

        try
        {
            foreach (var step in _steps)
            {
                var result = await context.CallEncinaActivityWithResultAsync<TData, TData>(
                    step.ExecuteActivityName,
                    currentData,
                    step.RetryOptions ?? _defaultRetryOptions);

                if (result.IsLeft)
                {
                    var error = result.Match(Left: e => e, Right: _ => default!);

                    // Step failed - begin compensation
                    var compensationResult = await CompensateAsync(context, executedSteps, currentData);

                    return new DurableSagaError
                    {
                        FailedStep = step.StepName,
                        OriginalError = error,
                        CompensationErrors = compensationResult,
                        WasCompensated = compensationResult.Count == 0 || compensationResult.All(c => c.Value == null)
                    };
                }

                currentData = result.Match(Right: d => d, Left: _ => currentData);

                if (!step.SkipCompensationOnFailure && step.CompensateActivityName != null)
                {
                    executedSteps.Push(step);
                }
            }

            return currentData;
        }
        catch (Exception ex)
        {
            // Unexpected error - try to compensate
            var compensationResult = await CompensateAsync(context, executedSteps, currentData);

            return new DurableSagaError
            {
                FailedStep = "Unknown (exception)",
                OriginalError = EncinaErrors.Create(
                    "durable.saga_exception",
                    $"Saga failed with exception: {ex.Message}",
                    ex),
                CompensationErrors = compensationResult,
                WasCompensated = false
            };
        }
    }

    private async Task<Dictionary<string, EncinaError?>> CompensateAsync(
        TaskOrchestrationContext context,
        Stack<DurableSagaStep<TData>> executedSteps,
        TData currentData)
    {
        var compensationErrors = new Dictionary<string, EncinaError?>();

        while (executedSteps.Count > 0)
        {
            var step = executedSteps.Pop();

            if (step.CompensateActivityName is null)
            {
                continue;
            }

            try
            {
                var compensateResult = await context.CallEncinaActivityWithResultAsync<TData, TData>(
                    step.CompensateActivityName,
                    currentData,
                    step.RetryOptions ?? _defaultRetryOptions);

                if (compensateResult.IsLeft)
                {
                    var error = compensateResult.Match(Left: e => e, Right: _ => default!);
                    compensationErrors[step.StepName] = error;
                }
                else
                {
                    compensationErrors[step.StepName] = null; // Success
                    currentData = compensateResult.Match(Right: d => d, Left: _ => currentData);
                }
            }
            catch (Exception ex)
            {
                compensationErrors[step.StepName] = EncinaErrors.Create(
                    "durable.compensation_exception",
                    $"Compensation for step '{step.StepName}' threw exception: {ex.Message}",
                    ex);
            }
        }

        return compensationErrors;
    }
}

/// <summary>
/// Represents an error that occurred during saga execution.
/// </summary>
public sealed class DurableSagaError
{
    /// <summary>
    /// Gets or sets the name of the step that failed.
    /// </summary>
    public required string FailedStep { get; set; }

    /// <summary>
    /// Gets or sets the original error that caused the saga to fail.
    /// </summary>
    public required EncinaError OriginalError { get; set; }

    /// <summary>
    /// Gets or sets the compensation errors (step name to error, null if compensation succeeded).
    /// </summary>
    public required Dictionary<string, EncinaError?> CompensationErrors { get; set; }

    /// <summary>
    /// Gets or sets whether all compensations completed successfully.
    /// </summary>
    public required bool WasCompensated { get; set; }
}

/// <summary>
/// Factory methods for creating saga builders.
/// </summary>
public static class DurableSagaBuilder
{
    /// <summary>
    /// Creates a new saga builder.
    /// </summary>
    /// <typeparam name="TData">The saga data type.</typeparam>
    /// <returns>A new saga builder.</returns>
    public static DurableSagaBuilder<TData> Create<TData>()
    {
        return new DurableSagaBuilder<TData>();
    }
}
