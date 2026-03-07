using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.DeadLetter;

/// <summary>
/// Background service that periodically cleans up expired dead letter messages.
/// </summary>
public sealed class DeadLetterCleanupProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DeadLetterOptions _options;
    private readonly ILogger<DeadLetterCleanupProcessor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterCleanupProcessor"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory.</param>
    /// <param name="options">The DLQ options.</param>
    /// <param name="logger">The logger.</param>
    public DeadLetterCleanupProcessor(
        IServiceScopeFactory scopeFactory,
        DeadLetterOptions options,
        ILogger<DeadLetterCleanupProcessor> logger)
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
        if (!_options.EnableAutomaticCleanup || !_options.RetentionPeriod.HasValue)
        {
            DeadLetterLog.CleanupProcessorDisabled(_logger);
            return;
        }

        DeadLetterLog.CleanupProcessorRunning(_logger, _options.CleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.CleanupInterval, stoppingToken);

                await using var scope = _scopeFactory.CreateAsyncScope();
                var store = scope.ServiceProvider.GetRequiredService<IDeadLetterStore>();

                var deleteResult = await store.DeleteExpiredAsync(stoppingToken);

                deleteResult.Match(
                    Right: count =>
                    {
                        if (count > 0)
                        {
                            DeadLetterLog.ExpiredMessagesCleanedUp(_logger, count);
                        }
                    },
                    Left: error => DeadLetterLog.CleanupError(
                        _logger, new InvalidOperationException(error.Message)));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                DeadLetterLog.CleanupError(_logger, ex);
            }
        }
    }
}
