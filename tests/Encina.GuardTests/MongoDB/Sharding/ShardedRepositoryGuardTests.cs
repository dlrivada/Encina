using Encina.MongoDB.Sharding;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;

namespace Encina.GuardTests.MongoDB.Sharding;

public class ShardedRepositoryGuardTests
{
    [Fact]
    public void Constructor_NullFactory_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new FunctionalShardedRepositoryMongoDB<TestEntity, Guid>(
                null!, e => e.Id, "test_collection",
                NullLogger<FunctionalShardedRepositoryMongoDB<TestEntity, Guid>>.Instance));

    [Fact]
    public void Constructor_NullIdSelector_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new FunctionalShardedRepositoryMongoDB<TestEntity, Guid>(
                Substitute.For<IShardedMongoCollectionFactory>(), null!, "test_collection",
                NullLogger<FunctionalShardedRepositoryMongoDB<TestEntity, Guid>>.Instance));

    [Fact]
    public void Constructor_NullCollectionName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new FunctionalShardedRepositoryMongoDB<TestEntity, Guid>(
                Substitute.For<IShardedMongoCollectionFactory>(), e => e.Id, null!,
                NullLogger<FunctionalShardedRepositoryMongoDB<TestEntity, Guid>>.Instance));

    [Fact]
    public void Constructor_EmptyCollectionName_Throws()
        => Should.Throw<ArgumentException>(() =>
            new FunctionalShardedRepositoryMongoDB<TestEntity, Guid>(
                Substitute.For<IShardedMongoCollectionFactory>(), e => e.Id, "",
                NullLogger<FunctionalShardedRepositoryMongoDB<TestEntity, Guid>>.Instance));

    [Fact]
    public void Constructor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new FunctionalShardedRepositoryMongoDB<TestEntity, Guid>(
                Substitute.For<IShardedMongoCollectionFactory>(), e => e.Id, "test_collection",
                null!));

    public class TestEntity { public Guid Id { get; set; } }
}
