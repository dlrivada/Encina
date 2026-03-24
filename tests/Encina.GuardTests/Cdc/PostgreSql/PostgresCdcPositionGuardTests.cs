using Encina.Cdc.PostgreSql;

namespace Encina.GuardTests.Cdc.PostgreSql;

/// <summary>
/// Guard clause tests for <see cref="PostgresCdcPosition"/>.
/// Verifies that null/invalid parameters are properly guarded.
/// </summary>
public sealed class PostgresCdcPositionGuardTests
{
    #region FromBytes Guards

    /// <summary>
    /// Verifies that FromBytes throws ArgumentNullException when bytes is null.
    /// </summary>
    [Fact]
    public void FromBytes_NullBytes_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => PostgresCdcPosition.FromBytes(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("bytes");
    }

    /// <summary>
    /// Verifies that FromBytes throws ArgumentException when bytes is not exactly 8 bytes.
    /// </summary>
    [Fact]
    public void FromBytes_InvalidLength_ShouldThrowArgumentException()
    {
        // Act
        var act = () => PostgresCdcPosition.FromBytes(new byte[4]);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion
}
