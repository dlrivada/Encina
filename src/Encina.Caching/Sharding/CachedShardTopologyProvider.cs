using Encina.Caching.Sharding.Configuration;
using Encina.Sharding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Caching.Sharding;

/// <summary>
/// Cached implementation of <see cref="IShardTopologyProvider"/> that holds the current
/// topology in a <c>volatile</c> reference and supports atomic refresh from an
/// <see cref="IShardTopologySource"/>.
/// </summary>
/// <remarks>
/// <para>
/// The topology is immutable (uses <c>FrozenDictionary</c>). On refresh, a new
/// <see cref="ShardTopology"/> instance is constructed from the source and swapped
/// in atomically via <see cref="Interlocked.Exchange{T}(ref T, T)"/>.
/// </para>
/// <para>
/// Optionally subscribes to <see cref="IShardTopologyChangeNotifier.TopologyChanged"/>
/// for push-based invalidation. The distributed cache (<see cref="ICacheProvider"/>)
/// is used to coordinate topology state across application instances.
/// </para>
/// </remarks>
#pragma warning disable CA1848 // Use the LoggerMessage delegates
public sealed class CachedShardTopologyProvider : IShardTopologyProvider, IDisposable
{
    private readonly IShardTopologySource _source;
    private readonly ICacheProvider _cache;
    private readonly ShardingCacheOptions _options;
    private readonly ILogger<CachedShardTopologyProvider> _logger;
    private readonly IShardTopologyChangeNotifier? _notifier;
    private volatile ShardTopology _current;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedShardTopologyProvider"/> class.
    /// </summary>
    /// <param name="initial">The initial shard topology.</param>
    /// <param name="source">The topology source for refreshes.</param>
    /// <param name="cache">The distributed cache provider.</param>
    /// <param name="options">The sharding cache configuration options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="notifier">Optional push-based change notifier.</param>
    public CachedShardTopologyProvider(
        ShardTopology initial,
        IShardTopologySource source,
        ICacheProvider cache,
        IOptions<ShardingCacheOptions> options,
        ILogger<CachedShardTopologyProvider> logger,
        IShardTopologyChangeNotifier? notifier = null)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _current = initial;
        _source = source;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
        _notifier = notifier;

        if (_notifier is not null)
        {
            _notifier.TopologyChanged += OnTopologyChanged;
        }
    }

    /// <inheritdoc />
    public ShardTopology GetTopology() => _current;

    /// <summary>
    /// Refreshes the topology from the configured <see cref="IShardTopologySource"/>.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            var shards = await _source.LoadShardsAsync(cancellationToken).ConfigureAwait(false);
            var newTopology = new ShardTopology(shards);

            var previous = Interlocked.Exchange(ref _current, newTopology);

            // Cache the topology for cross-instance coordination
            var cacheKey = ShardCacheKeyGenerator.ForTopology("shard:topology");
            await _cache.SetAsync(
                cacheKey,
                newTopology.ActiveShardIds,
                _options.TopologyCacheDuration,
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug(
                "Topology refreshed: {PreviousCount} → {NewCount} active shards",
                previous.ActiveShardIds.Count,
                newTopology.ActiveShardIds.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown, don't log as error
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh shard topology");
        }
    }

    /// <summary>
    /// Disposes the provider and unsubscribes from change notifications.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_notifier is not null)
        {
            _notifier.TopologyChanged -= OnTopologyChanged;
        }

        _disposed = true;
    }

    private void OnTopologyChanged(object? sender, EventArgs e)
    {
        _logger.LogDebug("Topology change notification received, triggering refresh");

        // Fire-and-forget: the push notification triggers an immediate refresh
        _ = Task.Run(async () =>
        {
            try
            {
                await RefreshAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push-triggered topology refresh failed");
            }
        });
    }
}
#pragma warning restore CA1848
