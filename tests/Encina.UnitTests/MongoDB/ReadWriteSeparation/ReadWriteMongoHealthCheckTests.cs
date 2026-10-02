using System.Net;
using Encina.Messaging.Health;
using Encina.MongoDB;
using Encina.MongoDB.ReadWriteSeparation;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
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
        result.Description!.ShouldContain(nameof(MongoException));
        result.Description!.ShouldNotContain("Connection failed");
        result.Data["error"].ShouldBe(nameof(MongoException));
    }

    [Fact]
    public async Task CheckHealthAsync_WithNoServers_ReturnsUnhealthy()
    {
        var result = await CheckAsync(ClusterType.Unknown);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("no servers");
    }

    [Fact]
    public async Task CheckHealthAsync_WithStandalone_ReturnsDegraded()
    {
        var result = await CheckAsync(ClusterType.Standalone, ServerType.Standalone);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("standalone");
    }

    [Fact]
    public async Task CheckHealthAsync_WithShardedCluster_ReturnsHealthy()
    {
        var result = await CheckAsync(ClusterType.Sharded, ServerType.ShardRouter);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("sharded");
        result.Data["servers"].ShouldBe(1);
    }

    [Fact]
    public async Task CheckHealthAsync_ReplicaSetWithoutPrimary_ReturnsUnhealthy()
    {
        var result = await CheckAsync(ClusterType.ReplicaSet, ServerType.ReplicaSetSecondary, ServerType.ReplicaSetArbiter);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Data["primary"].ShouldBe("unavailable");
        result.Data["arbiters"].ShouldBe(1);
    }

    [Fact]
    public async Task CheckHealthAsync_ReplicaSetWithoutSecondaries_ReturnsDegraded()
    {
        var result = await CheckAsync(ClusterType.ReplicaSet, ServerType.ReplicaSetPrimary);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data["primary"].ShouldBe("available");
    }

    [Fact]
    public async Task CheckHealthAsync_ReplicaSetWithPrimaryAndSecondary_ReturnsHealthy()
    {
        var result = await CheckAsync(ClusterType.ReplicaSet, ServerType.ReplicaSetPrimary, ServerType.ReplicaSetSecondary);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["secondaries"].ShouldBe(1);
    }

    private async Task<HealthCheckResult> CheckAsync(ClusterType clusterType, params ServerType[] serverTypes)
    {
        var clusterId = new ClusterId();
        var servers = serverTypes.Select((type, i) =>
        {
            var endPoint = new DnsEndPoint($"host-{i}", 27017);
            return new ServerDescription(new ServerId(clusterId, endPoint), endPoint, type: type);
        });
        _cluster.Description.Returns(new ClusterDescription(clusterId, false, null, clusterType, servers));

        return await new ReadWriteMongoHealthCheck(_mongoClient, _options).CheckHealthAsync();
    }
}
