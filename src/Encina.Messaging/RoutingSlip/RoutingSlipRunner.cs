using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Messaging.RoutingSlip;

/// <summary>
/// Executes routing slip definitions with full lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// The routing slip runner handles step execution, dynamic route modification,
/// and compensation on failure.
/// </para>
/// </remarks>
public sealed class RoutingSlipRunner : IRoutingSlipRunner
{
    private readonly IRequestContext _requestContext;
    private readonly RoutingSlipOptions _options;
    private readonly ILogger<RoutingSlipRunner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingSlipRunner"/> class.
    /// </summary>
    /// <param name="requestContext">The request context.</param>
    /// <param name="options">The routing slip options.</param>
    /// <param name="logger">The logger.</param>
    public RoutingSlipRunner(
        IRequestContext requestContext,
        RoutingSlipOptions options,
        ILogger<RoutingSlipRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _requestContext = requestContext;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, RoutingSlipResult<TData>>> RunAsync<TData>(
        BuiltRoutingSlipDefinition<TData> definition,
        CancellationToken cancellationToken = default)
        where TData : class, new()
    {
        return RunAsync(definition, new TData(), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, RoutingSlipResult<TData>>> RunAsync<TData>(
        BuiltRoutingSlipDefinition<TData> definition,
        TData initialData,
        CancellationToken cancellationToken = default)
        where TData : class, new()
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(initialData);

        var routingSlipId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        var initialStepCount = definition.Steps.Count;

        // Create mutable copies for the context
        var remainingSteps = new List<RoutingSlipStepDefinition<TData>>(definition.Steps);
        var activityLog = new List<RoutingSlipActivityEntry<TData>>();

        var context = new RoutingSlipContext<TData>(
            routingSlipId,
            definition.SlipType,
            _requestContext,
            remainingSteps,
            activityLog);

        RoutingSlipLog.Started(_logger, routingSlipId, definition.SlipType, initialStepCount);

        var currentData = initialData;
        var stepsExecuted = 0;
        var stepsAdded = 0;

        try
        {
            // Execute steps until the itinerary is empty
            while (remainingSteps.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Pop the next step
                var step = remainingSteps[0];
                remainingSteps.RemoveAt(0);

                var stepCountBefore = remainingSteps.Count;
                RoutingSlipLog.StepExecuting(_logger, routingSlipId, stepsExecuted + 1, step.Name);

                var stepResult = await step.Execute(currentData, context, cancellationToken)
                    .ConfigureAwait(false);

                if (stepResult.IsLeft)
                {
                    // Step failed - start compensation
                    var error = stepResult.Match(
                        Right: _ => EncinaErrors.Create(RoutingSlipErrorCodes.StepFailed, "Unexpected"),
                        Left: e => e);

                    RoutingSlipLog.StepFailed(_logger, routingSlipId, stepsExecuted + 1, step.Name, error.Message);

                    // Run compensation for completed steps (in reverse order)
                    await CompensateAsync(context, currentData, cancellationToken).ConfigureAwait(false);

                    stopwatch.Stop();
                    return error;
                }

                // Update data
                currentData = stepResult.Match(
                    Right: data => data,
                    Left: _ => currentData);

                // Track if steps were added during this execution
                var stepsAddedThisStep = remainingSteps.Count - stepCountBefore;
                if (stepsAddedThisStep > 0)
                {
                    stepsAdded += stepsAddedThisStep;
                    RoutingSlipLog.StepsModified(_logger, routingSlipId, step.Name, stepsAddedThisStep);
                }

                // Record activity for compensation
                context.RecordActivity(new RoutingSlipActivityEntry<TData>(
                    step.Name,
                    currentData,
                    step.Compensate,
                    DateTime.UtcNow,
                    step.Metadata));

                stepsExecuted++;
                RoutingSlipLog.StepCompleted(_logger, routingSlipId, stepsExecuted, step.Name);
            }

            // Run completion handler if defined
            if (definition.OnCompletion is not null)
            {
                RoutingSlipLog.CompletionHandlerExecuting(_logger, routingSlipId);
                await definition.OnCompletion(currentData, context, cancellationToken).ConfigureAwait(false);
                RoutingSlipLog.CompletionHandlerCompleted(_logger, routingSlipId);
            }

            stopwatch.Stop();
            var stepsRemoved = initialStepCount + stepsAdded - stepsExecuted;

            RoutingSlipLog.Completed(_logger, routingSlipId, stepsExecuted, stopwatch.Elapsed);

            return new RoutingSlipResult<TData>(
                routingSlipId,
                currentData,
                stepsExecuted,
                stepsAdded,
                stepsRemoved > 0 ? stepsRemoved : 0,
                stopwatch.Elapsed,
                activityLog);
        }
        catch (OperationCanceledException)
        {
            RoutingSlipLog.Cancelled(_logger, routingSlipId);

            // Run compensation for completed steps
            await CompensateAsync(context, currentData, CancellationToken.None).ConfigureAwait(false);

            return EncinaErrors.Create(RoutingSlipErrorCodes.HandlerCancelled, "Routing slip was cancelled");
        }
        catch (Exception ex)
        {
            RoutingSlipLog.Exception(_logger, routingSlipId, ex.Message, ex);

            // Run compensation for completed steps
            await CompensateAsync(context, currentData, CancellationToken.None).ConfigureAwait(false);

            return EncinaErrors.Create(RoutingSlipErrorCodes.HandlerFailed, ex.Message);
        }
    }

    private async Task CompensateAsync<TData>(
        RoutingSlipContext<TData> context,
        TData _,
        CancellationToken cancellationToken)
        where TData : class
    {
        var activityLog = context.GetActivityLog();

        if (activityLog.Count == 0)
        {
            return;
        }

        RoutingSlipLog.CompensationStarting(_logger, context.RoutingSlipId, activityLog.Count);

        // Compensate in reverse order
        for (var i = activityLog.Count - 1; i >= 0; i--)
        {
            var entry = activityLog[i];

            if (entry.Compensate is null)
            {
                RoutingSlipLog.StepNoCompensation(_logger, i + 1, entry.StepName);
                continue;
            }

            try
            {
                RoutingSlipLog.StepCompensating(_logger, i + 1, entry.StepName);

                // Use the data state from when this step executed
                await entry.Compensate(entry.DataAfterExecution, context, cancellationToken)
                    .ConfigureAwait(false);

                RoutingSlipLog.StepCompensated(_logger, i + 1, entry.StepName);
            }
            catch (Exception ex)
            {
                RoutingSlipLog.CompensationFailed(_logger, i + 1, entry.StepName, ex.Message, ex);

                if (!_options.ContinueCompensationOnFailure)
                {
                    throw;
                }
                // Continue with remaining compensations
            }
        }

        RoutingSlipLog.CompensationCompleted(_logger, context.RoutingSlipId);
    }
}

/// <summary>
/// LoggerMessage definitions for high-performance logging.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class RoutingSlipLog
{
    [LoggerMessage(
        EventId = 400,
        Level = LogLevel.Information,
        Message = "Routing slip {RoutingSlipId} started (type: {SlipType}, initial steps: {StepCount})")]
    public static partial void Started(ILogger logger, Guid routingSlipId, string slipType, int stepCount);

    [LoggerMessage(
        EventId = 401,
        Level = LogLevel.Debug,
        Message = "Routing slip {RoutingSlipId} executing step {StepNumber}: {StepName}")]
    public static partial void StepExecuting(ILogger logger, Guid routingSlipId, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 402,
        Level = LogLevel.Debug,
        Message = "Routing slip {RoutingSlipId} step {StepNumber} completed: {StepName}")]
    public static partial void StepCompleted(ILogger logger, Guid routingSlipId, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 403,
        Level = LogLevel.Warning,
        Message = "Routing slip {RoutingSlipId} step {StepNumber} failed: {StepName} - {ErrorMessage}")]
    public static partial void StepFailed(ILogger logger, Guid routingSlipId, int stepNumber, string stepName, string errorMessage);

    [LoggerMessage(
        EventId = 404,
        Level = LogLevel.Information,
        Message = "Routing slip {RoutingSlipId} completed successfully ({StepsExecuted} steps, {Duration})")]
    public static partial void Completed(ILogger logger, Guid routingSlipId, int stepsExecuted, TimeSpan duration);

    [LoggerMessage(
        EventId = 405,
        Level = LogLevel.Warning,
        Message = "Routing slip {RoutingSlipId} was cancelled")]
    public static partial void Cancelled(ILogger logger, Guid routingSlipId);

    [LoggerMessage(
        EventId = 406,
        Level = LogLevel.Error,
        Message = "Routing slip {RoutingSlipId} failed with exception: {ErrorMessage}")]
    public static partial void Exception(ILogger logger, Guid routingSlipId, string errorMessage, Exception exception);

    [LoggerMessage(
        EventId = 407,
        Level = LogLevel.Debug,
        Message = "Routing slip {RoutingSlipId} step {StepName} modified itinerary ({StepsAdded} steps added)")]
    public static partial void StepsModified(ILogger logger, Guid routingSlipId, string stepName, int stepsAdded);

    [LoggerMessage(
        EventId = 408,
        Level = LogLevel.Debug,
        Message = "Routing slip {RoutingSlipId} executing completion handler")]
    public static partial void CompletionHandlerExecuting(ILogger logger, Guid routingSlipId);

    [LoggerMessage(
        EventId = 409,
        Level = LogLevel.Debug,
        Message = "Routing slip {RoutingSlipId} completion handler completed")]
    public static partial void CompletionHandlerCompleted(ILogger logger, Guid routingSlipId);

    [LoggerMessage(
        EventId = 410,
        Level = LogLevel.Information,
        Message = "Routing slip {RoutingSlipId} starting compensation ({StepCount} steps to compensate)")]
    public static partial void CompensationStarting(ILogger logger, Guid routingSlipId, int stepCount);

    [LoggerMessage(
        EventId = 411,
        Level = LogLevel.Debug,
        Message = "Step {StepNumber} ({StepName}) has no compensation defined")]
    public static partial void StepNoCompensation(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 412,
        Level = LogLevel.Debug,
        Message = "Compensating step {StepNumber}: {StepName}")]
    public static partial void StepCompensating(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 413,
        Level = LogLevel.Debug,
        Message = "Step {StepNumber} ({StepName}) compensation completed")]
    public static partial void StepCompensated(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 414,
        Level = LogLevel.Error,
        Message = "Compensation failed for step {StepNumber} ({StepName}): {ErrorMessage}")]
    public static partial void CompensationFailed(ILogger logger, int stepNumber, string stepName, string errorMessage, Exception exception);

    [LoggerMessage(
        EventId = 415,
        Level = LogLevel.Information,
        Message = "Routing slip {RoutingSlipId} compensation completed")]
    public static partial void CompensationCompleted(ILogger logger, Guid routingSlipId);
}
