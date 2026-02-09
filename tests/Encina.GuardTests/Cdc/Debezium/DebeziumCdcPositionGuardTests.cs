using Encina.Cdc.Debezium;

namespace Encina.GuardTests.Cdc.Debezium;

/// <summary>
/// Guard clause tests for <see cref="DebeziumCdcPosition"/>.
/// Verifies that null/empty/whitespace parameters are properly guarded.
/// </summary>
public sealed class DebeziumCdcPositionGuardTests
{
    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when offsetJson is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOffsetJson_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new DebeziumCdcPosition(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("offsetJson");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when offsetJson is empty.
    /// </summary>
    [Fact]
    public void Constructor_EmptyOffsetJson_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new DebeziumCdcPosition("");

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("offsetJson");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentException when offsetJson is whitespace.
    /// </summary>
    [Fact]
    public void Constructor_WhitespaceOffsetJson_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new DebeziumCdcPosition("   ");

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("offsetJson");
    }

    #endregion

    #region FromBytes Guards

    /// <summary>
    /// Verifies that FromBytes throws ArgumentNullException when bytes is null.
    /// </summary>
    [Fact]
    public void FromBytes_NullBytes_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => DebeziumCdcPosition.FromBytes(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("bytes");
    }

    #endregion
}
