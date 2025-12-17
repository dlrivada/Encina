using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleMediator.Messaging.Outbox;
using System.Text.Json;

namespace SimpleMediator.EntityFrameworkCore.Outbox;

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
    public OutboxProcessor(
        IServiceProvider serviceProvider,
        OutboxOptions options,
        ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Outbox Processor starting (Interval: {Interval}, BatchSize: {BatchSize}, MaxRetries: {MaxRetries})",
            _options.ProcessingInterval,
            _options.BatchSize,
            _options.MaxRetries);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_options.ProcessingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox Processor stopping");
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

        _logger.LogDebug("Processing {Count} pending outbox messages", pendingMessages.Count);

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
                    _logger.LogError(
                        "Cannot find type {NotificationType} for outbox message {MessageId}",
                        message.NotificationType,
                        message.Id);

                    message.ErrorMessage = $"Type not found: {message.NotificationType}";
                    message.RetryCount++;
                    message.NextRetryAtUtc = CalculateNextRetry(message.RetryCount);
                    failureCount++;
                    continue;
                }

                var notification = JsonSerializer.Deserialize(message.Content, notificationType, JsonOptions);
                if (notification == null)
                {
                    _logger.LogError(
                        "Failed to deserialize notification for outbox message {MessageId}",
                        message.Id);

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

                _logger.LogDebug(
                    "Published notification {NotificationType} from outbox message {MessageId}",
                    notificationType.Name,
                    message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing outbox message {MessageId}",
                    message.Id);

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
            _logger.LogInformation(
                "Processed {TotalCount} outbox messages (Success: {SuccessCount}, Failed: {FailureCount})",
                successCount + failureCount,
                successCount,
                failureCount);
        }
    }

    private DateTime CalculateNextRetry(int retryCount)
    {
        var delay = _options.BaseRetryDelay * Math.Pow(2, retryCount - 1);
        return DateTime.UtcNow.Add(delay);
    }
}
