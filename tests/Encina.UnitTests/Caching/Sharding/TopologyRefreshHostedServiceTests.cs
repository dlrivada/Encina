using Encina.Caching;
using Encina.Caching.Sharding;
using Encina.Caching.Sharding.Configuration;
using Encina.Caching.Sharding.Services;
using Encina.Sharding;
using Encina.Sharding.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Caching.Sharding;

/// <summary>
/// Unit tests for <see cref="TopologyRefreshHostedService"/>: the refresh loop runs on a real
/// <see cref="PeriodicTimer"/> with a short interval and the tests wait on signals raised by the
/// refreshed dependencies, never on elapsed time.
/// </summary>
public sealed class TopologyRefreshHostedServiceTests
{
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(5);
    private static readonly TimeSpan SafetyTimeout = TimeSpan.FromSeconds(30);

    private readonly IShardTopologySource _source = Substitute.For<IShardTopologySource>();
    private readonly IShardDirectoryStore _innerDirectory = Substitute.For<IShardDirectoryStore>();
    private readonly ICacheProvider _cache = Substitute.For<ICacheProvider>();

    [Fact]
    public async Task StartAsync_RefreshesTheTopologyOnEveryTick()
    {
        // Arrange
        var refreshes = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        _source.LoadShardsAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref count) >= 2)
            {
                refreshes.TrySetResult();
            }

            return Task.FromResult<IEnumerable<ShardInfo>>([new ShardInfo("shard-1", "conn-1")]);
        });
        using var service = CreateService(new ShardingCacheOptions { TopologyRefreshInterval = Interval });

        // Act
        await service.StartAsync(CancellationToken.None);
        await refreshes.Task.WaitAsync(SafetyTimeout);
        await service.StopAsync(CancellationToken.None);

        // Assert
        count.ShouldBeGreaterThanOrEqualTo(2);
        _innerDirectory.DidNotReceive().GetAllMappings();
    }

    [Fact]
    public async Task StartAsync_WithDirectoryCachingEnabled_AlsoRefreshesTheDirectoryL1Cache()
    {
        // Arrange
        var refreshed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _source.LoadShardsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<ShardInfo>>([new ShardInfo("shard-1", "conn-1")]));
        _innerDirectory.GetAllMappings().Returns(_ =>
        {
            refreshed.TrySetResult();
            return new Dictionary<string, string> { ["key"] = "shard-1" };
        });
        var options = new ShardingCacheOptions { TopologyRefreshInterval = Interval, EnableDirectoryCaching = true };
        using var service = CreateService(options, CreateDirectoryStore());

        // Act
        await service.StartAsync(CancellationToken.None);
        await refreshed.Task.WaitAsync(SafetyTimeout);
        await service.StopAsync(CancellationToken.None);

        // Assert
        _innerDirectory.Received().GetAllMappings();
    }

    [Fact]
    public async Task StartAsync_WithDirectoryStoreButDirectoryCachingDisabled_DoesNotRefreshTheDirectory()
    {
        // Arrange
        var refreshes = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        _source.LoadShardsAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref count) >= 2)
            {
                refreshes.TrySetResult();
            }

            return Task.FromResult<IEnumerable<ShardInfo>>([new ShardInfo("shard-1", "conn-1")]);
        });
        var options = new ShardingCacheOptions { TopologyRefreshInterval = Interval, EnableDirectoryCaching = false };
        using var service = CreateService(options, CreateDirectoryStore());

        // Act
        await service.StartAsync(CancellationToken.None);
        await refreshes.Task.WaitAsync(SafetyTimeout);
        await service.StopAsync(CancellationToken.None);

        // Assert
        _innerDirectory.DidNotReceive().GetAllMappings();
    }

    [Fact]
    public async Task StopAsync_BeforeTheFirstTick_EndsTheLoopWithoutRefreshing()
    {
        // Arrange
        using var service = CreateService(new ShardingCacheOptions { TopologyRefreshInterval = TimeSpan.FromHours(1) });

        // Act
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None).WaitAsync(SafetyTimeout);

        // Assert
        await _source.DidNotReceive().LoadShardsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WithAlreadyCancelledToken_EndsTheLoopWithoutRefreshing()
    {
        // Arrange
        using var service = CreateService(new ShardingCacheOptions { TopologyRefreshInterval = Interval });
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        await service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None).WaitAsync(SafetyTimeout);

        // Assert
        await _source.DidNotReceive().LoadShardsAsync(Arg.Any<CancellationToken>());
    }

    private CachedShardDirectoryStore CreateDirectoryStore() =>
        new(_innerDirectory, _cache, Options.Create(new DirectoryCacheOptions()), NullLogger<CachedShardDirectoryStore>.Instance);

    private TopologyRefreshHostedService CreateService(ShardingCacheOptions options, CachedShardDirectoryStore? directoryStore = null)
    {
        var topology = new ShardTopology([new ShardInfo("shard-1", "conn-1")]);
        var provider = new CachedShardTopologyProvider(
            topology, _source, _cache, Options.Create(options), NullLogger<CachedShardTopologyProvider>.Instance);

        return new TopologyRefreshHostedService(
            provider, Options.Create(options), NullLogger<TopologyRefreshHostedService>.Instance, directoryStore);
    }
}
