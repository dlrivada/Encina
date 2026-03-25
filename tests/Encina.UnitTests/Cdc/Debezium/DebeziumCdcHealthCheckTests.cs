using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium;
using Encina.Cdc.Debezium.Health;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumCdcHealthCheck"/>.
/// </summary>
public sealed class DebeziumCdcHealthCheckTests
{
    [Fact]
    public void DefaultName_ShouldBeExpectedValue()
    {
        DebeziumCdcHealthCheck.DefaultName.ShouldBe("encina-cdc-debezium");
    }

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Arrange
        var connector = Substitute.For<ICdcConnector>();
        var positionStore = Substitute.For<ICdcPositionStore>();

        // Act
        var healthCheck = new DebeziumCdcHealthCheck(connector, positionStore);

        // Assert
        healthCheck.ShouldNotBeNull();
    }
}
