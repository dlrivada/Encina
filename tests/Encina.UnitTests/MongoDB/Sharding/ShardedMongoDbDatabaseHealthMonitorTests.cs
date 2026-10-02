using Encina.Database;
using Encina.MongoDB.Sharding;
using Encina.Sharding;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MongoDB.Sharding;

public sealed class ShardedMongoDbDatabaseHealthMonitorTests
{
    private readonly IMongoClient _client = Substitute.For<IMongoClient>();
    private readonly IMongoDatabase _database = Substitute.For<IMongoDatabase>();
    private readonly IShardedMongoCollectionFactory _factory = Substitute.For<IShardedMongoCollectionFactory>();

    private static readonly ShardTopology Topology = new([
        new ShardInfo("shard-0", "mongodb://shard0:27017/test", Weight: 1, IsActive: true)
    ]);

    public ShardedMongoDbDatabaseHealthMonitorTests()
    {
        _client.GetDatabase("admin").Returns(_database);
    }

    [Fact]
    public async Task CheckShardHealthAsync_NativeSharding_WhenPingSucceeds_ReturnsHealthy()
    {
        SetupPing(Task.FromResult(new BsonDocument("ok", 1)));
        var monitor = new ShardedMongoDbDatabaseHealthMonitor(_client, Topology, _factory, useNativeSharding: true);

        var result = await monitor.CheckShardHealthAsync("mongos");

        result.Status.ShouldBe(DatabaseHealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckShardHealthAsync_WhenPingFails_ReportsOnlyTheExceptionType()
    {
        SetupPing(Task.FromException<BsonDocument>(new MongoException("mongodb://user:secret@db-secret-host:27017 refused")));
        var monitor = new ShardedMongoDbDatabaseHealthMonitor(_client, Topology, _factory, useNativeSharding: true);

        var result = await monitor.CheckShardHealthAsync("mongos");

        result.Status.ShouldBe(DatabaseHealthStatus.Unhealthy);
        result.Description!.ShouldContain(nameof(MongoException));
        result.Description!.ShouldNotContain("secret");
    }

    [Fact]
    public async Task CheckShardHealthAsync_ApplicationLevel_UnknownShard_ReturnsUnhealthy()
    {
        var monitor = new ShardedMongoDbDatabaseHealthMonitor(_client, Topology, _factory, useNativeSharding: false);

        var result = await monitor.CheckShardHealthAsync("missing");

        result.Status.ShouldBe(DatabaseHealthStatus.Unhealthy);
        result.Description!.ShouldContain("not found");
    }

    private void SetupPing(Task<BsonDocument> outcome)
    {
        _database.RunCommandAsync<BsonDocument>(
                Arg.Any<Command<BsonDocument>>(),
                Arg.Any<ReadPreference>(),
                Arg.Any<CancellationToken>())
            .Returns(outcome);
    }
}
