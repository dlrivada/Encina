using Encina.MongoDB.Sharding;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.MongoDB.Sharding;

public class ShardingServiceCollectionExtGuardTests
{
    #region AddEncinaMongoDBSharding

    [Fact]
    public void AddEncinaMongoDBSharding_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            ShardingServiceCollectionExtensions.AddEncinaMongoDBSharding<TestEntity, Guid>(
                null!, _ => { }));

    [Fact]
    public void AddEncinaMongoDBSharding_NullConfigure_Throws()
    {
        var services = new ServiceCollection();
        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaMongoDBSharding<TestEntity, Guid>(null!));
    }

    #endregion

    #region AddEncinaMongoDBShardedReadWrite

    [Fact]
    public void AddEncinaMongoDBShardedReadWrite_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            ShardingServiceCollectionExtensions.AddEncinaMongoDBShardedReadWrite(null!));

    #endregion

    #region AddEncinaMongoDBReferenceTableStore

    [Fact]
    public void AddEncinaMongoDBReferenceTableStore_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            ShardingServiceCollectionExtensions.AddEncinaMongoDBReferenceTableStore(null!));

    #endregion

    public class TestEntity { public Guid Id { get; set; } }
}
