using Encina.Database;
using Encina.MongoDB.Health;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MongoDB.Health;

public sealed class MongoDbDatabaseHealthMonitorTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IMongoClient _mongoClient = Substitute.For<IMongoClient>();
    private readonly IMongoDatabase _database = Substitute.For<IMongoDatabase>();

    public MongoDbDatabaseHealthMonitorTests()
    {
        _serviceProvider.GetService(typeof(IMongoClient)).Returns(_mongoClient);
        _mongoClient.GetDatabase("admin").Returns(_database);
    }

    [Fact]
    public void ProviderName_IsMongoDb()
    {
        new MongoDbDatabaseHealthMonitor(_serviceProvider).ProviderName.ShouldBe("mongodb");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingSucceeds_ReturnsHealthy()
    {
        SetupPing(Task.FromResult(new BsonDocument("ok", 1)));
        var monitor = new MongoDbDatabaseHealthMonitor(_serviceProvider);

        var result = await monitor.CheckHealthAsync();

        result.Status.ShouldBe(DatabaseHealthStatus.Healthy);
        monitor.IsCircuitOpen.ShouldBeFalse();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingFails_ReportsOnlyTheExceptionTypeAndOpensCircuit()
    {
        SetupPing(Task.FromException<BsonDocument>(new MongoException("mongodb://user:secret@db-secret-host:27017 refused")));
        var monitor = new MongoDbDatabaseHealthMonitor(_serviceProvider);

        var result = await monitor.CheckHealthAsync();

        result.Status.ShouldBe(DatabaseHealthStatus.Unhealthy);
        result.Description!.ShouldContain(nameof(MongoException));
        result.Description!.ShouldNotContain("secret");
        monitor.IsCircuitOpen.ShouldBeTrue();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCircuitIsOpen_ReturnsUnhealthyWithoutPinging()
    {
        SetupPing(Task.FromException<BsonDocument>(new MongoException("boom")));
        var monitor = new MongoDbDatabaseHealthMonitor(_serviceProvider);
        await monitor.CheckHealthAsync();

        var result = await monitor.CheckHealthAsync();

        result.Status.ShouldBe(DatabaseHealthStatus.Unhealthy);
        result.Description!.ShouldContain("Circuit breaker is open");
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
