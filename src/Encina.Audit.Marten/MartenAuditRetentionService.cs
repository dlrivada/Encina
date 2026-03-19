using Encina.Audit.Marten.Diagnostics;
using Encina.Security.Audit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Audit.Marten;

/// <summary>
/// Background service that periodically crypto-shreds audit entries older than the
/// configured retention period by destroying their temporal encryption keys.
/// </summary>
/// <remarks>
/// <para>
/// This service is only registered when <see cref="MartenAuditOptions.EnableAutoPurge"/> is <c>true</c>.
/// It runs at intervals defined by <see cref="MartenAuditOptions.PurgeIntervalHours"/>.
/// </para>
/// <para>
/// Unlike database-backed audit stores that DELETE old rows, this service destroys temporal
/// encryption keys via <see cref="IAuditStore.PurgeEntriesAsync"/>. The encrypted events
/// remain in the immutable Marten event store, but their PII fields become permanently
/// unreadable — achieving GDPR data minimization without breaking SOX/NIS2 integrity.
/// </para>
/// </remarks>
internal sealed class MartenAuditRetentionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MartenAuditOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MartenAuditRetentionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MartenAuditRetentionService"/> class.
    /// </summary>
    public MartenAuditRetentionService(
        IServiceProvider serviceProvider,
        IOptions<MartenAuditOptions> options,
        TimeProvider timeProvider,
        ILogger<MartenAuditRetentionService> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceProvider = serviceProvider;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retentionDays = (int)_options.RetentionPeriod.TotalDays;

        MartenAuditLog.RetentionServiceStarted(
            _logger,
            _options.PurgeIntervalHours,
            retentionDays);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.PurgeIntervalHours));

        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await ExecutePurgeCycleAsync(stoppingToken).ConfigureAwait(false);
        }

        MartenAuditLog.RetentionServiceStopped(_logger);
    }

    private async Task ExecutePurgeCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cutoffUtc = _timeProvider.GetUtcNow().UtcDateTime - _options.RetentionPeriod;

            using var scope = _serviceProvider.CreateScope();
            var auditStore = scope.ServiceProvider.GetRequiredService<IAuditStore>();

            var result = await auditStore.PurgeEntriesAsync(cutoffUtc, cancellationToken)
                .ConfigureAwait(false);

            result.Match(
                Right: destroyedCount =>
                {
                    MartenAuditLog.RetentionCycleCompleted(_logger, destroyedCount);
                },
                Left: error =>
                {
                    MartenAuditLog.RecordFailed(
                        _logger,
                        Guid.Empty,
                        $"Retention purge failed: {error.Message}",
                        null);
                });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown — do not log as error
        }
        catch (Exception ex)
        {
            MartenAuditLog.RetentionCycleFailed(_logger, ex);
        }
    }
}
