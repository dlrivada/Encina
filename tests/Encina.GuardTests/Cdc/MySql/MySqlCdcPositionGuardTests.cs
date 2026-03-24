using Encina.Cdc.MySql;

namespace Encina.GuardTests.Cdc.MySql;

/// <summary>
/// Guard clause tests for <see cref="MySqlCdcPosition"/>.
/// Verifies that null/empty/whitespace parameters are properly guarded.
/// </summary>
public sealed class MySqlCdcPositionGuardTests
{
    #region Constructor (GTID mode) Guards

    /// <summary>
    /// Verifies that the GTID constructor throws ArgumentNullException when gtidSet is null.
    /// </summary>
    [Fact]
    public void Constructor_Gtid_NullGtidSet_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MySqlCdcPosition(null!);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    /// <summary>
    /// Verifies that the GTID constructor throws ArgumentException when gtidSet is empty.
    /// </summary>
    [Fact]
    public void Constructor_Gtid_EmptyGtidSet_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new MySqlCdcPosition("");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that the GTID constructor throws ArgumentException when gtidSet is whitespace.
    /// </summary>
    [Fact]
    public void Constructor_Gtid_WhitespaceGtidSet_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new MySqlCdcPosition("   ");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Constructor (File/Position mode) Guards

    /// <summary>
    /// Verifies that the file/position constructor throws ArgumentNullException when binlogFileName is null.
    /// </summary>
    [Fact]
    public void Constructor_FilePosition_NullBinlogFileName_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MySqlCdcPosition(null!, 100);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    /// <summary>
    /// Verifies that the file/position constructor throws ArgumentException when binlogFileName is empty.
    /// </summary>
    [Fact]
    public void Constructor_FilePosition_EmptyBinlogFileName_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new MySqlCdcPosition("", 100);

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    /// <summary>
    /// Verifies that the file/position constructor throws ArgumentException when binlogFileName is whitespace.
    /// </summary>
    [Fact]
    public void Constructor_FilePosition_WhitespaceBinlogFileName_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new MySqlCdcPosition("   ", 100);

        // Assert
        Should.Throw<ArgumentException>(act);
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
        var act = () => MySqlCdcPosition.FromBytes(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("bytes");
    }

    #endregion
}
