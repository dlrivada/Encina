using Encina.MongoDB.Sharding;
using Encina.Sharding;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Sharding;

public class ShardedCollectionFactoryGuardTests
{
    private static readonly IMongoClient Client = Substitute.For<IMongoClient>();
    private static readonly ShardTopology Topology = new([
        new ShardInfo("shard-0", "mongodb://shard0:27017/test", Weight: 1, IsActive: true),
        new ShardInfo("shard-1", "mongodb://shard1:27017/test", Weight: 1, IsActive: true)
    ]);

    #region ShardedMongoCollectionFactory — Native constructor

    [Fact]
    public void NativeCtor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ShardedMongoCollectionFactory(null!, "db"));

    [Fact]
    public void NativeCtor_NullDbName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ShardedMongoCollectionFactory(Client, null!));

    [Fact]
    public void NativeCtor_EmptyDbName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ShardedMongoCollectionFactory(Client, ""));

    [Fact]
    public void NativeCtor_WhitespaceDbName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ShardedMongoCollectionFactory(Client, "   "));

    #endregion

    #region ShardedMongoCollectionFactory — AppLevel constructor

    [Fact]
    public void AppLevelCtor_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ShardedMongoCollectionFactory(null!, "db", Topology));

    [Fact]
    public void AppLevelCtor_NullDbName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ShardedMongoCollectionFactory(Client, null!, Topology));

    [Fact]
    public void AppLevelCtor_EmptyDbName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new ShardedMongoCollectionFactory(Client, "", Topology));

    [Fact]
    public void AppLevelCtor_NullTopology_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ShardedMongoCollectionFactory(Client, "db", null!));

    #endregion

    #region GetCollectionForShard

    [Fact]
    public void GetCollectionForShard_NullShardId_Throws()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        Should.Throw<ArgumentException>(() =>
            factory.GetCollectionForShard<TestEntity>(null!, "coll"));
    }

    [Fact]
    public void GetCollectionForShard_EmptyShardId_Throws()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        Should.Throw<ArgumentException>(() =>
            factory.GetCollectionForShard<TestEntity>("", "coll"));
    }

    [Fact]
    public void GetCollectionForShard_NullCollectionName_Throws()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        Should.Throw<ArgumentException>(() =>
            factory.GetCollectionForShard<TestEntity>("shard-0", null!));
    }

    [Fact]
    public void GetCollectionForShard_EmptyCollectionName_Throws()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        Should.Throw<ArgumentException>(() =>
            factory.GetCollectionForShard<TestEntity>("shard-0", ""));
    }

    [Fact]
    public void GetCollectionForShard_Disposed_Throws()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        factory.Dispose();
        Should.Throw<ObjectDisposedException>(() =>
            factory.GetCollectionForShard<TestEntity>("shard-0", "coll"));
    }

    #endregion

    #region GetDefaultCollection

    [Fact]
    public void GetDefaultCollection_NullCollectionName_Throws()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        Should.Throw<ArgumentException>(() =>
            factory.GetDefaultCollection<TestEntity>(null!));
    }

    [Fact]
    public void GetDefaultCollection_EmptyCollectionName_Throws()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        Should.Throw<ArgumentException>(() =>
            factory.GetDefaultCollection<TestEntity>(""));
    }

    [Fact]
    public void GetDefaultCollection_Disposed_Throws()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        factory.Dispose();
        Should.Throw<ObjectDisposedException>(() =>
            factory.GetDefaultCollection<TestEntity>("coll"));
    }

    #endregion

    #region GetAllCollections

    [Fact]
    public void GetAllCollections_NullCollectionName_Throws()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        Should.Throw<ArgumentException>(() =>
            factory.GetAllCollections<TestEntity>(null!));
    }

    [Fact]
    public void GetAllCollections_EmptyCollectionName_Throws()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        Should.Throw<ArgumentException>(() =>
            factory.GetAllCollections<TestEntity>(""));
    }

    [Fact]
    public void GetAllCollections_Disposed_Throws()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        factory.Dispose();
        Should.Throw<ObjectDisposedException>(() =>
            factory.GetAllCollections<TestEntity>("coll"));
    }

    #endregion

    #region Dispose idempotency

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var factory = new ShardedMongoCollectionFactory(Client, "db");
        factory.Dispose();
        Should.NotThrow(() => factory.Dispose());
    }

    #endregion

    public class TestEntity { public Guid Id { get; set; } }
}
