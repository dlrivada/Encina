using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Encina.Diagnostics;
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
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly RoutingSlipOptions _options;
    private readonly ILogger<RoutingSlipRunner> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingSlipRunner"/> class.
    /// </summary>
    /// <param name="requestContextAccessor">
    /// Accessor for the ambient request context. Every run resolves the current context (or a
    /// fresh one when none is in flight) instead of capturing a snapshot at construction time,
    /// since this runner is registered as scoped but may outlive the caller's own dispatch.
    /// </param>
    /// <param name="options">The routing slip options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    public RoutingSlipRunner(
        IRequestContextAccessor requestContextAccessor,
        RoutingSlipOptions options,
        ILogger<RoutingSlipRunner> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(requestContextAccessor);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _requestContextAccessor = requestContextAccessor;
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
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

        // The ambient context set by IEncina.Send/Publish/Stream wins; a run started outside a
        // dispatch (a background job invoking the routing slip directly) gets a fresh context
        // instead of a null one, since steps require a non-null IRequestContext.
        var requestContext = _requestContextAccessor.RequestContext ?? RequestContext.Create();

        // Create mutable copies for the context
        var remainingSteps = new List<RoutingSlipStepDefinition<TData>>(definition.Steps);
        var activityLog = new List<RoutingSlipActivityEntry<TData>>();

        var context = new RoutingSlipContext<TData>(
            routingSlipId,
            definition.SlipType,
            requestContext,
            remainingSteps,
            activityLog);

        RoutingSlipLog.Started(_logger, routingSlipId, definition.SlipType, initialStepCount);

        var progress = new StepProgress<TData>(initialData);

        try
        {
            // Execute steps until the itinerary is empty
            var failure = await ExecuteStepsAsync(context, remainingSteps, progress, cancellationToken)
                .ConfigureAwait(false);
            if (failure is { } stepError)
            {
                stopwatch.Stop();
                return stepError;
            }

            // Run completion handler if defined
            if (definition.OnCompletion is not null)
            {
                RoutingSlipLog.CompletionHandlerExecuting(_logger, routingSlipId);
                await definition.OnCompletion(progress.CurrentData, context, cancellationToken).ConfigureAwait(false);
                RoutingSlipLog.CompletionHandlerCompleted(_logger, routingSlipId);
            }

            stopwatch.Stop();
            var stepsRemoved = initialStepCount + progress.StepsAdded - progress.StepsExecuted;

            RoutingSlipLog.Completed(_logger, routingSlipId, progress.StepsExecuted, stopwatch.Elapsed);

            return new RoutingSlipResult<TData>(
                routingSlipId,
                progress.CurrentData,
                progress.StepsExecuted,
                progress.StepsAdded,
                stepsRemoved > 0 ? stepsRemoved : 0,
                stopwatch.Elapsed,
                activityLog);
        }
        catch (OperationCanceledException)
        {
            RoutingSlipLog.Cancelled(_logger, routingSlipId);

            // Run compensation for completed steps
            await CompensateAsync(context, CancellationToken.None).ConfigureAwait(false);

            return EncinaErrors.Create(RoutingSlipErrorCodes.HandlerCancelled, "Routing slip was cancelled");
        }
        catch (Exception ex)
        {
            RoutingSlipLog.Exception(_logger, routingSlipId, ex.GetType().Name, ex.ForLogging());

            // Run compensation for completed steps
            await CompensateAsync(context, CancellationToken.None).ConfigureAwait(false);

            return EncinaErrors.Create(RoutingSlipErrorCodes.HandlerFailed, ex.Message);
        }
    }

    /// <summary>
    /// Executes the itinerary until it is empty, updating <paramref name="progress"/>.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> when every step succeeded; otherwise the error of the failed step,
    /// after the completed steps have been compensated.
    /// </returns>
    private async Task<EncinaError?> ExecuteStepsAsync<TData>(
        RoutingSlipContext<TData> context,
        List<RoutingSlipStepDefinition<TData>> remainingSteps,
        StepProgress<TData> progress,
        CancellationToken cancellationToken)
        where TData : class, new()
    {
        var routingSlipId = context.RoutingSlipId;

        while (remainingSteps.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Pop the next step
            var step = remainingSteps[0];
            remainingSteps.RemoveAt(0);

            var stepCountBefore = remainingSteps.Count;
            RoutingSlipLog.StepExecuting(_logger, routingSlipId, progress.StepsExecuted + 1, step.Name);

            var stepResult = await step.Execute(progress.CurrentData, context, cancellationToken)
                .ConfigureAwait(false);

            if (stepResult.IsLeft)
            {
                // Step failed - start compensation
                var error = stepResult.Match(
                    Right: _ => EncinaErrors.Create(RoutingSlipErrorCodes.StepFailed, "Unexpected"),
                    Left: e => e);

                // Only the error code: EncinaError.Message can carry personal data (#1259 review).
                RoutingSlipLog.StepFailed(_logger, routingSlipId, progress.StepsExecuted + 1, step.Name, error.GetCode().IfNone("encina.unknown"));

                // Run compensation for completed steps (in reverse order)
                await CompensateAsync(context, cancellationToken).ConfigureAwait(false);

                return error;
            }

            // Update data
            progress.CurrentData = stepResult.Match(
                Right: data => data,
                Left: _ => progress.CurrentData);

            // Track if steps were added during this execution
            var stepsAddedThisStep = remainingSteps.Count - stepCountBefore;
            if (stepsAddedThisStep > 0)
            {
                progress.StepsAdded += stepsAddedThisStep;
                RoutingSlipLog.StepsModified(_logger, routingSlipId, step.Name, stepsAddedThisStep);
            }

            // Record activity for compensation
            context.RecordActivity(new RoutingSlipActivityEntry<TData>(
                step.Name,
                progress.CurrentData,
                step.Compensate,
                _timeProvider.GetUtcNow().UtcDateTime,
                step.Metadata));

            progress.StepsExecuted++;
            RoutingSlipLog.StepCompleted(_logger, routingSlipId, progress.StepsExecuted, step.Name);
        }

        return null;
    }

    private sealed class StepProgress<TData>(TData initialData)
        where TData : class
    {
        public TData CurrentData { get; set; } = initialData;

        public int StepsExecuted { get; set; }

        public int StepsAdded { get; set; }
    }

    private async Task CompensateAsync<TData>(
        RoutingSlipContext<TData> context,
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
                RoutingSlipLog.CompensationFailed(_logger, i + 1, entry.StepName, ex.GetType().Name, ex.ForLogging());

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
        EventId = 2872,
        Level = LogLevel.Information,
        Message = "Routing slip {RoutingSlipId} started (type: {SlipType}, initial steps: {StepCount})")]
    public static partial void Started(ILogger logger, Guid routingSlipId, string slipType, int stepCount);

    [LoggerMessage(
        EventId = 2873,
        Level = LogLevel.Debug,
        Message = "Routing slip {RoutingSlipId} executing step {StepNumber}: {StepName}")]
    public static partial void StepExecuting(ILogger logger, Guid routingSlipId, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 2874,
        Level = LogLevel.Debug,
        Message = "Routing slip {RoutingSlipId} step {StepNumber} completed: {StepName}")]
    public static partial void StepCompleted(ILogger logger, Guid routingSlipId, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 2875,
        Level = LogLevel.Warning,
        Message = "Routing slip {RoutingSlipId} step {StepNumber} failed: {StepName} - {ErrorCode}")]
    public static partial void StepFailed(ILogger logger, Guid routingSlipId, int stepNumber, string stepName, string errorCode);

    [LoggerMessage(
        EventId = 2876,
        Level = LogLevel.Information,
        Message = "Routing slip {RoutingSlipId} completed successfully ({StepsExecuted} steps, {Duration})")]
    public static partial void Completed(ILogger logger, Guid routingSlipId, int stepsExecuted, TimeSpan duration);

    [LoggerMessage(
        EventId = 2877,
        Level = LogLevel.Warning,
        Message = "Routing slip {RoutingSlipId} was cancelled")]
    public static partial void Cancelled(ILogger logger, Guid routingSlipId);

    [LoggerMessage(
        EventId = 2878,
        Level = LogLevel.Error,
        Message = "Routing slip {RoutingSlipId} failed with exception: {ExceptionType}")]
    public static partial void Exception(ILogger logger, Guid routingSlipId, string exceptionType, Exception exception);

    [LoggerMessage(
        EventId = 2879,
        Level = LogLevel.Debug,
        Message = "Routing slip {RoutingSlipId} step {StepName} modified itinerary ({StepsAdded} steps added)")]
    public static partial void StepsModified(ILogger logger, Guid routingSlipId, string stepName, int stepsAdded);

    [LoggerMessage(
        EventId = 2880,
        Level = LogLevel.Debug,
        Message = "Routing slip {RoutingSlipId} executing completion handler")]
    public static partial void CompletionHandlerExecuting(ILogger logger, Guid routingSlipId);

    [LoggerMessage(
        EventId = 2881,
        Level = LogLevel.Debug,
        Message = "Routing slip {RoutingSlipId} completion handler completed")]
    public static partial void CompletionHandlerCompleted(ILogger logger, Guid routingSlipId);

    [LoggerMessage(
        EventId = 2882,
        Level = LogLevel.Information,
        Message = "Routing slip {RoutingSlipId} starting compensation ({StepCount} steps to compensate)")]
    public static partial void CompensationStarting(ILogger logger, Guid routingSlipId, int stepCount);

    [LoggerMessage(
        EventId = 2883,
        Level = LogLevel.Debug,
        Message = "Step {StepNumber} ({StepName}) has no compensation defined")]
    public static partial void StepNoCompensation(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 2884,
        Level = LogLevel.Debug,
        Message = "Compensating step {StepNumber}: {StepName}")]
    public static partial void StepCompensating(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 2885,
        Level = LogLevel.Debug,
        Message = "Step {StepNumber} ({StepName}) compensation completed")]
    public static partial void StepCompensated(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 2886,
        Level = LogLevel.Error,
        Message = "Compensation failed for step {StepNumber} ({StepName}): {ExceptionType}")]
    public static partial void CompensationFailed(ILogger logger, int stepNumber, string stepName, string exceptionType, Exception exception);

    [LoggerMessage(
        EventId = 2887,
        Level = LogLevel.Information,
        Message = "Routing slip {RoutingSlipId} compensation completed")]
    public static partial void CompensationCompleted(ILogger logger, Guid routingSlipId);
}
