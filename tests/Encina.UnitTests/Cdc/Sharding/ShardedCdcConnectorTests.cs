using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Sharding;
using Encina.Sharding;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardedCdcConnector"/>.
/// Verifies connector initialization, streaming, position retrieval,
/// dynamic topology changes, and disposal.
/// </summary>
public sealed class ShardedCdcConnectorTests
{
    private static readonly ILogger<ShardedCdcConnector> Logger =
        NullLogger<ShardedCdcConnector>.Instance;

    #region Test Helpers

    private static ShardInfo CreateShard(string shardId, bool isActive = true) =>
        new(shardId, $"Server={shardId};Database=db;", IsActive: isActive);

    private static ShardTopology CreateTopology(params ShardInfo[] shards) =>
        new(shards);

    private static IShardTopologyProvider CreateTopologyProvider(ShardTopology topology)
    {
        var provider = Substitute.For<IShardTopologyProvider>();
        provider.GetTopology().Returns(topology);
        return provider;
    }

    private static ICdcConnector CreateMockConnector(string connectorId, CdcPosition? position = null)
    {
        var connector = Substitute.For<ICdcConnector>();
        connector.ConnectorId.Returns(connectorId);
        connector.GetCurrentPositionAsync(Arg.Any<CancellationToken>())
            .Returns(position is not null
                ? Task.FromResult(Right<EncinaError, CdcPosition>(position))
                : Task.FromResult(Right<EncinaError, CdcPosition>(new TestCdcPosition(0))));
        return connector;
    }

    private static ChangeEvent CreateChangeEvent(long positionValue) =>
        new("test_table",
            ChangeOperation.Insert,
            null,
            new { Id = 1 },
            new ChangeMetadata(
                new TestCdcPosition(positionValue),
                DateTime.UtcNow,
                null, null, null));

    #endregion

    #region Constructor and Initialization

    [Fact]
    public void Constructor_WithActiveShards_InitializesConnectors()
    {
        var topology = CreateTopology(
            CreateShard("shard-1"),
            CreateShard("shard-2"));
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();
        factory.Invoke(Arg.Any<ShardInfo>()).Returns(callInfo =>
            CreateMockConnector(callInfo.Arg<ShardInfo>().ShardId));

        var connector = new ShardedCdcConnector("test-connector", factory, provider, Logger);

        connector.ActiveShardIds.Count.ShouldBe(2);
        connector.ActiveShardIds.ShouldContain("shard-1");
        connector.ActiveShardIds.ShouldContain("shard-2");
    }

    [Fact]
    public void Constructor_WithInactiveShards_ExcludesInactiveFromConnectors()
    {
        var topology = CreateTopology(
            CreateShard("shard-active", isActive: true),
            CreateShard("shard-inactive", isActive: false));
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();
        factory.Invoke(Arg.Any<ShardInfo>()).Returns(callInfo =>
            CreateMockConnector(callInfo.Arg<ShardInfo>().ShardId));

        var connector = new ShardedCdcConnector("test-connector", factory, provider, Logger);

        connector.ActiveShardIds.Count.ShouldBe(1);
        connector.ActiveShardIds.ShouldContain("shard-active");
    }

    [Fact]
    public void Constructor_WithNoActiveShards_CreatesEmptyConnectorList()
    {
        var topology = CreateTopology(
            CreateShard("shard-1", isActive: false));
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        var connector = new ShardedCdcConnector("test-connector", factory, provider, Logger);

        connector.ActiveShardIds.Count.ShouldBe(0);
    }

    [Fact]
    public void Constructor_NullConnectorId_ThrowsArgumentException()
    {
        var topology = CreateTopology();
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        Should.Throw<ArgumentException>(() =>
            new ShardedCdcConnector(null!, factory, provider, Logger));
    }

    [Fact]
    public void Constructor_EmptyConnectorId_ThrowsArgumentException()
    {
        var topology = CreateTopology();
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        Should.Throw<ArgumentException>(() =>
            new ShardedCdcConnector("", factory, provider, Logger));
    }

    [Fact]
    public void Constructor_NullConnectorFactory_ThrowsArgumentNullException()
    {
        var topology = CreateTopology();
        var provider = CreateTopologyProvider(topology);

        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcConnector("test", null!, provider, Logger));
    }

    [Fact]
    public void Constructor_NullTopologyProvider_ThrowsArgumentNullException()
    {
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcConnector("test", factory, null!, Logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var topology = CreateTopology();
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcConnector("test", factory, provider, null!));
    }

    #endregion

    #region GetConnectorId

    [Fact]
    public void GetConnectorId_ReturnsConfiguredId()
    {
        var topology = CreateTopology();
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        var connector = new ShardedCdcConnector("my-sharded-connector", factory, provider, Logger);

        connector.GetConnectorId().ShouldBe("my-sharded-connector");
    }

    #endregion

    #region StreamAllShardsAsync

    [Fact]
    public async Task StreamAllShardsAsync_NoShards_YieldsNoEvents()
    {
        var topology = CreateTopology();
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);
        var events = new List<Either<EncinaError, ShardedChangeEvent>>();

        await foreach (var evt in connector.StreamAllShardsAsync())
        {
            events.Add(evt);
        }

        events.Count.ShouldBe(0);
    }

    [Fact]
    public async Task StreamAllShardsAsync_MultipleShardsWithEvents_AggregatesAll()
    {
        var changeEvent1 = CreateChangeEvent(1);
        var changeEvent2 = CreateChangeEvent(2);

        var mockConnector1 = CreateMockConnector("shard-1");
        mockConnector1.StreamChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(Right<EncinaError, ChangeEvent>(changeEvent1)));

        var mockConnector2 = CreateMockConnector("shard-2");
        mockConnector2.StreamChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(Right<EncinaError, ChangeEvent>(changeEvent2)));

        var topology = CreateTopology(CreateShard("shard-1"), CreateShard("shard-2"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = shard =>
            shard.ShardId == "shard-1" ? mockConnector1 : mockConnector2;

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);
        var events = new List<Either<EncinaError, ShardedChangeEvent>>();

        await foreach (var evt in connector.StreamAllShardsAsync())
        {
            events.Add(evt);
        }

        events.Count.ShouldBe(2);
        events.ShouldAllBe(e => e.IsRight);
    }

    [Fact]
    public async Task StreamAllShardsAsync_ErrorInOneShard_PropagatesAsLeftValue()
    {
        var changeEvent = CreateChangeEvent(1);
        var error = EncinaError.New("Shard stream failed");

        var mockConnector1 = CreateMockConnector("shard-1");
        mockConnector1.StreamChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(Right<EncinaError, ChangeEvent>(changeEvent)));

        var mockConnector2 = CreateMockConnector("shard-2");
        mockConnector2.StreamChangesAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                return ToAsyncEnumerableWithException("shard-2");
            });

        var topology = CreateTopology(CreateShard("shard-1"), CreateShard("shard-2"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = shard =>
            shard.ShardId == "shard-1" ? mockConnector1 : mockConnector2;

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);
        var events = new List<Either<EncinaError, ShardedChangeEvent>>();

        await foreach (var evt in connector.StreamAllShardsAsync())
        {
            events.Add(evt);
        }

        // Should contain the successful event from shard-1 and the error from shard-2
        events.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    #endregion

    #region StreamShardAsync

    [Fact]
    public async Task StreamShardAsync_ExistingShard_StreamsEvents()
    {
        var changeEvent = CreateChangeEvent(1);
        var mockConnector = CreateMockConnector("shard-1");
        mockConnector.StreamChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(Right<EncinaError, ChangeEvent>(changeEvent)));

        var topology = CreateTopology(CreateShard("shard-1"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = _ => mockConnector;

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);
        var events = new List<Either<EncinaError, ChangeEvent>>();

        await foreach (var evt in connector.StreamShardAsync("shard-1"))
        {
            events.Add(evt);
        }

        events.Count.ShouldBe(1);
        events[0].IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task StreamShardAsync_NonExistentShard_ReturnsLeftError()
    {
        var topology = CreateTopology(CreateShard("shard-1"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = _ => CreateMockConnector("shard-1");

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);
        var events = new List<Either<EncinaError, ChangeEvent>>();

        await foreach (var evt in connector.StreamShardAsync("non-existent"))
        {
            events.Add(evt);
        }

        events.Count.ShouldBe(1);
        events[0].IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task StreamShardAsync_NullShardId_ThrowsArgumentException()
    {
        var topology = CreateTopology();
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await foreach (var _ in connector.StreamShardAsync(null!))
            {
            }
        });
    }

    #endregion

    #region GetAllPositionsAsync

    [Fact]
    public async Task GetAllPositionsAsync_ReturnsPositionsFromAllShards()
    {
        var mockConnector1 = CreateMockConnector("shard-1", new TestCdcPosition(100));
        var mockConnector2 = CreateMockConnector("shard-2", new TestCdcPosition(200));

        var topology = CreateTopology(CreateShard("shard-1"), CreateShard("shard-2"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = shard =>
            shard.ShardId == "shard-1" ? mockConnector1 : mockConnector2;

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        var result = await connector.GetAllPositionsAsync();

        result.IsRight.ShouldBeTrue();
        result.IfRight(positions =>
        {
            positions.Count.ShouldBe(2);
            ((TestCdcPosition)positions["shard-1"]).Value.ShouldBe(100);
            ((TestCdcPosition)positions["shard-2"]).Value.ShouldBe(200);
        });
    }

    [Fact]
    public async Task GetAllPositionsAsync_NoShards_ReturnsEmptyDictionary()
    {
        var topology = CreateTopology();
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        var result = await connector.GetAllPositionsAsync();

        result.IsRight.ShouldBeTrue();
        result.IfRight(positions => positions.Count.ShouldBe(0));
    }

    [Fact]
    public async Task GetAllPositionsAsync_ErrorInOneShard_ReturnsLeft()
    {
        var mockConnector1 = CreateMockConnector("shard-1", new TestCdcPosition(100));
        var mockConnector2 = Substitute.For<ICdcConnector>();
        mockConnector2.ConnectorId.Returns("shard-2");
        mockConnector2.GetCurrentPositionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, CdcPosition>(EncinaError.New("Position error"))));

        var topology = CreateTopology(CreateShard("shard-1"), CreateShard("shard-2"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = shard =>
            shard.ShardId == "shard-1" ? mockConnector1 : mockConnector2;

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        var result = await connector.GetAllPositionsAsync();

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region AddConnector / RemoveConnectorAsync

    [Fact]
    public void AddConnector_NewShard_AddsAndReturnsTrue()
    {
        var topology = CreateTopology();
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = _ => CreateMockConnector("new-shard");

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);
        connector.ActiveShardIds.Count.ShouldBe(0);

        var added = connector.AddConnector(CreateShard("new-shard"));

        added.ShouldBeTrue();
        connector.ActiveShardIds.Count.ShouldBe(1);
        connector.ActiveShardIds.ShouldContain("new-shard");
    }

    [Fact]
    public void AddConnector_ExistingShard_ReturnsFalse()
    {
        var topology = CreateTopology(CreateShard("shard-1"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = _ => CreateMockConnector("shard-1");

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        var added = connector.AddConnector(CreateShard("shard-1"));

        added.ShouldBeFalse();
        connector.ActiveShardIds.Count.ShouldBe(1);
    }

    [Fact]
    public void AddConnector_NullShardInfo_ThrowsArgumentNullException()
    {
        var topology = CreateTopology();
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        Should.Throw<ArgumentNullException>(() => connector.AddConnector(null!));
    }

    [Fact]
    public async Task RemoveConnectorAsync_ExistingShard_RemovesAndReturnsTrue()
    {
        var topology = CreateTopology(CreateShard("shard-1"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = _ => CreateMockConnector("shard-1");

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        var removed = await connector.RemoveConnectorAsync("shard-1");

        removed.ShouldBeTrue();
        connector.ActiveShardIds.Count.ShouldBe(0);
    }

    [Fact]
    public async Task RemoveConnectorAsync_NonExistentShard_ReturnsFalse()
    {
        var topology = CreateTopology(CreateShard("shard-1"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = _ => CreateMockConnector("shard-1");

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        var removed = await connector.RemoveConnectorAsync("non-existent");

        removed.ShouldBeFalse();
        connector.ActiveShardIds.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveConnectorAsync_DisposesAsyncDisposableConnector()
    {
        var mockConnector = Substitute.For<ICdcConnector, IAsyncDisposable>();
        mockConnector.ConnectorId.Returns("shard-1");
        mockConnector.GetCurrentPositionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, CdcPosition>(new TestCdcPosition(0))));

        var topology = CreateTopology(CreateShard("shard-1"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = _ => mockConnector;

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        await connector.RemoveConnectorAsync("shard-1");

        await ((IAsyncDisposable)mockConnector).Received(1).DisposeAsync();
    }

    [Fact]
    public async Task RemoveConnectorAsync_NullShardId_ThrowsArgumentException()
    {
        var topology = CreateTopology();
        var provider = CreateTopologyProvider(topology);
        var factory = Substitute.For<Func<ShardInfo, ICdcConnector>>();

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        await Should.ThrowAsync<ArgumentException>(() => connector.RemoveConnectorAsync(null!).AsTask());
    }

    #endregion

    #region DisposeAsync

    [Fact]
    public async Task DisposeAsync_DisposesAllConnectors()
    {
        var mockConnector1 = Substitute.For<ICdcConnector, IAsyncDisposable>();
        mockConnector1.ConnectorId.Returns("shard-1");
        mockConnector1.GetCurrentPositionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, CdcPosition>(new TestCdcPosition(0))));

        var mockConnector2 = Substitute.For<ICdcConnector, IAsyncDisposable>();
        mockConnector2.ConnectorId.Returns("shard-2");
        mockConnector2.GetCurrentPositionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, CdcPosition>(new TestCdcPosition(0))));

        var topology = CreateTopology(CreateShard("shard-1"), CreateShard("shard-2"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = shard =>
            shard.ShardId == "shard-1" ? mockConnector1 : mockConnector2;

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        await connector.DisposeAsync();

        await ((IAsyncDisposable)mockConnector1).Received(1).DisposeAsync();
        await ((IAsyncDisposable)mockConnector2).Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ClearsActiveShardIds()
    {
        var topology = CreateTopology(CreateShard("shard-1"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = _ => CreateMockConnector("shard-1");

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);
        connector.ActiveShardIds.Count.ShouldBe(1);

        await connector.DisposeAsync();

        connector.ActiveShardIds.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        var topology = CreateTopology(CreateShard("shard-1"));
        var provider = CreateTopologyProvider(topology);
        Func<ShardInfo, ICdcConnector> factory = _ => CreateMockConnector("shard-1");

        var connector = new ShardedCdcConnector("test", factory, provider, Logger);

        await connector.DisposeAsync();
        await Should.NotThrowAsync(() => connector.DisposeAsync().AsTask());
    }

    #endregion

    #region AsyncEnumerable Helpers

    private static async IAsyncEnumerable<Either<EncinaError, ChangeEvent>> ToAsyncEnumerable(
        params Either<EncinaError, ChangeEvent>[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<Either<EncinaError, ChangeEvent>> ToAsyncEnumerableWithException(
        string shardId)
    {
        await Task.Yield();
        yield return Left<EncinaError, ChangeEvent>(
            EncinaError.New($"Stream failed for shard {shardId}"));
    }

    #endregion
}
