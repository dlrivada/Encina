using Encina.Caching;
using Encina.Caching.Sharding;
using Encina.Caching.Sharding.Configuration;
using Encina.Sharding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching.Sharding;

/// <summary>
/// Unit tests for <see cref="CachedShardTopologyProvider"/>.
/// </summary>
public sealed class CachedShardTopologyProviderTests : IDisposable
{
    private readonly ShardTopology _topology;
    private readonly IShardTopologySource _source;
    private readonly ICacheProvider _cache;
    private readonly IOptions<ShardingCacheOptions> _options;
    private readonly ILogger<CachedShardTopologyProvider> _logger;

    public CachedShardTopologyProviderTests()
    {
        _topology = new ShardTopology([
            new ShardInfo("shard-1", "conn-1"),
            new ShardInfo("shard-2", "conn-2")
        ]);
        _source = Substitute.For<IShardTopologySource>();
        _cache = Substitute.For<ICacheProvider>();
        _options = Options.Create(new ShardingCacheOptions());
        _logger = NullLogger<CachedShardTopologyProvider>.Instance;
    }

    public void Dispose()
    {
        // No resources to clean up; provider.Dispose() tested explicitly below
    }

    // ────────────────────────────────────────────────────────────
    //  Constructor — null guards
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullInitial_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardTopologyProvider(null!, _source, _cache, _options, _logger));
    }

    [Fact]
    public void Constructor_NullSource_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardTopologyProvider(_topology, null!, _cache, _options, _logger));
    }

    [Fact]
    public void Constructor_NullCache_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardTopologyProvider(_topology, _source, null!, _options, _logger));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardTopologyProvider(_topology, _source, _cache, null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new CachedShardTopologyProvider(_topology, _source, _cache, _options, null!));
    }

    // ────────────────────────────────────────────────────────────
    //  GetTopology — returns initial topology
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void GetTopology_ReturnsInitialTopology()
    {
        using var provider = new CachedShardTopologyProvider(
            _topology, _source, _cache, _options, _logger);

        var result = provider.GetTopology();

        result.ShouldBeSameAs(_topology);
    }

    // ────────────────────────────────────────────────────────────
    //  RefreshAsync — updates topology from source
    // ────────────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshAsync_UpdatesTopologyFromSource()
    {
        var newShards = new List<ShardInfo>
        {
            new("shard-1", "conn-1"),
            new("shard-2", "conn-2"),
            new("shard-3", "conn-3")
        };

        _source.LoadShardsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<ShardInfo>>(newShards));

        using var provider = new CachedShardTopologyProvider(
            _topology, _source, _cache, _options, _logger);

        await provider.RefreshAsync(CancellationToken.None);

        var refreshed = provider.GetTopology();
        refreshed.ShouldNotBeSameAs(_topology);
        refreshed.ActiveShardIds.Count.ShouldBe(3);
    }

    [Fact]
    public async Task RefreshAsync_WritesTopologyToCache()
    {
        _source.LoadShardsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<ShardInfo>>([new ShardInfo("shard-1", "conn-1")]));

        using var provider = new CachedShardTopologyProvider(
            _topology, _source, _cache, _options, _logger);

        await provider.RefreshAsync(CancellationToken.None);

        await _cache.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_SourceThrows_DoesNotThrow()
    {
        _source.LoadShardsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<ShardInfo>>(new InvalidOperationException("Source failed")));

        using var provider = new CachedShardTopologyProvider(
            _topology, _source, _cache, _options, _logger);

        await Should.NotThrowAsync(() => provider.RefreshAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_CancellationRequested_DoesNotThrow()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _source.LoadShardsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IEnumerable<ShardInfo>>(new OperationCanceledException(cts.Token)));

        using var provider = new CachedShardTopologyProvider(
            _topology, _source, _cache, _options, _logger);

        await Should.NotThrowAsync(() => provider.RefreshAsync(cts.Token));
    }

    // ────────────────────────────────────────────────────────────
    //  Notifier integration
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithNotifier_SubscribesToTopologyChanged()
    {
        var notifier = Substitute.For<IShardTopologyChangeNotifier>();

        using var provider = new CachedShardTopologyProvider(
            _topology, _source, _cache, _options, _logger, notifier);

        notifier.Received(1).TopologyChanged += Arg.Any<EventHandler>();
    }

    [Fact]
    public void Dispose_WithNotifier_UnsubscribesFromTopologyChanged()
    {
        var notifier = Substitute.For<IShardTopologyChangeNotifier>();

        var provider = new CachedShardTopologyProvider(
            _topology, _source, _cache, _options, _logger, notifier);

        provider.Dispose();

        notifier.Received(1).TopologyChanged -= Arg.Any<EventHandler>();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var notifier = Substitute.For<IShardTopologyChangeNotifier>();

        var provider = new CachedShardTopologyProvider(
            _topology, _source, _cache, _options, _logger, notifier);

        Should.NotThrow(() =>
        {
            provider.Dispose();
            provider.Dispose();
        });
    }

    // ────────────────────────────────────────────────────────────
    //  StaticShardTopologySource
    // ────────────────────────────────────────────────────────────

    [Fact]
    public void StaticShardTopologySource_NullTopology_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new StaticShardTopologySource(null!));
    }

    [Fact]
    public async Task StaticShardTopologySource_LoadShardsAsync_ReturnsTopologyShards()
    {
        var source = new StaticShardTopologySource(_topology);

        var shards = await source.LoadShardsAsync();

        shards.ShouldNotBeNull();
        shards.Count().ShouldBe(2);
    }
}
