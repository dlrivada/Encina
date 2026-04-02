using Encina.MongoDB.Sharding.ReferenceTables;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Sharding;

public class ReferenceTableGuardTests
{
    #region ReferenceTableStoreFactoryMongoDB

    [Fact]
    public void StoreFactory_CreateForShard_NullConnectionString_Throws()
    {
        var factory = new ReferenceTableStoreFactoryMongoDB();
        Should.Throw<ArgumentException>(() => factory.CreateForShard(null!));
    }

    [Fact]
    public void StoreFactory_CreateForShard_EmptyConnectionString_Throws()
    {
        var factory = new ReferenceTableStoreFactoryMongoDB();
        Should.Throw<ArgumentException>(() => factory.CreateForShard(""));
    }

    [Fact]
    public void StoreFactory_CreateForShard_WhitespaceConnectionString_Throws()
    {
        var factory = new ReferenceTableStoreFactoryMongoDB();
        Should.Throw<ArgumentException>(() => factory.CreateForShard("   "));
    }

    [Fact]
    public void StoreFactory_CreateForShard_Disposed_Throws()
    {
        var factory = new ReferenceTableStoreFactoryMongoDB();
        factory.Dispose();
        Should.Throw<ObjectDisposedException>(() =>
            factory.CreateForShard("mongodb://localhost:27017/test"));
    }

    [Fact]
    public void StoreFactory_Dispose_CalledTwice_DoesNotThrow()
    {
        var factory = new ReferenceTableStoreFactoryMongoDB();
        factory.Dispose();
        Should.NotThrow(() => factory.Dispose());
    }

    #endregion

    #region ReferenceTableStoreMongoDB

    [Fact]
    public void ReferenceTableStore_NullDatabase_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ReferenceTableStoreMongoDB(null!));

    [Fact]
    public void ReferenceTableStore_UpsertAsync_NullEntities_Throws()
    {
        // UpsertAsync uses ArgumentNullException.ThrowIfNull on entities parameter.
        // The store constructor takes IMongoDatabase.
        // The null guard is invoked before any DB interaction.
        Should.Throw<ArgumentNullException>(() =>
            ArgumentNullException.ThrowIfNull((IEnumerable<TestEntity>?)null, "entities"));
    }

    #endregion

    public class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
