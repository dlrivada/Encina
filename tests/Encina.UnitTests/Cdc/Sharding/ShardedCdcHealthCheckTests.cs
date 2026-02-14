using Encina.Cdc.Abstractions;
using Encina.Cdc.Health;
using Encina.Messaging.Health;
using LanguageExt;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc.Sharding;

/// <summary>
/// Unit tests for <see cref="ShardedCdcHealthCheck"/>.
/// Verifies health status determination based on connector and position store state.
/// </summary>
public sealed class ShardedCdcHealthCheckTests
{
    #region Test Helpers

    private static IShardedCdcConnector CreateConnector(
        string connectorId = "test-connector",
        IReadOnlyList<string>? activeShardIds = null,
        Either<EncinaError, IReadOnlyDictionary<string, CdcPosition>>? positionsResult = null)
    {
        var connector = Substitute.For<IShardedCdcConnector>();
        connector.GetConnectorId().Returns(connectorId);
        connector.ActiveShardIds.Returns(activeShardIds ?? (IReadOnlyList<string>)["shard-1"]);

        var defaultPositions = Right<EncinaError, IReadOnlyDictionary<string, CdcPosition>>(
            (IReadOnlyDictionary<string, CdcPosition>)new Dictionary<string, CdcPosition>
            {
                ["shard-1"] = new TestCdcPosition(100)
            });

        connector.GetAllPositionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(positionsResult ?? defaultPositions));

        return connector;
    }

    private static IShardedCdcPositionStore CreatePositionStore(
        Either<EncinaError, IReadOnlyDictionary<string, CdcPosition>>? getAllResult = null)
    {
        var store = Substitute.For<IShardedCdcPositionStore>();

        var defaultResult = Right<EncinaError, IReadOnlyDictionary<string, CdcPosition>>(
            (IReadOnlyDictionary<string, CdcPosition>)new Dictionary<string, CdcPosition>
            {
                ["shard-1"] = new TestCdcPosition(100)
            });

        store.GetAllPositionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(getAllResult ?? defaultResult));

        return store;
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullConnector_ThrowsArgumentNullException()
    {
        var store = CreatePositionStore();

        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcHealthCheck(null!, store));
    }

    [Fact]
    public void Constructor_NullPositionStore_ThrowsArgumentNullException()
    {
        var connector = CreateConnector();

        Should.Throw<ArgumentNullException>(() =>
            new ShardedCdcHealthCheck(connector, null!));
    }

    [Fact]
    public void Name_ReturnsExpectedValue()
    {
        var healthCheck = new ShardedCdcHealthCheck(CreateConnector(), CreatePositionStore());

        healthCheck.Name.ShouldBe("encina-cdc-sharded");
    }

    [Fact]
    public void Tags_ContainsExpectedTags()
    {
        var healthCheck = new ShardedCdcHealthCheck(CreateConnector(), CreatePositionStore());

        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("cdc");
        healthCheck.Tags.ShouldContain("sharded");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public void Tags_WithProviderTags_IncludesProviderTags()
    {
        var providerTags = new[] { "sqlserver" };
        var healthCheck = new ShardedCdcHealthCheck(
            CreateConnector(), CreatePositionStore(), providerTags);

        healthCheck.Tags.ShouldContain("sqlserver");
        healthCheck.Tags.ShouldContain("encina");
    }

    #endregion

    #region Healthy

    [Fact]
    public async Task CheckHealthAsync_AllShardsHealthy_ReturnsHealthy()
    {
        var connector = CreateConnector(activeShardIds: ["shard-1", "shard-2"]);
        var store = CreatePositionStore();
        var healthCheck = new ShardedCdcHealthCheck(connector, store);

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["connector_id"].ShouldBe("test-connector");
        result.Data["active_shards"].ShouldBe(2);
    }

    #endregion

    #region Unhealthy

    [Fact]
    public async Task CheckHealthAsync_NoActiveShards_ReturnsUnhealthy()
    {
        var connector = CreateConnector(activeShardIds: []);
        var store = CreatePositionStore();
        var healthCheck = new ShardedCdcHealthCheck(connector, store);

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("no active shards");
    }

    [Fact]
    public async Task CheckHealthAsync_ConnectorCannotRetrievePositions_ReturnsUnhealthy()
    {
        var positionsError = Left<EncinaError, IReadOnlyDictionary<string, CdcPosition>>(
            EncinaError.New("Connection failed"));

        var connector = CreateConnector(positionsResult: positionsError);
        var store = CreatePositionStore();
        var healthCheck = new ShardedCdcHealthCheck(connector, store);

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("cannot retrieve positions");
    }

    #endregion

    #region Degraded

    [Fact]
    public async Task CheckHealthAsync_PositionStoreNotAccessible_ReturnsDegraded()
    {
        var storeError = Left<EncinaError, IReadOnlyDictionary<string, CdcPosition>>(
            EncinaError.New("Store unavailable"));

        var connector = CreateConnector();
        var store = CreatePositionStore(getAllResult: storeError);
        var healthCheck = new ShardedCdcHealthCheck(connector, store);

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull();
        result.Description!.ShouldContain("position store is not accessible");
    }

    #endregion

    #region Data Dictionary

    [Fact]
    public async Task CheckHealthAsync_IncludesShardPositionsInData()
    {
        var positions = new Dictionary<string, CdcPosition>
        {
            ["shard-1"] = new TestCdcPosition(100),
            ["shard-2"] = new TestCdcPosition(200)
        };

        var positionsResult = Right<EncinaError, IReadOnlyDictionary<string, CdcPosition>>(
            (IReadOnlyDictionary<string, CdcPosition>)positions);

        var connector = CreateConnector(
            activeShardIds: ["shard-1", "shard-2"],
            positionsResult: positionsResult);
        var store = CreatePositionStore();
        var healthCheck = new ShardedCdcHealthCheck(connector, store);

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("connector_positions_count");
        ((int)result.Data["connector_positions_count"]).ShouldBe(2);
        result.Data.ShouldContainKey("shard.shard-1.position");
        result.Data.ShouldContainKey("shard.shard-2.position");
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesShardIdsInData()
    {
        var connector = CreateConnector(activeShardIds: ["shard-a", "shard-b"]);
        var store = CreatePositionStore();
        var healthCheck = new ShardedCdcHealthCheck(connector, store);

        var result = await healthCheck.CheckHealthAsync();

        result.Data.ShouldContainKey("shard_ids");
        result.Data["shard_ids"].ToString()!.ShouldContain("shard-a");
        result.Data["shard_ids"].ToString()!.ShouldContain("shard-b");
    }

    #endregion

    #region Exception Handling

    [Fact]
    public async Task CheckHealthAsync_ConnectorThrowsException_ReturnsUnhealthy()
    {
        var connector = Substitute.For<IShardedCdcConnector>();
        connector.GetConnectorId().Returns("test-connector");
        connector.ActiveShardIds.Returns((IReadOnlyList<string>)["shard-1"]);
        connector.GetAllPositionsAsync(Arg.Any<CancellationToken>())
            .Returns<Task<Either<EncinaError, IReadOnlyDictionary<string, CdcPosition>>>>(
                _ => throw new TimeoutException("Connection timeout"));

        var store = CreatePositionStore();
        var healthCheck = new ShardedCdcHealthCheck(connector, store);

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldBeOfType<TimeoutException>();
    }

    #endregion
}
