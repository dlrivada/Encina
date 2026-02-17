using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Sharding.ReferenceTables;

/// <summary>
/// Background service that orchestrates periodic polling-based change detection and
/// startup synchronization for registered reference tables.
/// </summary>
/// <remarks>
/// <para>
/// On startup, tables configured with <see cref="ReferenceTableOptions.SyncOnStartup"/> = <c>true</c>
/// are replicated immediately. After that, the service enters a polling loop that checks
/// each <see cref="RefreshStrategy.Polling"/> table at its configured interval.
/// </para>
/// <para>
/// Tables using <see cref="RefreshStrategy.CdcDriven"/> are handled by
/// <c>CdcDrivenRefreshHandler&lt;TEntity&gt;</c> outside this service. Tables using
/// <see cref="RefreshStrategy.Manual"/> are not automatically refreshed.
/// </para>
/// </remarks>
internal sealed class ReferenceTableReplicationService(
    IReferenceTableReplicator replicator,
    IReferenceTableRegistry registry,
    PollingRefreshDetector pollingDetector,
    IOptions<ReferenceTableGlobalOptions> globalOptions,
    ILogger<ReferenceTableReplicationService> logger) : BackgroundService
{
    private readonly IReferenceTableReplicator _replicator = replicator ?? throw new ArgumentNullException(nameof(replicator));
    private readonly IReferenceTableRegistry _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    private readonly PollingRefreshDetector _pollingDetector = pollingDetector ?? throw new ArgumentNullException(nameof(pollingDetector));
    private readonly ReferenceTableGlobalOptions _globalOptions = globalOptions?.Value ?? throw new ArgumentNullException(nameof(globalOptions));
    private readonly ILogger<ReferenceTableReplicationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configurations = _registry.GetAllConfigurations();

        if (configurations.Count == 0)
        {
            _logger.LogDebug("No reference tables registered — replication service will not start");
            return;
        }

        _logger.LogInformation(
            "Reference table replication service started with {TableCount} registered tables",
            configurations.Count);

        // Phase 1: Startup synchronization
        await RunStartupSyncAsync(configurations, stoppingToken).ConfigureAwait(false);

        // Phase 2: Polling loop for Polling-strategy tables
        var pollingConfigs = configurations
            .Where(c => c.Options.RefreshStrategy == RefreshStrategy.Polling)
            .ToList();

        if (pollingConfigs.Count == 0)
        {
            _logger.LogDebug("No polling-strategy reference tables — polling loop will not start");
            return;
        }

        // Track last poll time per table for individual intervals
        var lastPollTimes = pollingConfigs.ToDictionary(
            c => c.EntityType,
            _ => DateTime.MinValue);

        var consecutiveErrors = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                foreach (var config in pollingConfigs)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    var lastPoll = lastPollTimes[config.EntityType];
                    var interval = config.Options.PollingInterval;

                    if (now - lastPoll < interval)
                        continue;

                    lastPollTimes[config.EntityType] = now;

                    var result = await _pollingDetector
                        .CheckAndReplicateAsync(config, stoppingToken)
                        .ConfigureAwait(false);

                    result.Match(
                        Right: _ => { },
                        Left: error => _logger.LogWarning(
                            "Polling check failed for reference table '{EntityType}': {ErrorMessage}",
                            config.EntityType.Name,
                            error.Message));
                }

                consecutiveErrors = 0;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                consecutiveErrors++;

                var delay = CalculateRetryDelay(consecutiveErrors);

                _logger.LogWarning(
                    ex,
                    "Error in reference table polling loop (attempt {Attempt}) — retrying in {Delay}ms",
                    consecutiveErrors,
                    delay.TotalMilliseconds);

                await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                continue;
            }

            // Wait for the shortest configured interval before next check
            var shortestInterval = pollingConfigs.Min(c => c.Options.PollingInterval);
            await Task.Delay(shortestInterval, stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Reference table replication service stopped");
    }

    private async Task RunStartupSyncAsync(
        IReadOnlyCollection<ReferenceTableConfiguration> configurations,
        CancellationToken cancellationToken)
    {
        var syncOnStartupConfigs = configurations
            .Where(c => c.Options.SyncOnStartup)
            .ToList();

        if (syncOnStartupConfigs.Count == 0)
        {
            _logger.LogDebug("No reference tables configured for startup sync");
            return;
        }

        _logger.LogInformation(
            "Running startup synchronization for {TableCount} reference tables",
            syncOnStartupConfigs.Count);

        var result = await _replicator.ReplicateAllAsync(cancellationToken).ConfigureAwait(false);

        result.Match(
            Right: rep =>
            {
                if (rep.IsComplete)
                {
                    _logger.LogInformation(
                        "Startup sync completed: {RowsSynced} rows synced across {ShardCount} shards in {Duration}ms",
                        rep.RowsSynced,
                        rep.ShardResults.Count,
                        rep.Duration.TotalMilliseconds);
                }
                else if (rep.IsPartial)
                {
                    _logger.LogWarning(
                        "Startup sync completed with partial failures: {RowsSynced} rows synced, " +
                        "{FailedCount} shard failures",
                        rep.RowsSynced,
                        rep.FailedShards.Count);
                }
            },
            Left: error => _logger.LogError(
                "Startup sync failed: {ErrorMessage}",
                error.Message));
    }

    private static TimeSpan CalculateRetryDelay(int retryCount)
    {
        var delaySeconds = Math.Min(Math.Pow(2, retryCount - 1), 60);
        return TimeSpan.FromSeconds(delaySeconds);
    }
}
