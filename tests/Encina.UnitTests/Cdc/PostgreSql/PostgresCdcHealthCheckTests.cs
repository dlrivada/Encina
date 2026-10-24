using Encina.Cdc.Abstractions;
using Encina.Cdc.PostgreSql;
using Encina.Cdc.PostgreSql.Health;
using Encina.Messaging.Health;
using LanguageExt;
using NpgsqlTypes;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Cdc.PostgreSql;

/// <summary>
/// Unit tests for <see cref="PostgresCdcHealthCheck"/>.
/// </summary>
public sealed class PostgresCdcHealthCheckTests
{
    private static ICdcConnector ConnectorReturning(Either<EncinaError, CdcPosition> result)
    {
        var connector = Substitute.For<ICdcConnector>();
        connector.ConnectorId.Returns("encina-cdc-postgresql");
        connector.GetCurrentPositionAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));
        return connector;
    }

    private static ICdcPositionStore StoreReturning(Either<EncinaError, Option<CdcPosition>> result)
    {
        var store = Substitute.For<ICdcPositionStore>();
        store.GetPositionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));
        return store;
    }

    private static Either<EncinaError, CdcPosition> SomePosition() =>
        Prelude.Right<EncinaError, CdcPosition>(new PostgresCdcPosition(new NpgsqlLogSequenceNumber(42)));

    [Fact]
    public void Constructor_SetsDefaultNameAndPostgresTags()
    {
        var check = new PostgresCdcHealthCheck(
            Substitute.For<ICdcConnector>(), Substitute.For<ICdcPositionStore>());

        check.Name.ShouldBe(PostgresCdcHealthCheck.DefaultName);
        check.Name.ShouldBe("encina-cdc-postgres");
        check.Tags.ShouldContain("encina");
        check.Tags.ShouldContain("cdc");
        check.Tags.ShouldContain("ready");
        check.Tags.ShouldContain("postgresql");
        check.Tags.ShouldContain("logical-replication");
    }

    [Fact]
    public void Constructor_NullConnector_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PostgresCdcHealthCheck(null!, Substitute.For<ICdcPositionStore>()));
    }

    [Fact]
    public void Constructor_NullPositionStore_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new PostgresCdcHealthCheck(Substitute.For<ICdcConnector>(), null!));
    }

    [Fact]
    public async Task CheckHealthAsync_ConnectorAndStoreAccessible_ReturnsHealthy()
    {
        var check = new PostgresCdcHealthCheck(
            ConnectorReturning(SomePosition()),
            StoreReturning(Option<CdcPosition>.None));

        var result = await check.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Data.ShouldContainKey("current_position");
        result.Data["last_saved_position"].ShouldBe("none");
    }

    [Fact]
    public async Task CheckHealthAsync_ConnectorFails_ReturnsUnhealthyWithErrorCodeOnly()
    {
        var failure = EncinaError.New("secret connection detail");
        var check = new PostgresCdcHealthCheck(
            ConnectorReturning(Prelude.Left<EncinaError, CdcPosition>(failure)),
            StoreReturning(Option<CdcPosition>.None));

        var result = await check.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Data.ShouldContainKey("connector_error");
        result.Data.Values.ShouldAllBe(v => !v.ToString()!.Contains("secret connection detail"));
    }

    [Fact]
    public async Task CheckHealthAsync_PositionStoreFails_ReturnsDegraded()
    {
        var check = new PostgresCdcHealthCheck(
            ConnectorReturning(SomePosition()),
            StoreReturning(Prelude.Left<EncinaError, Option<CdcPosition>>(EncinaError.New("store down"))));

        var result = await check.CheckHealthAsync(TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Data.ShouldContainKey("store_error");
    }
}
