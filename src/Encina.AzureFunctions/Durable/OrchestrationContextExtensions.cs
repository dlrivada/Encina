using LanguageExt;
using Microsoft.DurableTask;

namespace Encina.AzureFunctions.Durable;

/// <summary>
/// Extension methods for <see cref="TaskOrchestrationContext"/> to integrate with Encina's Railway Oriented Programming.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide seamless integration between Durable Functions orchestrations
/// and Encina's Either-based error handling, enabling ROP patterns in durable workflows.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Function("OrderOrchestrator")]
/// public async Task&lt;Either&lt;EncinaError, OrderResult&gt;&gt; RunOrchestrator(
///     [OrchestrationTrigger] TaskOrchestrationContext context)
/// {
///     var orderId = context.GetInput&lt;Guid&gt;();
///
///     // Call activity with ROP - returns Either instead of throwing
///     var paymentResult = await context.CallEncinaActivityAsync&lt;ProcessPayment, PaymentResult&gt;(
///         "ProcessPayment", new ProcessPayment(orderId));
///
///     return paymentResult.Bind(payment =&gt;
///         context.CallEncinaActivityAsync&lt;ShipOrder, ShipmentResult&gt;(
///             "ShipOrder", new ShipOrder(orderId, payment.TransactionId)));
/// }
/// </code>
/// </example>
public static class OrchestrationContextExtensions
{
    /// <summary>
    /// Calls an activity function and returns the result as an Either, catching any failures.
    /// </summary>
    /// <typeparam name="TInput">The activity input type.</typeparam>
    /// <typeparam name="TResult">The activity result type.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activityName">The name of the activity function.</param>
    /// <param name="input">The input to pass to the activity.</param>
    /// <param name="options">Optional task options for retry configuration.</param>
    /// <returns>Either the result or an error.</returns>
    /// <example>
    /// <code>
    /// var result = await context.CallEncinaActivityAsync&lt;ProcessPayment, PaymentResult&gt;(
    ///     "ProcessPayment", new ProcessPayment(orderId));
    /// </code>
    /// </example>
    public static async Task<Either<EncinaError, TResult>> CallEncinaActivityAsync<TInput, TResult>(
        this TaskOrchestrationContext context,
        string activityName,
        TInput input,
        TaskOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(activityName);

        try
        {
            var result = await context.CallActivityAsync<TResult>(activityName, input, options);
            return result;
        }
        catch (TaskFailedException ex)
        {
            return EncinaErrors.Create(
                "durable.activity_failed",
                $"Activity '{activityName}' failed: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Calls an activity function that returns an Either result, unwrapping the serialized Either.
    /// </summary>
    /// <typeparam name="TInput">The activity input type.</typeparam>
    /// <typeparam name="TResult">The activity result type.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activityName">The name of the activity function.</param>
    /// <param name="input">The input to pass to the activity.</param>
    /// <param name="options">Optional task options for retry configuration.</param>
    /// <returns>The Either result from the activity.</returns>
    /// <remarks>
    /// Use this when your activity function returns <c>Either&lt;EncinaError, TResult&gt;</c>.
    /// The result is serialized/deserialized using the <see cref="ActivityResult{T}"/> wrapper.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Activity that returns Either
    /// [Function("ProcessPayment")]
    /// public async Task&lt;ActivityResult&lt;PaymentResult&gt;&gt; ProcessPayment(
    ///     [ActivityTrigger] ProcessPayment command, IEncina encina)
    /// {
    ///     var result = await encina.Send(command);
    ///     return result.ToActivityResult();
    /// }
    ///
    /// // Orchestrator calling it
    /// var result = await context.CallEncinaActivityWithResultAsync&lt;ProcessPayment, PaymentResult&gt;(
    ///     "ProcessPayment", command);
    /// </code>
    /// </example>
    public static async Task<Either<EncinaError, TResult>> CallEncinaActivityWithResultAsync<TInput, TResult>(
        this TaskOrchestrationContext context,
        string activityName,
        TInput input,
        TaskOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(activityName);

        try
        {
            var activityResult = await context.CallActivityAsync<ActivityResult<TResult>>(
                activityName, input, options);

            return activityResult.ToEither();
        }
        catch (TaskFailedException ex)
        {
            return EncinaErrors.Create(
                "durable.activity_failed",
                $"Activity '{activityName}' failed: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Calls a sub-orchestrator and returns the result as an Either.
    /// </summary>
    /// <typeparam name="TInput">The sub-orchestrator input type.</typeparam>
    /// <typeparam name="TResult">The sub-orchestrator result type.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="orchestratorName">The name of the sub-orchestrator function.</param>
    /// <param name="input">The input to pass to the sub-orchestrator.</param>
    /// <param name="options">Optional task options.</param>
    /// <returns>Either the result or an error.</returns>
    public static async Task<Either<EncinaError, TResult>> CallEncinaSubOrchestratorAsync<TInput, TResult>(
        this TaskOrchestrationContext context,
        string orchestratorName,
        TInput input,
        TaskOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(orchestratorName);

        try
        {
            var result = await context.CallSubOrchestratorAsync<TResult>(orchestratorName, input, options);
            return result;
        }
        catch (TaskFailedException ex)
        {
            return EncinaErrors.Create(
                "durable.suborchestrator_failed",
                $"Sub-orchestrator '{orchestratorName}' failed: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Calls a sub-orchestrator that returns an Either result.
    /// </summary>
    /// <typeparam name="TInput">The sub-orchestrator input type.</typeparam>
    /// <typeparam name="TResult">The sub-orchestrator result type.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="orchestratorName">The name of the sub-orchestrator function.</param>
    /// <param name="input">The input to pass to the sub-orchestrator.</param>
    /// <param name="options">Optional task options.</param>
    /// <returns>The Either result from the sub-orchestrator.</returns>
    public static async Task<Either<EncinaError, TResult>> CallEncinaSubOrchestratorWithResultAsync<TInput, TResult>(
        this TaskOrchestrationContext context,
        string orchestratorName,
        TInput input,
        TaskOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(orchestratorName);

        try
        {
            var activityResult = await context.CallSubOrchestratorAsync<ActivityResult<TResult>>(
                orchestratorName, input, options);

            return activityResult.ToEither();
        }
        catch (TaskFailedException ex)
        {
            return EncinaErrors.Create(
                "durable.suborchestrator_failed",
                $"Sub-orchestrator '{orchestratorName}' failed: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Waits for an external event and returns the result as an Either.
    /// </summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="eventName">The name of the event to wait for.</param>
    /// <param name="timeout">Optional timeout for waiting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Either the event data or an error if timeout occurred.</returns>
    public static async Task<Either<EncinaError, T>> WaitForEncinaExternalEventAsync<T>(
        this TaskOrchestrationContext context,
        string eventName,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(eventName);

        try
        {
            if (timeout.HasValue)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var eventTask = context.WaitForExternalEvent<T>(eventName, cts.Token);
                var timerTask = context.CreateTimer(context.CurrentUtcDateTime.Add(timeout.Value), cts.Token);

                var completedTask = await Task.WhenAny(eventTask, timerTask);

                if (completedTask == timerTask)
                {
                    return EncinaErrors.Create(
                        "durable.event_timeout",
                        $"Timeout waiting for external event '{eventName}'");
                }

                await cts.CancelAsync();
                return await eventTask;
            }
            else
            {
                var result = await context.WaitForExternalEvent<T>(eventName, cancellationToken);
                return result;
            }
        }
        catch (OperationCanceledException)
        {
            return EncinaErrors.Create(
                "durable.event_cancelled",
                $"Wait for external event '{eventName}' was cancelled");
        }
        catch (TaskFailedException ex)
        {
            return EncinaErrors.Create(
                "durable.event_failed",
                $"Failed waiting for external event '{eventName}': {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Creates retry options from Encina retry configuration.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="firstRetryInterval">Initial retry interval.</param>
    /// <param name="backoffCoefficient">Backoff multiplier (default: 2.0).</param>
    /// <param name="maxRetryInterval">Maximum retry interval.</param>
    /// <returns>TaskOptions configured for retries.</returns>
    public static TaskOptions CreateRetryOptions(
        int maxRetries,
        TimeSpan firstRetryInterval,
        double backoffCoefficient = 2.0,
        TimeSpan? maxRetryInterval = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(firstRetryInterval, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(backoffCoefficient, 0);

        var retryPolicy = new RetryPolicy(
            maxNumberOfAttempts: maxRetries + 1,
            firstRetryInterval: firstRetryInterval,
            backoffCoefficient: backoffCoefficient,
            maxRetryInterval: maxRetryInterval);

        return TaskOptions.FromRetryPolicy(retryPolicy);
    }

    /// <summary>
    /// Gets the orchestration instance ID as a correlation ID for logging.
    /// </summary>
    /// <param name="context">The orchestration context.</param>
    /// <returns>The instance ID.</returns>
    public static string GetCorrelationId(this TaskOrchestrationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.InstanceId;
    }
}
