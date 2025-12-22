using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Encina.Messaging.Outbox;

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
/// <item><description>Publish notification via IMediator</description></item>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/>, <paramref name="options"/>, or <paramref name="logger"/> is null.</exception>
    public OutboxProcessor(
        IServiceProvider serviceProvider,
        OutboxOptions options,
        ILogger<OutboxProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
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
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var now = DateTime.UtcNow;

        // Query pending messages
        var pendingMessages = await dbContext.Set<OutboxMessage>()
            .Where(m =>
                m.ProcessedAtUtc == null &&
                (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now) &&
                m.RetryCount < _options.MaxRetries)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingMessages.Count == 0)
            return;

        Log.ProcessingPendingOutboxMessages(_logger, pendingMessages.Count);

        var successCount = 0;
        var failureCount = 0;

        foreach (var message in pendingMessages)
        {
            try
            {
                // Deserialize notification
                var notificationType = Type.GetType(message.NotificationType);
                if (notificationType == null)
                {
                    Log.TypeNotFound(_logger, message.NotificationType, message.Id);

                    message.ErrorMessage = $"Type not found: {message.NotificationType}";
                    message.RetryCount++;
                    message.NextRetryAtUtc = CalculateNextRetry(message.RetryCount);
                    failureCount++;
                    continue;
                }

                var notification = JsonSerializer.Deserialize(message.Content, notificationType, JsonOptions);
                if (notification == null)
                {
                    Log.DeserializationFailed(_logger, message.Id);

                    message.ErrorMessage = "Deserialization failed";
                    message.RetryCount++;
                    message.NextRetryAtUtc = CalculateNextRetry(message.RetryCount);
                    failureCount++;
                    continue;
                }

                // Publish notification
                await mediator.Publish((INotification)notification, cancellationToken);

                // Mark as processed
                message.ProcessedAtUtc = DateTime.UtcNow;
                message.ErrorMessage = null;
                successCount++;

                Log.PublishedNotification(_logger, notificationType.Name, message.Id);
            }
            catch (Exception ex)
            {
                Log.ErrorProcessingOutboxMessage(_logger, ex, message.Id);

                message.ErrorMessage = ex.Message;
                message.RetryCount++;
                message.NextRetryAtUtc = message.RetryCount < _options.MaxRetries
                    ? CalculateNextRetry(message.RetryCount)
                    : null;
                failureCount++;
            }
        }

        // Save changes
        await dbContext.SaveChangesAsync(cancellationToken);

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
