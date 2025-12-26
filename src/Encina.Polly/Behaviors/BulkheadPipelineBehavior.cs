using LanguageExt;
using Microsoft.Extensions.Logging;

namespace Encina.Polly;

/// <summary>
/// Pipeline behavior that implements bulkhead isolation pattern.
/// Limits concurrent executions per handler type to prevent cascade failures.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
/// <remarks>
/// <para>
/// This behavior provides:
/// </para>
/// <list type="bullet">
/// <item><description>Concurrency limiting per handler type</description></item>
/// <item><description>Queueing of excess requests up to a configured limit</description></item>
/// <item><description>Timeout for queued requests</description></item>
/// <item><description>Metrics for monitoring bulkhead utilization</description></item>
/// </list>
/// </remarks>
public sealed partial class BulkheadPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<BulkheadPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IBulkheadManager _bulkheadManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkheadPipelineBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="bulkheadManager">Bulkhead manager instance.</param>
    public BulkheadPipelineBehavior(
        ILogger<BulkheadPipelineBehavior<TRequest, TResponse>> logger,
        IBulkheadManager bulkheadManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bulkheadManager = bulkheadManager ?? throw new ArgumentNullException(nameof(bulkheadManager));
    }

    /// <inheritdoc/>
    public async ValueTask<Either<EncinaError, TResponse>> Handle(
        TRequest request,
        IRequestContext context,
        RequestHandlerCallback<TResponse> nextStep,
        CancellationToken cancellationToken)
    {
        // Check if request has [Bulkhead] attribute
        if (typeof(TRequest).GetCustomAttributes(typeof(BulkheadAttribute), true)
            .FirstOrDefault() is not BulkheadAttribute bulkheadAttribute)
        {
            // No bulkhead policy configured, pass through
            return await nextStep().ConfigureAwait(false);
        }

        var requestType = typeof(TRequest).Name;
        var key = requestType;

        // Try to acquire a permit
        var acquireResult = await _bulkheadManager.TryAcquireAsync(key, bulkheadAttribute, cancellationToken)
            .ConfigureAwait(false);

        if (!acquireResult.IsAcquired)
        {
            return HandleRejection(requestType, acquireResult);
        }

        LogBulkheadAcquired(
            _logger,
            requestType,
            acquireResult.Metrics.CurrentConcurrency,
            acquireResult.Metrics.MaxConcurrency,
            acquireResult.Metrics.CurrentQueuedCount);

        // Execute the handler with the bulkhead permit
        try
        {
            return await nextStep().ConfigureAwait(false);
        }
        finally
        {
            // Release the permit when done
            acquireResult.Releaser?.Dispose();
        }
    }

    private Either<EncinaError, TResponse> HandleRejection(
        string requestType,
        BulkheadAcquireResult acquireResult)
    {
        var metrics = acquireResult.Metrics;

        return acquireResult.RejectionReason switch
        {
            BulkheadRejectionReason.BulkheadFull => HandleBulkheadFullRejection(requestType, metrics),
            BulkheadRejectionReason.QueueTimeout => HandleQueueTimeoutRejection(requestType, metrics),
            BulkheadRejectionReason.Cancelled => HandleCancelledRejection(requestType, metrics),
            _ => HandleBulkheadFullRejection(requestType, metrics)
        };
    }

    private Either<EncinaError, TResponse> HandleBulkheadFullRejection(
        string requestType,
        BulkheadMetrics metrics)
    {
        LogBulkheadRejectedFull(
            _logger,
            requestType,
            metrics.CurrentConcurrency,
            metrics.MaxConcurrency,
            metrics.CurrentQueuedCount,
            metrics.MaxQueuedActions);

        var errorMessage = $"Bulkhead full for {requestType}. " +
                           $"Concurrent: {metrics.CurrentConcurrency}/{metrics.MaxConcurrency}. " +
                           $"Queued: {metrics.CurrentQueuedCount}/{metrics.MaxQueuedActions}. " +
                           $"Rejection rate: {metrics.RejectionRate:F1}%.";

        return EncinaError.New(errorMessage);
    }

    private Either<EncinaError, TResponse> HandleQueueTimeoutRejection(
        string requestType,
        BulkheadMetrics metrics)
    {
        LogBulkheadRejectedTimeout(
            _logger,
            requestType,
            metrics.CurrentConcurrency,
            metrics.MaxConcurrency);

        var errorMessage = $"Bulkhead queue timeout for {requestType}. " +
                           $"Concurrent: {metrics.CurrentConcurrency}/{metrics.MaxConcurrency}. " +
                           $"Request timed out while waiting in queue.";

        return EncinaError.New(errorMessage);
    }

    private Either<EncinaError, TResponse> HandleCancelledRejection(
        string requestType,
        BulkheadMetrics metrics)
    {
        LogBulkheadRejectedCancelled(
            _logger,
            requestType,
            metrics.CurrentConcurrency,
            metrics.MaxConcurrency);

        var errorMessage = $"Bulkhead request cancelled for {requestType}. " +
                           $"Request was cancelled while waiting in queue.";

        return EncinaError.New(errorMessage);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Bulkhead full for {RequestType}. Concurrent: {CurrentConcurrency}/{MaxConcurrency}. Queued: {QueuedCount}/{MaxQueued}. Request rejected.")]
    private static partial void LogBulkheadRejectedFull(
        ILogger logger,
        string requestType,
        int currentConcurrency,
        int maxConcurrency,
        int queuedCount,
        int maxQueued);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Bulkhead queue timeout for {RequestType}. Concurrent: {CurrentConcurrency}/{MaxConcurrency}. Request rejected.")]
    private static partial void LogBulkheadRejectedTimeout(
        ILogger logger,
        string requestType,
        int currentConcurrency,
        int maxConcurrency);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Debug,
        Message = "Bulkhead request cancelled for {RequestType}. Concurrent: {CurrentConcurrency}/{MaxConcurrency}.")]
    private static partial void LogBulkheadRejectedCancelled(
        ILogger logger,
        string requestType,
        int currentConcurrency,
        int maxConcurrency);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Bulkhead permit acquired for {RequestType}. Concurrent: {CurrentConcurrency}/{MaxConcurrency}. Queued: {QueuedCount}.")]
    private static partial void LogBulkheadAcquired(
        ILogger logger,
        string requestType,
        int currentConcurrency,
        int maxConcurrency,
        int queuedCount);
}
