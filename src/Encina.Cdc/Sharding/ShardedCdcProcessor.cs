using Encina.Cdc.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Cdc.Sharding;

/// <summary>
/// Background service that continuously streams and processes sharded CDC change events.
/// Consumes events from <see cref="IShardedCdcConnector"/> and dispatches them to registered
/// handlers via <see cref="ICdcDispatcher"/>, saving per-shard positions after successful dispatch.
/// </summary>
/// <remarks>
/// <para>
/// Follows the same poll-dispatch-save loop as <c>CdcProcessor</c> but operates on
/// <see cref="ShardedChangeEvent"/> instances from multiple shards:
/// <list type="number">
///   <item><description>Stream aggregated changes from all shards</description></item>
///   <item><description>Dispatch each event to the appropriate handler</description></item>
///   <item><description>Save the position per (shardId, connectorId) after successful processing</description></item>
/// </list>
/// </para>
/// <para>
/// Error handling uses exponential backoff retry with configurable
/// <see cref="CdcOptions.BaseRetryDelay"/> and <see cref="CdcOptions.MaxRetries"/>.
/// </para>
/// </remarks>
internal sealed class ShardedCdcProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ShardedCdcProcessor> _logger;
    private readonly CdcOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardedCdcProcessor"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving scoped services.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="options">Configuration options for CDC processing.</param>
    public ShardedCdcProcessor(
        IServiceProvider serviceProvider,
        ILogger<ShardedCdcProcessor> logger,
        CdcOptions options)
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
        if (!_options.Enabled)
        {
            CdcLog.ProcessorDisabled(_logger);
            return;
        }

        CdcLog.ShardedProcessorStarted(_logger, _options.PollingInterval, _options.BatchSize);

        var consecutiveErrors = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessShardedChangesAsync(stoppingToken).ConfigureAwait(false);
                consecutiveErrors = 0;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                consecutiveErrors++;

                var connectorId = GetConnectorId();

                if (consecutiveErrors <= _options.MaxRetries)
                {
                    var delay = CalculateRetryDelay(consecutiveErrors);
                    CdcLog.RetryingAfterError(_logger, ex, connectorId, consecutiveErrors, _options.MaxRetries, delay);
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
                else
                {
                    CdcLog.ErrorProcessingChangeEvents(_logger, ex, connectorId);
                    consecutiveErrors = 0;
                    await Task.Delay(_options.PollingInterval, stoppingToken).ConfigureAwait(false);
                }
            }

            await Task.Delay(_options.PollingInterval, stoppingToken).ConfigureAwait(false);
        }

        CdcLog.ShardedProcessorStopped(_logger);
    }

    private async Task ProcessShardedChangesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var connector = scope.ServiceProvider.GetRequiredService<IShardedCdcConnector>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICdcDispatcher>();
        var positionStore = scope.ServiceProvider.GetRequiredService<IShardedCdcPositionStore>();

        var connectorId = connector.GetConnectorId();
        var successCount = 0;
        var failureCount = 0;
        var totalCount = 0;

        await foreach (var shardedResult in connector.StreamAllShardsAsync(cancellationToken).ConfigureAwait(false))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            totalCount++;

            if (shardedResult.IsRight)
            {
                var shardedEvent = (ShardedChangeEvent)shardedResult;

                var dispatchResult = await dispatcher.DispatchAsync(shardedEvent.Event, cancellationToken)
                    .ConfigureAwait(false);

                if (dispatchResult.IsRight)
                {
                    successCount++;

                    if (_options.EnablePositionTracking)
                    {
                        await positionStore.SavePositionAsync(
                            shardedEvent.ShardId,
                            connectorId,
                            shardedEvent.ShardPosition,
                            cancellationToken).ConfigureAwait(false);

                        CdcLog.ShardPositionSaved(
                            _logger,
                            shardedEvent.ShardId,
                            connectorId,
                            shardedEvent.ShardPosition.ToString());
                    }
                }
                else
                {
                    failureCount++;
                }
            }
            else
            {
                failureCount++;
            }

            if (totalCount >= _options.BatchSize)
            {
                break;
            }
        }

        if (totalCount > 0)
        {
            CdcLog.ShardedProcessedChangeEvents(_logger, successCount, totalCount, failureCount, connectorId);
        }
    }

    private string GetConnectorId()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var connector = scope.ServiceProvider.GetService<IShardedCdcConnector>();
            return connector?.GetConnectorId() ?? "unknown-sharded";
        }
        catch
        {
            return "unknown-sharded";
        }
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        var delay = _options.BaseRetryDelay * Math.Pow(2, retryCount - 1);
        return delay;
    }
}
