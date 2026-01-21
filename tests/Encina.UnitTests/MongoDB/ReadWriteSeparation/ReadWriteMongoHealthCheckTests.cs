using Encina.Messaging.Health;
using Encina.MongoDB;
using Encina.MongoDB.ReadWriteSeparation;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.MongoDB.ReadWriteSeparation;

public sealed class ReadWriteMongoHealthCheckTests
{
    private readonly IMongoClient _mongoClient;
    private readonly ICluster _cluster;
    private readonly IOptions<EncinaMongoDbOptions> _options;

    public ReadWriteMongoHealthCheckTests()
    {
        _mongoClient = Substitute.For<IMongoClient>();
        _cluster = Substitute.For<ICluster>();
        _options = Options.Create(new EncinaMongoDbOptions
        {
            DatabaseName = "TestDb",
            UseReadWriteSeparation = true
        });

        _mongoClient.Cluster.Returns(_cluster);
    }

    [Fact]
    public void DefaultName_IsCorrect()
    {
        // Assert
        ReadWriteMongoHealthCheck.DefaultName.ShouldBe("encina-read-write-separation-mongodb");
    }

    [Fact]
    public void Constructor_ThrowsOnNullMongoClient()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoHealthCheck(null!, _options));
    }

    [Fact]
    public void Constructor_ThrowsOnNullOptions()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new ReadWriteMongoHealthCheck(_mongoClient, null!));
    }

    [Fact]
    public void Constructor_SetsDefaultName()
    {
        // Act
        var healthCheck = new ReadWriteMongoHealthCheck(_mongoClient, _options);

        // Assert
        healthCheck.Name.ShouldBe(ReadWriteMongoHealthCheck.DefaultName);
    }

    [Fact]
    public void Constructor_SetsCorrectTags()
    {
        // Act
        var healthCheck = new ReadWriteMongoHealthCheck(_mongoClient, _options);

        // Assert
        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("database");
        healthCheck.Tags.ShouldContain("read-write-separation");
        healthCheck.Tags.ShouldContain("mongodb");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public async Task CheckHealthAsync_OnException_ReturnsUnhealthy()
    {
        // Arrange
        _cluster.Description.Returns(_ => throw new MongoException("Connection failed"));

        var healthCheck = new ReadWriteMongoHealthCheck(_mongoClient, _options);

        // Act
        var result = await healthCheck.CheckHealthAsync();

        // Assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("Failed");
        result.Data.ShouldContainKey("error");
    }
}
