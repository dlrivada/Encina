using Encina.Messaging.SoftDelete;
using Encina.MongoDB.SoftDelete;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.SoftDelete;

public class SoftDeleteRepositoryGuardTests
{
    [Fact]
    public void Constructor_NullCollection_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SoftDeletableFunctionalRepositoryMongoDB<TestEntity, Guid>(
                null!,
                Substitute.For<ISoftDeleteEntityMapping<TestEntity, Guid>>(),
                new SoftDeleteOptions()));

    [Fact]
    public void Constructor_NullMapping_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SoftDeletableFunctionalRepositoryMongoDB<TestEntity, Guid>(
                Substitute.For<IMongoCollection<TestEntity>>(),
                null!,
                new SoftDeleteOptions()));

    [Fact]
    public void Constructor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new SoftDeletableFunctionalRepositoryMongoDB<TestEntity, Guid>(
                Substitute.For<IMongoCollection<TestEntity>>(),
                Substitute.For<ISoftDeleteEntityMapping<TestEntity, Guid>>(),
                null!));

    public class TestEntity { public Guid Id { get; set; } public bool IsDeleted { get; set; } }
}
