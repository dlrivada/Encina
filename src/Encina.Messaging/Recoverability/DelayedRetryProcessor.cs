using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Encina.Messaging.Serialization;
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
    public DelayedRetryProcessor(
        IServiceScopeFactory scopeFactory,
        RecoverabilityOptions options,
        ILogger<DelayedRetryProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
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
                DelayedRetryProcessorLog.ProcessingError(_logger, ex);
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

        var store = scope.ServiceProvider.GetService<IDelayedRetryStore>();
        if (store is null)
        {
            DelayedRetryProcessorLog.StoreNotConfigured(_logger);
            return;
        }

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
        try
        {
            DelayedRetryProcessorLog.ProcessingRetry(
                _logger,
                message.CorrelationId ?? RecoverabilityConstants.Unknown,
                message.RequestType,
                message.DelayedRetryAttempt + 1);

            // Deserialize the request
            var requestType = Type.GetType(message.RequestType);
            if (requestType is null)
            {
                DelayedRetryProcessorLog.UnknownRequestType(_logger, message.Id, message.RequestType);
                await store.MarkAsFailedAsync(message.Id, $"Unknown request type: {message.RequestType}", cancellationToken).ConfigureAwait(false);
                return;
            }

            var request = messageSerializer.Deserialize(message.RequestContent, requestType);
            if (request is null)
            {
                DelayedRetryProcessorLog.DeserializationFailed(_logger, message.Id, message.RequestType);
                await store.MarkAsFailedAsync(message.Id, "Failed to deserialize request", cancellationToken).ConfigureAwait(false);
                return;
            }

            // Execute through Encina pipeline
            // Note: The RecoverabilityPipelineBehavior will handle any further failures
            var result = await DispatchRequestAsync(encina, request, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                DelayedRetryProcessorLog.RetrySucceeded(
                    _logger,
                    message.CorrelationId ?? RecoverabilityConstants.Unknown,
                    message.RequestType,
                    message.DelayedRetryAttempt + 1);
            }
            else
            {
                // Check if there are more delayed retries available
                var nextDelayedRetryAttempt = message.DelayedRetryAttempt + 1;
                if (nextDelayedRetryAttempt < _options.DelayedRetries.Length)
                {
                    // Schedule next delayed retry
                    await ScheduleNextDelayedRetryAsync(
                        message,
                        request,
                        nextDelayedRetryAttempt,
                        cancellationToken).ConfigureAwait(false);

                    await store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // All delayed retries exhausted - permanent failure
                    await store.MarkAsFailedAsync(message.Id, result.ErrorMessage ?? "Unknown error", cancellationToken).ConfigureAwait(false);
                    await HandlePermanentFailureAsync(message, request, result.ErrorMessage ?? "Unknown error", cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            DelayedRetryProcessorLog.ProcessingException(_logger, ex, message.Id, message.RequestType);
            await store.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<DispatchResult> DispatchRequestAsync(
        IEncina encina,
        object request,
        CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await RuntimeTypeRequestDispatcher.SendAsync(encina, request, cancellationToken).ConfigureAwait(false);

            return outcome.Match(
                Right: _ => new DispatchResult(true, null),
                // Only the error code: EncinaError.Message can carry personal data (#1259 review).
                Left: error => new DispatchResult(false, error.GetCode().IfNone("encina.unknown")));
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new DispatchResult(false, ex.GetType().FullName ?? ex.GetType().Name);
        }
    }

    private async Task ScheduleNextDelayedRetryAsync(
        IDelayedRetryMessage originalMessage,
        object request,
        int nextDelayedRetryAttempt,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var scheduler = scope.ServiceProvider.GetService<IDelayedRetryScheduler>();
        if (scheduler is null)
        {
            DelayedRetryProcessorLog.SchedulerNotConfigured(_logger);
            return;
        }

        // Reconstruct context from the serialized data
        var context = DeserializeContext(originalMessage.ContextContent);
        context.IncrementDelayedRetry();

        var delay = _options.DelayedRetries[nextDelayedRetryAttempt];

        DelayedRetryProcessorLog.SchedulingNextRetry(
            _logger,
            originalMessage.CorrelationId ?? RecoverabilityConstants.Unknown,
            originalMessage.RequestType,
            nextDelayedRetryAttempt + 1,
            _options.DelayedRetries.Length,
            delay);

        // Use reflection to call generic ScheduleRetryAsync
        var requestType = Type.GetType(originalMessage.RequestType);
        if (requestType is null)
        {
            return;
        }

        var scheduleMethod = typeof(IDelayedRetryScheduler)
            .GetMethod(nameof(IDelayedRetryScheduler.ScheduleRetryAsync));

        if (scheduleMethod is null)
        {
            return;
        }

        var genericMethod = scheduleMethod.MakeGenericMethod(requestType);
        var task = (Task?)genericMethod.Invoke(scheduler, [request, context, delay, nextDelayedRetryAttempt, cancellationToken]);

        if (task is not null)
        {
            await task.ConfigureAwait(false);
        }
    }

    private static RecoverabilityContext DeserializeContext(string contextContent)
    {
        var serializable = JsonSerializer.Deserialize<SerializableRecoverabilityContext>(contextContent, JsonOptions);

        var context = new RecoverabilityContext
        {
            CorrelationId = serializable?.CorrelationId,
            IdempotencyKey = serializable?.IdempotencyKey,
            RequestTypeName = serializable?.RequestTypeName
        };

        // Restore retry counts
        for (var i = 0; i < (serializable?.ImmediateRetryCount ?? 0); i++)
        {
            context.IncrementImmediateRetry();
        }

        for (var i = 0; i < (serializable?.DelayedRetryCount ?? 0); i++)
        {
            context.IncrementDelayedRetry();
        }

        return context;
    }

    private async Task HandlePermanentFailureAsync(
        IDelayedRetryMessage message,
        object request,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        DelayedRetryProcessorLog.PermanentFailure(
            _logger,
            message.CorrelationId ?? RecoverabilityConstants.Unknown,
            message.RequestType,
            message.DelayedRetryAttempt + 1);

        if (_options.OnPermanentFailure is not null)
        {
            var context = DeserializeContext(message.ContextContent);
            context.RecordFailedAttempt(
                EncinaError.New($"[{RecoverabilityErrorCodes.PermanentlyFailed}] {errorMessage}"),
                null,
                ErrorClassification.Permanent);

            var failedMessage = context.CreateFailedMessage(request);

            try
            {
                await _options.OnPermanentFailure(failedMessage, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DelayedRetryProcessorLog.OnPermanentFailureCallbackFailed(
                    _logger,
                    ex,
                    message.CorrelationId ?? RecoverabilityConstants.Unknown,
                    message.RequestType);
            }
        }
    }

    private sealed record DispatchResult(bool IsSuccess, string? ErrorMessage);
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
        EventId = 2848,
        Level = LogLevel.Warning,
        Message = "IDelayedRetryStore not configured - delayed retries disabled")]
    public static partial void StoreNotConfigured(ILogger logger);

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
}
