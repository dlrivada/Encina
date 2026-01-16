using Encina.Messaging.Health;
using Encina.MongoDB.Health;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MongoDB.Health;

public sealed class MongoDbHealthCheckTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMongoClient _mongoClient;
    private readonly IMongoDatabase _database;

    public MongoDbHealthCheckTests()
    {
        _mongoClient = Substitute.For<IMongoClient>();
        _database = Substitute.For<IMongoDatabase>();
        _serviceProvider = Substitute.For<IServiceProvider>();

        _serviceProvider.GetService(typeof(IMongoClient)).Returns(_mongoClient);
        _mongoClient.GetDatabase("admin").Returns(_database);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        MongoDbHealthCheck.DefaultName.ShouldBe("encina-mongodb");
    }

    [Fact]
    public void Constructor_SetsNameFromOptions()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Name = "custom-mongodb" };

        // Act
        var healthCheck = new MongoDbHealthCheck(_serviceProvider, options);

        // Assert
        healthCheck.Name.ShouldBe("custom-mongodb");
    }

    [Fact]
    public void Constructor_SetsDefaultNameWhenOptionsNull()
    {
        // Act
        var healthCheck = new MongoDbHealthCheck(_serviceProvider, null);

        // Assert
        healthCheck.Name.ShouldBe(MongoDbHealthCheck.DefaultName);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenPingSucceeds_ReturnsHealthy()
    {
        // Arrange
        _database.RunCommandAsync<BsonDocument>(
            Arg.Any<Command<BsonDocument>>(),
            Arg.Any<ReadPreference>(),
            Arg.Any<CancellationToken>())
            .Returns(new BsonDocument("ok", 1));

        var healthCheck = new MongoDbHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("reachable");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenMongoExceptionThrown_ReturnsUnhealthy()
    {
        // Arrange
        _database.RunCommandAsync<BsonDocument>(
            Arg.Any<Command<BsonDocument>>(),
            Arg.Any<ReadPreference>(),
            Arg.Any<CancellationToken>())
            .Returns<BsonDocument>(_ => throw new MongoException("Connection failed"));

        var healthCheck = new MongoDbHealthCheck(_serviceProvider, null);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("failed");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenTimeout_ReturnsUnhealthy()
    {
        // Arrange
        var options = new ProviderHealthCheckOptions { Timeout = TimeSpan.FromMilliseconds(1) };

        _database.RunCommandAsync<BsonDocument>(
            Arg.Any<Command<BsonDocument>>(),
            Arg.Any<ReadPreference>(),
            Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), callInfo.Arg<CancellationToken>());
                return new BsonDocument("ok", 1);
            });

        var healthCheck = new MongoDbHealthCheck(_serviceProvider, options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("timed out");
    }
}
