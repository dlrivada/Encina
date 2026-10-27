using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Encina.Diagnostics;
using Encina.Messaging.Serialization;
using Encina.Recoverability;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Recoverability;

/// <summary>
/// Background processor that executes delayed retries when they are due.
/// </summary>
/// <remarks>
/// <para>
/// This processor runs periodically to check for pending delayed retries and
/// re-dispatches them through the Encina pipeline.
/// </para>
/// <para>
/// If a delayed retry fails and there are more delayed retry attempts configured,
/// it schedules the next delayed retry. Otherwise, the message goes to the DLQ.
/// </para>
/// </remarks>
public sealed class DelayedRetryProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RecoverabilityOptions _options;
    private readonly ILogger<DelayedRetryProcessor> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly DeadLetter.DeadLetterSourceCapture? _deadLetterCapture;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Gets or sets the processing interval.
    /// </summary>
    public TimeSpan ProcessingInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the batch size for processing.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelayedRetryProcessor"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory.</param>
    /// <param name="options">The recoverability options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">Optional time provider for the failed message timestamps.</param>
    /// <param name="deadLetterCapture">
    /// Optional dead letter capture, registered with the dead letter queue: the permanent failure that ends a
    /// delayed retry chain is captured once (keyed by the chain's <see cref="FailedMessage.Id"/>) while
    /// <c>DeadLetterOptions.IntegrateWithRecoverability</c> is on. A row that could not be re-dispatched (unknown
    /// type, unreadable payload) is captured from its stored type name and content. The capture runs before the
    /// row is failed: when it fails in a retryable way, the row stays pending for a later cycle and
    /// <c>OnPermanentFailure</c> waits; a capture the queue rejects (<c>dlq.capture_rejected</c>) fails the row anyway.
    /// </param>
    public DelayedRetryProcessor(
        IServiceScopeFactory scopeFactory,
        RecoverabilityOptions options,
        ILogger<DelayedRetryProcessor> logger,
        TimeProvider? timeProvider = null,
        DeadLetter.DeadLetterSourceCapture? deadLetterCapture = null)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _deadLetterCapture = deadLetterCapture;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DelayedRetryProcessorLog.ProcessorStarted(_logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingRetriesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                DelayedRetryProcessorLog.ProcessingError(_logger, ex.ForLogging());
            }

            try
            {
                await Task.Delay(ProcessingInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        DelayedRetryProcessorLog.ProcessorStopped(_logger);
    }

    private async Task ProcessPendingRetriesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        // The startup check guarantees the store when delayed retries are on; a missing one is an error.
        var store = scope.ServiceProvider.GetRequiredService<IDelayedRetryStore>();

        var encina = scope.ServiceProvider.GetService<IEncina>();
        if (encina is null)
        {
            DelayedRetryProcessorLog.EncinaNotConfigured(_logger);
            return;
        }

        var messageSerializer = scope.ServiceProvider.GetService<IMessageSerializer>() ?? new JsonMessageSerializer();

        var messages = await store.GetPendingMessagesAsync(BatchSize, cancellationToken).ConfigureAwait(false);

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessMessageAsync(message, store, encina, messageSerializer, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessMessageAsync(
        IDelayedRetryMessage message,
        IDelayedRetryStore store,
        IEncina encina,
        IMessageSerializer messageSerializer,
        CancellationToken cancellationToken)
    {
        var progress = new RetryProgress();

        try
        {
            DelayedRetryProcessorLog.ProcessingRetry(
                _logger,
                message.CorrelationId ?? RecoverabilityConstants.Unknown,
                message.RequestType,
                message.DelayedRetryAttempt + 1);

            await RetryMessageAsync(message, store, encina, messageSerializer, progress, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown: the row stays pending for the next run; it is never consumed or failed.
            throw;
        }
        catch (Exception ex)
        {
            DelayedRetryProcessorLog.ProcessingException(_logger, ex.ForLogging(), message.Id, message.RequestType);
            var errorText = ex.GetType().FullName ?? ex.GetType().Name;

            // An unexpected exception before the chain was settled is a terminal exit of the message; after it
            // (a store failure after success or a scheduled next attempt) only the row is failed.
            if (progress.Settled)
            {
                await store.MarkAsFailedAsync(message.Id, errorText, cancellationToken).ConfigureAwait(false);
                return;
            }

            await TryEndChainAsync(message, store, progress.Request, errorText, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RetryMessageAsync(
        IDelayedRetryMessage message,
        IDelayedRetryStore store,
        IEncina encina,
        IMessageSerializer messageSerializer,
        RetryProgress progress,
        CancellationToken cancellationToken)
    {
        // Deserialize the request
        var requestType = Type.GetType(message.RequestType);
        if (requestType is null)
        {
            DelayedRetryProcessorLog.UnknownRequestType(_logger, message.Id, message.RequestType);
            await FailTerminallyAsync(message, store, null, $"Unknown request type: {message.RequestType}", cancellationToken).ConfigureAwait(false);
            return;
        }

        var request = messageSerializer.Deserialize(message.RequestContent, requestType);
        if (request is null)
        {
            DelayedRetryProcessorLog.DeserializationFailed(_logger, message.Id, message.RequestType);
            await FailTerminallyAsync(message, store, null, "Failed to deserialize request", cancellationToken).ConfigureAwait(false);
            return;
        }

        progress.Request = request;

        // Execute through Encina pipeline
        // Note: The RecoverabilityPipelineBehavior will handle any further failures
        var result = await DispatchRequestAsync(encina, request, cancellationToken).ConfigureAwait(false);

        // A failure seen while shutting down (a cancelled dispatch) says nothing about the message.
        if (!result.IsSuccess)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        await ApplyDispatchResultAsync(message, store, request, result, progress, cancellationToken).ConfigureAwait(false);
    }

    // A terminal exit of a row that could not be re-dispatched: the message takes the permanent-failure
    // path once, like every other end of the chain.
    private Task FailTerminallyAsync(
        IDelayedRetryMessage message,
        IDelayedRetryStore store,
        object? request,
        string errorText,
        CancellationToken cancellationToken)
        => EndChainAsync(message, store, request, new FailureInfo(errorText, null, false), cancellationToken);

    // An exception thrown on the way to the end of the chain is logged and the row is failed, so a row that
    // keeps throwing cannot loop; a failed dead letter capture (a Left) still leaves it pending.
    private async Task TryEndChainAsync(
        IDelayedRetryMessage message,
        IDelayedRetryStore store,
        object? request,
        string errorText,
        CancellationToken cancellationToken)
    {
        try
        {
            await EndChainAsync(message, store, request, new FailureInfo(errorText, null, false), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            DelayedRetryProcessorLog.ProcessingException(_logger, ex.ForLogging(), message.Id, message.RequestType);
            await store.MarkAsFailedAsync(message.Id, errorText, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ApplyDispatchResultAsync(
        IDelayedRetryMessage message,
        IDelayedRetryStore store,
        object request,
        DispatchResult result,
        RetryProgress progress,
        CancellationToken cancellationToken)
    {
        if (result.IsSuccess)
        {
            progress.Settled = true;
            await store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
            DelayedRetryProcessorLog.RetrySucceeded(
                _logger,
                message.CorrelationId ?? RecoverabilityConstants.Unknown,
                message.RequestType,
                message.DelayedRetryAttempt + 1);
            return;
        }

        progress.Settled = true;

        var outcome = await TryScheduleNextAsync(message, request, result, cancellationToken).ConfigureAwait(false);
        if (outcome == NextAttempt.Scheduled)
        {
            await store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
            return;
        }

        // All delayed retries exhausted, a permanent error, no next attempt could be scheduled, or a Retry-After
        // above the ceiling
        var failure = outcome == NextAttempt.ExceedsCeiling
            ? CeilingFailure(result)
            : new FailureInfo(result.ErrorMessage ?? "Unknown error", result.Attempt, true);
        await EndChainAsync(message, store, request, failure, cancellationToken).ConfigureAwait(false);
    }

    // The chain ends with the ceiling's own error code; the original code stays in the error's details.
    private static FailureInfo CeilingFailure(DispatchResult result) =>
        new(
            RecoverabilityErrorCodes.RetryAfterExceedsCeiling,
            result.Attempt,
            true,
            RetryAfterCeiling.CreateError(result.ErrorMessage ?? RecoverabilityConstants.Unknown));

    // Only this processor decides attempt N+1 or permanent failure: the re-dispatch ran without
    // starting a chain of its own. A permanent error ends the chain at once; so does a failure to
    // schedule the next attempt (never reported as success) and a Retry-After above the ceiling.
    private async Task<NextAttempt> TryScheduleNextAsync(
        IDelayedRetryMessage message,
        object request,
        DispatchResult result,
        CancellationToken cancellationToken)
    {
        var nextDelayedRetryAttempt = message.DelayedRetryAttempt + 1;
        if (EndsChainBeforeScheduling(message, result, nextDelayedRetryAttempt))
        {
            return NextAttempt.Ended;
        }

        // The delay the API dictated (Retry-After) replaces the configured step; the attempt still counts
        // against DelayedRetries.Length. Never shortened and without jitter. Above the ceiling nothing is scheduled.
        var retryAfter = RetryAfterOf(result);
        if (retryAfter > _options.MaxRetryAfter)
        {
            RecordCeilingExceeded(message, result, retryAfter.Value);
            return NextAttempt.ExceedsCeiling;
        }

        var delay = retryAfter ?? _options.DelayedRetries[nextDelayedRetryAttempt];

        return await ScheduleNextDelayedRetryAsync(message, request, nextDelayedRetryAttempt, delay, cancellationToken).ConfigureAwait(false)
            ? NextAttempt.Scheduled
            : NextAttempt.Ended;
    }

    // True when the chain ends without a next attempt: a permanent error, an ambiguous failure of a non-idempotent
    // send (both counted as skipped), or no delayed retry left.
    private bool EndsChainBeforeScheduling(IDelayedRetryMessage message, DispatchResult result, int nextDelayedRetryAttempt)
    {
        if (SkipReason(message, result) is { } reason)
        {
            CountSkipped(reason, message, result);
            return true;
        }

        return nextDelayedRetryAttempt >= _options.DelayedRetries.Length;
    }

    private void RecordCeilingExceeded(IDelayedRetryMessage message, DispatchResult result, TimeSpan retryAfter)
    {
        CountSkipped(RetryMetrics.ReasonRetryAfterExceedsCeiling, message, result);
        DelayedRetryProcessorLog.RetryAfterExceedsCeiling(
            _logger,
            message.CorrelationId ?? RecoverabilityConstants.Unknown,
            message.RequestType,
            result.ErrorMessage ?? RecoverabilityConstants.Unknown,
            retryAfter,
            _options.MaxRetryAfter);
    }

    private static TimeSpan? RetryAfterOf(DispatchResult result) =>
        result.Error is { } error ? OutboundRetryRules.GetRetryAfter(error) : null;

    // Why the chain ends instead of scheduling the next attempt, or null. An ambiguous failure of a non-idempotent
    // send ends the chain like a permanent one, unless the original request carried an idempotency key.
    private static string? SkipReason(IDelayedRetryMessage message, DispatchResult result)
    {
        if (result.IsPermanent)
        {
            return RetryMetrics.ReasonPermanent;
        }

        return result.Error is { } error
            && !OutboundRetryRules.AllowsAutomaticRetry(error, ParseContext(message.ContextContent).IdempotencyKey, allowNonIdempotentRetry: false)
                ? RetryMetrics.ReasonNonIdempotent
                : null;
    }

    private static void CountSkipped(string reason, IDelayedRetryMessage message, DispatchResult result) =>
        RetryMetrics.RecordSkipped(
            reason,
            SimpleRequestName(message.RequestType),
            result.ErrorMessage ?? RecoverabilityConstants.Unknown,
            tenantId: null);

    // The stored request type is assembly qualified ("Ns.Outer+Inner, Assembly, Version=..."); the metric tags every
    // layer with the simple type name, so the counters of one request line up across layers.
    internal static string SimpleRequestName(string requestType)
    {
        var typeName = requestType.AsSpan();
        var comma = typeName.IndexOf(',');
        if (comma >= 0)
        {
            typeName = typeName[..comma];
        }

        var start = Math.Max(typeName.LastIndexOf('.'), typeName.LastIndexOf('+')) + 1;
        return typeName[start..].Trim().ToString();
    }

    private static async Task<DispatchResult> DispatchRequestAsync(
        IEncina encina,
        object request,
        CancellationToken cancellationToken)
    {
        // The marker tells RecoverabilityPipelineBehavior this is a delayed-retry re-dispatch: it
        // runs the handler with its immediate retries but starts no retry chain (#2083).
        using var redispatch = DelayedRetryRedispatch.Begin(request);
        var marker = redispatch.Marker;

        try
        {
            var outcome = await RuntimeTypeRequestDispatcher.SendAsync(encina, request, cancellationToken).ConfigureAwait(false);
            var isPermanent = marker.Classification == ErrorClassification.Permanent;

            return outcome.Match(
                Right: _ => new DispatchResult(true, null, false, null),
                // Only the error code: EncinaError.Message can carry personal data (#1259 review).
                Left: error => new DispatchResult(false, error.GetCode().IfNone("encina.unknown"), isPermanent, marker.Attempt, error));
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new DispatchResult(false, ex.GetType().FullName ?? ex.GetType().Name, false, marker.Attempt);
        }
    }

    private async Task<bool> ScheduleNextDelayedRetryAsync(
        IDelayedRetryMessage originalMessage,
        object request,
        int nextDelayedRetryAttempt,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var scheduler = scope.ServiceProvider.GetService<IDelayedRetryScheduler>();
        if (scheduler is null)
        {
            DelayedRetryProcessorLog.SchedulerNotConfigured(_logger);
            return false;
        }

        // Reconstruct context from the serialized data
        var context = RestoreContext(originalMessage.ContextContent);
        context.IncrementDelayedRetry();

        DelayedRetryProcessorLog.SchedulingNextRetry(
            _logger,
            originalMessage.CorrelationId ?? RecoverabilityConstants.Unknown,
            originalMessage.RequestType,
            nextDelayedRetryAttempt + 1,
            _options.DelayedRetries.Length,
            delay);

        var correlationId = originalMessage.CorrelationId ?? RecoverabilityConstants.Unknown;

        try
        {
            var scheduled = await InvokeScheduleRetryAsync(
                scheduler, originalMessage.RequestType, request, context, delay, nextDelayedRetryAttempt, cancellationToken).ConfigureAwait(false);

            // Only the error code: EncinaError.Message can carry personal data.
            scheduled.IfLeft(error => DelayedRetryProcessorLog.SchedulingNextRetryFailed(
                _logger, correlationId, originalMessage.RequestType, error.GetCode().IfNone(RecoverabilityConstants.Unknown)));

            return scheduled.IsRight;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            // A throwing scheduler is a failed schedule: the chain fails permanently, once.
            DelayedRetryProcessorLog.SchedulingNextRetryThrew(_logger, ex.ForLogging(), correlationId, originalMessage.RequestType);
            return false;
        }
    }

    // Calls the generic IDelayedRetryScheduler.ScheduleRetryAsync<TRequest> for a request type known at run time.
    private static async Task<Either<EncinaError, Unit>> InvokeScheduleRetryAsync(
        IDelayedRetryScheduler scheduler,
        string requestTypeName,
        object request,
        RecoverabilityContext context,
        TimeSpan delay,
        int delayedRetryAttempt,
        CancellationToken cancellationToken)
    {
        var requestType = Type.GetType(requestTypeName);
        var scheduleMethod = typeof(IDelayedRetryScheduler)
            .GetMethod(nameof(IDelayedRetryScheduler.ScheduleRetryAsync));

        if (requestType is null || scheduleMethod is null)
        {
            return EncinaErrors.Create(RecoverabilityErrorCodes.ScheduleRetryFailed, "The request type or the scheduler method could not be resolved.");
        }

        Task<Either<EncinaError, Unit>> task;
        try
        {
            task = (Task<Either<EncinaError, Unit>>)scheduleMethod
                .MakeGenericMethod(requestType)
                .Invoke(scheduler, [request, context, delay, delayedRetryAttempt, cancellationToken])!;
        }
        catch (TargetInvocationException tie) when (tie.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            throw;
        }

        return await task.ConfigureAwait(false);
    }

    private static SerializableRecoverabilityContext ParseContext(string contextContent)
    {
        try
        {
            return JsonSerializer.Deserialize<SerializableRecoverabilityContext>(contextContent, JsonOptions)
                ?? new SerializableRecoverabilityContext();
        }
        catch (JsonException)
        {
            // An unreadable persisted context must not stop the permanent-failure path.
            return new SerializableRecoverabilityContext();
        }
    }

    private RecoverabilityContext RestoreContext(string contextContent)
    {
        var serializable = ParseContext(contextContent);

        var context = new RecoverabilityContext(_timeProvider)
        {
            CorrelationId = serializable.CorrelationId,
            IdempotencyKey = serializable.IdempotencyKey,
            RequestTypeName = serializable.RequestTypeName
        };

        // The logical message keeps its original id and start time across the whole chain.
        if (serializable.Id != Guid.Empty)
        {
            context.RestoreChain(serializable.Id, serializable.StartedAtUtc);
        }

        // Restore retry counts
        for (var i = 0; i < serializable.ImmediateRetryCount; i++)
        {
            context.IncrementImmediateRetry();
        }

        for (var i = 0; i < serializable.DelayedRetryCount; i++)
        {
            context.IncrementDelayedRetry();
        }

        return context;
    }

    // The real failure of the last re-dispatch when the behavior reported it; otherwise a code-only error.
    private FailedMessage BuildFailedMessage(IDelayedRetryMessage message, object? request, FailureInfo failure)
    {
        var context = RestoreContext(message.ContextContent);

        if (failure.Dispatched)
        {
            // The delayed retry of this row has run: count it (the persisted count is the rows before it).
            context.IncrementDelayedRetry();
        }

        if (failure.Attempt is { } attempt)
        {
            context.AbsorbRedispatch(attempt);
        }
        else
        {
            context.RecordFailedAttempt(
                EncinaError.New($"[{RecoverabilityErrorCodes.PermanentlyFailed}] {failure.Text}"),
                null,
                ErrorClassification.Permanent);
        }

        // A row that could not be re-dispatched has no request object: the raw content stands in for it.
        var failedMessage = context.CreateFailedMessage(request ?? message.RequestContent) with { RequestType = message.RequestType };
        return failure.TerminalError is { } terminal ? failedMessage with { Error = terminal } : failedMessage;
    }

    // The end of the chain. The dead letter is captured before the row is failed: when the capture fails (it
    // logs the error code) the row stays pending and a later cycle runs it and captures it again, idempotently
    // on the chain id, instead of ending failed without its dead letter. Then OnPermanentFailure runs once.
    private async Task EndChainAsync(
        IDelayedRetryMessage message,
        IDelayedRetryStore store,
        object? request,
        FailureInfo failure,
        CancellationToken cancellationToken)
    {
        LogPermanentFailure(message);
        var failedMessage = FailedMessageFor(message, request, failure);

        if (!await CapturedOrRejectedAsync(message, request, failedMessage, cancellationToken).ConfigureAwait(false))
            return;

        await store.MarkAsFailedAsync(message.Id, failure.Text, cancellationToken).ConfigureAwait(false);
        await InvokeOnPermanentFailureAsync(message, failedMessage, cancellationToken).ConfigureAwait(false);
    }

    private void LogPermanentFailure(IDelayedRetryMessage message)
        => DelayedRetryProcessorLog.PermanentFailure(
            _logger,
            message.CorrelationId ?? RecoverabilityConstants.Unknown,
            message.RequestType,
            message.DelayedRetryAttempt + 1);

    // Built only when someone reads it: the callback or the dead letter capture.
    private FailedMessage? FailedMessageFor(IDelayedRetryMessage message, object? request, FailureInfo failure)
        => _options.OnPermanentFailure is null && _deadLetterCapture is null
            ? null
            : BuildFailedMessage(message, request, failure);

    private async Task InvokeOnPermanentFailureAsync(
        IDelayedRetryMessage message,
        FailedMessage? failedMessage,
        CancellationToken cancellationToken)
    {
        if (_options.OnPermanentFailure is null || failedMessage is null)
            return;

        try
        {
            await _options.OnPermanentFailure(failedMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            DelayedRetryProcessorLog.OnPermanentFailureCallbackFailed(
                _logger,
                ex.ForLogging(),
                message.CorrelationId ?? RecoverabilityConstants.Unknown,
                message.RequestType);
        }
    }

    // False only when the capture failed in a way worth trying again (the row then stays pending). A capture the
    // dead letter queue rejects can never succeed: the row is failed anyway (the capture logged the rejection).
    private async Task<bool> CapturedOrRejectedAsync(
        IDelayedRetryMessage message,
        object? request,
        FailedMessage? failedMessage,
        CancellationToken cancellationToken)
    {
        var captured = await CaptureDeadLetterAsync(message, request, failedMessage, cancellationToken).ConfigureAwait(false);
        return captured.IsRight || !DeadLetter.DeadLetterSourceCapture.IsRetryable(captured.LeftToArray()[0]);
    }

    // A row with no request object (unknown type, unreadable payload) is captured from its stored type name
    // and content, which IMessageSerializer wrote and the replay reads back.
    private Task<Either<EncinaError, Unit>> CaptureDeadLetterAsync(
        IDelayedRetryMessage message,
        object? request,
        FailedMessage? failedMessage,
        CancellationToken cancellationToken)
    {
        if (_deadLetterCapture is null || failedMessage is null)
            return Task.FromResult<Either<EncinaError, Unit>>(Unit.Default);

        if (request is not null)
            return _deadLetterCapture.CaptureFailedMessageAsync(failedMessage, cancellationToken);

        return _deadLetterCapture.CaptureSerializedAsync(
            message.RequestType,
            message.RequestContent,
            new DeadLetter.DeadLetterContext(
                failedMessage.Error,
                failedMessage.Exception,
                DeadLetter.DeadLetterSourcePatterns.Recoverability,
                failedMessage.TotalAttempts,
                failedMessage.FirstAttemptAtUtc,
                failedMessage.CorrelationId,
                failedMessage.Id.ToString("D")),
            cancellationToken);
    }

    // ErrorMessage is the error code or the exception type, never EncinaError.Message; Error is only read for its metadata.
    private sealed record DispatchResult(bool IsSuccess, string? ErrorMessage, bool IsPermanent, RecoverabilityContext? Attempt, EncinaError? Error = null);

    // What the permanent-failure path reports: the code or type text, the re-dispatch's own context
    // when the behavior saw it, whether a delayed retry of this row has run, and the error that replaces the
    // last one in the dead letter (the ceiling error), or null.
    private sealed record FailureInfo(string Text, RecoverabilityContext? Attempt, bool Dispatched, EncinaError? TerminalError = null);

    // How the chain continues after a failed delayed retry.
    private enum NextAttempt
    {
        Scheduled,
        Ended,
        ExceedsCeiling
    }

    // Where a row got to, so an unexpected exception takes the permanent-failure path only when the
    // chain was not already settled (a store failure after success or a scheduled next attempt does not).
    private sealed class RetryProgress
    {
        public object? Request { get; set; }

        public bool Settled { get; set; }
    }
}

/// <summary>
/// LoggerMessage definitions for delayed retry processor.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class DelayedRetryProcessorLog
{
    [LoggerMessage(
        EventId = 2845,
        Level = LogLevel.Information,
        Message = "Delayed retry processor started")]
    public static partial void ProcessorStarted(ILogger logger);

    [LoggerMessage(
        EventId = 2846,
        Level = LogLevel.Information,
        Message = "Delayed retry processor stopped")]
    public static partial void ProcessorStopped(ILogger logger);

    [LoggerMessage(
        EventId = 2847,
        Level = LogLevel.Error,
        Message = "Error during delayed retry processing")]
    public static partial void ProcessingError(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 2849,
        Level = LogLevel.Warning,
        Message = "IEncina not configured - delayed retries disabled")]
    public static partial void EncinaNotConfigured(ILogger logger);

    [LoggerMessage(
        EventId = 2850,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] Processing delayed retry #{Attempt} for {RequestType}")]
    public static partial void ProcessingRetry(
        ILogger logger, string correlationId, string requestType, int attempt);

    [LoggerMessage(
        EventId = 2851,
        Level = LogLevel.Warning,
        Message = "Unknown request type for delayed retry {MessageId}: {RequestType}")]
    public static partial void UnknownRequestType(ILogger logger, Guid messageId, string requestType);

    [LoggerMessage(
        EventId = 2852,
        Level = LogLevel.Warning,
        Message = "Failed to deserialize delayed retry {MessageId} of type {RequestType}")]
    public static partial void DeserializationFailed(ILogger logger, Guid messageId, string requestType);

    [LoggerMessage(
        EventId = 2853,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] Delayed retry #{Attempt} succeeded for {RequestType}")]
    public static partial void RetrySucceeded(
        ILogger logger, string correlationId, string requestType, int attempt);

    [LoggerMessage(
        EventId = 2854,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] Scheduling next delayed retry #{Attempt}/{MaxAttempts} for {RequestType} in {Delay}")]
    public static partial void SchedulingNextRetry(
        ILogger logger, string correlationId, string requestType, int attempt, int maxAttempts, TimeSpan delay);

    [LoggerMessage(
        EventId = 2855,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] {RequestType} permanently failed after {Attempts} delayed retries")]
    public static partial void PermanentFailure(
        ILogger logger, string correlationId, string requestType, int attempts);

    [LoggerMessage(
        EventId = 2856,
        Level = LogLevel.Warning,
        Message = "IDelayedRetryScheduler not configured - cannot schedule next retry")]
    public static partial void SchedulerNotConfigured(ILogger logger);

    [LoggerMessage(
        EventId = 2857,
        Level = LogLevel.Error,
        Message = "Exception processing delayed retry {MessageId} of type {RequestType}")]
    public static partial void ProcessingException(ILogger logger, Exception ex, Guid messageId, string requestType);

    [LoggerMessage(
        EventId = 2858,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] {RequestType} OnPermanentFailure callback failed")]
    public static partial void OnPermanentFailureCallbackFailed(
        ILogger logger, Exception ex, string correlationId, string requestType);

    [LoggerMessage(
        EventId = 5450,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] {RequestType} failed to schedule the next delayed retry: {ErrorCode}")]
    public static partial void SchedulingNextRetryFailed(
        ILogger logger, string correlationId, string requestType, string errorCode);

    [LoggerMessage(
        EventId = 5451,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] {RequestType} scheduling the next delayed retry threw")]
    public static partial void SchedulingNextRetryThrew(
        ILogger logger, Exception ex, string correlationId, string requestType);

    /// <remarks>
    /// Event IDs: 5450-5499 (see EventIdRanges.MessagingRecoverability). Only the original error code is
    /// logged, never <c>EncinaError.Message</c>.
    /// </remarks>
    [LoggerMessage(
        EventId = 5455,
        Level = LogLevel.Warning,
        Message = "[{CorrelationId}] {RequestType} next delayed retry not scheduled: Retry-After {RetryAfter} exceeds the ceiling {MaxRetryAfter}, dead-lettered ({ErrorCode})")]
    public static partial void RetryAfterExceedsCeiling(
        ILogger logger, string correlationId, string requestType, string errorCode, TimeSpan retryAfter, TimeSpan maxRetryAfter);
}
