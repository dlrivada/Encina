using System.Diagnostics.CodeAnalysis;
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
    private static readonly Random Jitter = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RecoverabilityPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="options">The recoverability options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="delayedRetryScheduler">Optional scheduler for delayed retries.</param>
    public RecoverabilityPipelineBehavior(
        RecoverabilityOptions options,
        ILogger<RecoverabilityPipelineBehavior<TRequest, TResponse>> logger,
        IDelayedRetryScheduler? delayedRetryScheduler = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options;
        _errorClassifier = options.ErrorClassifier ?? new DefaultErrorClassifier();
        _logger = logger;
        _delayedRetryScheduler = delayedRetryScheduler;
    }

    /// <inheritdoc />
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        var recoverabilityContext = new RecoverabilityContext
        {
            CorrelationId = context.CorrelationId,
            IdempotencyKey = context.IdempotencyKey,
            RequestTypeName = typeof(TRequest).Name
        };

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

        // If error is permanent, skip delayed retries
        if (recoverabilityContext.LastClassification == ErrorClassification.Permanent)
        {
            RecoverabilityLog.PermanentErrorDetected(
                _logger,
                context.CorrelationId,
                typeof(TRequest).Name,
                recoverabilityContext.LastError?.Message ?? RecoverabilityConstants.Unknown);

            await HandlePermanentFailureAsync(request, recoverabilityContext, cancellationToken).ConfigureAwait(false);
            return result;
        }

        // Schedule delayed retry if enabled and scheduler is available
        if (_options.EnableDelayedRetries && _delayedRetryScheduler is not null && _options.DelayedRetries.Length > 0)
        {
            var firstDelayedRetryDelay = GetDelayWithJitter(_options.DelayedRetries[0]);

            RecoverabilityLog.SchedulingDelayedRetry(
                _logger,
                context.CorrelationId,
                typeof(TRequest).Name,
                1,
                _options.DelayedRetries.Length,
                firstDelayedRetryDelay);

            await _delayedRetryScheduler.ScheduleRetryAsync(
                request,
                recoverabilityContext,
                firstDelayedRetryDelay,
                0,
                cancellationToken).ConfigureAwait(false);

            // Return error but note that delayed retry is scheduled
            return Either<EncinaError, TResponse>.Left(
                EncinaError.New($"[{RecoverabilityErrorCodes.DelayedRetryScheduled}] Immediate retries exhausted. Delayed retry scheduled in {firstDelayedRetryDelay.TotalSeconds:F0}s."));
        }

        // No delayed retries - permanent failure
        await HandlePermanentFailureAsync(request, recoverabilityContext, cancellationToken).ConfigureAwait(false);
        return result;
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
        var startTime = DateTime.UtcNow;

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
        var error = result.Match(
            Right: _ => throw new InvalidOperationException("Unexpected Right value"),
            Left: e => e);

        var duration = DateTime.UtcNow - startTime;
        var errorException = error.Exception.MatchUnsafe(ex => ex, () => null);
        var classification = _errorClassifier.Classify(error, errorException);

        recoverabilityContext.RecordFailedAttempt(error, errorException, classification, duration);

        if (classification == ErrorClassification.Permanent)
        {
            RecoverabilityLog.PermanentErrorOnAttempt(
                _logger,
                recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown,
                typeof(TRequest).Name,
                attempt + 1,
                error.Message);
            return AttemptResult.PermanentFailure(result);
        }

        RecoverabilityLog.TransientErrorOnAttempt(
            _logger,
            recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown,
            typeof(TRequest).Name,
            attempt + 1,
            _options.ImmediateRetries + 1,
            error.Message);

        return AttemptResult.TransientFailure(result);
    }

    private AttemptResult HandleException(
        Exception ex,
        RecoverabilityContext recoverabilityContext,
        DateTime startTime,
        int attempt)
    {
        var duration = DateTime.UtcNow - startTime;
        var error = EncinaError.New(ex, $"[{RecoverabilityErrorCodes.ExceptionThrown}] {ex.Message}");
        var classification = _errorClassifier.Classify(error, ex);

        recoverabilityContext.RecordFailedAttempt(error, ex, classification, duration);

        var result = Either<EncinaError, TResponse>.Left(error);

        if (classification == ErrorClassification.Permanent)
        {
            RecoverabilityLog.PermanentExceptionOnAttempt(
                _logger,
                ex,
                recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown,
                typeof(TRequest).Name,
                attempt + 1);
            return AttemptResult.PermanentFailure(result);
        }

        RecoverabilityLog.TransientExceptionOnAttempt(
            _logger,
            ex,
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
        var jitterFraction = (Jitter.NextDouble() * 2 - 1) * (_options.MaxJitterPercent / 100.0);
        var jitteredTicks = (long)(baseDelay.Ticks * (1 + jitterFraction));
        return TimeSpan.FromTicks(Math.Max(0, jitteredTicks));
    }

    private async Task HandlePermanentFailureAsync(
        TRequest request,
        RecoverabilityContext recoverabilityContext,
        CancellationToken cancellationToken)
    {
        var failedMessage = recoverabilityContext.CreateFailedMessage(request);

        RecoverabilityLog.MessagePermanentlyFailed(
            _logger,
            recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown,
            typeof(TRequest).Name,
            recoverabilityContext.TotalAttempts);

        if (_options.OnPermanentFailure is not null)
        {
            try
            {
                await _options.OnPermanentFailure(failedMessage, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RecoverabilityLog.OnPermanentFailureCallbackFailed(
                    _logger,
                    ex,
                    recoverabilityContext.CorrelationId ?? RecoverabilityConstants.Unknown,
                    typeof(TRequest).Name);
            }
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
}

/// <summary>
/// LoggerMessage definitions for high-performance logging.
/// </summary>
[ExcludeFromCodeCoverage]
internal static partial class RecoverabilityLog
{
    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] {RequestType} succeeded after {AttemptCount} retries")]
    public static partial void SucceededAfterRetry(
        ILogger logger, string correlationId, string requestType, int attemptCount);

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] {RequestType} transient error on attempt {Attempt}/{MaxAttempts}: {ErrorMessage}")]
    public static partial void TransientErrorOnAttempt(
        ILogger logger, string correlationId, string requestType, int attempt, int maxAttempts, string errorMessage);

    [LoggerMessage(
        EventId = 203,
        Level = LogLevel.Warning,
        Message = "[{CorrelationId}] {RequestType} permanent error on attempt {Attempt}: {ErrorMessage}")]
    public static partial void PermanentErrorOnAttempt(
        ILogger logger, string correlationId, string requestType, int attempt, string errorMessage);

    [LoggerMessage(
        EventId = 204,
        Level = LogLevel.Debug,
        Message = "[{CorrelationId}] {RequestType} transient exception on attempt {Attempt}/{MaxAttempts}")]
    public static partial void TransientExceptionOnAttempt(
        ILogger logger, Exception ex, string correlationId, string requestType, int attempt, int maxAttempts);

    [LoggerMessage(
        EventId = 205,
        Level = LogLevel.Warning,
        Message = "[{CorrelationId}] {RequestType} permanent exception on attempt {Attempt}")]
    public static partial void PermanentExceptionOnAttempt(
        ILogger logger, Exception ex, string correlationId, string requestType, int attempt);

    [LoggerMessage(
        EventId = 206,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] {RequestType} immediate retries exhausted after {RetryCount} attempts")]
    public static partial void ImmediateRetriesExhausted(
        ILogger logger, string correlationId, string requestType, int retryCount);

    [LoggerMessage(
        EventId = 207,
        Level = LogLevel.Information,
        Message = "[{CorrelationId}] {RequestType} scheduling delayed retry {Attempt}/{MaxAttempts} in {Delay}")]
    public static partial void SchedulingDelayedRetry(
        ILogger logger, string correlationId, string requestType, int attempt, int maxAttempts, TimeSpan delay);

    [LoggerMessage(
        EventId = 208,
        Level = LogLevel.Warning,
        Message = "[{CorrelationId}] {RequestType} permanent error detected: {ErrorMessage}")]
    public static partial void PermanentErrorDetected(
        ILogger logger, string correlationId, string requestType, string errorMessage);

    [LoggerMessage(
        EventId = 209,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] {RequestType} permanently failed after {TotalAttempts} attempts")]
    public static partial void MessagePermanentlyFailed(
        ILogger logger, string correlationId, string requestType, int totalAttempts);

    [LoggerMessage(
        EventId = 210,
        Level = LogLevel.Error,
        Message = "[{CorrelationId}] {RequestType} OnPermanentFailure callback failed")]
    public static partial void OnPermanentFailureCallbackFailed(
        ILogger logger, Exception ex, string correlationId, string requestType);
}
