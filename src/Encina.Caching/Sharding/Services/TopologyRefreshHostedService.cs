using Encina.Caching.Sharding.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Caching.Sharding.Services;

/// <summary>
/// Background service that periodically refreshes the cached shard topology
/// and optionally the directory store L1 cache.
/// </summary>
/// <remarks>
/// <para>
/// This service uses <see cref="PeriodicTimer"/> to invoke refresh operations at the
/// interval configured in <see cref="ShardingCacheOptions.TopologyRefreshInterval"/>.
/// </para>
/// <para>
/// On each tick, the service:
/// <list type="bullet">
///   <item><description>Calls <see cref="CachedShardTopologyProvider.RefreshAsync"/> to reload topology from the source.</description></item>
///   <item><description>If directory caching is enabled, calls <see cref="CachedShardDirectoryStore.RefreshL1FromInnerAsync"/> to reload mappings.</description></item>
/// </list>
/// </para>
/// <para>
/// All exceptions are caught and logged to prevent the service from crashing.
/// </para>
/// </remarks>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
public sealed class TopologyRefreshHostedService : IHostedService, IDisposable
{
    private readonly CachedShardTopologyProvider _topologyProvider;
    private readonly CachedShardDirectoryStore? _directoryStore;
    private readonly ShardingCacheOptions _options;
    private readonly ILogger<TopologyRefreshHostedService> _logger;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _executingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="TopologyRefreshHostedService"/> class.
    /// </summary>
    /// <param name="topologyProvider">The cached topology provider to refresh.</param>
    /// <param name="options">The sharding cache configuration options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="directoryStore">Optional cached directory store to refresh.</param>
    public TopologyRefreshHostedService(
        CachedShardTopologyProvider topologyProvider,
        IOptions<ShardingCacheOptions> options,
        ILogger<TopologyRefreshHostedService> logger,
        CachedShardDirectoryStore? directoryStore = null)
    {
        ArgumentNullException.ThrowIfNull(topologyProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _topologyProvider = topologyProvider;
        _directoryStore = directoryStore;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting topology refresh service with interval {Interval}",
            _options.TopologyRefreshInterval);

        // Subscribe to directory invalidation channel if applicable
        if (_directoryStore is not null)
        {
            await _directoryStore.SubscribeToInvalidationsAsync(cancellationToken).ConfigureAwait(false);
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timer = new PeriodicTimer(_options.TopologyRefreshInterval);
        _executingTask = ExecuteAsync(_cts.Token);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping topology refresh service");

        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }

        if (_executingTask is not null)
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken))
                .ConfigureAwait(false);
        }

        // Unsubscribe from directory invalidation channel
        if (_directoryStore is not null)
        {
            await _directoryStore.UnsubscribeFromInvalidationsAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Disposes the hosted service resources.
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
        _cts?.Dispose();
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_timer is null || !await _timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await _topologyProvider.RefreshAsync(stoppingToken).ConfigureAwait(false);

                if (_directoryStore is not null && _options.EnableDirectoryCaching)
                {
                    await _directoryStore.RefreshL1FromInnerAsync(stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during topology/directory refresh cycle");
            }
        }
    }
}
#pragma warning restore CA1848
