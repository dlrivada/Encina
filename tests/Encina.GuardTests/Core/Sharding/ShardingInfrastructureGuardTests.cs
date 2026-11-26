using Encina.Sharding;
using Encina.Sharding.ReferenceTables;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Core.Sharding;

/// <summary>
/// Guard clause tests for <see cref="ShardKeyExtractor"/> and <see cref="PollingRefreshDetector"/> (#3720).
/// </summary>
public sealed class ShardingInfrastructureGuardTests
{
    private readonly IReferenceTableReplicator _replicator = Substitute.For<IReferenceTableReplicator>();
    private readonly IReferenceTableStoreFactory _storeFactory = Substitute.For<IReferenceTableStoreFactory>();
    private readonly IShardTopologyProvider _topologyProvider = Substitute.For<IShardTopologyProvider>();
    private readonly IReferenceTableStateStore _stateStore = Substitute.For<IReferenceTableStateStore>();
    private readonly ILogger<PollingRefreshDetector> _logger = NullLogger<PollingRefreshDetector>.Instance;

    private PollingRefreshDetector CreateDetector() =>
        new(_replicator, _storeFactory, _topologyProvider, _stateStore, _logger);

    [Fact]
    public void ShardKeyExtractor_Extract_NullEntity_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() => ShardKeyExtractor.Extract<string>(null!))
            .ParamName.ShouldBe("entity");

    [Fact]
    public void PollingRefreshDetector_Constructor_NullReplicator_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            new PollingRefreshDetector(null!, _storeFactory, _topologyProvider, _stateStore, _logger));

    [Fact]
    public void PollingRefreshDetector_Constructor_NullStoreFactory_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            new PollingRefreshDetector(_replicator, null!, _topologyProvider, _stateStore, _logger));

    [Fact]
    public void PollingRefreshDetector_Constructor_NullTopologyProvider_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            new PollingRefreshDetector(_replicator, _storeFactory, null!, _stateStore, _logger));

    [Fact]
    public void PollingRefreshDetector_Constructor_NullStateStore_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            new PollingRefreshDetector(_replicator, _storeFactory, _topologyProvider, null!, _logger));

    [Fact]
    public void PollingRefreshDetector_Constructor_NullLogger_ThrowsArgumentNullException() =>
        Should.Throw<ArgumentNullException>(() =>
            new PollingRefreshDetector(_replicator, _storeFactory, _topologyProvider, _stateStore, null!));

    [Fact]
    public async Task PollingRefreshDetector_CheckAndReplicateAsync_NullConfig_ThrowsArgumentNullException() =>
        await Should.ThrowAsync<ArgumentNullException>(() => CreateDetector().CheckAndReplicateAsync(null!));
}
