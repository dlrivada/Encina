using Encina.Cdc.Abstractions;
using Encina.Cdc.Processing;
using Shouldly;

namespace Encina.UnitTests.Cdc.Sharding;

/// <summary>
/// Unit tests for <see cref="InMemoryShardedCdcPositionStore"/>.
/// Verifies CRUD operations, case-insensitive key handling, concurrent access,
/// and composite key (shardId, connectorId) independence.
/// </summary>
public sealed class InMemoryShardedCdcPositionStoreTests
{
    #region GetPositionAsync

    [Fact]
    public async Task GetPositionAsync_NoSavedPosition_ReturnsNone()
    {
        var store = new InMemoryShardedCdcPositionStore();

        var result = await store.GetPositionAsync("shard-1", "connector-1");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => default);
        option.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetPositionAsync_NullShardId_ThrowsArgumentException()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.GetPositionAsync(null!, "connector-1"));
    }

    [Fact]
    public async Task GetPositionAsync_EmptyShardId_ThrowsArgumentException()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.GetPositionAsync("", "connector-1"));
    }

    [Fact]
    public async Task GetPositionAsync_WhitespaceShardId_ThrowsArgumentException()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.GetPositionAsync("   ", "connector-1"));
    }

    [Fact]
    public async Task GetPositionAsync_NullConnectorId_ThrowsArgumentException()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.GetPositionAsync("shard-1", null!));
    }

    [Fact]
    public async Task GetPositionAsync_EmptyConnectorId_ThrowsArgumentException()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.GetPositionAsync("shard-1", ""));
    }

    #endregion

    #region SavePositionAsync

    [Fact]
    public async Task SavePositionAsync_ThenGetPositionAsync_ReturnsSavedPosition()
    {
        var store = new InMemoryShardedCdcPositionStore();
        var position = new TestCdcPosition(100);

        await store.SavePositionAsync("shard-1", "connector-1", position);
        var result = await store.GetPositionAsync("shard-1", "connector-1");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => default);
        option.IsSome.ShouldBeTrue();
        option.IfSome(p => ((TestCdcPosition)p).Value.ShouldBe(100));
    }

    [Fact]
    public async Task SavePositionAsync_OverwritesPreviousPosition()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await store.SavePositionAsync("shard-1", "connector-1", new TestCdcPosition(10));
        await store.SavePositionAsync("shard-1", "connector-1", new TestCdcPosition(20));

        var result = await store.GetPositionAsync("shard-1", "connector-1");
        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => default);
        option.IfSome(p => ((TestCdcPosition)p).Value.ShouldBe(20));
    }

    [Fact]
    public async Task SavePositionAsync_NullShardId_ThrowsArgumentException()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.SavePositionAsync(null!, "connector-1", new TestCdcPosition(1)));
    }

    [Fact]
    public async Task SavePositionAsync_NullConnectorId_ThrowsArgumentException()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.SavePositionAsync("shard-1", null!, new TestCdcPosition(1)));
    }

    [Fact]
    public async Task SavePositionAsync_NullPosition_ThrowsArgumentNullException()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await Should.ThrowAsync<ArgumentNullException>(
            () => store.SavePositionAsync("shard-1", "connector-1", null!));
    }

    #endregion

    #region DeletePositionAsync

    [Fact]
    public async Task DeletePositionAsync_RemovesPosition()
    {
        var store = new InMemoryShardedCdcPositionStore();
        await store.SavePositionAsync("shard-1", "connector-1", new TestCdcPosition(10));

        var deleteResult = await store.DeletePositionAsync("shard-1", "connector-1");

        deleteResult.IsRight.ShouldBeTrue();
        var getResult = await store.GetPositionAsync("shard-1", "connector-1");
        getResult.IsRight.ShouldBeTrue();
        var option = getResult.Match(Right: o => o, Left: _ => default);
        option.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task DeletePositionAsync_NonExistentKey_ReturnsSuccess()
    {
        var store = new InMemoryShardedCdcPositionStore();

        var result = await store.DeletePositionAsync("non-existent", "connector-1");

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DeletePositionAsync_NullShardId_ThrowsArgumentException()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.DeletePositionAsync(null!, "connector-1"));
    }

    [Fact]
    public async Task DeletePositionAsync_NullConnectorId_ThrowsArgumentException()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.DeletePositionAsync("shard-1", null!));
    }

    #endregion

    #region GetAllPositionsAsync

    [Fact]
    public async Task GetAllPositionsAsync_NoPositions_ReturnsEmptyDictionary()
    {
        var store = new InMemoryShardedCdcPositionStore();

        var result = await store.GetAllPositionsAsync("connector-1");

        result.IsRight.ShouldBeTrue();
        result.IfRight(positions => positions.Count.ShouldBe(0));
    }

    [Fact]
    public async Task GetAllPositionsAsync_ReturnsAllShardsForConnector()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await store.SavePositionAsync("shard-1", "connector-1", new TestCdcPosition(100));
        await store.SavePositionAsync("shard-2", "connector-1", new TestCdcPosition(200));
        await store.SavePositionAsync("shard-3", "connector-1", new TestCdcPosition(300));

        var result = await store.GetAllPositionsAsync("connector-1");

        result.IsRight.ShouldBeTrue();
        result.IfRight(positions =>
        {
            positions.Count.ShouldBe(3);
            ((TestCdcPosition)positions["SHARD-1"]).Value.ShouldBe(100);
            ((TestCdcPosition)positions["SHARD-2"]).Value.ShouldBe(200);
            ((TestCdcPosition)positions["SHARD-3"]).Value.ShouldBe(300);
        });
    }

    [Fact]
    public async Task GetAllPositionsAsync_ExcludesOtherConnectors()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await store.SavePositionAsync("shard-1", "connector-a", new TestCdcPosition(100));
        await store.SavePositionAsync("shard-2", "connector-b", new TestCdcPosition(200));

        var result = await store.GetAllPositionsAsync("connector-a");

        result.IsRight.ShouldBeTrue();
        result.IfRight(positions =>
        {
            positions.Count.ShouldBe(1);
            positions.ContainsKey("SHARD-1").ShouldBeTrue();
        });
    }

    [Fact]
    public async Task GetAllPositionsAsync_NullConnectorId_ThrowsArgumentException()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await Should.ThrowAsync<ArgumentException>(
            () => store.GetAllPositionsAsync(null!));
    }

    #endregion

    #region Case-Insensitive Keys

    [Fact]
    public async Task CaseInsensitive_ShardId_SaveAndGet()
    {
        var store = new InMemoryShardedCdcPositionStore();
        await store.SavePositionAsync("MyShardId", "connector-1", new TestCdcPosition(42));

        var result = await store.GetPositionAsync("myshardid", "connector-1");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => default);
        option.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task CaseInsensitive_ConnectorId_SaveAndGet()
    {
        var store = new InMemoryShardedCdcPositionStore();
        await store.SavePositionAsync("shard-1", "MyConnector", new TestCdcPosition(42));

        var result = await store.GetPositionAsync("shard-1", "myconnector");

        result.IsRight.ShouldBeTrue();
        var option = result.Match(Right: o => o, Left: _ => default);
        option.IsSome.ShouldBeTrue();
    }

    [Fact]
    public async Task CaseInsensitive_GetAllPositions_MatchesConnectorId()
    {
        var store = new InMemoryShardedCdcPositionStore();
        await store.SavePositionAsync("shard-1", "MyConnector", new TestCdcPosition(42));

        var result = await store.GetAllPositionsAsync("myconnector");

        result.IsRight.ShouldBeTrue();
        result.IfRight(positions => positions.Count.ShouldBe(1));
    }

    [Fact]
    public async Task CaseInsensitive_Delete_RemovesPosition()
    {
        var store = new InMemoryShardedCdcPositionStore();
        await store.SavePositionAsync("SHARD-1", "CONNECTOR-1", new TestCdcPosition(42));

        await store.DeletePositionAsync("shard-1", "connector-1");

        var result = await store.GetPositionAsync("Shard-1", "Connector-1");
        result.IsRight.ShouldBeTrue();
        result.IfRight(option => option.IsNone.ShouldBeTrue());
    }

    #endregion

    #region Composite Key Independence

    [Fact]
    public async Task CompositeKey_DifferentShards_SameConnector_AreIndependent()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await store.SavePositionAsync("shard-a", "connector-1", new TestCdcPosition(100));
        await store.SavePositionAsync("shard-b", "connector-1", new TestCdcPosition(200));

        var resultA = await store.GetPositionAsync("shard-a", "connector-1");
        var resultB = await store.GetPositionAsync("shard-b", "connector-1");

        resultA.IsRight.ShouldBeTrue();
        resultB.IsRight.ShouldBeTrue();

        resultA.IfRight(opt => opt.IfSome(p => ((TestCdcPosition)p).Value.ShouldBe(100)));
        resultB.IfRight(opt => opt.IfSome(p => ((TestCdcPosition)p).Value.ShouldBe(200)));
    }

    [Fact]
    public async Task CompositeKey_SameShard_DifferentConnectors_AreIndependent()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await store.SavePositionAsync("shard-1", "connector-a", new TestCdcPosition(100));
        await store.SavePositionAsync("shard-1", "connector-b", new TestCdcPosition(200));

        var resultA = await store.GetPositionAsync("shard-1", "connector-a");
        var resultB = await store.GetPositionAsync("shard-1", "connector-b");

        resultA.IsRight.ShouldBeTrue();
        resultB.IsRight.ShouldBeTrue();

        resultA.IfRight(opt => opt.IfSome(p => ((TestCdcPosition)p).Value.ShouldBe(100)));
        resultB.IfRight(opt => opt.IfSome(p => ((TestCdcPosition)p).Value.ShouldBe(200)));
    }

    [Fact]
    public async Task CompositeKey_DeleteOneShard_DoesNotAffectOther()
    {
        var store = new InMemoryShardedCdcPositionStore();

        await store.SavePositionAsync("shard-a", "connector-1", new TestCdcPosition(100));
        await store.SavePositionAsync("shard-b", "connector-1", new TestCdcPosition(200));

        await store.DeletePositionAsync("shard-a", "connector-1");

        var resultA = await store.GetPositionAsync("shard-a", "connector-1");
        var resultB = await store.GetPositionAsync("shard-b", "connector-1");

        resultA.IfRight(opt => opt.IsNone.ShouldBeTrue());
        resultB.IfRight(opt => opt.IsSome.ShouldBeTrue());
    }

    #endregion

    #region Concurrent Access

    [Fact]
    public async Task ConcurrentSaves_DifferentKeys_AllSucceed()
    {
        var store = new InMemoryShardedCdcPositionStore();
        var tasks = new List<Task>();

        for (var i = 0; i < 100; i++)
        {
            var shardId = $"shard-{i}";
            var position = new TestCdcPosition(i);
            tasks.Add(store.SavePositionAsync(shardId, "connector-1", position));
        }

        await Task.WhenAll(tasks);

        var result = await store.GetAllPositionsAsync("connector-1");
        result.IsRight.ShouldBeTrue();
        result.IfRight(positions => positions.Count.ShouldBe(100));
    }

    [Fact]
    public async Task ConcurrentSaveAndGet_SameKey_DoesNotCorrupt()
    {
        var store = new InMemoryShardedCdcPositionStore();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var tasks = new List<Task>();

        // Writer task
        tasks.Add(Task.Run(async () =>
        {
            for (var i = 0; i < 1000 && !cts.Token.IsCancellationRequested; i++)
            {
                await store.SavePositionAsync("shard-1", "connector-1", new TestCdcPosition(i));
            }
        }));

        // Reader tasks
        for (var r = 0; r < 5; r++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (var i = 0; i < 200 && !cts.Token.IsCancellationRequested; i++)
                {
                    var result = await store.GetPositionAsync("shard-1", "connector-1");
                    result.IsRight.ShouldBeTrue();
                }
            }));
        }

        await Task.WhenAll(tasks);
    }

    #endregion
}
