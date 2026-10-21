using Encina.Diagnostics;
using Encina.Polly.Diagnostics;
using Encina.Recoverability;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Encina.Polly;

/// <summary>
/// Pipeline behavior that implements retry logic using Polly.
/// Automatically retries failed requests based on <see cref="RetryAttribute"/> configuration.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
/// <remarks>
/// <para>
/// A failure is retried only when the <see cref="IErrorClassifier"/> does not classify it as
/// <see cref="ErrorClassification.Permanent"/>. A <c>Permanent</c> error is never retried, with or without
/// <see cref="RetryAttribute.RetryOnAllExceptions"/>, which only widens the retry to <c>Unknown</c> exceptions.
/// </para>
/// <para>
/// An outbound error that carries <c>http.retry_after</c> (see <see cref="OutboundErrorMetadataKeys"/>) is retried
/// after exactly that delay when it does not exceed <see cref="RetryAttribute.MaxDelayMs"/>; a longer delay is never
/// shortened, so the transient error is returned unchanged for an outer layer (delayed retry, outbox) to reschedule.
/// An ambiguous failure of a non-idempotent send (<see cref="OutboundErrorMetadataKeys.Ambiguous"/>) is retried
/// only with <see cref="RetryAttribute.RetryNonIdempotent"/> or when the request carries an idempotency key.
/// Every skipped retry is counted in <c>encina.retry.skipped_total</c> with its reason.
/// </para>
/// </remarks>
public sealed partial class RetryPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<RetryPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IErrorClassifier _errorClassifier;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="errorClassifier">Classifies a failure as transient, permanent or unknown.</param>
    /// <param name="timeProvider">Clock used for the retry delays; defaults to <see cref="TimeProvider.System"/>.</param>
    public RetryPipelineBehavior(
        ILogger<RetryPipelineBehavior<TRequest, TResponse>> logger,
        IErrorClassifier errorClassifier,
        TimeProvider? timeProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorClassifier = errorClassifier ?? throw new ArgumentNullException(nameof(errorClassifier));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Check if request has [Retry] attribute

        if (typeof(TRequest).GetCustomAttributes(typeof(RetryAttribute), true)
            .FirstOrDefault() is not RetryAttribute retryAttribute)
        {
            // No retry policy configured, pass through
            return await nextStep().ConfigureAwait(false);
        }

        // Build retry pipeline
        var pipeline = BuildRetryPipeline(retryAttribute, context);

        using var activity = RetryTelemetry.StartRetry(typeof(TRequest).Name, context.TenantId);

        // Execute with retry
        try
        {
            var result = await pipeline.ExecuteAsync(async ct => await nextStep().ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            RetryTelemetry.Complete(activity, result.IsRight, result.IsLeft ? ((EncinaError)result).GetCode().IfNone("encina.unknown") : null);
            return result;
        }
        catch (Exception ex)
        {
            LogRetryExhausted(_logger, typeof(TRequest).Name, retryAttribute.MaxAttempts, ex.ForLogging());
            RetryTelemetry.Complete(activity, false, ex.GetType().Name);
            return EncinaError.New(ex);
        }
    }

    private ResiliencePipeline<Either<EncinaError, TResponse>> BuildRetryPipeline(
        RetryAttribute config,
        IRequestContext context)
    {
        var requestType = typeof(TRequest).Name;
        var tenantId = context.TenantId;

        return new ResiliencePipelineBuilder<Either<EncinaError, TResponse>> { TimeProvider = _timeProvider }
            .AddRetry(new RetryStrategyOptions<Either<EncinaError, TResponse>>
            {
                MaxRetryAttempts = config.MaxAttempts - 1, // -1 because Polly counts retries, not total attempts
                Delay = TimeSpan.FromMilliseconds(config.BaseDelayMs),
                MaxDelay = TimeSpan.FromMilliseconds(config.MaxDelayMs),
                BackoffType = MapBackoffType(config.BackoffType),
                ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args.Outcome, config, context)),
                DelayGenerator = args => ValueTask.FromResult(RetryAfterOf(args.Outcome)),
                OnRetry = args => LogRetryAttempt(args, config, requestType, tenantId)
            })
            .Build();
    }

    private static DelayBackoffType MapBackoffType(BackoffType backoffType) => backoffType switch
    {
        BackoffType.Constant => DelayBackoffType.Constant,
        BackoffType.Linear => DelayBackoffType.Linear,
        BackoffType.Exponential => DelayBackoffType.Exponential,
        _ => DelayBackoffType.Exponential
    };

    private ValueTask LogRetryAttempt(
        OnRetryArguments<Either<EncinaError, TResponse>> args,
        RetryAttribute config,
        string requestType,
        string? tenantId)
    {
        var attemptNumber = args.AttemptNumber + 1; // +1 because Polly is zero-indexed
        var delay = args.RetryDelay;

        if (args.Outcome.Exception is not null)
        {
            LogRetryAttemptException(_logger, attemptNumber, config.MaxAttempts, requestType, delay.TotalMilliseconds, args.Outcome.Exception.ForLogging());
            RetryMetrics.RecordAttempt(requestType, args.Outcome.Exception.GetType().Name, tenantId);
        }
        else if (args.Outcome.Result is { } result && result.IsLeft)
        {
            var code = ((EncinaError)result).GetCode().IfNone("encina.unknown");
            LogRetryAttemptError(_logger, attemptNumber, config.MaxAttempts, requestType, delay.TotalMilliseconds, code);
            RetryMetrics.RecordAttempt(requestType, code, tenantId);
        }

        return ValueTask.CompletedTask;
    }

    // The delay the API dictated replaces the computed backoff for that attempt (never shortened, no jitter).
    private static TimeSpan? RetryAfterOf(Outcome<Either<EncinaError, TResponse>> outcome) =>
        outcome.Exception is null && outcome.Result.IsLeft
            ? OutboundRetryRules.GetRetryAfter((EncinaError)outcome.Result)
            : null;

    private bool ShouldRetry(Outcome<Either<EncinaError, TResponse>> outcome, RetryAttribute config, IRequestContext context)
    {
        if (outcome.Exception is { } exception)
        {
            return ShouldRetryException(exception, config, context);
        }

        // Only retry on Left (error) results
        return outcome.Result.IsLeft && ShouldRetryError((EncinaError)outcome.Result, config, context);
    }

    private bool ShouldRetryError(EncinaError error, RetryAttribute config, IRequestContext context)
    {
        return SkipReasonOf(error, config, context) is { } reason
            ? Skip(reason, error.GetCode().IfNone("encina.unknown"), context)
            : true;
    }

    // Why an error is not retried, or null when it is.
    private string? SkipReasonOf(EncinaError error, RetryAttribute config, IRequestContext context)
    {
        if (IsPermanent(error))
        {
            return RetryMetrics.ReasonPermanent;
        }

        return !OutboundRetryRules.AllowsAutomaticRetry(error, context.IdempotencyKey, config.RetryNonIdempotent)
            ? RetryMetrics.ReasonNonIdempotent
            : SkipReasonOfRetryAfter(error, config);
    }

    private bool IsPermanent(EncinaError error) =>
        _errorClassifier.Classify(error, error.GetCause().MatchUnsafe(ex => ex, () => null)) == ErrorClassification.Permanent;

    // A wait longer than the ceiling is never shortened: the transient error goes back to the caller
    // with http.retry_after so an outer layer can reschedule it.
    private static string? SkipReasonOfRetryAfter(EncinaError error, RetryAttribute config) =>
        OutboundRetryRules.GetRetryAfter(error) > TimeSpan.FromMilliseconds(config.MaxDelayMs)
            ? RetryMetrics.ReasonRetryAfterExceedsMax
            : null;

    private bool ShouldRetryException(Exception ex, RetryAttribute config, IRequestContext context)
    {
        var classification = _errorClassifier.Classify(EncinaError.New(ex), ex);

        return classification == ErrorClassification.Permanent
            ? Skip(RetryMetrics.ReasonPermanent, ex.GetType().Name, context)
            : IsRetryableException(classification, ex, config);
    }

    // Transient failures are retried; an unknown one only when the request opted in or it is a known transient type.
    private static bool IsRetryableException(ErrorClassification classification, Exception ex, RetryAttribute config) =>
        classification == ErrorClassification.Transient
        || config.RetryOnAllExceptions
        || ex is TimeoutException or HttpRequestException or IOException;

    // Counts and logs a retry that is not performed; always false so a caller can return it.
    private bool Skip(string reason, string errorCode, IRequestContext context)
    {
        RetryMetrics.RecordSkipped(reason, typeof(TRequest).Name, errorCode, context.TenantId);
        LogRetrySkipped(_logger, typeof(TRequest).Name, reason, errorCode);
        return false;
    }

    [LoggerMessage(
        EventId = 3918,
        Level = LogLevel.Error,
        Message = "Retry policy exhausted for {RequestType} after {MaxAttempts} attempts")]
    private static partial void LogRetryExhausted(
        ILogger logger,
        string requestType,
        int maxAttempts,
        Exception exception);

    [LoggerMessage(
        EventId = 3919,
        Level = LogLevel.Warning,
        Message = "Retry {AttemptNumber}/{MaxAttempts} for {RequestType} after {Delay}ms due to exception")]
    private static partial void LogRetryAttemptException(
        ILogger logger,
        int attemptNumber,
        int maxAttempts,
        string requestType,
        double delay,
        Exception exception);

    [LoggerMessage(
        EventId = 3920,
        Level = LogLevel.Warning,
        Message = "Retry {AttemptNumber}/{MaxAttempts} for {RequestType} after {Delay}ms due to error: {Error}")]
    private static partial void LogRetryAttemptError(
        ILogger logger,
        int attemptNumber,
        int maxAttempts,
        string requestType,
        double delay,
        string error);

    [LoggerMessage(
        EventId = 3921,
        Level = LogLevel.Information,
        Message = "Retry skipped for {RequestType} ({Reason}): {ErrorCode}")]
    private static partial void LogRetrySkipped(
        ILogger logger,
        string requestType,
        string reason,
        string errorCode);
}
