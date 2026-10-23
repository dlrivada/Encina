using System.Diagnostics.CodeAnalysis;
using Encina.Diagnostics;
using Encina.Recoverability;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Recoverability;

/// <summary>
/// Pipeline behavior that implements the Recoverability Pipeline with immediate and delayed retries.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// <para>
/// The Recoverability Pipeline implements a two-phase retry strategy:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <b>Immediate retries</b>: Fast, in-memory retries with optional exponential backoff.
/// Ideal for transient failures like network hiccups or temporary unavailability.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Delayed retries</b>: Persistent, scheduled retries using the Scheduling pattern.
/// Provides longer recovery windows for extended outages.
/// </description>
/// </item>
/// </list>
/// <para>
/// Errors are classified using <see cref="IErrorClassifier"/> to determine if they should be retried.
/// Permanent errors skip retries and go directly to the Dead Letter Queue (DLQ).
/// </para>
/// </remarks>
public sealed class RecoverabilityPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly RecoverabilityOptions _options;
    private readonly IErrorClassifier _errorClassifier;
    private readonly ILogger<RecoverabilityPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IDelayedRetryScheduler? _delayedRetryScheduler;
    private readonly TimeProvider _timeProvider;
    private readonly DeadLetter.DeadLetterSourceCapture? _deadLetterCapture;
    private readonly Func<double> _jitterSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="options">The recoverability options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="delayedRetryScheduler">Optional scheduler for delayed retries.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    /// <param name="deadLetterCapture">
    /// Optional dead letter capture, registered with the dead letter queue: a permanent failure is captured
    /// once (keyed by <see cref="FailedMessage.Id"/>) while <c>DeadLetterOptions.IntegrateWithRecoverability</c>
    /// is on. A failed capture is logged by the capture; the request already returns its failure.
    /// </param>
    /// <param name="errorClassifier">
    /// Optional classifier registered in DI. <see cref="RecoverabilityOptions.ErrorClassifier"/> takes precedence;
    /// without either, <see cref="DefaultErrorClassifier"/> is used.
    /// </param>
    public RecoverabilityPipelineBehavior(
        RecoverabilityOptions options,
        ILogger<RecoverabilityPipelineBehavior<TRequest, TResponse>> logger,
        IDelayedRetryScheduler? delayedRetryScheduler = null,
        TimeProvider? timeProvider = null,
        DeadLetter.DeadLetterSourceCapture? deadLetterCapture = null,
        IErrorClassifier? errorClassifier = null)
        : this(options, logger, delayedRetryScheduler, timeProvider, deadLetterCapture, errorClassifier, jitterSource: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/> class
    /// with a configurable jitter source.
    /// </summary>
    /// <param name="options">The recoverability options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="delayedRetryScheduler">Optional scheduler for delayed retries.</param>
    /// <param name="timeProvider">Optional time provider for testability.</param>
    /// <param name="deadLetterCapture">Optional dead letter capture.</param>
    /// <param name="errorClassifier">Optional classifier registered in DI.</param>
    /// <param name="jitterSource">A source of values in [0, 1), allowing tests to pin the jitter.</param>
    internal RecoverabilityPipelineBehavior(
        RecoverabilityOptions options,
        ILogger<RecoverabilityPipelineBehavior<TRequest, TResponse>> logger,
        IDelayedRetryScheduler? delayedRetryScheduler,
        TimeProvider? timeProvider,
        DeadLetter.DeadLetterSourceCapture? deadLetterCapture,
        IErrorClassifier? errorClassifier,
        Func<double>? jitterSource)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _errorClassifier = options.ErrorClassifier ?? errorClassifier ?? new DefaultErrorClassifier();
        _logger = logger;
        _delayedRetryScheduler = delayedRetryScheduler;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _deadLetterCapture = deadLetterCapture;
        _jitterSource = jitterSource ?? Random.Shared.NextDouble;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // The behavior's clock stamps the chain, so a dead letter's first-failure and dead-letter instants agree.
        var recoverabilityContext = new RecoverabilityContext(_timeProvider)
        {
            CorrelationId = context.CorrelationId,
            IdempotencyKey = context.IdempotencyKey,
            RequestTypeName = typeof(TRequest).Name
        };

        // Matched to this very request (reference or value), so an outer behavior that re-enters the
        // pipeline keeps one chain while a nested send of another request never inherits it.
        var redispatch = DelayedRetryRedispatch.For(request);

        // Try initial execution + immediate retries
        var result = await ExecuteWithImmediateRetriesAsync(
            recoverabilityContext,
            nextStep,
            cancellationToken).ConfigureAwait(false);

        // If successful, return
        if (result.IsRight)
        {
            return result;
        }

        return await HandleFailureAsync(request, context, result, recoverabilityContext, redispatch, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Either<EncinaError, TResponse>> HandleFailureAsync(
        TRequest request,
        IRequestContext context,
        Either<EncinaError, TResponse> result,
        RecoverabilityContext recoverabilityContext,
        DelayedRetryRedispatch? redispatch,
        CancellationToken cancellationToken)
    {
        // A delayed-retry re-dispatch starts no new chain: the processor decides attempt N+1 or
        // permanent failure from the outcome and the classification reported here (#2083).
        if (redispatch is not null)
        {
            redispatch.Report(recoverabilityContext);
            return result;
        }

        // A permanent error skips delayed retries; an ambiguous failure of a non-idempotent send is returned to the
        // caller like a permanent one, unless the request carries an idempotency key (the provider can deduplicate).
        if (await RejectPermanentAsync(request, context, recoverabilityContext, cancellationToken).ConfigureAwait(false)
            || await RejectNonIdempotentAsync(request, context, recoverabilityContext, cancellationToken).ConfigureAwait(false))
        {
            return result;
        }

        // A Retry-After above the ceiling schedules nothing: the message is dead-lettered with its own error code.
        if (await RejectRetryAfterAboveCeilingAsync(request, context, recoverabilityContext, cancellationToken).ConfigureAwait(false) is { } ceilingError)
        {
            return Either<EncinaError, TResponse>.Left(ceilingError);
        }

        // Schedule delayed retry if enabled and scheduler is available
        if (await TryScheduleFirstDelayedRetryAsync(request, recoverabilityContext, cancellationToken).ConfigureAwait(false))
        {
            // Return error but note that delayed retry is scheduled
            return Either<EncinaError, TResponse>.Left(
                EncinaError.New($"[{RecoverabilityErrorCodes.DelayedRetryScheduled}] Immediate retries exhausted. Delayed retry scheduled."));
        }

        // Not scheduled (disabled, or the schedule failed so the message would be lost): fail permanently.

        // No delayed retries - permanent failure
        await HandlePermanentFailureAsync(request, recoverabilityContext, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async ValueTask<bool> RejectPermanentAsync(
        TRequest request,
        IRequestContext context,
        RecoverabilityContext recoverabilityContext,
        CancellationToken cancellationToken)
    {
        if (recoverabilityContext.LastClassification != ErrorClassification.Permanent)
        {
            return false;
        }

        CountSkipped(RetryMetrics.ReasonPermanent, recoverabilityContext, context.TenantId);
        LogPermanentErrorDetected(context.CorrelationId, recoverabilityContext);
        await HandlePermanentFailureAsync(request, recoverabilityContext, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async ValueTask<bool> TryScheduleFirstDelayedRetryAsync(
        TRequest request,
        RecoverabilityContext recoverabilityContext,
        CancellationToken cancellationToken) =>
        _delayedRetryScheduler is not null
        && CanScheduleDelayedRetry
        && await ScheduleFirstDelayedRetryAsync(_delayedRetryScheduler, request, recoverabilityContext, cancellationToken).ConfigureAwait(false);

    // True when the failure was an ambiguous one of a non-idempotent send without an idempotency key: the
    // message took the permanent-failure path and is not retried.
    private async ValueTask<bool> RejectNonIdempotentAsync(
        TRequest request,
        IRequestContext context,
        RecoverabilityContext recoverabilityContext,
        CancellationToken cancellationToken)
    {
        if (recoverabilityContext.LastError is not { } lastError
            || OutboundRetryRules.AllowsAutomaticRetry(lastError, recoverabilityContext.IdempotencyKey, allowNonIdempotentRetry: false))
        {
            return false;
        }

        CountSkipped(RetryMetrics.ReasonNonIdempotent, recoverabilityContext, context.TenantId);
        RecoverabilityLog.NonIdempotentRetrySkipped(
            _logger,
            context.CorrelationId,
            typeof(TRequest).Name,
            lastError.GetCode().IfNone(RecoverabilityConstants.Unknown));
        await HandlePermanentFailureAsync(request, recoverabilityContext, cancellationToken).ConfigureAwait(false);
        return true;
    }

    // The error the dead letter and the caller carry when the first delayed retry would wait longer than
    // MaxRetryAfter, or null when the retry is not held back by the ceiling (no scheduler, delayed retries off,
    // no Retry-After, or one at or below the ceiling).
    private async ValueTask<EncinaError?> RejectRetryAfterAboveCeilingAsync(
        TRequest request,
        IRequestContext context,
        RecoverabilityContext recoverabilityContext,
        CancellationToken cancellationToken)
    {
        if (_delayedRetryScheduler is null
            || !CanScheduleDelayedRetry
            || recoverabilityContext.LastError is not { } lastError
            || OutboundRetryRules.GetRetryAfter(lastError) is not { } retryAfter
            || retryAfter <= _options.MaxRetryAfter)
        {
            return null;
        }

        var originalCode = lastError.GetCode().IfNone(RecoverabilityConstants.Unknown);
        CountSkipped(RetryMetrics.ReasonRetryAfterExceedsCeiling, recoverabilityContext, context.TenantId);
        RecoverabilityLog.RetryAfterExceedsCeiling(
            _logger,
            context.CorrelationId,
            typeof(TRequest).Name,
            originalCode,
            retryAfter,
            _options.MaxRetryAfter);

        var ceilingError = RetryAfterCeiling.CreateError(originalCode);
        await HandlePermanentFailureAsync(request, recoverabilityContext, cancellationToken, ceilingError).ConfigureAwait(false);
        return ceilingError;
    }

    private static void CountSkipped(string reason, RecoverabilityContext recoverabilityContext, string? tenantId) =>
        RetryMetrics.RecordSkipped(
            reason,
            typeof(TRequest).Name,
            recoverabilityContext.LastError?.GetCode().IfNone(RecoverabilityConstants.Unknown) ?? RecoverabilityConstants.Unknown,
            tenantId);

    // The delay the API dictated (Retry-After) replaces the configured first delayed retry step, never shortened
    // and without jitter.
    private TimeSpan FirstDelayedRetryDelay(RecoverabilityContext recoverabilityContext) =>
        (recoverabilityContext.LastError is { } lastError ? OutboundRetryRules.GetRetryAfter(lastError) : null)
        ?? GetDelayWithJitter(_options.DelayedRetries[0]);

    private void LogPermanentErrorDetected(string correlationId, RecoverabilityContext recoverabilityContext) =>
        RecoverabilityLog.PermanentErrorDetected(
            _logger,
            correlationId,
            typeof(TRequest).Name,
            recoverabilityContext.LastError?.GetCode().IfNone(RecoverabilityConstants.Unknown) ?? RecoverabilityConstants.Unknown);

    private bool CanScheduleDelayedRetry => _options.EnableDelayedRetries && _options.DelayedRetries.Length > 0;

    private async ValueTask<bool> ScheduleFirstDelayedRetryAsync(
        IDelayedRetryScheduler scheduler,
        TRequest request,
        RecoverabilityContext recoverabilityContext,
        CancellationToken cancellationToken)
    {
        var correlationId = recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown;
        var firstDelayedRetryDelay = FirstDelayedRetryDelay(recoverabilityContext);

        RecoverabilityLog.SchedulingDelayedRetry(
            _logger,
            correlationId,
            typeof(TRequest).Name,
            1,
            _options.DelayedRetries.Length,
            firstDelayedRetryDelay);

        Either<EncinaError, Unit> scheduleResult;
        try
        {
            scheduleResult = await scheduler.ScheduleRetryAsync(
                request,
                recoverabilityContext,
                firstDelayedRetryDelay,
                0,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!(ex is OperationCanceledException && cancellationToken.IsCancellationRequested))
        {
            // A throwing scheduler is a failed schedule, never a silent loss. Only the exception
            // type is kept: its message can carry personal data.
            RecoverabilityLog.SchedulingDelayedRetryThrew(_logger, ex.ForLogging(), correlationId, typeof(TRequest).Name);
            return false;
        }

        scheduleResult.IfLeft(error =>
            RecoverabilityLog.SchedulingDelayedRetryFailed(
                _logger,
                correlationId,
                typeof(TRequest).Name,
                error.GetCode().IfNone(RecoverabilityConstants.Unknown)));

        return scheduleResult.IsRight;
    }

    private async ValueTask<Either<EncinaError, TResponse>> ExecuteWithImmediateRetriesAsync(
        RecoverabilityContext recoverabilityContext,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        Either<EncinaError, TResponse> lastResult = default;

        while (attempt <= _options.ImmediateRetries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Either<EncinaError, TResponse>.Left(
                    EncinaError.New($"[{RecoverabilityErrorCodes.Cancelled}] Operation was cancelled"));
            }

            var attemptResult = await ExecuteSingleAttemptAsync(
                recoverabilityContext, nextStep, attempt).ConfigureAwait(false);

            lastResult = attemptResult.Result;

            if (attemptResult.ShouldReturn)
            {
                return lastResult;
            }

            attempt++;
            recoverabilityContext.IncrementImmediateRetry();

            if (attempt <= _options.ImmediateRetries)
            {
                var delay = CalculateImmediateRetryDelay(attempt);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        RecoverabilityLog.ImmediateRetriesExhausted(
            _logger,
            recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown,
            typeof(TRequest).Name,
            _options.ImmediateRetries);

        return lastResult;
    }

    private async ValueTask<AttemptResult> ExecuteSingleAttemptAsync(
        RecoverabilityContext recoverabilityContext,
        RequestHandlerCallback<TResponse> nextStep,
        int attempt)
    {
        var startTime = _timeProvider.GetUtcNow().UtcDateTime;

        try
        {
            var result = await nextStep().ConfigureAwait(false);

            if (result.IsRight)
            {
                LogSuccessAfterRetry(recoverabilityContext, attempt);
                return AttemptResult.Success(result);
            }

            return HandleErrorResult(result, recoverabilityContext, startTime, attempt);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HandleException(ex, recoverabilityContext, startTime, attempt);
        }
    }

    private void LogSuccessAfterRetry(RecoverabilityContext recoverabilityContext, int attempt)
    {
        if (attempt > 0)
        {
            RecoverabilityLog.SucceededAfterRetry(
                _logger,
                recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown,
                typeof(TRequest).Name,
                attempt);
        }
    }

    private AttemptResult HandleErrorResult(
        Either<EncinaError, TResponse> result,
        RecoverabilityContext recoverabilityContext,
        DateTime startTime,
        int attempt)
    {
        var error = (EncinaError)result;

        var duration = _timeProvider.GetUtcNow().UtcDateTime - startTime;
        var errorException = error.Exception.MatchUnsafe(ex => ex, () => null);
        var classification = _errorClassifier.Classify(error, errorException);

        recoverabilityContext.RecordFailedAttempt(error, errorException, classification, duration);

        var permanent = classification == ErrorClassification.Permanent;
        LogErrorOnAttempt(error, recoverabilityContext, attempt, permanent);

        // Retrying now would be wrong when the failure is permanent, ambiguous on a non-idempotent send, or the API
        // asked to be retried later (Retry-After), which the delayed retry honours. HandleFailureAsync decides what follows.
        return permanent || StopsImmediateRetries(error, recoverabilityContext)
            ? AttemptResult.PermanentFailure(result)
            : AttemptResult.TransientFailure(result);
    }

    private void LogErrorOnAttempt(EncinaError error, RecoverabilityContext recoverabilityContext, int attempt, bool permanent)
    {
        var correlationId = recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown;
        var code = error.GetCode().IfNone(RecoverabilityConstants.Unknown);

        if (permanent)
        {
            RecoverabilityLog.PermanentErrorOnAttempt(_logger, correlationId, typeof(TRequest).Name, attempt + 1, code);
            return;
        }

        RecoverabilityLog.TransientErrorOnAttempt(
            _logger, correlationId, typeof(TRequest).Name, attempt + 1, _options.ImmediateRetries + 1, code);
    }

    private static bool StopsImmediateRetries(EncinaError error, RecoverabilityContext recoverabilityContext) =>
        !OutboundRetryRules.AllowsAutomaticRetry(error, recoverabilityContext.IdempotencyKey, allowNonIdempotentRetry: false)
        || OutboundRetryRules.GetRetryAfter(error) is not null;

    private AttemptResult HandleException(
        Exception ex,
        RecoverabilityContext recoverabilityContext,
        DateTime startTime,
        int attempt)
    {
        var duration = _timeProvider.GetUtcNow().UtcDateTime - startTime;
        // Only the exception type: its message can carry personal data (AGENTS.md section 3).
        var error = EncinaErrors.Create(RecoverabilityErrorCodes.ExceptionThrown, ex.GetType().Name, ex);
        var classification = _errorClassifier.Classify(error, ex);

        recoverabilityContext.RecordFailedAttempt(error, ex, classification, duration);

        var result = Either<EncinaError, TResponse>.Left(error);

        if (classification == ErrorClassification.Permanent)
        {
            RecoverabilityLog.PermanentExceptionOnAttempt(
                _logger,
                ex.ForLogging(),
                recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown,
                typeof(TRequest).Name,
                attempt + 1);
            return AttemptResult.PermanentFailure(result);
        }

        RecoverabilityLog.TransientExceptionOnAttempt(
            _logger,
            ex.ForLogging(),
            recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown,
            typeof(TRequest).Name,
            attempt + 1,
            _options.ImmediateRetries + 1);

        return AttemptResult.TransientFailure(result);
    }

    private readonly struct AttemptResult
    {
        public Either<EncinaError, TResponse> Result { get; }
        public bool ShouldReturn { get; }

        private AttemptResult(Either<EncinaError, TResponse> result, bool shouldReturn)
        {
            Result = result;
            ShouldReturn = shouldReturn;
        }

        public static AttemptResult Success(Either<EncinaError, TResponse> result) => new(result, true);
        public static AttemptResult PermanentFailure(Either<EncinaError, TResponse> result) => new(result, true);
        public static AttemptResult TransientFailure(Either<EncinaError, TResponse> result) => new(result, false);
    }

    private TimeSpan CalculateImmediateRetryDelay(int attempt)
    {
        var baseDelay = _options.ImmediateRetryDelay;

        if (_options.UseExponentialBackoffForImmediateRetries)
        {
            // Exponential backoff: delay * 2^(attempt-1)
            var multiplier = Math.Pow(2, attempt - 1);
            baseDelay = TimeSpan.FromTicks((long)(baseDelay.Ticks * multiplier));
        }

        return GetDelayWithJitter(baseDelay);
    }

    private TimeSpan GetDelayWithJitter(TimeSpan baseDelay)
    {
        if (!_options.UseJitter || _options.MaxJitterPercent <= 0)
        {
            return baseDelay;
        }

        // Add ±jitter%
        var jitterFraction = (_jitterSource() * 2 - 1) * (_options.MaxJitterPercent / 100.0);
        var jitteredTicks = (long)(baseDelay.Ticks * (1 + jitterFraction));
        return TimeSpan.FromTicks(Math.Max(0, jitteredTicks));
    }

    private async Task HandlePermanentFailureAsync(
        TRequest request,
        RecoverabilityContext recoverabilityContext,
        CancellationToken cancellationToken,
        EncinaError? terminalError = null)
    {
        var failedMessage = recoverabilityContext.CreateFailedMessage(request);
        if (terminalError is { } terminal)
        {
            failedMessage = failedMessage with { Error = terminal };
        }

        RecoverabilityLog.MessagePermanentlyFailed(
            _logger,
            recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown,
            typeof(TRequest).Name,
            recoverabilityContext.TotalAttempts);

        // A cancelled request is not a terminal failure of the message.
        if (_deadLetterCapture is not null && !cancellationToken.IsCancellationRequested)
        {
            // A Left is logged by the capture (error code only); the request already returns its failure.
            await _deadLetterCapture.CaptureFailedMessageAsync(failedMessage, cancellationToken).ConfigureAwait(false);
        }

        await InvokeOnPermanentFailureAsync(failedMessage, recoverabilityContext, cancellationToken).ConfigureAwait(false);
    }

    private async Task InvokeOnPermanentFailureAsync(
        FailedMessage failedMessage,
        RecoverabilityContext recoverabilityContext,
        CancellationToken cancellationToken)
    {
        if (_options.OnPermanentFailure is null)
            return;

        try
        {
            await _options.OnPermanentFailure(failedMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RecoverabilityLog.OnPermanentFailureCallbackFailed(
                _logger,
                ex.ForLogging(),
                recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown,
                typeof(TRequest).Name);
        }
    }
}

/// <summary>
/// Constants used across recoverability components.
/// </summary>
internal static class RecoverabilityConstants
{
    /// <summary>
    /// Default fallback value for unknown correlation IDs or error messages.
    /// </summary>
    public const string Unknown = "unknown";
}

/// <summary>
/// Error codes for recoverability operations.
/// </summary>
/// <remarks>
/// These codes are included in error messages as prefixes for identification.
/// </remarks>
public static class RecoverabilityErrorCodes
{
    /// <summary>
    /// Operation was cancelled.
    /// </summary>
    public const string Cancelled = "recoverability.cancelled";

    /// <summary>
    /// An exception was thrown during execution.
    /// </summary>
    public const string ExceptionThrown = "recoverability.exception";

    /// <summary>
    /// All immediate retries exhausted, delayed retry has been scheduled.
    /// </summary>
    public const string DelayedRetryScheduled = "recoverability.delayed_retry_scheduled";

    /// <summary>
    /// All retries exhausted, message permanently failed.
    /// </summary>
    public const string PermanentlyFailed = "recoverability.permanently_failed";

    /// <summary>
    /// Unknown error during recoverability processing.
    /// </summary>
    public const string Unknown = "recoverability.unknown";

    /// <summary>
    /// Failed to schedule delayed retry.
    /// </summary>
    public const string ScheduleRetryFailed = "recoverability.schedule_retry_failed";

    /// <summary>
    /// Failed to cancel scheduled retry.
    /// </summary>
    public const string CancelRetryFailed = "recoverability.cancel_retry_failed";

    /// <summary>
    /// A <c>Retry-After</c> above the configured ceiling (<see cref="RecoverabilityOptions.MaxRetryAfter"/>,
    /// <c>OutboxOptions.MaxRetryAfter</c>): nothing is scheduled and the message is dead-lettered with this code.
    /// </summary>
    public const string RetryAfterExceedsCeiling = "recoverability.retry_after_exceeds_ceiling";
}

/// <summary>
/// The rule shared by the three asynchronous retry layers for a <c>Retry-After</c> above the ceiling.
/// </summary>
internal static class RetryAfterCeiling
{
    /// <summary>Metadata key of the error code that carried the <c>Retry-After</c>.</summary>
    internal const string OriginalErrorCodeKey = "original_error_code";

    /// <summary>
    /// Creates the error a dead letter carries. Its message is fixed text: the original error's message never
    /// reaches it (AGENTS.md section 3); only the original code is kept, in the details.
    /// </summary>
    internal static EncinaError CreateError(string originalErrorCode) =>
        EncinaErrors.Create(
            RecoverabilityErrorCodes.RetryAfterExceedsCeiling,
            "The Retry-After of the failure exceeds the configured ceiling; the message was dead-lettered.",
            details: new Dictionary<string, object?> { [OriginalErrorCodeKey] = originalErrorCode });
}

/// <summary>
/// LoggerMessage definitions for high-performance logging.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class RecoverabilityLog
{
    [LoggerMessage(
        EventId = 2861,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] {RequestType} succeeded after {AttemptCount} retries")]
    public static partial void SucceededAfterRetry(
        ILogger logger, string correlationId, string requestType, int attemptCount);

    /// <remarks>
    /// Only the error code is logged: <c>EncinaError.Message</c> can carry personal data
    /// (#1259 review).
    /// </remarks>
    [LoggerMessage(
        EventId = 2862,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] {RequestType} transient error on attempt {Attempt}/{MaxAttempts}: {ErrorCode}")]
    public static partial void TransientErrorOnAttempt(
        ILogger logger, string correlationId, string requestType, int attempt, int maxAttempts, string errorCode);

    /// <remarks>
    /// Only the error code is logged: <c>EncinaError.Message</c> can carry personal data
    /// (#1259 review).
    /// </remarks>
    [LoggerMessage(
        EventId = 2863,
        Level = LogLevel.Warning,
        Message = "[{CorrelationId}] {RequestType} permanent error on attempt {Attempt}: {ErrorCode}")]
    public static partial void PermanentErrorOnAttempt(
        ILogger logger, string correlationId, string requestType, int attempt, string errorCode);

    [LoggerMessage(
        EventId = 2864,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] {RequestType} transient exception on attempt {Attempt}/{MaxAttempts}")]
    public static partial void TransientExceptionOnAttempt(
        ILogger logger, Exception ex, string correlationId, string requestType, int attempt, int maxAttempts);

    [LoggerMessage(
        EventId = 2865,
        Level = LogLevel.Warning,
        Message = "[{CorrelationId}] {RequestType} permanent exception on attempt {Attempt}")]
    public static partial void PermanentExceptionOnAttempt(
        ILogger logger, Exception ex, string correlationId, string requestType, int attempt);

    [LoggerMessage(
        EventId = 2866,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] {RequestType} immediate retries exhausted after {RetryCount} attempts")]
    public static partial void ImmediateRetriesExhausted(
        ILogger logger, string correlationId, string requestType, int retryCount);

    [LoggerMessage(
        EventId = 2867,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] {RequestType} scheduling delayed retry {Attempt}/{MaxAttempts} in {Delay}")]
    public static partial void SchedulingDelayedRetry(
        ILogger logger, string correlationId, string requestType, int attempt, int maxAttempts, TimeSpan delay);

    /// <remarks>
    /// Only the error code is logged: <c>EncinaError.Message</c> can carry personal data
    /// (#1259 review).
    /// </remarks>
    [LoggerMessage(
        EventId = 2868,
        Level = LogLevel.Warning,
        Message = "[{CorrelationId}] {RequestType} permanent error detected: {ErrorCode}")]
    public static partial void PermanentErrorDetected(
        ILogger logger, string correlationId, string requestType, string errorCode);

    [LoggerMessage(
        EventId = 2869,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] {RequestType} permanently failed after {TotalAttempts} attempts")]
    public static partial void MessagePermanentlyFailed(
        ILogger logger, string correlationId, string requestType, int totalAttempts);

    [LoggerMessage(
        EventId = 2870,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] {RequestType} OnPermanentFailure callback failed")]
    public static partial void OnPermanentFailureCallbackFailed(
        ILogger logger, Exception ex, string correlationId, string requestType);

    /// <remarks>
    /// Only the error code is logged: <c>EncinaError.Message</c> can carry personal data
    /// (#1259 review).
    /// </remarks>
    [LoggerMessage(
        EventId = 2871,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] {RequestType} failed to schedule delayed retry: {ErrorCode}")]
    public static partial void SchedulingDelayedRetryFailed(
        ILogger logger, string correlationId, string requestType, string errorCode);

    [LoggerMessage(
        EventId = 5452,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] {RequestType} scheduling the delayed retry threw")]
    public static partial void SchedulingDelayedRetryThrew(
        ILogger logger, Exception ex, string correlationId, string requestType);

    [LoggerMessage(
        EventId = 5453,
        Level = LogLevel.Warning,
        Message = "[{CorrelationId}] {RequestType} not retried automatically: ambiguous failure of a non-idempotent send ({ErrorCode})")]
    public static partial void NonIdempotentRetrySkipped(
        ILogger logger, string correlationId, string requestType, string errorCode);

    /// <remarks>
    /// Event IDs: 5450-5499 (see EventIdRanges.MessagingRecoverability). Only the original error code is
    /// logged, never <c>EncinaError.Message</c>.
    /// </remarks>
    [LoggerMessage(
        EventId = 5454,
        Level = LogLevel.Warning,
        Message = "[{CorrelationId}] {RequestType} not retried: Retry-After {RetryAfter} exceeds the ceiling {MaxRetryAfter}, dead-lettered ({ErrorCode})")]
    public static partial void RetryAfterExceedsCeiling(
        ILogger logger, string correlationId, string requestType, string errorCode, TimeSpan retryAfter, TimeSpan maxRetryAfter);
}
