using Encina.Sharding;
using Encina.Sharding.ReferenceTables;

namespace Encina.GuardTests.Core.Sharding;

/// <summary>
/// Guard clause tests for <see cref="PollingRefreshDetector"/>: null constructor arguments and a null
/// configuration passed to <see cref="PollingRefreshDetector.CheckAndReplicateAsync"/>.
/// </summary>
public sealed class PollingRefreshDetectorGuardTests
{
    private readonly IReferenceTableReplicator _replicator = Substitute.For<IReferenceTableReplicator>();
    private readonly IReferenceTableStoreFactory _storeFactory = Substitute.For<IReferenceTableStoreFactory>();
    private readonly IShardTopologyProvider _topologyProvider = Substitute.For<IShardTopologyProvider>();
    private readonly IReferenceTableStateStore _stateStore = Substitute.For<IReferenceTableStateStore>();
    private readonly ILogger<PollingRefreshDetector> _logger = NullLogger<PollingRefreshDetector>.Instance;

    [Fact]
    public void Constructor_NullReplicator_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() =>
            new PollingRefreshDetector(null!, _storeFactory, _topologyProvider, _stateStore, _logger))
            .ParamName.ShouldBe("replicator");

    [Fact]
    public void Constructor_NullStoreFactory_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() =>
            new PollingRefreshDetector(_replicator, null!, _topologyProvider, _stateStore, _logger))
            .ParamName.ShouldBe("storeFactory");

    [Fact]
    public void Constructor_NullTopologyProvider_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() =>
            new PollingRefreshDetector(_replicator, _storeFactory, null!, _stateStore, _logger))
            .ParamName.ShouldBe("topologyProvider");

    [Fact]
    public void Constructor_NullStateStore_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() =>
            new PollingRefreshDetector(_replicator, _storeFactory, _topologyProvider, null!, _logger))
            .ParamName.ShouldBe("stateStore");

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
        => Should.Throw<ArgumentNullException>(() =>
            new PollingRefreshDetector(_replicator, _storeFactory, _topologyProvider, _stateStore, null!))
            .ParamName.ShouldBe("logger");

    [Fact]
    public async Task CheckAndReplicateAsync_NullConfig_ThrowsArgumentNullException()
    {
        var detector = new PollingRefreshDetector(_replicator, _storeFactory, _topologyProvider, _stateStore, _logger);

        var exception = await Should.ThrowAsync<ArgumentNullException>(() => detector.CheckAndReplicateAsync(null!));

        exception.ParamName.ShouldBe("config");
    }

    [Fact]
    public async Task CheckAndReplicateAsync_EmptyTopology_ReturnsPrimaryShardNotFound()
    {
        _topologyProvider.GetTopology().Returns(new ShardTopology([]));
        var detector = new PollingRefreshDetector(_replicator, _storeFactory, _topologyProvider, _stateStore, _logger);
        var config = new ReferenceTableConfiguration(typeof(string), new ReferenceTableOptions());

        var result = await detector.CheckAndReplicateAsync(config);

        result.IsLeft.ShouldBeTrue();
        _storeFactory.DidNotReceiveWithAnyArgs().CreateForShard(default!);
    }
}
