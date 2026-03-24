using Encina.Cdc.Abstractions;
using Encina.Cdc.MySql.Health;

namespace Encina.GuardTests.Cdc.MySql;

/// <summary>
/// Guard clause tests for <see cref="MySqlCdcHealthCheck"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class MySqlCdcHealthCheckGuardTests
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
        var act = () => new MySqlCdcHealthCheck(null!, positionStore);

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
        var act = () => new MySqlCdcHealthCheck(connector, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("positionStore");
    }

    #endregion
}
