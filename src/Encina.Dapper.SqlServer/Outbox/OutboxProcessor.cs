using System.Text.Json;
using Encina.Messaging;
using Encina.Messaging.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Dapper.SqlServer.Outbox;

/// <summary>
/// Background service that processes pending outbox messages and publishes them through the Encina.
/// Runs periodically to ensure reliable event delivery with retry logic.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxOptions _options;
    private readonly TimeProvider _timeProvider;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessor"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scopes.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="options">Configuration options for outbox processing.</param>
    /// <param name="timeProvider">Optional time provider for testability. Defaults to <see cref="TimeProvider.System"/>.</param>
    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger,
        OutboxOptions options,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableProcessor)
        {
            MessagingLog.OutboxProcessorDisabled(_logger);
            return;
        }

        MessagingLog.OutboxProcessorStarted(_logger, _options.ProcessingInterval, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                MessagingLog.ErrorProcessingOutboxMessages(_logger, ex);
            }

            await Task.Delay(_options.ProcessingInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        var messages = await store.GetPendingMessagesAsync(
            _options.BatchSize,
            _options.MaxRetries,
            cancellationToken).ConfigureAwait(false);

        var messagesList = messages.ToList();
        if (messagesList.Count == 0)
        {
            return;
        }

        MessagingLog.ProcessingPendingOutboxMessages(_logger, messagesList.Count);

        var (successCount, failureCount) = await ProcessMessagesAsync(
            messagesList, store, encina, cancellationToken).ConfigureAwait(false);

        await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (successCount > 0 || failureCount > 0)
        {
            MessagingLog.ProcessedOutboxMessages(_logger, successCount + failureCount, successCount, failureCount);
        }
    }

    private async Task<(int SuccessCount, int FailureCount)> ProcessMessagesAsync(
        IReadOnlyList<IOutboxMessage> messages,
        IOutboxStore store,
        IEncina encina,
        CancellationToken cancellationToken)
    {
        var successCount = 0;
        var failureCount = 0;

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var success = await ProcessSingleMessageAsync(message, store, encina, cancellationToken)
                .ConfigureAwait(false);

            if (success)
            {
                successCount++;
            }
            else
            {
                failureCount++;
            }
        }

        return (successCount, failureCount);
    }

    private async Task<bool> ProcessSingleMessageAsync(
        IOutboxMessage message,
        IOutboxStore store,
        IEncina encina,
        CancellationToken cancellationToken)
    {
        try
        {
            var notification = DeserializeNotification(message);

            if (notification is null)
            {
                await MarkAsFailedWithRetryAsync(store, message, "Failed to deserialize notification", cancellationToken)
                    .ConfigureAwait(false);
                return false;
            }

            await encina.Publish(notification, cancellationToken).ConfigureAwait(false);
            await store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);

            MessagingLog.ProcessedOutboxMessage(_logger, message.Id, message.NotificationType);
            return true;
        }
        catch (Exception ex)
        {
            await HandleProcessingErrorAsync(store, message, ex, cancellationToken).ConfigureAwait(false);
            return false;
        }
    }

    private static INotification? DeserializeNotification(IOutboxMessage message)
    {
        var notificationType = Type.GetType(message.NotificationType);

        if (notificationType is null)
        {
            return null;
        }

        var notification = JsonSerializer.Deserialize(message.Content, notificationType, JsonOptions);

        return notification as INotification;
    }

    private async Task MarkAsFailedWithRetryAsync(
        IOutboxStore store,
        IOutboxMessage message,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var nextRetry = CalculateNextRetryOrNull(message.RetryCount + 1);

        await store.MarkAsFailedAsync(message.Id, errorMessage, nextRetry, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task HandleProcessingErrorAsync(
        IOutboxStore store,
        IOutboxMessage message,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var nextRetry = CalculateNextRetryOrNull(message.RetryCount + 1);

        await store.MarkAsFailedAsync(message.Id, ex.Message, nextRetry, cancellationToken)
            .ConfigureAwait(false);

        MessagingLog.FailedToProcessOutboxMessage(
            _logger, ex, message.Id, message.RetryCount + 1, _options.MaxRetries, nextRetry);
    }

    private DateTime? CalculateNextRetryOrNull(int retryCount)
    {
        if (retryCount >= _options.MaxRetries)
        {
            return null;
        }

        return CalculateNextRetry(retryCount);
    }

    private DateTime CalculateNextRetry(int retryCount)
    {
        var delay = _options.BaseRetryDelay * Math.Pow(2, retryCount - 1);
        return _timeProvider.GetUtcNow().UtcDateTime.Add(delay);
    }
}
