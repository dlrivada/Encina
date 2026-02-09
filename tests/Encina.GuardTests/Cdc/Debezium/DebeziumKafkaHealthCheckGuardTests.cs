using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium.Kafka;
using NSubstitute;

namespace Encina.GuardTests.Cdc.Debezium;

/// <summary>
/// Guard clause tests for <see cref="DebeziumKafkaHealthCheck"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class DebeziumKafkaHealthCheckGuardTests
{
    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when connector is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConnector_ShouldThrowArgumentNullException()
    {
        // Arrange
        var positionStore = Substitute.For<ICdcPositionStore>();

        // Act
        var act = () => new DebeziumKafkaHealthCheck(null!, positionStore);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connector");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when positionStore is null.
    /// </summary>
    [Fact]
    public void Constructor_NullPositionStore_ShouldThrowArgumentNullException()
    {
        // Arrange
        var connector = Substitute.For<ICdcConnector>();

        // Act
        var act = () => new DebeziumKafkaHealthCheck(connector, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("positionStore");
    }

    #endregion
}
