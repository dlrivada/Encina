using Encina.MongoDB.Sharding;
using Encina.Sharding;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Sharding;

public class ShardedHealthMonitorGuardTests
{
    private static readonly IMongoClient Client = Substitute.For<IMongoClient>();
    private static readonly IShardedMongoCollectionFactory Factory = Substitute.For<IShardedMongoCollectionFactory>();
    private static readonly ShardTopology Topology = new([
        new ShardInfo("shard-0", "mongodb://shard0:27017/test", Weight: 1, IsActive: true)
    ]);

    #region Constructor

    [Fact]
    public void Ctor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ShardedMongoDbDatabaseHealthMonitor(null!, Topology, Factory, true));

    [Fact]
    public void Ctor_NullTopology_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ShardedMongoDbDatabaseHealthMonitor(Client, null!, Factory, true));

    [Fact]
    public void Ctor_NullFactory_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ShardedMongoDbDatabaseHealthMonitor(Client, Topology, null!, true));

    #endregion

    #region CheckShardHealthAsync

    [Fact]
    public async Task CheckShardHealthAsync_NullShardId_Throws()
    {
        var monitor = new ShardedMongoDbDatabaseHealthMonitor(Client, Topology, Factory, true);
        await Should.ThrowAsync<ArgumentException>(
            () => monitor.CheckShardHealthAsync(null!));
    }

    [Fact]
    public async Task CheckShardHealthAsync_EmptyShardId_Throws()
    {
        var monitor = new ShardedMongoDbDatabaseHealthMonitor(Client, Topology, Factory, true);
        await Should.ThrowAsync<ArgumentException>(
            () => monitor.CheckShardHealthAsync(""));
    }

    [Fact]
    public async Task CheckShardHealthAsync_WhitespaceShardId_Throws()
    {
        var monitor = new ShardedMongoDbDatabaseHealthMonitor(Client, Topology, Factory, true);
        await Should.ThrowAsync<ArgumentException>(
            () => monitor.CheckShardHealthAsync("   "));
    }

    #endregion

    #region GetShardPoolStatistics

    [Fact]
    public void GetShardPoolStatistics_NullShardId_Throws()
    {
        var monitor = new ShardedMongoDbDatabaseHealthMonitor(Client, Topology, Factory, true);
        Should.Throw<ArgumentException>(() =>
            monitor.GetShardPoolStatistics(null!));
    }

    [Fact]
    public void GetShardPoolStatistics_EmptyShardId_Throws()
    {
        var monitor = new ShardedMongoDbDatabaseHealthMonitor(Client, Topology, Factory, true);
        Should.Throw<ArgumentException>(() =>
            monitor.GetShardPoolStatistics(""));
    }

    #endregion
}
