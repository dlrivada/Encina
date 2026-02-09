using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium.Kafka;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Unit tests for <see cref="DebeziumKafkaHealthCheck"/>.
/// Verifies construction and constant values.
/// </summary>
public sealed class DebeziumKafkaHealthCheckTests
{
    #region DefaultName

    /// <summary>
    /// Verifies that the DefaultName constant has the expected value.
    /// </summary>
    [Fact]
    public void DefaultName_ShouldBeEncinaCdcDebeziumKafka()
    {
        DebeziumKafkaHealthCheck.DefaultName.ShouldBe("encina-cdc-debezium-kafka");
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Verifies that the constructor succeeds with valid parameters.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_ShouldNotThrow()
    {
        // Arrange
        var connector = Substitute.For<ICdcConnector>();
        var positionStore = Substitute.For<ICdcPositionStore>();

        // Act & Assert
        Should.NotThrow(() => new DebeziumKafkaHealthCheck(connector, positionStore));
    }

    #endregion
}
