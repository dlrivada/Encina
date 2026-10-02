using System.Diagnostics.CodeAnalysis;
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
    private const string UnexpectedStepExceptionMessage = "Saga step threw an unexpected exception.";

    private readonly SagaOrchestrator _orchestrator;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly ILogger<SagaRunner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaRunner"/> class.
    /// </summary>
    /// <param name="orchestrator">The saga orchestrator for state management.</param>
    /// <param name="requestContextAccessor">
    /// Accessor for the ambient request context. Every run resolves the current context (or a
    /// fresh one when none is in flight) instead of capturing a snapshot at construction time,
    /// since this runner is registered as scoped but may outlive the caller's own dispatch.
    /// </param>
    /// <param name="logger">The logger.</param>
    public SagaRunner(
        SagaOrchestrator orchestrator,
        IRequestContextAccessor requestContextAccessor,
        ILogger<SagaRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(orchestrator);
        ArgumentNullException.ThrowIfNull(requestContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _orchestrator = orchestrator;
        _requestContextAccessor = requestContextAccessor;
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

        // The ambient context set by IEncina.Send/Publish/Stream wins; a run started outside a
        // dispatch (a background job invoking the saga directly) gets a fresh context instead of
        // a null one, since steps require a non-null IRequestContext. That fallback context is
        // anonymous: it carries no TenantId and no UserId, since RequestContext.Create() has no
        // ambient dispatch to copy them from. Callers that need a tenant-scoped saga run outside a
        // dispatch must set IRequestContextAccessor.RequestContext themselves before calling RunAsync.
        var requestContext = _requestContextAccessor.RequestContext ?? RequestContext.Create();

        // Start the saga
        var startResult = await _orchestrator.StartAsync(
            definition.SagaType,
            initialData,
            definition.Timeout,
            cancellationToken).ConfigureAwait(false);

        if (startResult.IsLeft)
        {
            return (EncinaError)startResult;
        }

        var sagaId = startResult.Match(Right: id => id, Left: _ => Guid.Empty);

        Log.SagaStarted(_logger, sagaId, definition.SagaType, definition.StepCount);

        var progress = new RunProgress<TData>(initialData);

        try
        {
            return await ExecuteStepsAsync(definition, progress, sagaId, requestContext, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return await HandleCancelledAsync(definition, progress, sagaId, requestContext, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return await HandleExceptionAsync(definition, progress, sagaId, requestContext, ex)
                .ConfigureAwait(false);
        }
    }

    private async ValueTask<Either<EncinaError, SagaResult<TData>>> ExecuteStepsAsync<TData>(
        BuiltSagaDefinition<TData> definition,
        RunProgress<TData> progress,
        Guid sagaId,
        IRequestContext requestContext,
        CancellationToken cancellationToken)
        where TData : class, new()
    {
        // Execute each step
        for (var i = 0; i < definition.Steps.Count; i++)
        {
            var step = definition.Steps[i];
            Log.StepExecuting(_logger, sagaId, i + 1, step.Name);

            var stepResult = await step.Execute(progress.Data, requestContext, cancellationToken)
                .ConfigureAwait(false);

            if (stepResult.IsLeft)
            {
                return await HandleStepFailedAsync(
                    definition, progress.Data, sagaId, i, stepResult, requestContext, cancellationToken)
                    .ConfigureAwait(false);
            }

            // Update data and advance
            progress.Data = stepResult.Match(
                Right: data => data,
                Left: _ => progress.Data);

            progress.StepsExecuted++;

            // Advance orchestrator state
            await _orchestrator.AdvanceAsync<TData>(
                sagaId,
                _ => progress.Data,
                cancellationToken).ConfigureAwait(false);

            Log.StepCompleted(_logger, sagaId, i + 1, step.Name);
        }

        // All steps completed - mark saga as completed
        await _orchestrator.CompleteAsync(sagaId, cancellationToken).ConfigureAwait(false);

        Log.SagaCompleted(_logger, sagaId, progress.StepsExecuted);

        return new SagaResult<TData>(sagaId, progress.Data, progress.StepsExecuted);
    }

    private async ValueTask<Either<EncinaError, SagaResult<TData>>> HandleStepFailedAsync<TData>(
        BuiltSagaDefinition<TData> definition,
        TData data,
        Guid sagaId,
        int stepIndex,
        Either<EncinaError, TData> stepResult,
        IRequestContext requestContext,
        CancellationToken cancellationToken)
        where TData : class, new()
    {
        // Step failed - start compensation
        var error = stepResult.Match(
            Right: _ => EncinaErrors.Create(SagaErrorCodes.StepFailed, "Unexpected"),
            Left: e => e);

        // Only the error code: EncinaError.Message can carry personal data (#1259 review),
        // and StartCompensationAsync persists this string in the saga state store.
        var errorCode = error.GetCode().IfNone("encina.unknown");
        Log.StepFailed(_logger, sagaId, stepIndex + 1, definition.Steps[stepIndex].Name, errorCode);

        // Run compensation for completed steps
        await CompensateAsync(definition, data, stepIndex - 1, requestContext, cancellationToken)
            .ConfigureAwait(false);

        // Mark saga as compensated
        await _orchestrator.StartCompensationAsync(sagaId, errorCode, cancellationToken)
            .ConfigureAwait(false);

        return error;
    }

    private async ValueTask<Either<EncinaError, SagaResult<TData>>> HandleCancelledAsync<TData>(
        BuiltSagaDefinition<TData> definition,
        RunProgress<TData> progress,
        Guid sagaId,
        IRequestContext requestContext,
        CancellationToken cancellationToken)
        where TData : class, new()
    {
        Log.SagaCancelled(_logger, sagaId);

        // Run compensation for completed steps
        await CompensateAsync(definition, progress.Data, progress.StepsExecuted - 1, requestContext, cancellationToken)
            .ConfigureAwait(false);

        // Persist only the error code (#1469): FailAsync stores this string in the saga state.
        await _orchestrator.FailAsync(sagaId, SagaErrorCodes.HandlerCancelled, CancellationToken.None)
            .ConfigureAwait(false);

        return EncinaErrors.Create(SagaErrorCodes.HandlerCancelled, "Saga was cancelled");
    }

    private async ValueTask<Either<EncinaError, SagaResult<TData>>> HandleExceptionAsync<TData>(
        BuiltSagaDefinition<TData> definition,
        RunProgress<TData> progress,
        Guid sagaId,
        IRequestContext requestContext,
        Exception ex)
        where TData : class, new()
    {
        // The exception object goes to the logger as the structured exception; its Message is
        // never part of the log text, the persisted state or the returned error (#1469).
        Log.SagaException(_logger, sagaId, ex.GetType().Name, ex);

        // Run compensation for completed steps
        await CompensateAsync(definition, progress.Data, progress.StepsExecuted - 1, requestContext, CancellationToken.None)
            .ConfigureAwait(false);

        await _orchestrator.FailAsync(sagaId, SagaErrorCodes.HandlerFailed, CancellationToken.None)
            .ConfigureAwait(false);

        return EncinaErrors.Create(SagaErrorCodes.HandlerFailed, UnexpectedStepExceptionMessage, ex);
    }

    /// <summary>Mutable progress of one run, read by the failure handlers to compensate the executed steps.</summary>
    private sealed class RunProgress<TData>(TData data)
    {
        public TData Data { get; set; } = data;

        public int StepsExecuted { get; set; }
    }

    private async Task CompensateAsync<TData>(
        BuiltSagaDefinition<TData> definition,
        TData data,
        int fromStep,
        IRequestContext requestContext,
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
                await step.Compensate(data, requestContext, cancellationToken).ConfigureAwait(false);
                Log.StepCompensated(_logger, i + 1, step.Name);
            }
            catch (Exception ex)
            {
                // Log but continue with other compensations
                Log.CompensationFailed(_logger, i + 1, step.Name, ex.GetType().Name, ex);
            }
        }
    }
}

/// <summary>
/// LoggerMessage definitions for high-performance logging.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class Log
{
    [LoggerMessage(
        EventId = 2888,
        Level = LogLevel.Information,
        Message = "Low-ceremony saga {SagaId} started (type: {SagaType}, steps: {StepCount})")]
    public static partial void SagaStarted(ILogger logger, Guid sagaId, string sagaType, int stepCount);

    [LoggerMessage(
        EventId = 2889,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} executing step {StepNumber}: {StepName}")]
    public static partial void StepExecuting(ILogger logger, Guid sagaId, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 2890,
        Level = LogLevel.Debug,
        Message = "Saga {SagaId} step {StepNumber} completed: {StepName}")]
    public static partial void StepCompleted(ILogger logger, Guid sagaId, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 2891,
        Level = LogLevel.Warning,
        Message = "Saga {SagaId} step {StepNumber} failed: {StepName} - {ErrorMessage}")]
    public static partial void StepFailed(ILogger logger, Guid sagaId, int stepNumber, string stepName, string errorMessage);

    [LoggerMessage(
        EventId = 2892,
        Level = LogLevel.Information,
        Message = "Saga {SagaId} completed successfully ({StepsExecuted} steps)")]
    public static partial void SagaCompleted(ILogger logger, Guid sagaId, int stepsExecuted);

    [LoggerMessage(
        EventId = 2893,
        Level = LogLevel.Warning,
        Message = "Saga {SagaId} was cancelled")]
    public static partial void SagaCancelled(ILogger logger, Guid sagaId);

    [LoggerMessage(
        EventId = 2894,
        Level = LogLevel.Error,
        Message = "Saga {SagaId} failed with exception of type {ExceptionType}")]
    public static partial void SagaException(ILogger logger, Guid sagaId, string exceptionType, Exception exception);

    [LoggerMessage(
        EventId = 2895,
        Level = LogLevel.Debug,
        Message = "Step {StepNumber} ({StepName}) has no compensation defined")]
    public static partial void StepNoCompensation(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 2896,
        Level = LogLevel.Debug,
        Message = "Compensating step {StepNumber}: {StepName}")]
    public static partial void StepCompensating(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 2897,
        Level = LogLevel.Debug,
        Message = "Step {StepNumber} ({StepName}) compensation completed")]
    public static partial void StepCompensated(ILogger logger, int stepNumber, string stepName);

    [LoggerMessage(
        EventId = 2898,
        Level = LogLevel.Error,
        Message = "Compensation failed for step {StepNumber} ({StepName}) with exception of type {ExceptionType}")]
    public static partial void CompensationFailed(ILogger logger, int stepNumber, string stepName, string exceptionType, Exception exception);
}
