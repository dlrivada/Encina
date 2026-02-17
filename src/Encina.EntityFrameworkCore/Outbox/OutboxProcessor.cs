using System.Text.Json;
using Encina.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.EntityFrameworkCore.Outbox;

/// <summary>
/// Background service that processes pending outbox messages.
/// </summary>
/// <remarks>
/// <para>
/// This service runs periodically to publish notifications that were stored in the outbox.
/// It implements retry logic with exponential backoff for transient failures.
/// </para>
/// <para>
/// <b>Processing Algorithm</b>:
/// <list type="number">
/// <item><description>Query for pending messages (not processed, retry time reached)</description></item>
/// <item><description>Deserialize notification from JSON</description></item>
/// <item><description>Publish notification via IEncina</description></item>
/// <item><description>Mark as processed or schedule retry on failure</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Retry Strategy</b>: Exponential backoff with configurable base delay.
/// Formula: <c>NextRetry = BaseDelay * 2^RetryCount</c>
/// </para>
/// </remarks>
public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeProvider _timeProvider;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for creating scopes.</param>
    /// <param name="options">The outbox options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider for obtaining current UTC time. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/>, <paramref name="options"/>, or <paramref name="logger"/> is null.</exception>
    public OutboxProcessor(
        IServiceProvider serviceProvider,
        OutboxOptions options,
        ILogger<OutboxProcessor> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.OutboxProcessorStarting(_logger, _options.ProcessingInterval, _options.BatchSize, _options.MaxRetries);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Log.ErrorProcessingOutboxMessages(_logger, ex);
            }

            await Task.Delay(_options.ProcessingInterval, stoppingToken);
        }

        Log.OutboxProcessorStopping(_logger);
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
        var encina = scope.ServiceProvider.GetRequiredService<IEncina>();

        var pendingMessages = await GetPendingMessagesAsync(dbContext, cancellationToken);

        if (pendingMessages.Count == 0)
        {
            return;
        }

        Log.ProcessingPendingOutboxMessages(_logger, pendingMessages.Count);

        var (successCount, failureCount) = await ProcessMessagesAsync(pendingMessages, encina, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (successCount > 0 || failureCount > 0)
        {
            Log.ProcessedOutboxMessages(_logger, successCount + failureCount, successCount, failureCount);
        }
    }

    private async Task<List<OutboxMessage>> GetPendingMessagesAsync(
        DbContext dbContext,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return await dbContext.Set<OutboxMessage>()
            .Where(m =>
                m.ProcessedAtUtc == null &&
                (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now) &&
                m.RetryCount < _options.MaxRetries)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);
    }

    private async Task<(int SuccessCount, int FailureCount)> ProcessMessagesAsync(
        List<OutboxMessage> messages,
        IEncina encina,
        CancellationToken cancellationToken)
    {
        var successCount = 0;
        var failureCount = 0;

        foreach (var message in messages)
        {
            var success = await ProcessSingleMessageAsync(message, encina, cancellationToken);

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
        OutboxMessage message,
        IEncina encina,
        CancellationToken cancellationToken)
    {
        try
        {
            var notification = DeserializeNotification(message);

            if (notification is null)
            {
                return false;
            }

            await encina.Publish(notification, cancellationToken);

            message.ProcessedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            message.ErrorMessage = null;

            Log.PublishedNotification(_logger, notification.GetType().Name, message.Id);
            return true;
        }
        catch (Exception ex)
        {
            HandleProcessingError(message, ex);
            return false;
        }
    }

    private INotification? DeserializeNotification(OutboxMessage message)
    {
        var notificationType = Type.GetType(message.NotificationType);

        if (notificationType is null)
        {
            Log.TypeNotFound(_logger, message.NotificationType, message.Id);
            MarkMessageForRetry(message, $"Type not found: {message.NotificationType}");
            return null;
        }

        var notification = JsonSerializer.Deserialize(message.Content, notificationType, JsonOptions);

        if (notification is null)
        {
            Log.DeserializationFailed(_logger, message.Id);
            MarkMessageForRetry(message, "Deserialization failed");
            return null;
        }

        return (INotification)notification;
    }

    private void MarkMessageForRetry(OutboxMessage message, string errorMessage)
    {
        message.ErrorMessage = errorMessage;
        message.RetryCount++;
        message.NextRetryAtUtc = CalculateNextRetry(message.RetryCount);
    }

    private void HandleProcessingError(OutboxMessage message, Exception ex)
    {
        Log.ErrorProcessingOutboxMessage(_logger, ex, message.Id);

        message.ErrorMessage = ex.Message;
        message.RetryCount++;
        message.NextRetryAtUtc = message.RetryCount < _options.MaxRetries
            ? CalculateNextRetry(message.RetryCount)
            : null;
    }

    private DateTime CalculateNextRetry(int retryCount)
    {
        var delay = _options.BaseRetryDelay * Math.Pow(2, retryCount - 1);
        return _timeProvider.GetUtcNow().UtcDateTime.Add(delay);
    }
}
