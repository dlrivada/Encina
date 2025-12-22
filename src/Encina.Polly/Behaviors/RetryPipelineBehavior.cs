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
public sealed partial class RetryPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<RetryPipelineBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public RetryPipelineBehavior(ILogger<RetryPipelineBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        var pipeline = BuildRetryPipeline(retryAttribute, request);

        // Execute with retry
        try
        {
            return await pipeline.ExecuteAsync(async ct => await nextStep().ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogRetryExhausted(_logger, typeof(TRequest).Name, retryAttribute.MaxAttempts, ex);
            return EncinaError.New(ex);
        }
    }

    private ResiliencePipeline<Either<EncinaError, TResponse>> BuildRetryPipeline(
        RetryAttribute config,
        TRequest _)
    {
        var requestType = typeof(TRequest).Name;

        return new ResiliencePipelineBuilder<Either<EncinaError, TResponse>>()
            .AddRetry(new RetryStrategyOptions<Either<EncinaError, TResponse>>
            {
                MaxRetryAttempts = config.MaxAttempts - 1, // -1 because Polly counts retries, not total attempts
                Delay = TimeSpan.FromMilliseconds(config.BaseDelayMs),
                MaxDelay = TimeSpan.FromMilliseconds(config.MaxDelayMs),
                BackoffType = config.BackoffType switch
                {
                    BackoffType.Constant => DelayBackoffType.Constant,
                    BackoffType.Linear => DelayBackoffType.Linear,
                    BackoffType.Exponential => DelayBackoffType.Exponential,
                    _ => DelayBackoffType.Exponential
                },
                ShouldHandle = new PredicateBuilder<Either<EncinaError, TResponse>>()
                    .HandleResult(result => ShouldRetry(result, config))
                    .Handle<Exception>(ex => ShouldRetryException(ex, config)),
                OnRetry = args =>
                {
                    var attemptNumber = args.AttemptNumber + 1; // +1 because Polly is zero-indexed
                    var delay = args.RetryDelay;

                    if (args.Outcome.Exception is not null)
                    {
                        LogRetryAttemptException(_logger, attemptNumber, config.MaxAttempts, requestType, delay.TotalMilliseconds, args.Outcome.Exception);
                    }
                    else if (args.Outcome.Result is { } result && result.IsLeft)
                    {
                        result.IfLeft(error =>
                        {
                            LogRetryAttemptError(_logger, attemptNumber, config.MaxAttempts, requestType, delay.TotalMilliseconds, error.Message);
                        });
                    }

                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    private static bool ShouldRetry(Either<EncinaError, TResponse> result, RetryAttribute _)
    {
        // Only retry on Left (error) results
        return result.IsLeft;
    }

    private static bool ShouldRetryException(Exception ex, RetryAttribute config)
    {
        if (config.RetryOnAllExceptions)
        {
            return true;
        }

        // Only retry transient failures by default
        return ex is TimeoutException
            or HttpRequestException
            or IOException;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Retry policy exhausted for {RequestType} after {MaxAttempts} attempts")]
    private static partial void LogRetryExhausted(
        ILogger logger,
        string requestType,
        int maxAttempts,
        Exception exception);

    [LoggerMessage(
        EventId = 2,
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
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Retry {AttemptNumber}/{MaxAttempts} for {RequestType} after {Delay}ms due to error: {Error}")]
    private static partial void LogRetryAttemptError(
        ILogger logger,
        int attemptNumber,
        int maxAttempts,
        string requestType,
        double delay,
        string error);
}
