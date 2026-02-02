using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.Audit;

/// <summary>
/// Background service that automatically purges old audit entries based on retention policy.
/// </summary>
/// <remarks>
/// <para>
/// This service runs periodically to remove audit entries older than the configured
/// <see cref="AuditOptions.RetentionDays"/>. The purge interval is controlled by
/// <see cref="AuditOptions.PurgeIntervalHours"/>.
/// </para>
/// <para>
/// <b>Activation:</b>
/// The service only runs when <see cref="AuditOptions.EnableAutoPurge"/> is <c>true</c>.
/// If disabled, the service exits immediately after startup.
/// </para>
/// <para>
/// <b>Error Handling:</b>
/// Exceptions during purge operations are logged but do not crash the service.
/// The service continues running and will retry on the next scheduled interval.
/// </para>
/// <para>
/// <b>Considerations:</b>
/// <list type="bullet">
/// <item>Schedule purges during off-peak hours to minimize performance impact</item>
/// <item>Monitor purge operations via logs to ensure retention policy is enforced</item>
/// <item>Large purge operations may take time; the service waits for completion before scheduling next run</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Enable automatic purging in options
/// services.AddEncinaAudit(options =>
/// {
///     options.EnableAutoPurge = true;
///     options.PurgeIntervalHours = 24;
///     options.RetentionDays = 2555; // 7 years
/// });
/// </code>
/// </example>
public sealed class AuditRetentionService : BackgroundService
{
    private readonly IAuditStore _auditStore;
    private readonly AuditOptions _options;
    private readonly ILogger<AuditRetentionService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditRetentionService"/> class.
    /// </summary>
    /// <param name="auditStore">The audit store to purge entries from.</param>
    /// <param name="options">The audit options containing retention configuration.</param>
    /// <param name="logger">The logger for recording purge operations.</param>
    /// <param name="timeProvider">Optional time provider for testability. Defaults to system time.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public AuditRetentionService(
        IAuditStore auditStore,
        IOptions<AuditOptions> options,
        ILogger<AuditRetentionService> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(auditStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _auditStore = auditStore;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableAutoPurge)
        {
            Log.AuditRetentionServiceDisabled(_logger);
            return;
        }

        Log.AuditRetentionServiceStarted(_logger, _options.PurgeIntervalHours, _options.RetentionDays);

        var interval = TimeSpan.FromHours(_options.PurgeIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, _timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected when service is stopping
                break;
            }

            await ExecutePurgeAsync(stoppingToken).ConfigureAwait(false);
        }

        Log.AuditRetentionServiceStopped(_logger);
    }

    private async Task ExecutePurgeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cutoffDate = _timeProvider.GetUtcNow().DateTime.AddDays(-_options.RetentionDays);

            Log.AuditRetentionPurgeStarted(_logger, cutoffDate);

            var result = await _auditStore.PurgeEntriesAsync(cutoffDate, cancellationToken).ConfigureAwait(false);

            result.Match(
                Right: count =>
                {
                    if (count > 0)
                    {
                        Log.AuditRetentionPurgeCompleted(_logger, count, cutoffDate);
                    }
                    else
                    {
                        Log.AuditRetentionNothingToPurge(_logger);
                    }
                },
                Left: error =>
                {
                    Log.AuditRetentionPurgeFailed(_logger, error.Message);
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Log.AuditRetentionPurgeCancelled(_logger);
        }
        catch (Exception ex)
        {
            Log.AuditRetentionPurgeError(_logger, ex);
        }
    }
}
