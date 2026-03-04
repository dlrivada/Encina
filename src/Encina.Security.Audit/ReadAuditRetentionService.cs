using System.Diagnostics;
using Encina.Security.Audit.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Security.Audit;

/// <summary>
/// Background service that automatically purges old read audit entries based on retention policy.
/// </summary>
/// <remarks>
/// <para>
/// This service runs periodically to remove read audit entries older than the configured
/// <see cref="ReadAuditOptions.RetentionDays"/>. The purge interval is controlled by
/// <see cref="ReadAuditOptions.PurgeIntervalHours"/>.
/// </para>
/// <para>
/// <b>Activation:</b>
/// The service only runs when <see cref="ReadAuditOptions.EnableAutoPurge"/> is <c>true</c>.
/// If disabled, the service exits immediately after startup.
/// </para>
/// <para>
/// <b>Error Handling:</b>
/// Exceptions during purge operations are logged but do not crash the service.
/// The service continues running and will retry on the next scheduled interval.
/// </para>
/// <para>
/// <b>Observability:</b>
/// Emits OpenTelemetry traces via <see cref="ReadAuditActivitySource"/> and
/// metrics via <see cref="ReadAuditMeter"/> for monitoring purge performance
/// and retention enforcement.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaReadAuditing(options =>
/// {
///     options.EnableAutoPurge = true;
///     options.PurgeIntervalHours = 24;
///     options.RetentionDays = 2555; // 7 years for SOX
/// });
/// </code>
/// </example>
public sealed class ReadAuditRetentionService : BackgroundService
{
    private readonly IReadAuditStore _readAuditStore;
    private readonly ReadAuditOptions _options;
    private readonly ILogger<ReadAuditRetentionService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadAuditRetentionService"/> class.
    /// </summary>
    /// <param name="readAuditStore">The read audit store to purge entries from.</param>
    /// <param name="options">The read audit options containing retention configuration.</param>
    /// <param name="logger">The logger for recording purge operations.</param>
    /// <param name="timeProvider">Optional time provider for testability. Defaults to system time.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
    public ReadAuditRetentionService(
        IReadAuditStore readAuditStore,
        IOptions<ReadAuditOptions> options,
        ILogger<ReadAuditRetentionService> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(readAuditStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _readAuditStore = readAuditStore;
        _options = options.Value;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableAutoPurge)
        {
            ReadAuditLog.RetentionServiceDisabled(_logger);
            return;
        }

        ReadAuditLog.RetentionServiceStarted(_logger, _options.PurgeIntervalHours, _options.RetentionDays);

        var interval = TimeSpan.FromHours(_options.PurgeIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, _timeProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ExecutePurgeAsync(stoppingToken).ConfigureAwait(false);
        }

        ReadAuditLog.RetentionServiceStopped(_logger);
    }

    private async Task ExecutePurgeAsync(CancellationToken cancellationToken)
    {
        var activity = ReadAuditActivitySource.StartPurge();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var cutoffDate = _timeProvider.GetUtcNow().AddDays(-_options.RetentionDays);

            ReadAuditLog.PurgeStarted(_logger, cutoffDate);

            var result = await _readAuditStore.PurgeEntriesAsync(cutoffDate, cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();
            ReadAuditMeter.PurgeDuration.Record(stopwatch.Elapsed.TotalMilliseconds);

            result.Match(
                Right: count =>
                {
                    if (count > 0)
                    {
                        ReadAuditLog.PurgeCompleted(_logger, count, cutoffDate);
                        ReadAuditMeter.EntriesPurgedTotal.Add(count);
                    }
                    else
                    {
                        ReadAuditLog.NothingToPurge(_logger);
                    }

                    ReadAuditActivitySource.Complete(activity);
                },
                Left: error =>
                {
                    ReadAuditLog.PurgeFailed(_logger, error.Message);
                    ReadAuditActivitySource.Failed(activity, error.Message);
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ReadAuditLog.PurgeCancelled(_logger);
            ReadAuditActivitySource.Failed(activity, "Cancelled");
        }
        catch (Exception ex)
        {
            ReadAuditLog.PurgeError(_logger, ex);
            ReadAuditActivitySource.Failed(activity, ex.Message);
        }
    }
}
