using Encina.MongoDB.ReadWriteSeparation;
using Encina.MongoDB.Sharding;
using Encina.Sharding;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MongoDB.Sharding;

/// <summary>
/// Pins the database name taken from a shard connection string by the two application-level
/// sharding factories. The extraction helper is private, so it is driven through
/// <c>GetCollectionForShard</c>; a <see cref="MongoClient"/> connects lazily, so no server is needed.
/// </summary>
public sealed class ShardedFactoryDatabaseNameExtractionTests
{
    private const string FallbackDatabase = "fallback";

    [Theory]
    [InlineData("mongodb://h:27017/db?x=1", "db")]
    [InlineData("mongodb://h:27017/db", "db")]
    [InlineData("mongodb://h:27017/my%2Ddb?x=1", "my-db")]
    [InlineData("mongodb://user:pw@h:27017/db?authSource=admin&retryWrites=true", "db")]
    [InlineData("mongodb://h:27017", FallbackDatabase)]
    [InlineData("mongodb://h:27017/", FallbackDatabase)]
    [InlineData("mongodb://h:27017/?x=1", FallbackDatabase)]
    public void ShardedMongoCollectionFactory_UsesTheDatabaseOfTheConnectionString(
        string connectionString,
        string expectedDatabase)
    {
        using var factory = new ShardedMongoCollectionFactory(
            Substitute.For<IMongoClient>(),
            FallbackDatabase,
            TopologyOf(connectionString));

        var result = factory.GetCollectionForShard<BsonDocument>("shard-0", "orders");

        DatabaseNameOf(result).ShouldBe(expectedDatabase);
    }

    [Theory]
    [InlineData("mongodb://h:27017/db?x=1", "db")]
    [InlineData("mongodb://h:27017/db", "db")]
    [InlineData("mongodb://h:27017/my%2Ddb?x=1", "my-db")]
    [InlineData("mongodb://user:pw@h:27017/db?authSource=admin&retryWrites=true", "db")]
    [InlineData("mongodb://h:27017", FallbackDatabase)]
    [InlineData("mongodb://h:27017/", FallbackDatabase)]
    [InlineData("mongodb://h:27017/?x=1", FallbackDatabase)]
    public void ShardedReadWriteMongoCollectionFactory_UsesTheDatabaseOfTheConnectionString(
        string connectionString,
        string expectedDatabase)
    {
        using var factory = new ShardedReadWriteMongoCollectionFactory(
            Substitute.For<IMongoClient>(),
            FallbackDatabase,
            TopologyOf(connectionString),
            new MongoReadWriteSeparationOptions());

        var read = factory.GetReadCollectionForShard<BsonDocument>("shard-0", "orders");
        var write = factory.GetWriteCollectionForShard<BsonDocument>("shard-0", "orders");

        DatabaseNameOf(read).ShouldBe(expectedDatabase);
        DatabaseNameOf(write).ShouldBe(expectedDatabase);
    }

    private static ShardTopology TopologyOf(string connectionString)
        => new([new ShardInfo("shard-0", connectionString, Weight: 1, IsActive: true)]);

    private static string DatabaseNameOf(LanguageExt.Either<EncinaError, IMongoCollection<BsonDocument>> result)
    {
        result.IsRight.ShouldBeTrue();
        return result.Match(
            Right: collection => collection.Database.DatabaseNamespace.DatabaseName,
            Left: _ => string.Empty);
    }
}
