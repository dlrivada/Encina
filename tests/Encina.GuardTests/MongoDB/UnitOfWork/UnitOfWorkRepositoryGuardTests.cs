using Encina.MongoDB.UnitOfWork;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.UnitOfWork;

public class UnitOfWorkRepositoryGuardTests
{
    [Fact]
    public void Constructor_NullCollection_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkRepositoryMongoDB<TestEntity, Guid>(null!, e => e.Id, null!));

    [Fact]
    public void Constructor_NullUnitOfWork_Throws()
    {
        var collection = Substitute.For<IMongoCollection<TestEntity>>();
        Should.Throw<ArgumentNullException>(() =>
            new UnitOfWorkRepositoryMongoDB<TestEntity, Guid>(collection, e => e.Id, null!));
    }

    public class TestEntity { public Guid Id { get; set; } }
}
