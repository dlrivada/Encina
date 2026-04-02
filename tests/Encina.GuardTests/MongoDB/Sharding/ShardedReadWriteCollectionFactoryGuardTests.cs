using Encina.MongoDB.ReadWriteSeparation;
using Encina.MongoDB.Sharding;
using Encina.Sharding;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Sharding;

public class ShardedReadWriteCollectionFactoryGuardTests
{
    private static readonly IMongoClient Client = Substitute.For<IMongoClient>();
    private static readonly MongoReadWriteSeparationOptions RwOptions = new();
    private static readonly ShardTopology Topology = new([
        new ShardInfo("shard-0", "mongodb://shard0:27017/test", Weight: 1, IsActive: true)
    ]);

    #region Native constructor

    [Fact]
    public void NativeCtor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ShardedReadWriteMongoCollectionFactory(null!, "db", RwOptions));

    [Fact]
    public void NativeCtor_NullDbName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ShardedReadWriteMongoCollectionFactory(Client, null!, RwOptions));

    [Fact]
    public void NativeCtor_EmptyDbName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ShardedReadWriteMongoCollectionFactory(Client, "", RwOptions));

    [Fact]
    public void NativeCtor_WhitespaceDbName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ShardedReadWriteMongoCollectionFactory(Client, "  ", RwOptions));

    [Fact]
    public void NativeCtor_NullRwOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ShardedReadWriteMongoCollectionFactory(Client, "db", (MongoReadWriteSeparationOptions)null!));

    #endregion

    #region AppLevel constructor

    [Fact]
    public void AppLevelCtor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ShardedReadWriteMongoCollectionFactory(null!, "db", Topology, RwOptions));

    [Fact]
    public void AppLevelCtor_NullDbName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ShardedReadWriteMongoCollectionFactory(Client, null!, Topology, RwOptions));

    [Fact]
    public void AppLevelCtor_NullTopology_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ShardedReadWriteMongoCollectionFactory(Client, "db", null!, RwOptions));

    [Fact]
    public void AppLevelCtor_NullRwOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ShardedReadWriteMongoCollectionFactory(Client, "db", Topology, null!));

    #endregion

    #region GetReadCollectionForShard

    [Fact]
    public void GetReadCollectionForShard_NullShardId_Throws()
    {
        var factory = new ShardedReadWriteMongoCollectionFactory(Client, "db", RwOptions);
        Should.Throw<ArgumentException>(() =>
            factory.GetReadCollectionForShard<TestEntity>(null!, "coll"));
    }

    [Fact]
    public void GetReadCollectionForShard_EmptyShardId_Throws()
    {
        var factory = new ShardedReadWriteMongoCollectionFactory(Client, "db", RwOptions);
        Should.Throw<ArgumentException>(() =>
            factory.GetReadCollectionForShard<TestEntity>("", "coll"));
    }

    [Fact]
    public void GetReadCollectionForShard_NullCollection_Throws()
    {
        var factory = new ShardedReadWriteMongoCollectionFactory(Client, "db", RwOptions);
        Should.Throw<ArgumentException>(() =>
            factory.GetReadCollectionForShard<TestEntity>("shard-0", null!));
    }

    [Fact]
    public void GetReadCollectionForShard_Disposed_Throws()
    {
        var factory = new ShardedReadWriteMongoCollectionFactory(Client, "db", RwOptions);
        factory.Dispose();
        Should.Throw<ObjectDisposedException>(() =>
            factory.GetReadCollectionForShard<TestEntity>("shard-0", "coll"));
    }

    #endregion

    #region GetWriteCollectionForShard

    [Fact]
    public void GetWriteCollectionForShard_NullShardId_Throws()
    {
        var factory = new ShardedReadWriteMongoCollectionFactory(Client, "db", RwOptions);
        Should.Throw<ArgumentException>(() =>
            factory.GetWriteCollectionForShard<TestEntity>(null!, "coll"));
    }

    [Fact]
    public void GetWriteCollectionForShard_NullCollection_Throws()
    {
        var factory = new ShardedReadWriteMongoCollectionFactory(Client, "db", RwOptions);
        Should.Throw<ArgumentException>(() =>
            factory.GetWriteCollectionForShard<TestEntity>("shard-0", null!));
    }

    [Fact]
    public void GetWriteCollectionForShard_Disposed_Throws()
    {
        var factory = new ShardedReadWriteMongoCollectionFactory(Client, "db", RwOptions);
        factory.Dispose();
        Should.Throw<ObjectDisposedException>(() =>
            factory.GetWriteCollectionForShard<TestEntity>("shard-0", "coll"));
    }

    #endregion

    #region Dispose idempotency

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var factory = new ShardedReadWriteMongoCollectionFactory(Client, "db", RwOptions);
        factory.Dispose();
        Should.NotThrow(() => factory.Dispose());
    }

    #endregion

    public class TestEntity { public Guid Id { get; set; } }
}
