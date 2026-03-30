using Encina.MongoDB;
using Encina.MongoDB.BulkOperations;
using Encina.MongoDB.Modules;
using Encina.MongoDB.ReadWriteSeparation;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB;

public class BulkAndInfraGuardTests
{
    private static readonly IMongoClient Client = Substitute.For<IMongoClient>();
    private static readonly IOptions<EncinaMongoDbOptions> Opts = Options.Create(new EncinaMongoDbOptions { DatabaseName = "test" });

    #region BulkOperationsMongoDB

    [Fact]
    public void BulkOps_NullCollection_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new BulkOperationsMongoDB<TestEntity, Guid>(null!, e => e.Id));

    #endregion

    #region ReadWriteMongoCollectionFactory

    [Fact]
    public void ReadWriteFactory_NullClient_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoCollectionFactory(null!, Opts));

    [Fact]
    public void ReadWriteFactory_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoCollectionFactory(Client, null!));

    #endregion

    public class TestEntity { public Guid Id { get; set; } }
}
