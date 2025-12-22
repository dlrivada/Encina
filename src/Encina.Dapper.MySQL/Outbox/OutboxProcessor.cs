using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Encina.Messaging.Outbox;

namespace Encina.Dapper.MySQL.Outbox;

/// <summary>
/// Background service that processes pending outbox messages and publishes them through the mediator.
/// Runs periodically to ensure reliable event delivery with retry logic.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxOptions _options;
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
    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor> logger,
        OutboxOptions options)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableProcessor)
        {
            Log.OutboxProcessorDisabled(_logger);
            return;
        }

        Log.OutboxProcessorStarted(_logger, _options.ProcessingInterval, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Log.ErrorProcessingOutboxMessages(_logger, ex);
            }

            await Task.Delay(_options.ProcessingInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var messages = await store.GetPendingMessagesAsync(
            _options.BatchSize,
            _options.MaxRetries,
            cancellationToken).ConfigureAwait(false);

        var messagesList = messages.ToList();
        if (messagesList.Count == 0)
            return;

        Log.ProcessingPendingOutboxMessages(_logger, messagesList.Count);

        var successCount = 0;
        var failureCount = 0;

        foreach (var message in messagesList)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Deserialize notification
                var notificationType = Type.GetType(message.NotificationType);
                if (notificationType == null)
                {
                    var nextRetry = message.RetryCount + 1 < _options.MaxRetries
                        ? CalculateNextRetry(message.RetryCount + 1)
                        : (DateTime?)null;

                    await store.MarkAsFailedAsync(
                        message.Id,
                        $"Type not found: {message.NotificationType}",
                        nextRetry,
                        cancellationToken).ConfigureAwait(false);
                    failureCount++;
                    continue;
                }

                var notification = JsonSerializer.Deserialize(message.Content, notificationType, JsonOptions);
                if (notification == null)
                {
                    var nextRetry = message.RetryCount + 1 < _options.MaxRetries
                        ? CalculateNextRetry(message.RetryCount + 1)
                        : (DateTime?)null;

                    await store.MarkAsFailedAsync(
                        message.Id,
                        "Failed to deserialize notification content",
                        nextRetry,
                        cancellationToken).ConfigureAwait(false);
                    failureCount++;
                    continue;
                }

                // Publish notification
                await mediator.Publish((INotification)notification, cancellationToken).ConfigureAwait(false);

                // Mark as processed
                await store.MarkAsProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                successCount++;

                Log.ProcessedOutboxMessage(_logger, message.Id, message.NotificationType);
            }
            catch (Exception ex)
            {
                var nextRetry = message.RetryCount + 1 < _options.MaxRetries
                    ? CalculateNextRetry(message.RetryCount + 1)
                    : (DateTime?)null;

                await store.MarkAsFailedAsync(
                    message.Id,
                    ex.Message,
                    nextRetry,
                    cancellationToken).ConfigureAwait(false);
                failureCount++;

                Log.FailedToProcessOutboxMessage(_logger, ex, message.Id, message.RetryCount + 1, _options.MaxRetries, nextRetry);
            }
        }

        await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (successCount > 0 || failureCount > 0)
        {
            Log.ProcessedOutboxMessages(_logger, successCount + failureCount, successCount, failureCount);
        }
    }

    private DateTime CalculateNextRetry(int retryCount)
    {
        var delay = _options.BaseRetryDelay * Math.Pow(2, retryCount - 1);
        return DateTime.UtcNow.Add(delay);
    }
}
