using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Health;
using Encina.Messaging.Health;
using LanguageExt;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="CdcHealthCheck"/>.
/// </summary>
public sealed class CdcHealthCheckTests
{
    #region Test Helpers

    /// <summary>
    /// Concrete test implementation since CdcHealthCheck has a protected constructor.
    /// </summary>
    private sealed class TestCdcHealthCheck : CdcHealthCheck
    {
        public TestCdcHealthCheck(
            string name,
            ICdcConnector connector,
            ICdcPositionStore positionStore,
            IReadOnlyCollection<string>? providerTags = null)
            : base(name, connector, positionStore, providerTags)
        {
        }
    }

    private static ICdcConnector CreateHealthyConnector(string connectorId = "test-connector")
    {
        var connector = Substitute.For<ICdcConnector>();
        connector.ConnectorId.Returns(connectorId);
        connector.GetCurrentPositionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, CdcPosition>(new TestCdcPosition(42))));
        return connector;
    }

    private static ICdcPositionStore CreateHealthyStore()
    {
        var store = Substitute.For<ICdcPositionStore>();
        store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, Option<CdcPosition>>(Some<CdcPosition>(new TestCdcPosition(40)))));
        return store;
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_NullConnector_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TestCdcHealthCheck("test", null!, CreateHealthyStore()));
    }

    [Fact]
    public void Constructor_NullPositionStore_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            new TestCdcHealthCheck("test", CreateHealthyConnector(), null!));
    }

    #endregion

    #region CheckHealthAsync

    [Fact]
    public async Task CheckHealthAsync_AllHealthy_ReturnsHealthy()
    {
        var healthCheck = new TestCdcHealthCheck(
            "test-cdc",
            CreateHealthyConnector(),
            CreateHealthyStore());

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_ConnectorFails_ReturnsUnhealthy()
    {
        var connector = Substitute.For<ICdcConnector>();
        connector.ConnectorId.Returns("failing-connector");
        connector.GetCurrentPositionAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Left<EncinaError, CdcPosition>(EncinaError.New("Connection failed"))));

        var healthCheck = new TestCdcHealthCheck(
            "test-cdc",
            connector,
            CreateHealthyStore());

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("not accessible");
    }

    [Fact]
    public async Task CheckHealthAsync_PositionStoreFails_ReturnsDegraded()
    {
        var store = Substitute.For<ICdcPositionStore>();
        store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Left<EncinaError, Option<CdcPosition>>(EncinaError.New("Store error"))));

        var healthCheck = new TestCdcHealthCheck(
            "test-cdc",
            CreateHealthyConnector(),
            store);

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description!.ShouldContain("position store");
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesConnectorId_InData()
    {
        var healthCheck = new TestCdcHealthCheck(
            "test-cdc",
            CreateHealthyConnector("my-connector"),
            CreateHealthyStore());

        var result = await healthCheck.CheckHealthAsync();

        result.Data.ShouldContainKey("connector_id");
        result.Data["connector_id"].ShouldBe("my-connector");
    }

    [Fact]
    public async Task CheckHealthAsync_Healthy_IncludesPositionData()
    {
        var healthCheck = new TestCdcHealthCheck(
            "test-cdc",
            CreateHealthyConnector(),
            CreateHealthyStore());

        var result = await healthCheck.CheckHealthAsync();

        result.Data.ShouldContainKey("current_position");
        result.Data.ShouldContainKey("last_saved_position");
    }

    [Fact]
    public async Task CheckHealthAsync_NoSavedPosition_ShowsNone()
    {
        var store = Substitute.For<ICdcPositionStore>();
        store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(
                Right<EncinaError, Option<CdcPosition>>(Option<CdcPosition>.None)));

        var healthCheck = new TestCdcHealthCheck(
            "test-cdc",
            CreateHealthyConnector(),
            store);

        var result = await healthCheck.CheckHealthAsync();

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data["last_saved_position"].ShouldBe("none");
    }

    #endregion

    #region Tags

    [Fact]
    public void Tags_DefaultTags_ContainsEncinaCdcReady()
    {
        var healthCheck = new TestCdcHealthCheck(
            "test-cdc",
            CreateHealthyConnector(),
            CreateHealthyStore());

        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("cdc");
        healthCheck.Tags.ShouldContain("ready");
    }

    [Fact]
    public void Tags_WithProviderTags_CombinesTags()
    {
        var healthCheck = new TestCdcHealthCheck(
            "test-cdc",
            CreateHealthyConnector(),
            CreateHealthyStore(),
            ["sqlserver"]);

        healthCheck.Tags.ShouldContain("encina");
        healthCheck.Tags.ShouldContain("cdc");
        healthCheck.Tags.ShouldContain("ready");
        healthCheck.Tags.ShouldContain("sqlserver");
    }

    #endregion
}
