using LanguageExt;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Sagas.LowCeremony;

/// <summary>
/// Executes saga definitions with full lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// The saga runner coordinates with <see cref="SagaOrchestrator"/> for state persistence
/// while handling the actual step execution and compensation logic.
/// </para>
/// </remarks>
public sealed class SagaRunner : ISagaRunner
{
    private readonly SagaOrchestrator _orchestrator;
    private readonly IRequestContext _requestContext;
    private readonly ILogger<SagaRunner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaRunner"/> class.
    /// </summary>
    /// <param name="orchestrator">The saga orchestrator for state management.</param>
    /// <param name="requestContext">The request context.</param>
    /// <param name="logger">The logger.</param>
    public SagaRunner(
        SagaOrchestrator orchestrator,
        IRequestContext requestContext,
        ILogger<SagaRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(logger);

        _orchestrator = orchestrator;
        _requestContext = requestContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, SagaResult<TData>>> RunAsync<TData>(
        BuiltSagaDefinition<TData> definition,
        CancellationToken cancellationToken = default)
        where TData : class, new()
    {
        return RunAsync(definition, new TData(), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, SagaResult<TData>>> RunAsync<TData>(
        BuiltSagaDefinition<TData> definition,
        TData initialData,
        CancellationToken cancellationToken = default)
        where TData : class, new()
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(initialData);

        // Start the saga
        var sagaId = await _orchestrator.StartAsync(
            definition.SagaType,
            initialData,
            definition.Timeout,
            cancellationToken).ConfigureAwait(false);

        Log.SagaStarted(_logger, sagaId, definition.SagaType, definition.StepCount);

        var currentData = initialData;
        var stepsExecuted = 0;

        try
        {
            // Execute each step
            for (var i = 0; i < definition.Steps.Count; i++)
            {
                var step = definition.Steps[i];
                Log.StepExecuting(_logger, sagaId, i + 1, step.Name);

                var stepResult = await step.Execute(currentData, _requestContext, cancellationToken)
                    .ConfigureAwait(false);

                if (stepResult.IsLeft)
                {
                    // Step failed - start compensation
                    var error = stepResult.Match(
                        Right: _ => EncinaErrors.Create(SagaErrorCodes.StepFailed, "Unexpected"),
                        Left: e => e);

                    Log.StepFailed(_logger, sagaId, i + 1, step.Name, error.Message);

                    // Run compensation for completed steps
                    await CompensateAsync(definition, currentData, i - 1, cancellationToken)
                        .ConfigureAwait(false);

                    // Mark saga as compensated
                    await _orchestrator.StartCompensationAsync(sagaId, error.Message, cancellationToken)
                        .ConfigureAwait(false);

                    return error;
                }

                // Update data and advance
                currentData = stepResult.Match(
                    Right: data => data,
                    Left: _ => currentData);

                stepsExecuted++;

                // Advance orchestrator state
                await _orchestrator.AdvanceAsync<TData>(
                    sagaId,
                    _ => currentData,
                    cancellationToken).ConfigureAwait(false);

                Log.StepCompleted(_logger, sagaId, i + 1, step.Name);
            }

            // All steps completed - mark saga as completed
            await _orchestrator.CompleteAsync(sagaId, cancellationToken).ConfigureAwait(false);

            Log.SagaCompleted(_logger, sagaId, stepsExecuted);

            return new SagaResult<TData>(sagaId, currentData, stepsExecuted);
        }
        catch (OperationCanceledException)
        {
            Log.SagaCancelled(_logger, sagaId);

            // Run compensation for completed steps
            await CompensateAsync(definition, currentData, stepsExecuted - 1, cancellationToken)
                .ConfigureAwait(false);

            await _orchestrator.FailAsync(sagaId, "Operation was cancelled", CancellationToken.None)
                .ConfigureAwait(false);

            return EncinaErrors.Create(SagaErrorCodes.HandlerCancelled, "Saga was cancelled");
        }
        catch (Exception ex)
        {
            Log.SagaException(_logger, sagaId, ex.Message, ex);

            // Run compensation for completed steps
            await CompensateAsync(definition, currentData, stepsExecuted - 1, CancellationToken.None)
                .ConfigureAwait(false);

            await _orchestrator.FailAsync(sagaId, ex.Message, CancellationToken.None)
                .ConfigureAwait(false);

            return EncinaErrors.Create(SagaErrorCodes.HandlerFailed, ex.Message);
        }
    }

    private async Task CompensateAsync<TData>(
        BuiltSagaDefinition<TData> definition,
        TData data,
        int fromStep,
        CancellationToken cancellationToken)
        where TData : class, new()
    {
        // Run compensation in reverse order
        for (var i = fromStep; i >= 0; i--)
        {
            var step = definition.Steps[i];

            if (step.Compensate == null)
            {
                Log.StepNoCompensation(_logger, i + 1, step.Name);
                continue;
            }

            try
            {
                Log.StepCompensating(_logger, i + 1, step.Name);
                await step.Compensate(data, _requestContext, cancellationToken).ConfigureAwait(false);
                Log.StepCompensated(_logger, i + 1, step.Name);
            }
            catch (Exception ex)
            {
                // Log but continue with other compensations
                Log.CompensationFailed(_logger, i + 1, step.Name, ex.Message, ex);
            }
        }
    }
}

/// <summary>
/// LoggerMessage definitions for high-performance logging.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(
        EventId = 300,
        Level = LogLevel.Information,
        Message = "Low-ceremony saga {SagaId} started (type: {SagaType}, steps: {StepCount})")]
    public static partial void SagaStarted(ILogger logger, Guid sagaId, string sagaType, int stepCount);

    [LoggerMessage(
        EventId = 301,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} executing step {StepNumber}: {StepName}")]
    public static partial void StepExecuting(ILogger logger, Guid sagaId, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 302,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} step {StepNumber} completed: {StepName}")]
    public static partial void StepCompleted(ILogger logger, Guid sagaId, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 303,
        Level = LogLevel.Warning,
        Message = "Saga {SagaId} step {StepNumber} failed: {StepName} - {ErrorMessage}")]
    public static partial void StepFailed(ILogger logger, Guid sagaId, int stepNumber, string stepName, string errorMessage);

    [LoggerMessage(
        EventId = 304,
        Level = LogLevel.Information,
        Message = "Saga {SagaId} completed successfully ({StepsExecuted} steps)")]
    public static partial void SagaCompleted(ILogger logger, Guid sagaId, int stepsExecuted);

    [LoggerMessage(
        EventId = 305,
        Level = LogLevel.Warning,
        Message = "Saga {SagaId} was cancelled")]
    public static partial void SagaCancelled(ILogger logger, Guid sagaId);

    [LoggerMessage(
        EventId = 306,
        Level = LogLevel.Error,
        Message = "Saga {SagaId} failed with exception: {ErrorMessage}")]
    public static partial void SagaException(ILogger logger, Guid sagaId, string errorMessage, Exception exception);

    [LoggerMessage(
        EventId = 307,
        Level = LogLevel.Debug,
        Message = "Step {StepNumber} ({StepName}) has no compensation defined")]
    public static partial void StepNoCompensation(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 308,
        Level = LogLevel.Debug,
        Message = "Compensating step {StepNumber}: {StepName}")]
    public static partial void StepCompensating(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 309,
        Level = LogLevel.Debug,
        Message = "Step {StepNumber} ({StepName}) compensation completed")]
    public static partial void StepCompensated(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 310,
        Level = LogLevel.Error,
        Message = "Compensation failed for step {StepNumber} ({StepName}): {ErrorMessage}")]
    public static partial void CompensationFailed(ILogger logger, int stepNumber, string stepName, string errorMessage, Exception exception);
}
